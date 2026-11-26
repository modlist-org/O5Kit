// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using O5Kit.Core;
using O5Kit.Factory;
using UnityEngine;
using UnityEngine.UI;

namespace O5Kit.Control;

/// <summary>Clickable button with hover and pressed tints. Content is either a text label or an icon.</summary>
public class O5Button : O5Object {
    /// <summary>Fired on left-click via <see cref="Click"/>.</summary>
    public Action? OnClick { get; set; }

    /// <summary>Text label. Null for icon buttons.</summary>
    public TMPro.TextMeshProUGUI? Label { get; }

    /// <summary>Label string. Get/set proxy for <see cref="Label"/> text. No-op for icon buttons.</summary>
    public string Text {
        get => Label != null && Label ? Label.text : string.Empty;
        set {
            if (Label != null && Label) {
                Label.text = value;
            }
        }
    }

    /// <summary>Icon glyph. Null for text buttons.</summary>
    public TMPro.TextMeshProUGUI? Icon { get; }

    /// <summary>Background tinted on hover.</summary>
    public Image Background { get; }

    /// <summary>Resting background color. Hover returns to this.</summary>
    public Color NormalColor { get; set; }

    private const float PressedScale = 0.968f;
    private const float ScaleDuration = 0.3f;

    private ITweenHandle? _hoverTween;
    private ITweenHandle? _scaleTween;
    private float _currentScale = 1f;
    private Vector2 _scalePosOffset;
    private bool _hovered;
    private bool _pressed;
    private bool _holding;

    /// <summary>Creates a text button.</summary>
    /// <param name="ctx">Owning kit context.</param>
    /// <param name="id">Stable identifier.</param>
    /// <param name="rect">Root rect, usually from <see cref="Factory.O5Factory"/>.</param>
    /// <param name="label">Text label.</param>
    /// <param name="background">Background image.</param>
    /// <param name="options">Behaviour. Null uses defaults.</param>
    public O5Button(
        O5Context ctx,
        string id,
        RectTransform rect,
        TMPro.TextMeshProUGUI label,
        Image background,
        O5ButtonOptions? options = null
    ) : this(ctx, id, rect, label, null, background, options) { }

    internal O5Button(
        O5Context ctx,
        string id,
        RectTransform rect,
        TMPro.TextMeshProUGUI? label,
        TMPro.TextMeshProUGUI? icon,
        Image background,
        O5ButtonOptions? options = null
    ) : base(ctx, id, rect) {
        Label = label;
        Icon = icon;
        Background = background;
        OnClick = options?.OnClick;
        NormalColor = Ctx.Theme.ObjectButton;

        UpdateVisual(true);
    }

    /// <summary>Replaces the icon glyph on an icon button.</summary>
    public void SetIcon(O5Icon icon) => O5FactoryIcons.SetIcon(Icon, icon);

    /// <summary>Plays the hover-in tint. Wired to pointer-enter by the factory.</summary>
    public void OnHoverEnter() {
        if (IsDisposed) {
            return;
        }

        _hovered = true;
        Tint(_pressed ? Ctx.Theme.ButtonPressed : Ctx.Theme.ButtonHover, _pressed ? 0.08f : 0.12f);
    }

    /// <summary>Plays the hover-out tint. Wired to pointer-exit by the factory.</summary>
    public void OnHoverExit() {
        if (IsDisposed) {
            return;
        }

        _hovered = false;
        Tint(NormalColor, 0.12f);
    }

    /// <summary>Plays the pressed tint and shrinks slightly while held. Wired to left pointer-down by the factory.</summary>
    public void OnPressEnter() {
        if (IsDisposed) {
            return;
        }

        _hovered = true;
        _pressed = true;
        _holding = true;
        Tint(Ctx.Theme.ButtonPressed, 0.08f);
        if (Rect) {
            _scaleTween?.Kill();
            _scaleTween = Ctx.Tween.TweenFloat(() => _currentScale, ApplyScale, PressedScale, ScaleDuration, null, O5Ease.OutQuad);
        }
    }

    /// <summary>Settles back to the hover/normal tint and springs scale back to normal. Wired to pointer-up by the factory.</summary>
    public void OnPressExit() => OnPressExit(false);

    /// <summary>Settles back to the hover/normal tint, springs scale back to normal, and optionally invokes click if released while still hovered.</summary>
    public void OnPressExit(bool invokeClick) {
        if (IsDisposed || (!_pressed && !_holding)) {
            return;
        }

        bool shouldClick = invokeClick && _holding && _hovered;
        _pressed = false;
        Tint(_hovered ? Ctx.Theme.ButtonHover : NormalColor, 0.12f);

        if (_holding) {
            _holding = false;
            _scaleTween?.Kill();
            _scaleTween = Rect
                ? Ctx.Tween.TweenFloat(() => _currentScale, ApplyScale, 1f, ScaleDuration, null, O5Ease.OutBack)
                : null;
        }

        if (shouldClick) {
            OnClick?.Invoke();
        }
    }

    /// <summary>Physical press: plays the pressed tint and invokes <see cref="OnClick"/> on press-down.</summary>
    public void Press() {
        if (IsDisposed) {
            return;
        }

        _pressed = true;
        Tint(Ctx.Theme.ButtonPressed, 0.08f);
        OnClick?.Invoke();
        if (IsDisposed) {
            return;
        }

        _pressed = false;
        Tint(_hovered ? Ctx.Theme.ButtonHover : NormalColor, 0.12f);
    }

