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
    /// <summary>Value to reset to on middle-click. Null disables reset and hides the dot.</summary>
    public double? DefaultValue { get; private set; }

    /// <summary>Drag range lower bound.</summary>
    public double Min { get; set; }

    /// <summary>Drag range upper bound.</summary>
    public double Max { get; set; }

    /// <summary>Text-input lower bound. Null falls back to <see cref="Min"/>.</summary>
    public double? InputMin { get; set; }

    /// <summary>Text-input upper bound. Null falls back to <see cref="Max"/>.</summary>
    public double? InputMax { get; set; }

    /// <summary>Effective text-input lower bound.</summary>
    public double EffectiveInputMin => InputMin ?? Min;

    /// <summary>Effective text-input upper bound.</summary>
    public double EffectiveInputMax => InputMax ?? Max;

    /// <summary>Current value.</summary>
    public double Value { get; private set; }

    /// <summary>Display format, e.g. <c>"F2"</c>.</summary>
    public string Format { get; set; }

    /// <summary>Where clamping applies.</summary>
    public ClampMode ClampMode { get; set; }

    /// <summary>Whether the fill bar is shown.</summary>
    public bool ShowFill { get; set; } = true;

    /// <summary>Fired on every value change.</summary>
    public Action<double>? OnChanged { get; set; }

    /// <summary>Fired when a drag or text edit commits.</summary>
    public Action<double>? OnComplete { get; set; }

    /// <summary>Value transform applied on drag/set.</summary>
    public Func<double, double>? SliderFilter { get; set; }

    /// <summary>Value transform applied on text input. Null means no transform (free input).</summary>
    public Func<double, double>? InputFilter { get; set; }

    /// <summary>Maps (min, value, max) to 0..1 for the fill bar and previews. Null means linear.</summary>
    public Func<double, double, double, double>? NormalizeFunc { get; set; }

    /// <summary>Inverse of <see cref="NormalizeFunc"/>: maps (min, t, max) back to a value. Null means linear. Used by <see cref="SetNormalized"/>.</summary>
    public Func<double, double, double, double>? DenormalizeFunc { get; set; }

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
    public double? LastValidValue { get; private set; }
    private bool _isUpdatingFromCode;

    private ITweenHandle? _fillTween, _changeTween, _stateTween;

    /// <summary>Creates a slider over factory-built visuals.</summary>
    /// <param name="ctx">Owning kit context.</param>
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
    /// <param name="min">Drag range lower bound.</param>
    /// <param name="max">Drag range upper bound.</param>
    /// <param name="value">Initial value.</param>
    /// <param name="format">Display format.</param>
    /// <param name="clampMode">Where clamping applies.</param>
    /// <param name="sliderFilter">Value transform for drag/set.</param>
    /// <param name="inputFilter">Value transform for text input. Null = free.</param>
    /// <param name="onChanged">Change callback.</param>
    /// <param name="onComplete">Commit callback.</param>
    /// <param name="inputMin">Text-input lower bound. Null falls back to min.</param>
    /// <param name="inputMax">Text-input upper bound. Null falls back to max.</param>
    /// <param name="normalizeFunc">(min, value, max) to 0..1 mapping. Null means linear.</param>
    /// <param name="denormalizeFunc">Inverse mapping for <see cref="SetNormalized"/>. Null means linear.</param>
    public O5Slider(
        O5Context ctx,
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
        double? defaultValue,
        double min,
        double max,
        double value,
        string format,
        ClampMode clampMode,
        Func<double, double>? sliderFilter,
        Func<double, double>? inputFilter,
        Action<double>? onChanged,
        Action<double>? onComplete,
        double? inputMin = null,
        double? inputMax = null,
        Func<double, double, double, double>? normalizeFunc = null,
        Func<double, double, double, double>? denormalizeFunc = null
    ) : base(ctx, id, rect) {
        FillRect = fillRect;
        FillImage = fillImage;
        FillImage.color = Ctx.Theme.ObjectActive;
        Label = label;
        InputCore = new O5InputCore(ctx, valueInputField, null, value.ToString(format),
            (val) => {
                if (_isUpdatingFromCode) {
                    return;
                }

                var (rawResult, rawState) = Evaluator<double>.Evaluate(val, Value);
                bool clampsInput = ClampMode is ClampMode.Input or ClampMode.All;
                LastValidValue = rawState != EvalState.Error
                    ? ApplyInputFilter(ClampSafe(rawResult, EffectiveInputMin, EffectiveInputMax, clampsInput))
                    : null;

                double previewResult = rawResult;
                EvalState previewState = rawState;
                if (rawState != EvalState.Error && clampsInput) {
                    // The text range determines what can be committed; the visible
                    // preview is bounded by the slider track as well. This keeps
                    // < and > useful when text input intentionally has a wider range.
                    double previewMin = Math.Max(Min, EffectiveInputMin);
                    double previewMax = Math.Min(Max, EffectiveInputMax);
                    if (previewMin > previewMax) {
                        previewMin = EffectiveInputMin;
                        previewMax = EffectiveInputMax;
                    }

                    if (rawResult < previewMin) {
                        previewResult = previewMin;
                        previewState = EvalState.UnderRange;
                    } else if (rawResult > previewMax) {
                        previewResult = previewMax;
                        previewState = EvalState.OverRange;
                    }
                }

                bool isCalc = rawState != EvalState.Error;
                if (isCalc) {
                    bool isSameValue = double.TryParse(val, out double parsedVal) &&
                        Math.Abs(parsedVal - previewResult) < 0.0000001 &&
                        previewState is not (EvalState.OverRange or EvalState.UnderRange);

                    if (isSameValue) {
                        PreviewLabel.text = "";
                        SetStateVisuals(MathVisuals.GetStateColor(Ctx.Theme, previewState), true, previewResult);
                    } else {
                        string valStr = ApplyInputFilter(previewResult).ToString();
                        string symbol = previewState switch {
                            EvalState.OverRange => "<",
                            EvalState.UnderRange => ">",
                            _ => "="
                        };

                        PreviewLabel.text = $"{valStr} {symbol} <color=#00000000>{val}</color>";
                        SetStateVisuals(MathVisuals.GetStateColor(Ctx.Theme, previewState), true, previewResult);
                    }
                } else {
                    PreviewLabel.text = "";
                    SetStateVisuals(MathVisuals.GetStateColor(Ctx.Theme, rawState), true);
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
                SetStateVisuals(Ctx.Theme.ObjectActive, false);
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
        ChangedUpImage.color = Ctx.Theme.ObjectBG;
        OutlineImage = outlineImage;
        DefaultValue = defaultValue;
        Min = min;
        Max = max;
        InputMin = inputMin;
        InputMax = inputMax;
        OnChanged = onChanged;
        OnComplete = onComplete;
        Format = format;
        ClampMode = clampMode;
        SliderFilter = sliderFilter;
        InputFilter = inputFilter;
        NormalizeFunc = normalizeFunc;
        DenormalizeFunc = denormalizeFunc;
        Value = ApplySliderFilter(value);
        Value = ClampSafe(Value, Min, Max, ClampMode is ClampMode.All);

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

    /// <summary>Sets the value (drag/code path), optionally invoking <see cref="OnChanged"/>.</summary>
    /// <param name="value">New value. NaN is ignored.</param>
    /// <param name="invoke">Fire the change callback.</param>
    /// <param name="noFilter">Skip <see cref="SliderFilter"/>.</param>
    public void Set(double value, bool invoke = true, bool noFilter = false) {
        if (IsDisposed) {
            return;
        }

        if (double.IsNaN(value)) {
            return;
        }

        if (!noFilter) {
            value = ApplySliderFilter(value);
        }

        Value = ClampSafe(value, Min, Max, ClampMode is ClampMode.All);

        if (invoke) {
            OnChanged?.Invoke(Value);
        }

        _isUpdatingFromCode = true;
        InputCore.SetValue(Value.ToString(Format), false);
        _isUpdatingFromCode = false;

        UpdateVisual();
    }

    private void SetFromInput(double value) {
        if (IsDisposed || double.IsNaN(value)) {
            return;
        }

        Value = ClampSafe(
            value,
            EffectiveInputMin,
            EffectiveInputMax,
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
    public void SetDefaultValue(double? value, bool noAnimate = false) {
        if (IsDisposed) {
            return;
        }

        if (value == null || double.IsNaN(value.Value)) {
            DefaultValue = null;
            UpdateVisual(noAnimate);
            return;
        }

        DefaultValue = ClampSafe(ApplySliderFilter(value.Value), Min, Max, ClampMode is ClampMode.Slider or ClampMode.All);
        UpdateVisual(noAnimate);
    }

    private static double ClampSafe(double value, double min, double max, bool clamp) {
        if (double.IsNaN(value)) {
            return value;
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

    /// <summary>Current value as 0..1 across the drag range.</summary>
    public double Normalize() => Normalize(Value);

    /// <summary>Arbitrary value as 0..1 across the drag range.</summary>
    /// <param name="value">Value to normalize.</param>
    public double Normalize(double value) => (NormalizeFunc ?? LinearNormalize)(Min, value, Max);

    /// <summary>Sets the value from a 0..1 position.</summary>
    /// <param name="t">Normalized position.</param>
    /// <param name="invoke">Fire the change callback.</param>
    public void SetNormalized(double t, bool invoke = true) => Set((DenormalizeFunc ?? LinearDenormalize)(Min, t, Max), invoke);

    /// <summary>Default linear (min, value, max) to 0..1 mapping.</summary>
    public static double LinearNormalize(double min, double value, double max) {
        double span = max - min;
        if (span == 0d) {
            return 0d;
        }

        return (value - min) / span;
    }

    /// <summary>Default linear (min, t, max) to value mapping.</summary>
    public static double LinearDenormalize(double min, double t, double max) => min + ((max - min) * t);

    /// <summary>Logarithmic (min, value, max) to 0..1 mapping. Requires min &gt; 0; falls back to linear otherwise.</summary>
    public static double LogNormalize(double min, double value, double max) {
        if (min <= 0d || max <= 0d || value <= 0d || max == min) {
            return LinearNormalize(min, value, max);
        }

        return Math.Log(value / min) / Math.Log(max / min);
    }

    /// <summary>Inverse of <see cref="LogNormalize"/>. Requires min &gt; 0; falls back to linear otherwise.</summary>
    public static double LogDenormalize(double min, double t, double max) {
        if (min <= 0d || max <= 0d || max == min) {
            return LinearDenormalize(min, t, max);
        }

        return min * Math.Pow(max / min, t);
    }

    private double ApplySliderFilter(double v) => SliderFilter?.Invoke(v) ?? v;

    private double ApplyInputFilter(double v) => InputFilter?.Invoke(v) ?? v;

    private static float ToFillT(double normalized) => (float)Math.Clamp(normalized, 0d, 1d);

    /// <summary>Refreshes fill, changed-dots and value box.</summary>
    /// <param name="noAnimate">Snap instead of animating.</param>
    public void UpdateVisual(bool noAnimate = false) {
        if (IsDisposed) {
            return;
        }

        _fillTween?.Kill();
        _changeTween?.Kill();

        float changeAlpha = DefaultValue.HasValue && Math.Abs(DefaultValue.Value - Value) > 0.0001 ? 1f : 0f;

        if (noAnimate) {
            if (ShowFill) {
                Vector2 fra = FillRect.anchorMax;
                fra.x = ToFillT(Normalize());
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
            _fillTween = TweenAnchorMaxX(FillRect, ToFillT(Normalize()), 0.6f, O5Ease.OutExpo);
        }

        var changed = ChangedImage;
        var changedUp = ChangedUpImage;
        bool showFill = ShowFill;
        float changedStart = changed.color.a;
        float changedUpStart = changedUp.color.a;
        _changeTween = Ctx.Tween.TweenFloat(
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

    private void SetStateVisuals(Color targetColor, bool isCalculating, double? value = null) {
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
        _stateTween = Ctx.Tween.TweenFloat(
            () => 0f,
            x => {
                if (outline) {
                    outline.color = Color.Lerp(startOutline, O5Palette.WithAlpha(targetColor, isCalculating ? targetColor.a : 0f), x);
                }

                if (fill) {
                    fill.color = Color.Lerp(startFill, O5Palette.WithAlpha(targetColor, targetFillAlpha), x);
                }

                if (changed) {
                    changed.color = Color.Lerp(startChanged, O5Palette.WithAlpha(targetColor, changed.color.a), x);
                }

                if (field) {
                    field.caretColor = Color.Lerp(startCaret, O5Palette.WithAlpha(targetColor, field.caretColor.a), x);
                }
            },
            1f, 0.2f);

        if (showFill && value.HasValue && isCalculating) {
            _fillTween?.Kill();
            _fillTween = TweenAnchorMaxX(FillRect, ToFillT(Normalize(value.Value)), 0.4f, O5Ease.OutExpo);
        }
    }

    private ITweenHandle TweenAnchorMaxX(RectTransform rect, float targetX, float duration, O5Ease ease) {
        var r = rect;
        float startX = r.anchorMax.x;
        return Ctx.Tween.TweenFloat(
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
