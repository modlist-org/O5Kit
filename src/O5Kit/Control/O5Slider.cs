// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using O5Kit.Core;
using O5Kit.Eval;
using UnityEngine;
using UnityEngine.UI;

#if IL2CPP
using MelonLoader;
#endif

namespace O5Kit.Control;

/// <summary>Where value clamping applies.</summary>
public enum ClampMode {
    /// <summary>No clamping anywhere.</summary>
    None,
    /// <summary>Clamp drags, free text entry.</summary>
    Slider,
    /// <summary>Clamp text entry, free drags.</summary>
    Input,
    /// <summary>Clamp everywhere.</summary>
    All,
}

/// <summary>Numeric slider with fill bar, formula input box and live preview.</summary>
public class O5Slider : O5Object {
    /// <summary>Value to reset to on middle-click.</summary>
    public float DefaultValue { get; private set; }

    /// <summary>Range lower bound.</summary>
    public float Min { get; set; }

    /// <summary>Range upper bound.</summary>
    public float Max { get; set; }

    /// <summary>Current value.</summary>
    public float Value { get; private set; }

    /// <summary>Display format, e.g. <c>"F2"</c>.</summary>
    public string Format { get; set; }

    /// <summary>Where clamping applies.</summary>
    public ClampMode ClampMode { get; set; }

    /// <summary>Whether the fill bar is shown.</summary>
    public bool ShowFill { get; set; } = true;

    /// <summary>Fired on every value change.</summary>
    public Action<float>? OnChanged { get; set; }

    /// <summary>Fired when a drag or text edit commits.</summary>
    public Action<float>? OnComplete { get; set; }

    /// <summary>Optional value transform applied on set.</summary>
    public Func<float, float>? Filter { get; set; }

    /// <summary>Fill bar rect (anchor-x driven).</summary>
    public RectTransform FillRect { get; }

    /// <summary>Fill bar image.</summary>
    public Image FillImage { get; }

    /// <summary>Label text.</summary>
    public TMPro.TextMeshProUGUI Label { get; }

    /// <summary>Value box editing logic.</summary>
    public O5InputCore InputCore { get; }

    /// <summary>Live formula preview (e.g. <c>"3 = 1+2"</c>).</summary>
    public TMPro.TextMeshProUGUI PreviewLabel { get; }

    /// <summary>Dot shown while the value differs from default.</summary>
    public Image ChangedImage { get; }

    /// <summary>Second changed-dot above the fill bar.</summary>
    public Image ChangedUpImage { get; }

    /// <summary>Hover/state outline image.</summary>
    public Image OutlineImage { get; }

    /// <summary>Last valid formula result, if any.</summary>
    public float? LastValidValue { get; private set; }
    private bool _isUpdatingFromCode;

    private ITweenHandle? _fillTween, _changeTween, _stateTween;

