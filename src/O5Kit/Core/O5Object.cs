// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Collections.Generic;
using UnityEngine;

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
            field ??= Rect.GetComponent<CanvasGroup>() ?? Rect.gameObject.AddComponent<CanvasGroup>();
            return field;
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
        if (IsDisposed) {
            return;
        }

        _blockTween?.Kill();

        float targetAlpha = blocked ? 0.4f : 1f;

        CanvasGroup.interactable = !blocked;
        CanvasGroup.blocksRaycasts = !blocked;

        if (noAnimate) {
            CanvasGroup.alpha = targetAlpha;
            return;
        }

        _blockTween = Ctx.Tween.TweenFloat(
            () => CanvasGroup.alpha,
            v => {
                if (!IsDisposed) {
                    CanvasGroup.alpha = v;
                }
            },
            targetAlpha, 0.2f);
    }

    /// <summary>Releases tweens, tick registration and event hooks. Idempotent.</summary>
    public virtual void Dispose() {
        if (IsDisposed) {
            return;
        }

        IsDisposed = true;
        _blockTween?.Kill();
        _blockTween = null;
        if (EnabledWhen != null) {
            Ctx.EnabledChanged -= ApplyEnabled;
        }

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
