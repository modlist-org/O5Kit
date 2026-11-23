// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using O5Kit.Core;
using O5Kit.Eval;
using O5Kit.Factory;
using O5Kit.Input;
using UnityEngine;
using UnityEngine.UI;

#if IL2CPP
using MelonLoader;
#endif

namespace O5Kit.Control;

/// <summary>Numeric slider with fill bar, formula input box and live preview.</summary>
public class O5Slider : O5Object {
    /// <summary>Value to reset to on middle-click. Null disables reset and hides the dot.</summary>
    public double? DefaultValue { get; set; }

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

    /// <summary>Clamp drags/code-sets into <c>[Min, Max]</c>.</summary>
    public bool ClampDrag { get; set; }

    /// <summary>Clamp text input into the text-input range.</summary>
    public bool ClampInput { get; set; }

    /// <summary>Whether the fill bar is shown.</summary>
    public bool ShowFill {
        get;
        set {
            field = value;
            if (FillRect != null && FillRect) {
                FillRect.gameObject.SetActive(value);
            }
        }
    } = true;

    /// <summary>Fired on every value change.</summary>
    public Action<double>? OnChanged { get; set; }

    /// <summary>Fired when a drag or text edit commits.</summary>
    public Action<double>? OnComplete { get; set; }

    /// <summary>Value transform applied on drag/code-set. Null means identity.</summary>
    public Func<double, double>? DragFilter { get; set; }

    /// <summary>Value transform applied on text input. Null means identity (free input).</summary>
    public Func<double, double>? InputFilter { get; set; }

    /// <summary>Fill/preview mapping. Null means linear.</summary>
    public IValueScale? Scale { get; set; }

    /// <summary>Fill bar rect (anchor-x driven).</summary>
    public RectTransform FillRect { get; }

    /// <summary>Fill bar image.</summary>
    public Image FillImage { get; }

    /// <summary>Label text.</summary>
    public TMPro.TextMeshProUGUI Label { get; }

    /// <summary>Label string. Get/set proxy for <see cref="Label"/> text.</summary>
    public string LabelText {
        get => Label != null && Label ? Label.text : string.Empty;
        set {
            if (Label != null && Label) {
                Label.text = value;
            }
        }
    }

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
    private Color? _accentColor;
    private static O5Slider? _hovered;

    /// <summary>Optional per-slider accent color overriding theme accent for fill, changed dot, hover outline, and caret. Null follows theme accent.</summary>
    public Color? AccentColor {
        get => _accentColor;
        set {
            _accentColor = value;
            ApplyAccentVisuals();
        }
    }

    internal Color EffectiveAccent => _accentColor ?? Ctx.Theme.ObjectActive;
    internal Color EffectiveHoverOutline => _accentColor ?? Ctx.Theme.ControlOutlineHover;

    /// <summary>True when any slider is currently hovered by the pointer.</summary>
    public static bool AnyHovered => _hovered != null
        && !_hovered.IsDisposed
        && _hovered.Rect != null
        && _hovered.Rect
        && _hovered.Rect.gameObject.activeInHierarchy;

    /// <summary>Optional gate enabling mouse-wheel adjustment on hovered sliders.</summary>
    public static Func<bool>? EnableWheelAdjust { get; set; }

    /// <summary>Fired whenever a slider value changes via mouse wheel.</summary>
    public static Action<O5Slider>? OnAnyWheelChanged { get; set; }