    /// <summary>Creates a slider over factory-built visuals.</summary>
    /// <param name="id">Stable identifier.</param>
    /// <param name="rect">Root rect.</param>
    /// <param name="fillRect">Fill bar rect.</param>
    /// <param name="fillImage">Fill bar image.</param>
    /// <param name="label">Label text.</param>
    /// <param name="valueInputField">Value box input.</param>
    /// <param name="previewLabel">Formula preview text.</param>
    /// <param name="changedImage">Changed-dot image.</param>
    /// <param name="changedUpImage">Fill-bar changed-dot image.</param>
    /// <param name="outlineImage">Hover outline image.</param>
    /// <param name="defaultValue">Reset target.</param>
    /// <param name="min">Range lower bound.</param>
    /// <param name="max">Range upper bound.</param>
    /// <param name="value">Initial value.</param>
    /// <param name="format">Display format.</param>
    /// <param name="clampMode">Where clamping applies.</param>
    /// <param name="filter">Optional value transform.</param>
    /// <param name="onChanged">Change callback.</param>
    /// <param name="onComplete">Commit callback.</param>
    public O5Slider(
        string id,
        RectTransform rect,
        RectTransform fillRect,
        Image fillImage,
        TMPro.TextMeshProUGUI label,
        TMPro.TMP_InputField valueInputField,
        TMPro.TextMeshProUGUI previewLabel,
        Image changedImage,
        Image changedUpImage,
        Image outlineImage,
        float defaultValue,
        float min,
        float max,
        float value,
        string format,
        ClampMode clampMode,
        Func<float, float>? filter,
        Action<float>? onChanged,
        Action<float>? onComplete
    ) : base(id, rect) {
        FillRect = fillRect;
        FillImage = fillImage;
        FillImage.color = O5Boot.Theme.ObjectActive;
        Label = label;
        InputCore = new O5InputCore(valueInputField, null, value.ToString(format),
            (val) => {
                if (_isUpdatingFromCode) {
                    return;
                }

                var (result, state) = ClampMode switch {
                    ClampMode.None => Evaluator<float>.Evaluate(val, Value),
                    ClampMode.Slider => Evaluator<float>.Evaluate(val, Value),
                    ClampMode.Input => Evaluator<float>.Evaluate(val, Value, Min, Max),
                    ClampMode.All => Evaluator<float>.Evaluate(val, Value, Min, Max),
                    _ => throw new ArgumentOutOfRangeException(nameof(ClampMode), (object)ClampMode, null)
                };

                LastValidValue = state != EvalState.Error ? ApplyFilter(result) : null;

                bool isCalc = state != EvalState.Error;
                if (isCalc) {
                    bool isSameValue = float.TryParse(val, out float parsedVal) &&
                        Math.Abs(parsedVal - result) < 0.0001f;

                    if (isSameValue) {
                        PreviewLabel.text = "";
                        SetStateVisuals(MathVisuals.GetStateColor(state), true, result);
                    } else {
                        string valStr = (Filter?.Invoke(result) ?? result).ToString();
                        string symbol = state switch {
                            EvalState.OverRange => "<",
                            EvalState.UnderRange => ">",
                            _ => "="
                        };

                        PreviewLabel.text = $"{valStr} {symbol} <color=#00000000>{val}</color>";
                        SetStateVisuals(MathVisuals.GetStateColor(state), true, result);
                    }
                } else {
                    PreviewLabel.text = "";
                    SetStateVisuals(MathVisuals.GetStateColor(state), true);
                }
            },
            (val) => {
                if (LastValidValue == null) {
                    InputCore.SetValue(Value.ToString(Format), false);
                } else {
                    SetFromInput(LastValidValue.Value);
                    OnComplete?.Invoke(Value);
                }

                PreviewLabel.text = "";
                SetStateVisuals(O5Boot.Theme.ObjectActive, false);
            }
        );
        valueInputField.onSelect.AddListener(
#if IL2CPP
            DelegateSupport.ConvertDelegate<UnityEngine.Events.UnityAction<string>>(new Action<string>(
#endif
                (_) => InputCore.SetValue(Value.ToString("R"), false)
#if IL2CPP
            ))
#endif
        );
        PreviewLabel = previewLabel;
        ChangedImage = changedImage;
        ChangedUpImage = changedUpImage;
        ChangedUpImage.color = O5Boot.Theme.ObjectBG;
        OutlineImage = outlineImage;
        DefaultValue = defaultValue;
        Min = min;
        Max = max;
        OnChanged = onChanged;
        OnComplete = onComplete;
        Format = format;
        ClampMode = clampMode;
        Filter = filter;
        Value = ApplyFilter(value);
        Value = ClampSafe(Value, Min, Max, ClampMode is ClampMode.Slider or ClampMode.All);

        RegisterTick();
        UpdateVisual(true);
    }

    /// <inheritdoc/>
    public override void Tick() {
        if (IsDisposed) {
            return;
        }

        InputCore.OnTick();
    }

    /// <summary>Sets the value, optionally invoking <see cref="OnChanged"/>.</summary>
    /// <param name="value">New value. NaN is ignored.</param>
    /// <param name="invoke">Fire the change callback.</param>
    /// <param name="noFilter">Skip <see cref="Filter"/>.</param>
    public void Set(float value, bool invoke = true, bool noFilter = false) {
        if (IsDisposed) {
            return;
        }

        if (float.IsNaN(value)) {
            return;
        }

        if (!noFilter) {
            value = ApplyFilter(value);
        }

        Value = ClampSafe(value, Min, Max, ClampMode is ClampMode.Slider or ClampMode.All);

        if (invoke) {
            OnChanged?.Invoke(Value);
        }

        _isUpdatingFromCode = true;
        InputCore.SetValue(Value.ToString(Format), false);
        _isUpdatingFromCode = false;

        UpdateVisual();
    }

    private void SetFromInput(float value) {
        if (IsDisposed || float.IsNaN(value)) {
            return;
        }

        Value = ClampSafe(
            value,
            Min,
            Max,
            ClampMode is ClampMode.Input or ClampMode.All
        );

        OnChanged?.Invoke(Value);

        _isUpdatingFromCode = true;
        InputCore.SetValue(Value.ToString(Format), false);
        _isUpdatingFromCode = false;

        UpdateVisual();
    }

    /// <summary>Moves the reset target and refreshes visuals.</summary>
    /// <param name="value">New default.</param>
    /// <param name="noAnimate">Snap instead of animating.</param>
    public void SetDefaultValue(float value, bool noAnimate = false) {
        if (IsDisposed || float.IsNaN(value)) {
            return;
        }

        DefaultValue = ClampSafe(ApplyFilter(value), Min, Max, ClampMode is ClampMode.Slider or ClampMode.All);
        UpdateVisual(noAnimate);
    }

    private float ClampSafe(float value, float min, float max, bool clamp) {
        if (float.IsNaN(value)) {
            return Value;
        }

        if (!clamp) {
            return value;
        }

        if (value < min) {
            return min;
        }

        if (value > max) {
            return max;
        }

        return value;
    }

