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

    /// <inheritdoc/>
    public override string? ShownValue => ((float)Value).ToString("R", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>Display format, e.g. <c>"F2"</c>.</summary>
    public string Format { get; set; }

    /// <summary>Clamp drags and wheel steps into <c>[Min, Max]</c>.</summary>
    public bool ClampDrag { get; set; }

    /// <summary>Clamp text input, code sets and the initial value into the text-input range.</summary>
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

    private ITweenHandle? _fillTween, _changeTween, _stateTween, _shakeTween;
    private float _shakeOffset, _shakeAppliedOffset;
    private float _shakeAppliedX = float.NaN;
#if !IL2CPP
    private Canvas.WillRenderCanvases? _shakeRenderHook;
    private bool _shakeRenderHooked;
#endif

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
                if (double.IsNaN(rawResult) || double.IsInfinity(rawResult)) {
                    rawState = EvalState.Error;
                }
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
                    PlayErrorShake();
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
            new Action<string>((_) => {
                LastValidValue = Value;
                InputCore.SetValue(Value.ToString("R"), false);
            })
#else
            (_) => {
                LastValidValue = Value;
                InputCore.SetValue(Value.ToString("R"), false);
            }
#endif
        );
        DisableTrailingZwspAdvance(valueInputField.textComponent);
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
        Value = ClampSafe(Value, EffectiveInputMin, EffectiveInputMax, ClampInput);
        LastValidValue = Value;

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

        // uGUI sends no PointerExit to a deactivated object, so a hidden slider can stay hovered.
        if (ReferenceEquals(_hovered, this) && !Rect.gameObject.activeInHierarchy) {
            _hovered = null;
        }

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

        // Raw number, before the filter / clamp / display rounding: a change smaller than the shown
        // decimals (or than a filter step) still counts. Float precision, so a float setting
        // reflecting this slider's own double value isn't a change.
        bool changed = (float)value != (float)_lastRaw && (float)value != (float)Value;
        _lastRaw = value;
        value = ApplyDragFilter(value);
        // Drag and wheel paths clamp by ClampDrag themselves; code/sync Set must respect ClampInput
        // so unclamped text-input values are not truncated on subsequent syncs.
        Value = ClampSafe(value, EffectiveInputMin, EffectiveInputMax, ClampInput);

        if (notify) {
            OnChanged?.Invoke(Value);
        }

        string shown = Value.ToString(Format);
        _isUpdatingFromCode = true;
        InputCore.SetValue(shown, false);
        _isUpdatingFromCode = false;

        UpdateVisual();
        FlashIfExternal(changed, notify);
    }

    private double _lastRaw = double.NaN;

    private void SetFromInput(double value) {
        if (IsDisposed || double.IsNaN(value)) {
            return;
        }
        _lastRaw = value;

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
                SetFillT(ToFillT(Normalize()));
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
            _fillTween = TweenFillT(ToFillT(Normalize()), 0.6f, O5Ease.OutExpo);
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
            _fillTween = TweenFillT(ToFillT(Normalize(value.Value)), 0.4f, O5Ease.OutExpo);
        }
    }

    // Damped-spring style shake: ~3.5 decaying oscillations, first swing to the left (~12px), settling exactly at 0.
    private const float ShakeTotalDuration = 0.55f;
    private const float ShakeAmplitude = 16f;
    private const float ShakeFrequency = 6.5f;
    private const float ShakeDecay = 3f;
    private const float ShakeBlendDuration = 0.06f;

    /// <summary>Shakes the slider horizontally to signal an invalid formula with a natural damped oscillation (left first). Re-triggering restarts smoothly from the current offset.</summary>
    public void PlayErrorShake() {
        if (IsDisposed || !Rect) {
            return;
        }

        _shakeTween?.Kill();
        float start = _shakeOffset;
        SubscribeShakeRender(true);
        // One linear 0..1 driver with the curve evaluated here, so no tween chaining or backend easing quirks are involved.
        _shakeTween = Ctx.Tween.TweenFloat(
            () => 0f,
            p => SetShakeOffset(EvaluateShake(start, p * ShakeTotalDuration)),
            1f, ShakeTotalDuration,
            () => {
                SetShakeOffset(0f);
                SubscribeShakeRender(false);
                _shakeTween = null;
            },
            O5Ease.Linear);
    }

    /// <summary>Cancels any active error shake and restores the slider's resting horizontal position.</summary>
    public void ResetShake() {
        _shakeTween?.Kill();
        _shakeTween = null;
        SetShakeOffset(0f);
        SubscribeShakeRender(false);
    }

    internal static float EvaluateShake(float start, float time) {
        if (time <= 0f) {
            return start;
        }

        float u = Mathf.Clamp01(time / ShakeTotalDuration);
        if (u >= 1f) {
            return 0f;
        }

        // Exponential decay times a linear fade guarantees the curve lands on exactly 0 at the end.
        float envelope = Mathf.Exp(-ShakeDecay * u) * (1f - u);
        float wave = -Mathf.Sin(2f * (float)Math.PI * ShakeFrequency * time);

        // Leftover offset from an interrupted shake eases out quickly instead of jumping.
        float blend = 1f - Mathf.Clamp01(time / ShakeBlendDuration);
        return ShakeAmplitude * envelope * wave + start * blend * blend;
    }

    private void SetShakeOffset(float offset) {
        _shakeOffset = offset;
        ApplyShakeOffset();
    }

    // Rebases on the rect's live x whenever something else (e.g. a layout rebuild) moved it since our last write,
    // so the shake never fights the layout and always settles on the current layout position.
    private void ApplyShakeOffset() {
        if (IsDisposed || !Rect) {
            _shakeAppliedX = float.NaN;
            _shakeAppliedOffset = 0f;
            return;
        }

        if (_shakeOffset == 0f && _shakeAppliedOffset == 0f) {
            _shakeAppliedX = float.NaN;
            return;
        }

        Vector2 pos = Rect.anchoredPosition;
        bool untouched = !float.IsNaN(_shakeAppliedX) && Mathf.Abs(pos.x - _shakeAppliedX) < 0.001f;
        float baseX = untouched ? pos.x - _shakeAppliedOffset : pos.x;
        pos.x = baseX + _shakeOffset;
        Rect.anchoredPosition = pos;
        _shakeAppliedOffset = _shakeOffset;
        _shakeAppliedX = _shakeOffset == 0f ? float.NaN : pos.x;
    }

    // Layout groups rebuild inside Canvas.willRenderCanvases; re-applying after them keeps the offset visible
    // even when the parent layout is rebuilt every frame.
    private void SubscribeShakeRender(bool on) {
#if !IL2CPP
        if (on == _shakeRenderHooked) {
            return;
        }

        _shakeRenderHook ??= ApplyShakeOffset;
        if (on) {
            Canvas.willRenderCanvases += _shakeRenderHook;
        } else {
            Canvas.willRenderCanvases -= _shakeRenderHook;
        }

        _shakeRenderHooked = on;
#endif
    }

    // The fill keeps full width and slides left (anchors t-1..t) under the
    // background-shaped clip, so tiny values never squash the 9-slice corners.
    // The changed-dot counter-slides to stay pinned to the background.
    private void SetFillT(float t) {
        FillRect.anchorMin = new Vector2(t - 1f, FillRect.anchorMin.y);
        FillRect.anchorMax = new Vector2(t, FillRect.anchorMax.y);
        RectTransform dot = ChangedUpImage.rectTransform;
        dot.anchorMin = new Vector2(1f - t, dot.anchorMin.y);
        dot.anchorMax = new Vector2(1f - t, dot.anchorMax.y);
    }

    private ITweenHandle TweenFillT(float targetT, float duration, O5Ease ease) {
        float startT = FillRect.anchorMax.x;
        return Ctx.Tween.TweenFloat(
            () => 0f,
            t => {
                if (FillRect && ChangedUpImage) {
                    SetFillT(Mathf.LerpUnclamped(startT, targetT, t));
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
        ResetShake();
        InputCore.Dispose();
        base.Dispose();
    }

    // Unity 6 TMP_InputField sets textWrappingMode = PreserveWhitespaceNoWrap, which counts
    // the trailing U+200B caret sentinel as visible width (~14px in monospace fonts), whereas
    // TMP 3.0.x (Unity 2020-2022) treats U+200B as 0px. Use NoWrap so ValueInput's explicit
    // -14px right padding is consistent across all Unity versions and fonts.
    private static bool _wrapModePropResolved;
    private static System.Reflection.PropertyInfo? _wrapModeProp;
    private static object? _wrapModeNoWrap;

    private static void DisableTrailingZwspAdvance(TMPro.TMP_Text? text) {
        if (text == null) {
            return;
        }
        if (!_wrapModePropResolved) {
            _wrapModePropResolved = true;
            _wrapModeProp = typeof(TMPro.TMP_Text).GetProperty("textWrappingMode");
            if (_wrapModeProp != null && _wrapModeProp.CanWrite && _wrapModeProp.PropertyType.IsEnum) {
                try {
                    _wrapModeNoWrap = Enum.Parse(_wrapModeProp.PropertyType, "NoWrap");
                } catch {
                    _wrapModeNoWrap = Enum.ToObject(_wrapModeProp.PropertyType, 0);
                }
            }
        }
        if (_wrapModeProp != null && _wrapModeNoWrap != null) {
            try {
                _wrapModeProp.SetValue(text, _wrapModeNoWrap);
            } catch {
            }
        }
    }
}