    internal void SetHovered(bool hovered) {
        if (hovered) {
            _hovered = this;
        } else if (ReferenceEquals(_hovered, this)) {
            _hovered = null;
        }
    }

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
    /// <param name="options">Behaviour. Null uses defaults.</param>
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
        O5SliderOptions? options = null
    ) : base(ctx, id, rect) {
        options ??= new O5SliderOptions();
        FillRect = fillRect;
        FillImage = fillImage;
        FillImage.color = Ctx.Theme.ObjectActive;
        Label = label;
        InputCore = new O5InputCore(ctx, valueInputField, null, options.Value.ToString(options.Format),
            (val) => {
                if (_isUpdatingFromCode) {
                    return;
                }

                var (rawResult, rawState) = Evaluator<double>.Evaluate(val, Value);
                LastValidValue = rawState != EvalState.Error
                    ? ApplyInputFilter(ClampSafe(rawResult, EffectiveInputMin, EffectiveInputMax, ClampInput))
                    : null;

                double previewResult = rawResult;
                EvalState previewState = rawState;
                if (rawState != EvalState.Error && ClampInput) {
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
                SetStateVisuals(EffectiveAccent, false);
            }
        );
        valueInputField.onSelect.AddListener(
#if IL2CPP
            new Action<string>((_) => InputCore.SetValue(Value.ToString("R"), false))
#else
            (_) => InputCore.SetValue(Value.ToString("R"), false)
#endif
        );
        PreviewLabel = previewLabel;
        ChangedImage = changedImage;
        ChangedUpImage = changedUpImage;
        ChangedUpImage.color = Ctx.Theme.ObjectBG;
        OutlineImage = outlineImage;
        DefaultValue = options.DefaultValue;
        Min = options.Min;
        Max = options.Max;
        InputMin = options.InputMin;
        InputMax = options.InputMax;
        OnChanged = options.OnChanged;
        OnComplete = options.OnComplete;
        Format = options.Format;
        ClampDrag = options.ClampDrag;
        ClampInput = options.ClampInput;
        DragFilter = options.DragFilter;
        InputFilter = options.InputFilter;
        Scale = options.Scale;
        ShowFill = options.ShowFill;
        Value = ApplyDragFilter(options.Value);
        Value = ClampSafe(Value, Min, Max, ClampDrag);

        RegisterTick();
        UpdateVisual(true);
    }

    /// <summary>Builds a slider under <paramref name="parent"/>. Owns all GameObjects; the caller only passes behaviour.</summary>
    /// <param name="ctx">Owning kit context.</param>
    /// <param name="parent">Parent transform.</param>
    /// <param name="id">Stable identifier.</param>
    /// <param name="options">Behaviour. Null uses defaults.</param>
    public static O5Slider Create(O5Context ctx, Transform parent, string id, O5SliderOptions? options = null)
        => O5Factory.Slider(ctx, parent, id, options);

    /// <summary>Builds a slider under <paramref name="parent"/> with inline configuration.</summary>
    /// <param name="ctx">Owning kit context.</param>
    /// <param name="parent">Parent transform.</param>
    /// <param name="id">Stable identifier.</param>
    /// <param name="configure">Mutates a fresh <see cref="O5SliderOptions"/>.</param>
    public static O5Slider Create(O5Context ctx, Transform parent, string id, Action<O5SliderOptions> configure) {
        var options = new O5SliderOptions();
        configure?.Invoke(options);
        return Create(ctx, parent, id, options);
    }

    /// <inheritdoc/>
    public override void Tick() {
        if (IsDisposed) {
            return;
        }

        InputCore.OnTick();

        if (ReferenceEquals(_hovered, this)
            && EnableWheelAdjust?.Invoke() == true
            && !O5Input.GetMouseButton(0)
            && CanvasGroup.interactable) {
            float wheel = O5Input.MouseScrollDelta.y;
            if (Mathf.Abs(wheel) > 0.0001f) {
                double step = Math.Max(0.01d, Math.Abs(Max - Min) * 0.01d);
                if (UnityEngine.Input.GetKey(KeyCode.LeftShift) || UnityEngine.Input.GetKey(KeyCode.RightShift)) {
                    step *= 0.1d;
                }
                double next = ApplyDragFilter(Value + wheel * step);
                if (ClampDrag) {
                    next = ClampSafe(next, Min, Max, true);
                }
                if (Math.Abs(next - Value) > 1e-9d) {
                    Set(next, true);
                    OnComplete?.Invoke(Value);
                    OnAnyWheelChanged?.Invoke(this);
                }
            }
        }
    }

    /// <summary>Sets the value (drag/code path).</summary>
    /// <param name="value">New value. NaN is ignored.</param>
    /// <param name="notify">Fire the change callback.</param>
    public void Set(double value, bool notify = true) {
        if (IsDisposed) {
            return;
        }

        if (double.IsNaN(value)) {
            return;
        }

        value = ApplyDragFilter(value);
        Value = ClampSafe(value, Min, Max, ClampDrag);

        if (notify) {
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
            ClampInput
        );

        OnChanged?.Invoke(Value);

        _isUpdatingFromCode = true;
        InputCore.SetValue(Value.ToString(Format), false);
        _isUpdatingFromCode = false;

        UpdateVisual();
    }

    /// <summary>Restores <see cref="DefaultValue"/>. No-op when null.</summary>
    /// <param name="notify">Fire the change callback.</param>
    public void Reset(bool notify = true) {
        if (DefaultValue.HasValue) {
            Set(DefaultValue.Value, notify);
            if (notify) {
                OnComplete?.Invoke(Value);
            }
        }
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

        DefaultValue = ClampSafe(ApplyDragFilter(value.Value), Min, Max, ClampDrag);
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
    public double Normalize(double value) => (Scale ?? O5ValueScale.Linear).Normalize(Min, value, Max);

    /// <summary>Sets the value from a 0..1 position.</summary>
    /// <param name="t">Normalized position.</param>
    /// <param name="notify">Fire the change callback.</param>
    public void SetNormalized(double t, bool notify = true) => Set((Scale ?? O5ValueScale.Linear).Denormalize(Min, t, Max), notify);

    private double ApplyDragFilter(double v) => DragFilter?.Invoke(v) ?? v;

    private double ApplyInputFilter(double v) => InputFilter?.Invoke(v) ?? v;

    private static float ToFillT(double normalized) => (float)MathCompat.Clamp(normalized, 0d, 1d);

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

    private void ApplyAccentVisuals() {
        if (IsDisposed) {
            return;
        }

        Color accent = EffectiveAccent;
        if (FillImage) {
            FillImage.color = O5Palette.WithAlpha(accent, FillImage.color.a);
        }
        if (ChangedImage) {
            ChangedImage.color = O5Palette.WithAlpha(accent, ChangedImage.color.a);
        }
        if (OutlineImage && OutlineImage.color.a > 0.001f) {
            OutlineImage.color = O5Palette.WithAlpha(EffectiveHoverOutline, OutlineImage.color.a);
        }
        if (InputCore != null && InputCore.InputField) {
            var caret = accent;
            caret.a = InputCore.InputField.caretColor.a;
            InputCore.InputField.caretColor = caret;
            InputCore.InputField.selectionColor = _accentColor.HasValue
                ? O5Palette.WithAlpha(accent, 0.25f)
                : Ctx.Theme.MenuHover;
        }
    }

    /// <inheritdoc/>
    protected override void OnThemeChanged(O5Theme prev, O5Theme next) {
        if (ChangedUpImage) {
            ChangedUpImage.color = O5Palette.WithAlpha(next.ObjectBG, ChangedUpImage.color.a);
        }
        if (PreviewLabel) {
            PreviewLabel.color = next.TextDim;
        }
        if (_accentColor.HasValue) {
            ApplyAccentVisuals();
            return;
        }
        if (FillImage && O5Palette.ApproxEquals(FillImage.color, prev.ObjectActive)) {
            FillImage.color = next.ObjectActive;
        }
        if (ChangedImage) {
            ChangedImage.color = O5Palette.WithAlpha(next.ObjectActive, ChangedImage.color.a);
        }
        InputCore.RefreshTheme();
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
