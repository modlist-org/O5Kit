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

    /// <summary>Icon image. Null for text buttons.</summary>
    public Image? Icon { get; }

    /// <summary>Background tinted on hover.</summary>
    public Image Background { get; }

    /// <summary>Resting background color. Hover returns to this.</summary>
    public Color NormalColor { get; set; }

    private ITweenHandle? _hoverTween;
    private bool _hovered;
    private bool _pressed;

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
    ) : base(ctx, id, rect) {
        Label = label;
        Background = background;
        OnClick = options?.OnClick;
        NormalColor = Ctx.Theme.ObjectButton;

        UpdateVisual(true);
    }

    /// <summary>Creates an icon button.</summary>
    /// <param name="ctx">Owning kit context.</param>
    /// <param name="id">Stable identifier.</param>
    /// <param name="rect">Root rect, usually from <see cref="Factory.O5Factory"/>.</param>
    /// <param name="icon">Icon image.</param>
    /// <param name="background">Background image.</param>
    /// <param name="options">Behaviour. Null uses defaults.</param>
    public O5Button(
        O5Context ctx,
        string id,
        RectTransform rect,
        Image icon,
        Image background,
        O5ButtonOptions? options = null
    ) : base(ctx, id, rect) {
        Icon = icon;
        Background = background;
        OnClick = options?.OnClick;
        NormalColor = Ctx.Theme.ObjectButton;

        UpdateVisual(true);
    }

    /// <summary>Plays the hover-in tint. Wired to pointer-enter by the factory.</summary>
    public void OnHoverEnter() {
        if (IsDisposed) {
            return;
        }

        _hovered = true;
        if (_pressed) {
            return;
        }

        Tint(Ctx.Theme.ButtonHover, 0.12f);
    }

    /// <summary>Plays the hover-out tint. Wired to pointer-exit by the factory.</summary>
    public void OnHoverExit() {
        if (IsDisposed) {
            return;
        }

        _hovered = false;
        if (_pressed) {
            return;
        }

        Tint(NormalColor, 0.12f);
    }

    /// <summary>Plays the pressed tint. Wired to left pointer-down by the factory.</summary>
    public void OnPressEnter() {
        if (IsDisposed) {
            return;
        }

        _pressed = true;
        Tint(Ctx.Theme.ButtonPressed, 0.08f);
    }

    /// <summary>Settles back to the hover tint (still over the button) or the normal tint. Wired to pointer-up by the factory.</summary>
    public void OnPressExit() {
        if (IsDisposed || !_pressed) {
            return;
        }

        _pressed = false;
        Tint(_hovered ? Ctx.Theme.ButtonHover : NormalColor, 0.12f);
    }

    /// <summary>Physical press: plays the pressed tint and invokes <see cref="OnClick"/> on press-down.</summary>
    public void Press() {
        if (IsDisposed) {
            return;
        }

        OnPressEnter();
        OnClick?.Invoke();
        if (IsDisposed) {
            return;
        }

        _pressed = false;
        Tint(_hovered ? Ctx.Theme.ButtonHover : NormalColor, 0.12f);
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
    public override void Dispose() {
        if (IsDisposed) {
            return;
        }

        _hoverTween?.Kill();
        _hoverTween = null;
        OnClick = null;
        base.Dispose();
    }
}
