// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using O5Kit.Core;
using O5Kit.Factory;
using O5Kit.Resource;
using UnityEngine;
using UnityEngine.UI;

namespace O5Kit.Control;

/// <summary>On/off toggle with a sliding circle, tint and a changed-dot.</summary>
public class O5Toggle : O5Object {
    /// <summary>Value to reset to on middle-click. Null disables reset and hides the dot.</summary>
    public bool? DefaultValue { get; set; }

    /// <summary>Current value.</summary>
    public bool Value { get; private set; }

    /// <inheritdoc/>
    public override string? ShownValue => Value ? "1" : "0";

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

    /// <summary>Sliding circle icon glyph.</summary>
    public TMPro.TextMeshProUGUI CircleIcon { get; }

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
    /// <param name="circleIcon">Sliding circle icon text.</param>
    /// <param name="circleRect">Circle layout rect.</param>
    /// <param name="changedImage">Changed-dot image.</param>
    /// <param name="options">Behaviour. Null uses defaults.</param>
    public O5Toggle(
        O5Context ctx,
        string id,
        RectTransform rect,
        TMPro.TextMeshProUGUI label,
        TMPro.TextMeshProUGUI circleIcon,
        RectTransform circleRect,
        Image changedImage,
        O5ToggleOptions? options = null
    ) : base(ctx, id, rect) {
        options ??= new O5ToggleOptions();
        Label = label;

        CircleIcon = circleIcon;
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

        bool changed = Value != value;
        Value = value;

        if (notify) {
            OnChanged?.Invoke(value);
        }

        UpdateVisual(noAnimate: !changed);
        FlashIfExternal(changed, notify);
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

        O5FactoryIcons.ApplyGlyph(CircleIcon, Ctx.Fonts.Icons, Value ? O5Icons.Circle : O5Icons.ToggleCircle);

        var theme = Ctx.Theme;
        Color targetColor = Value ? theme.ObjectActive : theme.ObjectInactive;
        float changedTarget = DefaultValue.HasValue && DefaultValue.Value != Value ? 1f : 0f;

        CircleRect.sizeDelta = new Vector2(26f, 26f);

        if (noAnimate) {
            CircleIcon.fontSize = 26f;
            CircleIcon.color = targetColor;

            var c0 = ChangedImage.color;
            c0.a = changedTarget;
            ChangedImage.color = c0;

            return;
        }

        CircleIcon.fontSize = 30f;
        var icon = CircleIcon;
        _circleTween = Ctx.Tween.TweenFloat(
            () => 0f,
            t => {
                if (icon) {
                    icon.fontSize = Mathf.LerpUnclamped(30f, 26f, t);
                }
            },
            1f, 0.3f, ease: O5Ease.OutQuad);

        _tintTween = Ctx.Tween.TweenColor(() => icon.color, v => { if (icon) icon.color = v; }, targetColor, 0.15f, ease: O5Ease.OutQuad);

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
    protected override void OnThemeChanged(O5Theme prev, O5Theme next) {
        if (ChangedImage) {
            ChangedImage.color = O5Palette.WithAlpha(next.ObjectActive, ChangedImage.color.a);
        }
        UpdateVisual(true);
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
