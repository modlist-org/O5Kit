// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using O5Kit.Core;
using O5Kit.Factory;
using UnityEngine;
using UnityEngine.UI;

namespace O5Kit.Control;

/// <summary>On/off toggle with a sliding circle, tint and a changed-dot.</summary>
public class O5Toggle : O5Object {
    /// <summary>Value to reset to on middle-click. Null disables reset and hides the dot.</summary>
    public bool? DefaultValue { get; set; }

    /// <summary>Current value.</summary>
    public bool Value { get; private set; }

    /// <summary>Fired when the value changes via <see cref="Set"/>.</summary>
    public Action<bool>? OnChanged { get; set; }

    /// <summary>Label text.</summary>
    public TMPro.TextMeshProUGUI Label { get; }

    /// <summary>Label string. Get/set proxy for <see cref="Label"/> text.</summary>
    public string Text {
        get => Label != null && Label ? Label.text : string.Empty;
        set {
            if (Label != null && Label) {
                Label.text = value;
            }
        }
    }

    /// <summary>Sliding circle image.</summary>
    public Image CircleImage { get; }

    /// <summary>Dot shown while the value differs from default.</summary>
    public Image ChangedImage { get; }

    /// <summary>Circle layout rect.</summary>
    public RectTransform CircleRect { get; }

    private ITweenHandle? _circleTween, _tintTween, _changeTween;

    /// <summary>Creates a toggle over factory-built visuals.</summary>
    /// <param name="ctx">Owning kit context.</param>
    /// <param name="id">Stable identifier.</param>
    /// <param name="rect">Root rect.</param>
    /// <param name="label">Label text.</param>
    /// <param name="circleImage">Sliding circle image.</param>
    /// <param name="circleRect">Circle layout rect.</param>
    /// <param name="changedImage">Changed-dot image.</param>
    /// <param name="options">Behaviour. Null uses defaults.</param>
    public O5Toggle(
        O5Context ctx,
        string id,
        RectTransform rect,
        TMPro.TextMeshProUGUI label,
        Image circleImage,
        RectTransform circleRect,
        Image changedImage,
        O5ToggleOptions? options = null
    ) : base(ctx, id, rect) {
        options ??= new O5ToggleOptions();
        Label = label;

        CircleImage = circleImage;
        CircleRect = circleRect;
        ChangedImage = changedImage;
        DefaultValue = options.DefaultValue;
        Value = options.Value;
        OnChanged = options.OnChanged;

        UpdateVisual(true);
    }

    /// <summary>Builds a toggle under <paramref name="parent"/>. Owns all GameObjects; the caller only passes behaviour.</summary>
    /// <param name="ctx">Owning kit context.</param>
    /// <param name="parent">Parent transform.</param>
    /// <param name="id">Stable identifier.</param>
    /// <param name="options">Behaviour. Null uses defaults.</param>
    public static O5Toggle Create(O5Context ctx, Transform parent, string id, O5ToggleOptions? options = null)
        => O5Factory.Toggle(ctx, parent, id, options);

    /// <summary>Builds a toggle under <paramref name="parent"/> with inline configuration.</summary>
    /// <param name="ctx">Owning kit context.</param>
    /// <param name="parent">Parent transform.</param>
    /// <param name="id">Stable identifier.</param>
    /// <param name="configure">Mutates a fresh <see cref="O5ToggleOptions"/>.</param>
    public static O5Toggle Create(O5Context ctx, Transform parent, string id, Action<O5ToggleOptions> configure) {
        var options = new O5ToggleOptions();
        configure?.Invoke(options);
        return Create(ctx, parent, id, options);
    }

    /// <summary>Sets the value.</summary>
    /// <param name="value">New value.</param>
    /// <param name="notify">Fire the change callback.</param>
    public void Set(bool value, bool notify = true) {
        if (IsDisposed) {
            return;
        }

        Value = value;

        if (notify) {
            OnChanged?.Invoke(value);
        }

        UpdateVisual();
    }

    /// <summary>Flips the current value.</summary>
    /// <param name="notify">Fire the change callback.</param>
    public void Toggle(bool notify = true) => Set(!Value, notify);

    /// <summary>Restores <see cref="DefaultValue"/>. No-op when null.</summary>
    /// <param name="notify">Fire the change callback.</param>
    public void Reset(bool notify = true) {
        if (DefaultValue.HasValue) {
            Set(DefaultValue.Value, notify);
        }
    }

    /// <summary>Refreshes circle sprite, tint and changed-dot.</summary>
    /// <param name="noAnimate">Snap instead of animating.</param>
    public void UpdateVisual(bool noAnimate = false) {
        if (IsDisposed) {
            return;
        }

        _circleTween?.Kill();
        _tintTween?.Kill();
        _changeTween?.Kill();

        var onSprite = Ctx.Sprites.Icon("toggle-on");
        var offSprite = Ctx.Sprites.Icon("toggle-off");
        if (Value && onSprite != null) {
            CircleImage.sprite = onSprite;
        } else if (!Value && offSprite != null) {
            CircleImage.sprite = offSprite;
        }

        var theme = Ctx.Theme;
        Color targetColor = Value ? theme.ObjectActive : theme.ObjectInactive;
        float changedTarget = DefaultValue.HasValue && DefaultValue.Value != Value ? 1f : 0f;

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
        _circleTween = Ctx.Tween.TweenFloat(
            () => 0f,
            t => {
                if (rect) {
                    rect.sizeDelta = Vector2.LerpUnclamped(fromSize, toSize, t);
                }
            },
            1f, 0.3f, ease: O5Ease.OutQuad);

        var img = CircleImage;
        _tintTween = Ctx.Tween.TweenColor(() => img.color, v => img.color = v, targetColor, 0.15f, ease: O5Ease.OutQuad);

        var changed = ChangedImage;
        _changeTween = Ctx.Tween.TweenFloat(
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
