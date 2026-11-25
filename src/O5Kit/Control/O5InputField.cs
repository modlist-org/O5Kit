// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using O5Kit.Core;
using O5Kit.Factory;
using UnityEngine;
using UnityEngine.UI;

namespace O5Kit.Control;

/// <summary>Single- or multi-line text field with icon, placeholder and changed-dot.</summary>
public sealed class O5InputField : O5Object {
    /// <summary>Shared editing logic (focus tracking, caret, placeholder).</summary>
    public O5InputCore Core { get; }

    /// <summary>Value to reset to on middle-click. Null disables reset.</summary>
    public string? DefaultValue { get; set; }

    /// <summary>Current text.</summary>
    public string Value => Core.Value;

    /// <inheritdoc/>
    public override string? ShownValue => Value;

    /// <summary>Change callback.</summary>
    public Action<string>? OnChanged => Core.OnChanged;

    /// <summary>Underlying TMP input.</summary>
    public TMPro.TMP_InputField InputField => Core.InputField;

    /// <summary>Placeholder text. May be null for code-constructed fields.</summary>
    public TMPro.TextMeshProUGUI? Placeholder => Core.Placeholder;

    /// <summary>Trailing icon glyph.</summary>
    public TMPro.TextMeshProUGUI Icon { get; }

    /// <summary>Dot shown while the value differs from default.</summary>
    public Image ChangedImage { get; }

    private ITweenHandle? _changeTween, _iconTween;
    private bool? _iconFocused;

    /// <summary>Creates a text field over factory-built visuals.</summary>
    /// <param name="ctx">Owning kit context.</param>
    /// <param name="id">Stable identifier.</param>
    /// <param name="rect">Root rect.</param>
    /// <param name="inputField">TMP input.</param>
    /// <param name="placeholder">Placeholder text.</param>
    /// <param name="icon">Trailing icon.</param>
    /// <param name="changedImage">Changed-dot image.</param>
    /// <param name="options">Behaviour. Null uses defaults.</param>
    public O5InputField(
        O5Context ctx,
        string id,
        RectTransform rect,
        TMPro.TMP_InputField inputField,
        TMPro.TextMeshProUGUI placeholder,
        TMPro.TextMeshProUGUI icon,
        Image changedImage,
        O5InputOptions? options = null
    ) : base(ctx, id, rect) {
        options ??= new O5InputOptions();
        DefaultValue = options.DefaultValue;
        ChangedImage = changedImage;
        Icon = icon;

        Core = new O5InputCore(ctx, inputField, placeholder, options.Value ?? string.Empty, val => {
            UpdateVisual();
            options.OnChanged?.Invoke(val);
        }, options.OnEndEdit, options.Multiline);

        RegisterTick();
        UpdateVisual(true);
    }

    /// <summary>Builds a text field under <paramref name="parent"/>. Owns all GameObjects; the caller only passes behaviour.</summary>
    /// <param name="ctx">Owning kit context.</param>
    /// <param name="parent">Parent transform.</param>
    /// <param name="id">Stable identifier.</param>
    /// <param name="options">Behaviour. Null uses defaults.</param>
    public static O5InputField Create(O5Context ctx, Transform parent, string id, O5InputOptions? options = null)
        => O5Factory.Input(ctx, parent, id, options);

    /// <summary>Builds a text field under <paramref name="parent"/> with inline configuration.</summary>
    /// <param name="ctx">Owning kit context.</param>
    /// <param name="parent">Parent transform.</param>
    /// <param name="id">Stable identifier.</param>
    /// <param name="configure">Mutates a fresh <see cref="O5InputOptions"/>.</param>
    public static O5InputField Create(O5Context ctx, Transform parent, string id, Action<O5InputOptions> configure) {
        var options = new O5InputOptions();
        configure?.Invoke(options);
        return Create(ctx, parent, id, options);
    }

    /// <summary>Replaces the text.</summary>
    /// <param name="value">New text.</param>
    /// <param name="notify">Fire the change callback.</param>
    public void Set(string value, bool notify = true) {
        if (IsDisposed) {
            return;
        }

        bool changed = !string.Equals(Core.Value ?? string.Empty, value ?? string.Empty, StringComparison.Ordinal);
        Core.SetValue(value, false);
        if (notify) {
            Core.OnChanged?.Invoke(value);
        }

        UpdateVisual();
        FlashIfExternal(changed, notify);
    }

    /// <summary>Restores <see cref="DefaultValue"/>. No-op when null.</summary>
    /// <param name="notify">Fire the change callback.</param>
    public void Reset(bool notify = true) {
        if (DefaultValue != null) {
            Set(DefaultValue, notify);
        }
    }

    /// <summary>Refreshes the changed-dot.</summary>
    /// <param name="noAnimate">Snap instead of fading.</param>
    public void UpdateVisual(bool noAnimate = false) {
        if (IsDisposed) {
            return;
        }

        _changeTween?.Kill();
        float target = (DefaultValue != null && DefaultValue != Core.Value) ? 1f : 0f;
        var changed = ChangedImage;
        if (noAnimate) {
            Color c = changed.color;
            c.a = target;
            changed.color = c;
            return;
        }

        _changeTween = Ctx.Tween.TweenFloat(
            () => changed.color.a,
            v => {
                if (changed) {
                    Color c = changed.color;
                    c.a = v;
                    changed.color = c;
                }
            },
            target, 0.2f);
    }

    /// <summary>Replaces the trailing icon.</summary>
    public void SetIcon(O5Icon icon) => O5FactoryIcons.SetIcon(Icon, icon);

    private void UpdateIcon(bool focused) {
        if (IsDisposed || Icon == null || !Icon.enabled || string.IsNullOrEmpty(Icon.text)) {
            return;
        }
        if (_iconFocused == focused) {
            return;
        }
        _iconFocused = focused;

        _iconTween?.Kill();
        var icon = Icon;
        _iconTween = Ctx.Tween.TweenFloat(
            () => icon.color.a,
            v => {
                if (icon) {
                    Color c = icon.color;
                    c.a = v;
                    icon.color = c;
                }
            },
            focused ? 0f : Ctx.Theme.TextFaintAlpha, focused ? 0.2f : 0.3f, ease: O5Ease.OutQuad);
    }

    /// <inheritdoc/>
    public override void Tick() {
        if (IsDisposed) {
            return;
        }

        Core.OnTick();
        UpdateIcon(InputField.isFocused);
    }

    /// <inheritdoc/>
    protected override void OnThemeChanged(O5Theme prev, O5Theme next) {
        Core.RefreshTheme();
        if (ChangedImage) {
            ChangedImage.color = O5Palette.WithAlpha(next.ObjectActive, ChangedImage.color.a);
        }
        if (Icon) {
            Icon.color = O5Palette.WithAlpha(next.Text, Icon.color.a);
        }
        if (Placeholder) {
            Placeholder.color = O5Palette.WithAlpha(next.Text, Placeholder.color.a);
        }
    }

    /// <inheritdoc/>
    public override void Dispose() {
        if (IsDisposed) {
            return;
        }

        Core.Dispose();
        _changeTween?.Kill();
        _iconTween?.Kill();
        _changeTween = _iconTween = null;
        base.Dispose();
    }
}
