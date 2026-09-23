// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using O5Kit.Core;
using UnityEngine;

#if IL2CPP
using MelonLoader;
#endif

namespace O5Kit.Control;

/// <summary>Shared focus/caret/placeholder logic behind text-based controls.</summary>
public class O5InputCore {
    /// <summary>Current text.</summary>
    public string Value { get; private set; }

    /// <summary>Change callback.</summary>
    public Action<string>? OnChanged { get; set; }

    /// <summary>End-edit (submit/blur) callback.</summary>
    public Action<string>? OnEndEdit { get; set; }

    /// <summary>Underlying TMP input.</summary>
    public TMPro.TMP_InputField InputField { get; }

    /// <summary>Placeholder text. May be null.</summary>
    public TMPro.TextMeshProUGUI? Placeholder { get; }

    private ITweenHandle? _caretTween, _placeholderTween;
    private bool _caretLooping, _hasFocused;
    private bool _suppressChanged;

    /// <summary>Wires a TMP input with focus tracking and caret animation.</summary>
    /// <param name="inputField">TMP input to drive.</param>
    /// <param name="placeholder">Placeholder text, if any.</param>
    /// <param name="value">Initial text.</param>
    /// <param name="onChanged">Change callback.</param>
    /// <param name="onEndEdit">End-edit callback.</param>
    /// <param name="multiline">Multi-line mode.</param>
    public O5InputCore(TMPro.TMP_InputField inputField, TMPro.TextMeshProUGUI? placeholder, string value, Action<string>? onChanged, Action<string>? onEndEdit, bool multiline = false) {
        InputField = inputField;
        Placeholder = placeholder;
        Value = value;
        OnChanged = onChanged;
        OnEndEdit = onEndEdit;

        SetupInputField(multiline);

        if (InputField.text != (value ?? string.Empty)) {
            InputField.text = value ?? string.Empty;
        }

        InputField.onValueChanged.AddListener(
#if IL2CPP
            DelegateSupport.ConvertDelegate<UnityEngine.Events.UnityAction<string>>(new Action<string>(
#endif
                OnValueChanged
#if IL2CPP
            ))
#endif
        );

        InputField.onEndEdit.AddListener(
#if IL2CPP
            DelegateSupport.ConvertDelegate<UnityEngine.Events.UnityAction<string>>(new Action<string>(
#endif
                OnValueEndEdit
#if IL2CPP
            ))
#endif
        );
    }

    /// <summary>Polls focus state. Call per frame from the owning control.</summary>
    public void OnTick() {
        bool focused = InputField.isFocused;
        if (focused == _hasFocused) {
            return;
        }

        _hasFocused = focused;
        O5InputBlocker.SetFocused(focused, focused ? InputField.gameObject : null);

        UpdateCaretAnimation(focused);
        UpdatePlaceholder(focused);
    }

    /// <summary>Replaces the text, optionally invoking <see cref="OnChanged"/>.</summary>
    /// <param name="value">New text.</param>
    /// <param name="invoke">Fire the change callback.</param>
    public void SetValue(string value, bool invoke = true) {
        Value = value ?? string.Empty;
        if (InputField.text != Value) {
            _suppressChanged = true;
            InputField.text = Value;
            _suppressChanged = false;
        }

        if (invoke) {
            OnChanged?.Invoke(Value);
        }
    }

    private void SetupInputField(bool multiline) {
        InputField.lineType = multiline
            ? TMPro.TMP_InputField.LineType.MultiLineNewline
            : TMPro.TMP_InputField.LineType.SingleLine;
        InputField.lineLimit = 0;
        InputField.richText = false;
        InputField.customCaretColor = true;
        InputField.caretColor = O5Boot.Theme.ObjectActive;
        InputField.caretBlinkRate = 0f;
        InputField.caretWidth = 2;
        InputField.selectionColor = O5Boot.Theme.MenuHover;
    }

    private void OnValueChanged(string value) {
        Value = value;
        UpdateCaretAnimation(InputField.isFocused);
        if (!_suppressChanged) {
            OnChanged?.Invoke(value);
        }
    }

    private void OnValueEndEdit(string value) => OnEndEdit?.Invoke(value);

    private void UpdateCaretAnimation(bool focused) {
        if (focused) {
            if (_caretLooping) {
                return;
            }

            _caretLooping = true;
            _caretTween?.Kill();
            InputField.caretColor = O5Boot.Theme.ObjectActive;
            PlayCaretDown();
            return;
        }

        _caretLooping = false;
        _caretTween?.Kill();
        InputField.caretColor = O5Boot.Theme.ObjectActive;
    }

    private void PlayCaretDown() {
        if (!_caretLooping) {
            return;
        }

        var field = InputField;
        var active = O5Boot.Theme.ObjectActive;
        _caretTween = O5Boot.Tween.TweenFloat(
            () => field ? field.caretColor.a : 0.35f,
            v => {
                if (field) {
                    var color = active;
                    color.a = v;
                    field.caretColor = color;
                }
            },
            0.35f, 0.55f, PlayCaretUp, O5Ease.InOutSine);
    }

    private void PlayCaretUp() {
        if (!_caretLooping) {
            return;
        }

        var field = InputField;
        var active = O5Boot.Theme.ObjectActive;
        _caretTween = O5Boot.Tween.TweenFloat(
            () => field ? field.caretColor.a : 1f,
            v => {
                if (field) {
                    var color = active;
                    color.a = v;
                    field.caretColor = color;
                }
            },
            1f, 0.12f, PlayCaretDown);
    }

    private void UpdatePlaceholder(bool focused) {
        if (Placeholder == null) {
            return;
        }

        _placeholderTween?.Kill();

        float target = focused ? 0f : 0.2f;
        float duration = focused ? 0.2f : 0.3f;

        var ph = Placeholder;
        _placeholderTween = O5Boot.Tween.TweenFloat(
            () => ph.color.a,
            v => {
                if (ph) {
                    Color c = ph.color;
                    c.a = v;
                    ph.color = c;
                }
            },
            target, duration, ease: O5Ease.OutQuad);
    }

    /// <summary>Releases focus state and tweens. The owning control calls this on dispose.</summary>
    public void Dispose() {
        if (_hasFocused) {
            _hasFocused = false;
            O5InputBlocker.SetFocused(false);
        }

        _caretLooping = false;
        _caretTween?.Kill();
        _placeholderTween?.Kill();
        _caretTween = _placeholderTween = null;
    }
}
