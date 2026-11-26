// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Collections.Generic;
using O5Kit.Behaviour;
using UnityEngine;
using UnityEngine.UI;

namespace O5Kit.Core;

/// <summary>Base of every O5Kit control. Tickable, blockable, disposable.</summary>
public abstract class O5Object {
    private static readonly List<O5Object> _tickables = new();

    /// <summary>Owning kit context. Never shared across consumers.</summary>
    public O5Context Ctx { get; }

    /// <summary>Stable identifier for lookup and debugging.</summary>
    public string Id { get; }

    /// <summary>Root rect of the control.</summary>
    public RectTransform Rect { get; }

    /// <summary>Fired once when the control is disposed.</summary>
    public Action? OnDisposed;

    /// <summary>Whether <see cref="Dispose"/> has run.</summary>
    public bool IsDisposed { get; private set; }

    /// <summary>
    /// When set, the control auto-blocks/unblocks as the predicate flips.
    /// The owning context broadcasts via <see cref="O5Context.NotifyEnabledChanged"/>.
    /// </summary>
    public Func<bool>? EnabledWhen {
        get;
        set {
            if (field == value) {
                return;
            }

            if (field != null) {
                Ctx.EnabledChanged -= ApplyEnabled;
            }

            field = value;
            if (field != null) {
                Ctx.EnabledChanged += ApplyEnabled;
                SetBlocked(!field(), true);
            }
        }
    }

    /// <summary>Lazy canvas group used for blocking and fading.</summary>
    protected CanvasGroup CanvasGroup {
        get {
            if ((field == null || !field) && Rect != null && Rect) {
                field = Rect.GetComponent<CanvasGroup>() ?? Rect.gameObject.AddComponent<CanvasGroup>();
            }
            return field!;
        }
    }

    private ITweenHandle? _blockTween;

    /// <summary>Creates a control over an existing rect.</summary>
    /// <param name="ctx">Owning kit context.</param>
    /// <param name="id">Stable identifier.</param>
    /// <param name="rect">Root rect, usually built by <see cref="Factory.O5Factory"/>.</param>
    protected O5Object(O5Context ctx, string id, RectTransform rect) {
        Ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
        Id = id;
        Rect = rect;
        Ctx.ThemeTransitioned += HandleThemeTransitioned;
        _createdFrame = Time.frameCount;
    }

    private void HandleThemeTransitioned(O5Theme prev, O5Theme next) {
        if (IsDisposed || Rect == null || !Rect) {
            return;
        }
        OnThemeChanged(prev, next);
    }

    /// <summary>Called when the owning context's theme changes via <see cref="O5Context.SetTheme"/>.</summary>
    protected virtual void OnThemeChanged(O5Theme prev, O5Theme next) {
    }

    private void ApplyEnabled(bool enabled) {
        if (IsDisposed || EnabledWhen == null) {
            return;
        }

        SetBlocked(!enabled);
    }

    /// <summary>Dims and deactivates the control, or restores it.</summary>
    /// <param name="blocked">True to dim and block input.</param>
    /// <param name="noAnimate">Snap instead of fading.</param>
    public virtual void SetBlocked(bool blocked, bool noAnimate = false) {
        if (IsDisposed || Rect == null || !Rect) {
            return;
        }

        var group = CanvasGroup;
        if (group == null || !group) {
            return;
        }

        _blockTween?.Kill();

        float targetAlpha = blocked ? 0.4f : 1f;

        if (blocked) {
            _blockedGroups.Add(group);
        } else {
            _blockedGroups.Remove(group);
        }
        group.interactable = !blocked;
        group.blocksRaycasts = !blocked;

        if (noAnimate) {
            group.alpha = targetAlpha;
            return;
        }

        _blockTween = Ctx.Tween.TweenFloat(
            () => group ? group.alpha : targetAlpha,
            v => {
                if (!IsDisposed && group) {
                    group.alpha = v;
                }
            },
            targetAlpha, 0.2f);
    }

    // ---- external change bar ----

    /// <summary>Length of the change flash in seconds (OutExpo fade from full).</summary>
    public const float ChangeFlashDuration = 0.3f;
    private const float ChangeBarWidth = 3f;
    private const float ChangeBarInset = 4f;
    private const float ChangeBarFallbackGap = 10f;

    /// <summary>When false, <see cref="FlashExternalChange"/> does nothing (controls inside
    /// another control, such as a color picker's own sliders).</summary>
    public bool FlashOnExternalChange { get; set; } = true;

    /// <summary>Shown value for controls that aren't one of the value controls (which report
    /// their own through <see cref="ShownValue"/>).</summary>
    public Func<string?>? ValueProbe { get; set; }