    /// <summary>Current value as 0..1 across the range.</summary>
    public float Normalize() => Mathf.InverseLerp(Min, Max, Value);

    /// <summary>Arbitrary value as 0..1 across the range.</summary>
    /// <param name="value">Value to normalize.</param>
    public float Normalize(float value) => Mathf.InverseLerp(Min, Max, value);

    /// <summary>Sets the value from a 0..1 position.</summary>
    /// <param name="t">Normalized position.</param>
    /// <param name="invoke">Fire the change callback.</param>
    public void SetNormalized(float t, bool invoke = true) => Set(Mathf.Lerp(Min, Max, t), invoke);

    private float ApplyFilter(float v) => Filter?.Invoke(v) ?? v;

    /// <summary>Refreshes fill, changed-dots and value box.</summary>
    /// <param name="noAnimate">Snap instead of animating.</param>
    public void UpdateVisual(bool noAnimate = false) {
        if (IsDisposed) {
            return;
        }

        _fillTween?.Kill();
        _changeTween?.Kill();

        float changeAlpha = Math.Abs(DefaultValue - Value) > 0.001f ? 1f : 0f;

        if (noAnimate) {
            if (ShowFill) {
                Vector2 fra = FillRect.anchorMax;
                fra.x = Normalize();
                FillRect.anchorMax = fra;
            }

            Color ci = ChangedImage.color;
            ci.a = changeAlpha;
            ChangedImage.color = ci;
            if (ShowFill) {
                Color cui = ChangedUpImage.color;
                cui.a = changeAlpha;
                ChangedUpImage.color = cui;
            }

            return;
        }

        if (ShowFill) {
            _fillTween = TweenAnchorMaxX(FillRect, Normalize(), 0.6f, O5Ease.OutExpo);
        }

        var changed = ChangedImage;
        var changedUp = ChangedUpImage;
        bool showFill = ShowFill;
        float changedStart = changed.color.a;
        float changedUpStart = changedUp.color.a;
        _changeTween = O5Boot.Tween.TweenFloat(
            () => 0f,
            t => {
                if (changed) {
                    var c = changed.color;
                    c.a = Mathf.LerpUnclamped(changedStart, changeAlpha, t);
                    changed.color = c;
                }

                if (showFill && changedUp) {
                    var c = changedUp.color;
                    c.a = Mathf.LerpUnclamped(changedUpStart, changeAlpha, t);
                    changedUp.color = c;
                }
            },
            1f, 0.2f);
    }

    private void SetStateVisuals(Color targetColor, bool isCalculating, float? value = null) {
        if (IsDisposed) {
            return;
        }

        _stateTween?.Kill();

        float targetFillAlpha = isCalculating ? (value.HasValue ? 0.3f : 0f) : 1f;

        Color startOutline = OutlineImage.color;
        Color startFill = FillImage.color;
        Color startChanged = ChangedImage.color;
        Color startCaret = InputCore.InputField.caretColor;

        var outline = OutlineImage;
        var fill = FillImage;
        var changed = ChangedImage;
        var field = InputCore.InputField;
        bool showFill = ShowFill;
        _stateTween = O5Boot.Tween.TweenFloat(
            () => 0f,
            x => {
                if (outline) {
                    outline.color = Color.Lerp(startOutline, new Color(targetColor.r, targetColor.g, targetColor.b, isCalculating ? targetColor.a : 0f), x);
                }

                if (fill) {
                    fill.color = Color.Lerp(startFill, new Color(targetColor.r, targetColor.g, targetColor.b, targetFillAlpha), x);
                }

                if (changed) {
                    changed.color = Color.Lerp(startChanged, new Color(targetColor.r, targetColor.g, targetColor.b, changed.color.a), x);
                }

                if (field) {
                    field.caretColor = Color.Lerp(startCaret, new Color(targetColor.r, targetColor.g, targetColor.b, field.caretColor.a), x);
                }
            },
            1f, 0.2f);

        if (showFill && value.HasValue && isCalculating) {
            _fillTween?.Kill();
            _fillTween = TweenAnchorMaxX(FillRect, Normalize(value.Value), 0.4f, O5Ease.OutExpo);
        }
    }

    private ITweenHandle TweenAnchorMaxX(RectTransform rect, float targetX, float duration, O5Ease ease) {
        var r = rect;
        float startX = r.anchorMax.x;
        return O5Boot.Tween.TweenFloat(
            () => 0f,
            t => {
                if (r) {
                    var am = r.anchorMax;
                    am.x = Mathf.LerpUnclamped(startX, targetX, t);
                    r.anchorMax = am;
                }
            },
            1f, duration, ease: ease);
    }

    /// <inheritdoc/>
    public override void Dispose() {
        if (IsDisposed) {
            return;
        }

        _fillTween?.Kill();
        _changeTween?.Kill();
        _stateTween?.Kill();
        _fillTween = _changeTween = _stateTween = null;
        InputCore.Dispose();
        base.Dispose();
    }
}
