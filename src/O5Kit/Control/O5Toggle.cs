// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using O5Kit.Core;
using UnityEngine;
using UnityEngine.UI;

namespace O5Kit.Control;

/// <summary>On/off toggle with a sliding circle, tint and a changed-dot.</summary>
public class O5Toggle : O5Object {
    /// <summary>Value to reset to on middle-click.</summary>
    public bool DefaultValue { get; }

    /// <summary>Current value.</summary>
    public bool Value { get; private set; }

    /// <summary>Fired when the value changes via <see cref="Set"/>.</summary>
    public Action<bool>? OnChanged;

    /// <summary>Label text.</summary>
    public TMPro.TextMeshProUGUI Label { get; }

    /// <summary>Sliding circle image.</summary>
    public Image CircleImage { get; }

    /// <summary>Dot shown while the value differs from default.</summary>
    public Image ChangedImage { get; }

    /// <summary>Circle layout rect.</summary>
    public RectTransform CircleRect { get; }

    private ITweenHandle? _circleTween, _tintTween, _changeTween;

    /// <summary>Creates a toggle over factory-built visuals.</summary>
    /// <param name="id">Stable identifier.</param>
    /// <param name="rect">Root rect.</param>
    /// <param name="label">Label text.</param>
    /// <param name="circleImage">Sliding circle image.</param>
    /// <param name="circleRect">Circle layout rect.</param>
    /// <param name="changedImage">Changed-dot image.</param>
    /// <param name="defaultValue">Reset target.</param>
    /// <param name="value">Initial value.</param>
    /// <param name="onChanged">Change callback.</param>
    public O5Toggle(
        string id,
        RectTransform rect,
        TMPro.TextMeshProUGUI label,
        Image circleImage,
        RectTransform circleRect,
        Image changedImage,
        bool defaultValue,
        bool value,
        Action<bool>? onChanged
    ) : base(id, rect) {
        Label = label;

        CircleImage = circleImage;
        CircleRect = circleRect;
        ChangedImage = changedImage;
        DefaultValue = defaultValue;
        Value = value;
        OnChanged = onChanged;

        UpdateVisual(true);
    }

    /// <summary>Sets the value, optionally invoking <see cref="OnChanged"/>.</summary>
    /// <param name="value">New value.</param>
    /// <param name="invoke">Fire the change callback.</param>
    public void Set(bool value, bool invoke = true) {
        if (IsDisposed) {
            return;
        }

        Value = value;

        if (invoke) {
            OnChanged?.Invoke(value);
        }

        UpdateVisual();
    }

    /// <summary>Flips the current value.</summary>
    public void Toggle() => Set(!Value);

    /// <summary>Restores <see cref="DefaultValue"/>.</summary>
    public void Reset() => Set(DefaultValue);

    /// <summary>Refreshes circle sprite, tint and changed-dot.</summary>
    /// <param name="noAnimate">Snap instead of animating.</param>
    public void UpdateVisual(bool noAnimate = false) {
        if (IsDisposed) {
            return;
        }

        _circleTween?.Kill();
        _tintTween?.Kill();
        _changeTween?.Kill();

        var onSprite = O5Boot.Sprites.Icon("toggle-on");
        var offSprite = O5Boot.Sprites.Icon("toggle-off");
        if (Value && onSprite != null) {
            CircleImage.sprite = onSprite;
        } else if (!Value && offSprite != null) {
            CircleImage.sprite = offSprite;
        }

        var theme = O5Boot.Theme;
        Color targetColor = Value ? theme.ObjectActive : theme.ObjectInactive;
        float changedTarget = DefaultValue != Value ? 1f : 0f;

        if (noAnimate) {
            CircleRect.sizeDelta = new Vector2(26f, 26f);
            CircleImage.color = targetColor;

            var c0 = ChangedImage.color;
            c0.a = changedTarget;
            ChangedImage.color = c0;

            return;
        }

        CircleRect.sizeDelta = new Vector2(30f, 30f);
        var fromSize = new Vector2(30f, 30f);
        var toSize = new Vector2(26f, 26f);
        var rect = CircleRect;
        _circleTween = O5Boot.Tween.TweenFloat(
            () => 0f,
            t => {
                if (rect) {
                    rect.sizeDelta = Vector2.LerpUnclamped(fromSize, toSize, t);
                }
            },
            1f, 0.3f, ease: O5Ease.OutQuad);

        var img = CircleImage;
        _tintTween = O5Boot.Tween.TweenColor(() => img.color, v => img.color = v, targetColor, 0.15f, ease: O5Ease.OutQuad);

        var changed = ChangedImage;
        _changeTween = O5Boot.Tween.TweenFloat(
            () => changed.color.a,
            v => {
                if (changed) {
                    var c = changed.color;
                    c.a = v;
                    changed.color = c;
                }
            },
            changedTarget, 0.2f);
    }

    /// <inheritdoc/>
    public override void Dispose() {
        if (IsDisposed) {
            return;
        }

        _circleTween?.Kill();
        _tintTween?.Kill();
        _changeTween?.Kill();
        _circleTween = _tintTween = _changeTween = null;
        base.Dispose();
    }
}
