// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using O5Kit.Core;
using UnityEngine;
using UnityEngine.UI;

namespace O5Kit.Control;

/// <summary>Single- or multi-line text field with icon, placeholder and changed-dot.</summary>
public sealed class O5InputField : O5Object {
    /// <summary>Shared editing logic (focus tracking, caret, placeholder).</summary>
    public O5InputCore Core { get; }

    /// <summary>Value to reset to on middle-click. Null disables reset.</summary>
    public string? DefaultValue { get; }

    /// <summary>Current text.</summary>
    public string Value => Core.Value;

    /// <summary>Change callback.</summary>
    public Action<string>? OnChanged => Core.OnChanged;

    /// <summary>Underlying TMP input.</summary>
    public TMPro.TMP_InputField InputField => Core.InputField;

    /// <summary>Placeholder text. May be null for code-constructed fields.</summary>
    public TMPro.TextMeshProUGUI? Placeholder => Core.Placeholder;

    /// <summary>Trailing icon image.</summary>
    public Image IconImage { get; }

    /// <summary>Dot shown while the value differs from default.</summary>
    public Image ChangedImage { get; }

    private ITweenHandle? _changeTween, _iconTween;

    /// <summary>Creates a text field over factory-built visuals.</summary>
    /// <param name="id">Stable identifier.</param>
    /// <param name="rect">Root rect.</param>
    /// <param name="inputField">TMP input.</param>
    /// <param name="placeholder">Placeholder text.</param>
    /// <param name="iconImage">Trailing icon.</param>
    /// <param name="changedImage">Changed-dot image.</param>
    /// <param name="defaultValue">Reset target.</param>
    /// <param name="value">Initial text.</param>
    /// <param name="onChanged">Change callback.</param>
    /// <param name="onEndEdit">End-edit callback.</param>
    /// <param name="multiline">Multi-line mode.</param>
    public O5InputField(
        string id,
        RectTransform rect,
        TMPro.TMP_InputField inputField,
        TMPro.TextMeshProUGUI placeholder,
        Image iconImage,
        Image changedImage,
        string? defaultValue,
        string? value,
        Action<string>? onChanged,
        Action<string>? onEndEdit = null,
        bool multiline = false
    ) : base(id, rect) {
        DefaultValue = defaultValue;
        ChangedImage = changedImage;
        IconImage = iconImage;

        Core = new O5InputCore(inputField, placeholder, value ?? string.Empty, val => {
            UpdateVisual();
            onChanged?.Invoke(val);
        }, onEndEdit, multiline);

        RegisterTick();
        UpdateVisual(true);
    }

    /// <summary>Replaces the text, optionally invoking <see cref="OnChanged"/>.</summary>
    /// <param name="value">New text.</param>
    /// <param name="invoke">Fire the change callback.</param>
    public void Set(string value, bool invoke = true) {
        if (IsDisposed) {
            return;
        }

        Core.SetValue(value, false);
        if (invoke) {
            Core.OnChanged?.Invoke(value);
        }

        UpdateVisual();
    }

    /// <summary>Restores <see cref="DefaultValue"/>. No-op when null.</summary>
    public void Reset() {
        if (DefaultValue != null) {
            Set(DefaultValue);
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

        _changeTween = O5Boot.Tween.TweenFloat(
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

    private void UpdateIconImage(bool focused) {
        if (IsDisposed || IconImage == null || !IconImage.enabled || IconImage.sprite == null) {
            return;
        }

        _iconTween?.Kill();
        var icon = IconImage;
        _iconTween = O5Boot.Tween.TweenFloat(
            () => icon.color.a,
            v => {
                if (icon) {
                    Color c = icon.color;
                    c.a = v;
                    icon.color = c;
                }
            },
            focused ? 0f : 0.2f, focused ? 0.2f : 0.3f, ease: O5Ease.OutQuad);
    }

    /// <inheritdoc/>
    public override void Tick() {
        if (IsDisposed) {
            return;
        }

        Core.OnTick();
        UpdateIconImage(InputField.isFocused);
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