    /// <summary>The value as displayed, for telling whether a rebuilt control now shows something
    /// else. Null when the control has no value.</summary>
    public virtual string? ShownValue => ValueProbe?.Invoke();

    // Groups dimmed by SetBlocked: change bars look through these (a locked field's bar stays
    // as bright as any other) but still follow real fades such as the window fading out.
    private static readonly HashSet<CanvasGroup> _blockedGroups = new();

    private readonly int _createdFrame;
    private CanvasGroup? _changeBarGroup;
    private Image? _changeBar;
    private ITweenHandle? _changeTween;
    private float _changeProgress = 1f;
    private float _changeStart = -1f;

    /// <summary>Seconds since the running change flash started, or -1 when none is showing.</summary>
    public float ChangeFlashElapsed {
        get {
            if (_changeStart < 0f || _changeProgress >= 1f) {
                return -1f;
            }
            return Time.unscaledTime - _changeStart;
        }
    }

    /// <summary>
    /// Lights a rounded bar in the empty gutter left of the control, as tall as the control
    /// (a little shorter), then fades it out: the value was changed from outside, not by the
    /// pointer on this control. Calling it again restarts at full brightness.
    /// </summary>
    /// <param name="elapsed">Seconds already played (to carry a flash over to a rebuilt control).</param>
    public void FlashExternalChange(float elapsed = 0f) {
        if (IsDisposed || !FlashOnExternalChange || Rect == null || !Rect || !Rect.gameObject.activeInHierarchy) {
            return;
        }
        float start = Mathf.Clamp01(elapsed / ChangeFlashDuration);
        if (start >= 1f) {
            return;
        }

        var bar = EnsureChangeBar();
        PlaceChangeBar(bar);
        _changeTween?.Kill();
        _changeStart = Time.unscaledTime - elapsed;
        _changeProgress = start;
        ApplyChangeBar();
        _changeTween = Ctx.Tween.TweenFloat(
            () => _changeProgress,
            v => {
                _changeProgress = v;
                ApplyChangeBar();
            },
            1f, ChangeFlashDuration * (1f - start), null, O5Ease.Linear);
    }

    /// <summary>For a control's own <c>Set(value, notify)</c>: flashes when the shown value
    /// changed and the call came from code (<paramref name="notify"/> false), not while the
    /// control is still being built.</summary>
    protected void FlashIfExternal(bool changed, bool notify) {
        if (changed && !notify && Time.frameCount != _createdFrame) {
            FlashExternalChange();
        }
    }

    private Image EnsureChangeBar() {
        if (_changeBar != null && _changeBar) {
            return _changeBar;
        }
        var go = new GameObject("ChangeBar");
        var rect = go.AddComponent<RectTransform>();
        // Beside the control, not inside it: a blocked (dimmed) control mustn't dim its bar.
        rect.SetParent(Rect.parent != null ? Rect.parent : Rect, false);
        go.AddComponent<LayoutElement>().ignoreLayout = true;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        var image = go.AddComponent<Image>();
        image.sprite = Ctx.Shapes.Get(new O5Shape(ChangeBarWidth * 0.5f));
        image.type = Image.Type.Sliced;
        image.raycastTarget = false;
        image.color = Color.clear;
        _changeBarGroup = go.AddComponent<CanvasGroup>();
        _changeBarGroup.ignoreParentGroups = true;
        _changeBarGroup.interactable = false;
        _changeBarGroup.blocksRaycasts = false;
        _changeBar = image;
        return image;
    }

    private static readonly Vector3[] _corners = new Vector3[4];

    // Centred in the gutter: the left padding of the nearest layout around the control (a card's
    // content), or else the gap to the first ancestor that starts further left (a scroll view's inset).
    private void PlaceChangeBar(Image bar) {
        var rect = bar.rectTransform;
        rect.SetAsLastSibling();
        Rect.GetWorldCornersSafe(_corners);
        float controlLeft = _corners[0].x;
        float? center = null;
        for (Transform? t = Rect.parent; t != null; t = t.parent) {
            var ancestor = t.AsRectTransform();
            if (ancestor == null) {
                continue;
            }
            var group = ancestor.GetComponent<LayoutGroup>();
            bool isHorizontal = group != null && (
#if IL2CPP
                group.TryCast<HorizontalLayoutGroup>() != null
#else
                group is HorizontalLayoutGroup
#endif
            );
            if (group != null && group.enabled && group.padding.left > 0 && !isHorizontal) {
                ancestor.GetWorldCornersSafe(_corners);
                float scale = ancestor.lossyScale.x;
                center = _corners[0].x + group.padding.left * 0.5f * scale;
                break;
            }
        }
        if (center == null) {
            float prevLeft = controlLeft;
            for (Transform? t = Rect.parent; t != null; t = t.parent) {
                var ancestor = t.AsRectTransform();
                if (ancestor == null) {
                    continue;
                }
                ancestor.GetWorldCornersSafe(_corners);
                float left = _corners[0].x;
                if (left < prevLeft - 0.5f * Mathf.Max(0.0001f, ancestor.lossyScale.x)) {
                    center = (left + prevLeft) * 0.5f;
                    break;
                }
                prevLeft = left;
            }
        }
        float worldX = center ?? controlLeft - ChangeBarFallbackGap * 0.5f * Rect.lossyScale.x;
        Rect.GetWorldCornersSafe(_corners);
        float worldY = (_corners[0].y + _corners[1].y) * 0.5f;
        var parent = rect.parent.AsRectTransform();
        float parentScaleY = parent != null ? Mathf.Max(0.0001f, parent.lossyScale.y) : 1f;
        float height = Rect.rect.height * Rect.lossyScale.y / parentScaleY;
        rect.sizeDelta = new Vector2(ChangeBarWidth, Mathf.Max(0f, height - ChangeBarInset * 2f));
        rect.position = new Vector3(worldX, worldY, Rect.position.z);
    }

