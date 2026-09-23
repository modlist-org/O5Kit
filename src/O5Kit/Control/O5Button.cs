// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using O5Kit.Core;
using UnityEngine;
using UnityEngine.UI;

namespace O5Kit.Control;

/// <summary>Clickable button with hover tint. Content is either a text label or an icon.</summary>
public class O5Button : O5Object {
    /// <summary>Fired on left-click via <see cref="Click"/>.</summary>
    public Action? OnClick { get; set; }

    /// <summary>Text label. Null for icon buttons.</summary>
    public TMPro.TextMeshProUGUI? Label { get; }

    /// <summary>Icon image. Null for text buttons.</summary>
    public Image? Icon { get; }

    /// <summary>Background tinted on hover.</summary>
    public Image Background { get; }

    /// <summary>Resting background color. Hover returns to this.</summary>
    public Color NormalColor { get; set; }

    private ITweenHandle? _hoverTween;

    /// <summary>Creates a text button.</summary>
    /// <param name="id">Stable identifier.</param>
    /// <param name="rect">Root rect, usually from <see cref="Factory.O5Factory"/>.</param>
    /// <param name="label">Text label.</param>
    /// <param name="background">Background image.</param>
    /// <param name="onClick">Click callback.</param>
    public O5Button(
        string id,
        RectTransform rect,
        TMPro.TextMeshProUGUI label,
        Image background,
        Action? onClick
    ) : base(id, rect) {
        Label = label;
        Background = background;
        OnClick = onClick;
        NormalColor = O5Boot.Theme.ObjectButton;

        UpdateVisual(true);
    }

    /// <summary>Creates an icon button.</summary>
    /// <param name="id">Stable identifier.</param>
    /// <param name="rect">Root rect, usually from <see cref="Factory.O5Factory"/>.</param>
    /// <param name="icon">Icon image.</param>
    /// <param name="background">Background image.</param>
    /// <param name="onClick">Click callback.</param>
    public O5Button(
        string id,
        RectTransform rect,
        Image icon,
        Image background,
        Action? onClick
    ) : base(id, rect) {
        Icon = icon;
        Background = background;
        OnClick = onClick;
        NormalColor = O5Boot.Theme.ObjectButton;

        UpdateVisual(true);
    }

    /// <summary>Plays the hover-in tint. Wired to pointer-enter by the factory.</summary>
    public void OnHoverEnter() {
        if (IsDisposed) {
            return;
        }

        _hoverTween?.Kill();
        var bg = Background;
        _hoverTween = O5Boot.Tween.TweenColor(() => bg.color, v => bg.color = v,
            O5Boot.Theme.ObjectActiveBright, 0.12f);
    }

    /// <summary>Plays the hover-out tint. Wired to pointer-exit by the factory.</summary>
    public void OnHoverExit() {
        if (IsDisposed) {
            return;
        }

        _hoverTween?.Kill();
        var bg = Background;
        var normal = NormalColor;
        _hoverTween = O5Boot.Tween.TweenColor(() => bg.color, v => bg.color = v,
            normal, 0.12f);
    }

    /// <summary>Invokes <see cref="OnClick"/> and flashes the background.</summary>
    /// <param name="invoke">False to play only the visual.</param>
    public void Click(bool invoke = true) {
        if (IsDisposed) {
            return;
        }

        if (invoke) {
            OnClick?.Invoke();
        }

        UpdateVisual();
    }

    /// <summary>Snaps or fades the background back to <see cref="NormalColor"/>.</summary>
    /// <param name="noAnimate">Snap instead of fading.</param>
    public void UpdateVisual(bool noAnimate = false) {
        if (IsDisposed) {
            return;
        }

        _hoverTween?.Kill();

        if (noAnimate) {
            Background.color = NormalColor;
            return;
        }

        var bg = Background;
        var normal = NormalColor;
        _hoverTween = O5Boot.Tween.TweenColor(() => bg.color, v => bg.color = v,
            normal, 0.2f);
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