    /// <summary>Snaps the button scale back to normal (e.g. when hidden or disabled mid-press).</summary>
    public void ResetScale() {
        _holding = false;
        _pressed = false;
        _scaleTween?.Kill();
        _scaleTween = null;
        ApplyScale(1f);
    }

    private void ApplyScale(float scale) {
        _currentScale = scale;
        if (!Rect) {
            return;
        }

        Vector2 center = Rect.rect.center;
        bool needsOffset = _scalePosOffset.sqrMagnitude > 1e-8f || center.sqrMagnitude > 1e-6f;
        if (needsOffset) {
            Vector2 basePos = Rect.anchoredPosition - _scalePosOffset;
            Vector3 rotated = Rect.localRotation * new Vector3((1f - scale) * center.x, (1f - scale) * center.y, 0f);
            Vector2 newOffset = new(rotated.x, rotated.y);
            Rect.localScale = new Vector3(scale, scale, 1f);
            Rect.anchoredPosition = basePos + newOffset;
            _scalePosOffset = newOffset;
        } else {
            Rect.localScale = new Vector3(scale, scale, 1f);
        }
    }

    /// <inheritdoc/>
    public override void SetBlocked(bool blocked, bool noAnimate = false) {
        base.SetBlocked(blocked, noAnimate);
        if (!blocked || IsDisposed || Rect == null || !Rect) {
            return;
        }
        if (_holding) {
            OnPressExit();
        }
        if (_hovered) {
            _hovered = false;
            if (noAnimate) {
                _hoverTween?.Kill();
                if (Background) {
                    Background.color = NormalColor;
                }
            } else {
                Tint(NormalColor, 0.12f);
            }
        }
    }

    /// <summary>Invokes <see cref="OnClick"/> and flashes the pressed tint (for programmatic clicks).</summary>
    /// <param name="notify">False to play only the visual.</param>
    public void Click(bool notify = true) {
        if (IsDisposed) {
            return;
        }

        if (notify) {
            OnClick?.Invoke();
        }

        Flash();
    }

    /// <summary>Builds a button under <paramref name="parent"/>. Owns all GameObjects; the caller only passes behaviour.</summary>
    /// <param name="ctx">Owning kit context.</param>
    /// <param name="parent">Parent transform.</param>
    /// <param name="id">Stable identifier.</param>
    /// <param name="options">Behaviour. Null uses defaults.</param>
    public static O5Button Create(O5Context ctx, Transform parent, string id, O5ButtonOptions? options = null)
        => O5Factory.Button(ctx, parent, id, options);

    /// <summary>Builds a button under <paramref name="parent"/> with inline configuration.</summary>
    /// <param name="ctx">Owning kit context.</param>
    /// <param name="parent">Parent transform.</param>
    /// <param name="id">Stable identifier.</param>
    /// <param name="configure">Mutates a fresh <see cref="O5ButtonOptions"/>.</param>
    public static O5Button Create(O5Context ctx, Transform parent, string id, Action<O5ButtonOptions> configure) {
        var options = new O5ButtonOptions();
        configure?.Invoke(options);
        return Create(ctx, parent, id, options);
    }

    /// <summary>Snaps or fades the background back to <see cref="NormalColor"/>.</summary>
    /// <param name="noAnimate">Snap instead of fading.</param>
    public void UpdateVisual(bool noAnimate = false) {
        if (IsDisposed) {
            return;
        }

        _pressed = false;
        if (noAnimate) {
            _hoverTween?.Kill();
            Background.color = NormalColor;
            return;
        }

        Tint(NormalColor, 0.2f);
    }

    private void Flash() {
        _hoverTween?.Kill();

        var bg = Background;
        var pressed = Ctx.Theme.ButtonPressed;
        Color settle = _hovered ? Ctx.Theme.ButtonHover : NormalColor;
        _hoverTween = Ctx.Tween.TweenColor(() => bg.color, v => bg.color = v,
            pressed, 0.06f, () => {
                if (IsDisposed) {
                    return;
                }

                Tint(settle, 0.12f);
            });
    }

    private void Tint(Color target, float duration) {
        _hoverTween?.Kill();

        var bg = Background;
        _hoverTween = Ctx.Tween.TweenColor(() => bg.color, v => bg.color = v,
            target, duration);
    }

    /// <inheritdoc/>
    protected override void OnThemeChanged(O5Theme prev, O5Theme next) {
        if (O5Palette.ApproxEquals(NormalColor, prev.ObjectButton)) {
            NormalColor = next.ObjectButton;
        } else if (O5Palette.ApproxEquals(NormalColor, prev.ObjectActive)) {
            NormalColor = next.ObjectActive;
        }
        _hoverTween?.Kill();
        if (Background) {
            Background.color = _pressed ? next.ButtonPressed : (_hovered ? next.ButtonHover : NormalColor);
        }
    }

    /// <inheritdoc/>
    public override void Dispose() {
        if (IsDisposed) {
            return;
        }

        _hoverTween?.Kill();
        _hoverTween = null;
        _scaleTween?.Kill();
        _scaleTween = null;
        _holding = false;
        _pressed = false;
        if (Rect) {
            ApplyScale(1f);
        }
        OnClick = null;
        base.Dispose();
    }
}