    private void ApplyChangeBar() {
        if (_changeBar == null || !_changeBar) {
            return;
        }
        // OutExpo: bright at once, then a fast falloff.
        float alpha = _changeProgress >= 1f ? 0f : Mathf.Pow(2f, -10f * _changeProgress);
        Color c = Ctx.Theme.ObjectActive;
        c.a = alpha;
        _changeBar.color = c;
        if (_changeBarGroup != null && _changeBarGroup) {
            _changeBarGroup.alpha = alpha > 0f ? InheritedAlpha(_changeBar.transform.parent) : 1f;
        }
    }

    // What the parents' canvas groups would multiply in, minus the dimming of blocked controls.
    private static float InheritedAlpha(Transform? from) {
        float alpha = 1f;
        for (Transform? t = from; t != null; t = t.parent) {
            var group = t.GetComponent<CanvasGroup>();
            if (group == null || !group.enabled) {
                continue;
            }
            if (!_blockedGroups.Contains(group)) {
                alpha *= group.alpha;
            }
            if (group.ignoreParentGroups) {
                break;
            }
        }
        return alpha;
    }

    /// <summary>Releases tweens, tick registration and event hooks. Idempotent.</summary>
    public virtual void Dispose() {
        if (IsDisposed) {
            return;
        }

        IsDisposed = true;
        if (Rect != null && Rect && Rect.GetComponent<CanvasGroup>() is { } ownGroup) {
            _blockedGroups.Remove(ownGroup);
        }
        _blockTween?.Kill();
        _blockTween = null;
        _changeTween?.Kill();
        _changeTween = null;
        if (_changeBar != null && _changeBar) {
            UnityEngine.Object.Destroy(_changeBar.gameObject);
        }
        _changeBar = null;
        if (EnabledWhen != null) {
            Ctx.EnabledChanged -= ApplyEnabled;
        }
        Ctx.ThemeTransitioned -= HandleThemeTransitioned;

        UnregisterTick();
        OnDisposed?.Invoke();
        OnDisposed = null;
    }

    /// <summary>Opts into per-frame <see cref="Tick"/> calls.</summary>
    protected void RegisterTick() => _tickables.Add(this);

    /// <summary>Opts out of per-frame <see cref="Tick"/> calls.</summary>
    protected void UnregisterTick() => _tickables.Remove(this);

    /// <summary>
    /// Disposes controls whose GameObject died without <see cref="Dispose"/>
    /// (e.g. a scene wipe that ignores DontDestroyOnLoad, as seen in
    /// Superliminal 2019.4) and drops them from the tick list. Otherwise a
    /// single dead control throws every frame and starves the rest.
    /// Returns the number removed.
    /// </summary>
    public static int PurgeDead() {
        _blockedGroups.RemoveWhere(group => group == null || !group);
        int removed = 0;
        for (int i = _tickables.Count - 1; i >= 0; i--) {
            O5Object t;
            try {
                t = _tickables[i];
            } catch {
                continue;
            }
            bool dead;
            try {
                dead = t == null || t.IsDisposed || t.Rect == null;
            } catch {
                dead = true;
            }
            if (!dead) {
                continue;
            }
            try {
                t?.Dispose();
            } catch {
            }
            try {
                _tickables.RemoveAt(i);
            } catch {
            }
            removed++;
        }
        return removed;
    }

    /// <summary>Per-frame update for ticking controls. Called by <see cref="TickAll"/>.</summary>
    public virtual void Tick() {
    }

    /// <summary>Ticks every registered control. Call once per frame.</summary>
    public static void TickAll() {
        for (int i = 0; i < _tickables.Count; i++) {
            _tickables[i].Tick();
        }
    }
}
