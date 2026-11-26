// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using O5Kit.Behaviour;
using O5Kit.Core;
using O5Kit.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace O5Kit.Control;

/// <summary>Channel slider space for <see cref="O5ColorPicker"/>.</summary>
public enum O5ColorMode {
    Rgb,
    Hsv,
    Oklch,
}

/// <summary>HSV wheel + RGB/HSV/OKLCH channel sliders + hex field, in an auto-placed popup.</summary>
public sealed class O5ColorPicker : O5Object {
    private const int TextureSize = 256;
    private const int SrgbLutSize = 2048;
    private const float PopupWidth = 360f;
    private const float PopupHeight = 578f;
    private const float PopupScale = 0.9f;
    private const float RingInner = 0.37f;
    private const float RingOuter = 0.49f;
    private const float TriangleRadius = 0.32f;
    private const float OklchMaxChroma = 0.37f;
    private const float OklchPadX = 0.03f;
    private const float OklchHueBarYMin = 0.03f;
    private const float OklchHueBarYMax = 0.14f;
    private const float OklchChartYMin = 0.20f;
    private const float OklchChartYMax = 0.97f;
    private const float OklchChartCorner = 0.03f;
    private const float OklchHueCorner = 0.025f;

    private static readonly byte[] LinearToSrgbLut = BuildLinearToSrgbLut();
    private static readonly Color32[] OklchHueStripLut = BuildOklchHueStripLut();

    /// <summary>Value to reset to on middle-click. Null disables reset.</summary>
    public Color? DefaultValue { get; set; }

    /// <summary>Current color.</summary>
    public Color Value { get; private set; }

    /// <inheritdoc/>
    public override string? ShownValue => ColorUtility.ToHtmlStringRGBA(Value);

    /// <summary>Whether the popup is open.</summary>
    public bool Expanded { get; private set; }

    /// <summary>Fired on every change.</summary>
    public Action<Color>? OnChanged { get; set; }

    /// <summary>Fired on commit.</summary>
    public Action<Color>? OnComplete { get; set; }

    private readonly RectTransform _canvasRect;
    private readonly Camera? _canvasCamera;
    private readonly RectTransform _popupRect;
    private readonly GameObject _popup;
    private readonly GameObject _popupBlocker;
    private readonly CanvasGroup _popupCanvas;
    private readonly GameObject _body;
    private readonly RectTransform _bodyRect;
    private readonly CanvasGroup _bodyCanvas;
    private readonly Image _preview;
    private readonly TMPro.TextMeshProUGUI _previewLabel;
    private readonly RectTransform _wheelRect;
    private readonly RectTransform _hueHandle;
    private readonly RectTransform _colorHandle;
    private readonly O5InputField _hexInput;
    private readonly Image _hexOutline;
    private readonly O5Slider[] _sliders;
    private readonly Image _rgbModeBackground;
    private readonly Image _hsvModeBackground;
    private readonly Image? _oklchModeBackground;
    private readonly TMPro.TextMeshProUGUI _rgbModeLabel;
    private readonly TMPro.TextMeshProUGUI _hsvModeLabel;
    private readonly TMPro.TextMeshProUGUI? _oklchModeLabel;
    private readonly Texture2D _texture;
    private readonly Sprite _textureSprite;
    private readonly Color32[] _pixels = new Color32[TextureSize * TextureSize];
    private readonly float[] _maxChromaByX = new float[TextureSize];

    private float _hue;
    private float _saturation;
    private float _brightness;
    private float _oklchL;
    private float _oklchC;
    private float _oklchH;
    private float _renderedHue = -1f;
    private bool _renderedOklch;
    private bool _suppressHex;
    private Color? _pendingHexColor;
    private O5ColorMode _mode;
    private DragTarget _dragTarget;
    private ITweenHandle? _popupTween, _popupFadeTween, _validationTween;

    private enum DragTarget { None, Hue, Triangle }

    /// <summary>Creates a color picker over factory-built visuals.</summary>
    /// <param name="ctx">Owning kit context.</param>
    /// <param name="id">Stable identifier.</param>
    /// <param name="rect">Root rect.</param>
    /// <param name="canvasRect">Popup coordinate space (usually the canvas rect).</param>
    /// <param name="canvasCamera">Camera for hover checks. Null for overlay canvases.</param>
    /// <param name="body">Popup body object (reparented into the popup).</param>
    /// <param name="bodyCanvas">Popup body fade group.</param>
    /// <param name="preview">Preview swatch image.</param>
    /// <param name="previewLabel">Preview label.</param>
    /// <param name="wheelRect">Wheel interaction rect.</param>
    /// <param name="hueHandle">Hue ring handle.</param>
    /// <param name="colorHandle">Triangle picker handle.</param>
    /// <param name="hexInput">Hex text field.</param>
    /// <param name="sharedOutline">Header validation outline.</param>
    /// <param name="sliders">Four channel sliders (RGB or HSV + alpha).</param>
    /// <param name="rgbModeBackground">RGB tab background.</param>
    /// <param name="rgbModeLabel">RGB tab label.</param>
    /// <param name="hsvModeBackground">HSV tab background.</param>
    /// <param name="hsvModeLabel">HSV tab label.</param>
    /// <param name="options">Behaviour. Null uses defaults.</param>
    public O5ColorPicker(
        O5Context ctx,
        string id,
        RectTransform rect,
        RectTransform canvasRect,
        Camera? canvasCamera,
        GameObject body,
        CanvasGroup bodyCanvas,
        Image preview,
        TMPro.TextMeshProUGUI previewLabel,
        RectTransform wheelRect,
        RectTransform hueHandle,
        RectTransform colorHandle,
        O5InputField hexInput,
        Image sharedOutline,
        O5Slider[] sliders,
        Image rgbModeBackground,
        TMPro.TextMeshProUGUI rgbModeLabel,
        Image hsvModeBackground,
        TMPro.TextMeshProUGUI hsvModeLabel,
        O5ColorPickerOptions? options = null
    ) : this(
        ctx, id, rect, canvasRect, canvasCamera, body, bodyCanvas, preview, previewLabel,
        wheelRect, hueHandle, colorHandle, hexInput, sharedOutline, sliders,
        rgbModeBackground, rgbModeLabel, hsvModeBackground, hsvModeLabel, null, null, options
    ) { }

    /// <summary>Creates a color picker over factory-built visuals with RGB, HSV, and OKLCH mode tabs.</summary>
    public O5ColorPicker(
        O5Context ctx,
        string id,
        RectTransform rect,
        RectTransform canvasRect,
        Camera? canvasCamera,
        GameObject body,
        CanvasGroup bodyCanvas,
        Image preview,
        TMPro.TextMeshProUGUI previewLabel,
        RectTransform wheelRect,
        RectTransform hueHandle,
        RectTransform colorHandle,
        O5InputField hexInput,
        Image sharedOutline,
        O5Slider[] sliders,
        Image rgbModeBackground,
        TMPro.TextMeshProUGUI rgbModeLabel,
        Image hsvModeBackground,
        TMPro.TextMeshProUGUI hsvModeLabel,
        Image? oklchModeBackground,
        TMPro.TextMeshProUGUI? oklchModeLabel,
        O5ColorPickerOptions? options = null
    ) : base(ctx, id, rect) {
        options ??= new O5ColorPickerOptions();
        _canvasRect = canvasRect;
        _canvasCamera = canvasCamera;
        _body = body;
        _bodyCanvas = bodyCanvas;
        _preview = preview;
        _previewLabel = previewLabel;
        _wheelRect = wheelRect;
        _hueHandle = hueHandle;
        _colorHandle = colorHandle;
        _sliders = sliders;
        _rgbModeBackground = rgbModeBackground;
        _rgbModeLabel = rgbModeLabel;
        _hsvModeBackground = hsvModeBackground;
        _hsvModeLabel = hsvModeLabel;
        _oklchModeBackground = oklchModeBackground;
        _oklchModeLabel = oklchModeLabel;
        _hexInput = hexInput;
        _hexOutline = sharedOutline;
        // The picker's own fields follow the picker; only the picker itself shows outside changes.
        foreach (var slider in sliders) {
            slider.FlashOnExternalChange = false;
        }
        if (hexInput != null) {
            hexInput.FlashOnExternalChange = false;
        }
        OnChanged = options.OnChanged;
        OnComplete = options.OnComplete;
        DefaultValue = options.DefaultValue;

        _popup = new GameObject("ColorPickerPopup");
        _popup.transform.SetParent(canvasRect, false);
        _popupRect = _popup.AddComponent<RectTransform>();
        _popupRect.anchorMin = new Vector2(0.5f, 0.5f);
        _popupRect.anchorMax = new Vector2(0.5f, 0.5f);
        _popupRect.pivot = new Vector2(0f, 1f);
        _popupRect.sizeDelta = new Vector2(PopupWidth, PopupHeight);
        _popupCanvas = _popup.AddComponent<CanvasGroup>();

        RectTransform bodyTransform = body.GetComponent<RectTransform>();
        bodyTransform.SetParent(_popupRect, false);
        _bodyRect = bodyTransform;
        _bodyRect.anchorMin = new Vector2(0f, 1f);
        _bodyRect.anchorMax = new Vector2(1f, 1f);
        _bodyRect.pivot = new Vector2(0.5f, 1f);
        _bodyRect.offsetMin = new Vector2(12f, -566f);
        _bodyRect.offsetMax = new Vector2(-12f, -12f);

        _popupBlocker = CreatePopupBlocker(canvasRect);
        _popupBlocker.SetActive(false);
        var blockerEvents = _popupBlocker.AddComponent<OventHandler>();
        blockerEvents.OnClick += button => {
            if (button == PointerEventData.InputButton.Left) {
                SetExpanded(false);
            }
        };

        _texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false) {
            name = $"ColorPicker_{id}",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        _textureSprite = Sprite.Create(_texture, new Rect(0f, 0f, TextureSize, TextureSize), new Vector2(0.5f, 0.5f), 100f);
        wheelRect.GetComponent<Image>().sprite = _textureSprite;

        Set(options.Value, false);
        SetMode(O5ColorMode.Rgb);
        SetExpanded(false, true);
        RegisterTick();
        OnDisposed += () => {
            _popup?.SetActive(false);
            if (_popup) {
                UnityEngine.Object.Destroy(_popup);
            }

            if (_popupBlocker) {
                UnityEngine.Object.Destroy(_popupBlocker);
            }
        };
    }

    /// <summary>Flips the popup open state.</summary>
    public void ToggleExpanded() => SetExpanded(!Expanded);

    /// <summary>Builds a color picker under <paramref name="parent"/>. Owns all GameObjects; the caller only passes behaviour.</summary>
    /// <param name="ctx">Owning kit context.</param>
    /// <param name="parent">Parent transform.</param>
    /// <param name="id">Stable identifier.</param>
    /// <param name="options">Behaviour. <see cref="O5ColorPickerOptions.CanvasRect"/> is required.</param>
    public static O5ColorPicker Create(O5Context ctx, Transform parent, string id, O5ColorPickerOptions? options = null)
        => Factory.O5Factory.ColorPicker(ctx, parent, id, options);

    /// <summary>Builds a color picker under <paramref name="parent"/> with inline configuration.</summary>
    /// <param name="ctx">Owning kit context.</param>
    /// <param name="parent">Parent transform.</param>
    /// <param name="id">Stable identifier.</param>
    /// <param name="configure">Mutates a fresh <see cref="O5ColorPickerOptions"/>.</param>
    public static O5ColorPicker Create(O5Context ctx, Transform parent, string id, Action<O5ColorPickerOptions> configure) {
        var options = new O5ColorPickerOptions();
        configure?.Invoke(options);
        return Create(ctx, parent, id, options);
    }

    /// <inheritdoc/>
    public override void Tick() {
        if (!IsDisposed && Expanded) {
            PositionPopup();
        }
    }

    /// <summary>Opens or closes the popup.</summary>
    /// <param name="expanded">True to open.</param>
    /// <param name="noAnimate">Snap instead of animating.</param>
    public void SetExpanded(bool expanded, bool noAnimate = false) {
        if (IsDisposed) {
            return;
        }

        _popupTween?.Kill();
        Expanded = expanded;
        if (expanded) {
            PositionPopup();
            _popup.SetActive(true);
            _popupBlocker.SetActive(true);
            _popupBlocker.transform.SetSiblingIndex(_popupBlocker.transform.parent.childCount - 1);
            _popup.transform.SetSiblingIndex(_popup.transform.parent.childCount - 1);
            _popupRect.localScale = new Vector3(PopupScale * 0.96f, PopupScale * 0.96f, 1f);
            _popupCanvas.alpha = 0f;
            _popupCanvas.interactable = true;
            _popupCanvas.blocksRaycasts = true;
            _body.SetActive(true);
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_bodyRect);
            UpdateTexture();
            UpdateHandles();
            _bodyCanvas.alpha = 1f;
            if (noAnimate) {
                _popupRect.localScale = new Vector3(PopupScale, PopupScale, 1f);
                _popupCanvas.alpha = 1f;
                return;
            }

            PlayPopupAnimation(true);
        } else {
            _bodyCanvas.interactable = false;
            _bodyCanvas.blocksRaycasts = false;
            _popupCanvas.interactable = false;
            _popupCanvas.blocksRaycasts = false;
            if (noAnimate) {
                _popup.SetActive(false);
                _popupBlocker.SetActive(false);
                _body.SetActive(false);
                _bodyCanvas.alpha = 0f;
                _popupCanvas.alpha = 0f;
                return;
            }

            var popup = _popup;
            var blocker = _popupBlocker;
            var body = _body;
            var bodyCanvas = _bodyCanvas;
            var canvas = _popupCanvas;
            PlayPopupAnimation(false, () => {
                if (Expanded) {
                    return;
                }

                popup.SetActive(false);
                blocker.SetActive(false);
                body.SetActive(false);
                bodyCanvas.alpha = 0f;
                canvas.alpha = 0f;
            });
        }

        _bodyCanvas.interactable = expanded;
        _bodyCanvas.blocksRaycasts = expanded;
    }

    private void PlayPopupAnimation(bool opening, Action? onDone = null) {
        _popupTween?.Kill();
        _popupFadeTween?.Kill();

        var popupRect = _popupRect;
        var popupCanvas = _popupCanvas;
        var fromScale = popupRect.localScale;
        var toScale = opening
            ? new Vector3(PopupScale, PopupScale, 1f)
            : new Vector3(PopupScale * 0.96f, PopupScale * 0.96f, 1f);
        float fromAlpha = popupCanvas.alpha;
        float toAlpha = opening ? 1f : 0f;

        _popupTween = Ctx.Tween.TweenFloat(
            () => 0f,
            t => {
                if (popupRect) {
                    popupRect.localScale = Vector3.LerpUnclamped(fromScale, toScale, t);
                }
            },
            1f, 0.2f, onDone, O5Ease.OutBack);
        _popupFadeTween = Ctx.Tween.TweenFloat(
            () => 0f,
            t => {
                if (popupCanvas) {
                    popupCanvas.alpha = Mathf.LerpUnclamped(fromAlpha, toAlpha, t);
                }
            },
            1f, 0.16f);
    }

    private static GameObject CreatePopupBlocker(RectTransform parent) {
        GameObject blockerObject = new("ColorPickerPopupBlocker");
        blockerObject.transform.SetParent(parent, false);
        RectTransform blocker = blockerObject.AddComponent<RectTransform>();
        blocker.anchorMin = Vector2.zero;
        blocker.anchorMax = Vector2.one;
        blocker.offsetMin = Vector2.zero;
        blocker.offsetMax = Vector2.zero;
        Image image = blockerObject.AddComponent<Image>();
        image.color = Color.clear;
        return blockerObject;
    }

    private void PositionPopup() {
        if (!_canvasRect || !Rect) {
            return;
        }

        Vector3 bottomWorld = Rect.TransformPoint(new Vector3(Rect.rect.xMin, Rect.rect.yMin, 0f));
        Vector2 bottomScreen = RectTransformUtility.WorldToScreenPoint(null, bottomWorld);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, bottomScreen, null, out Vector2 bottomPosition);

        float popupWidth = PopupWidth * PopupScale;
        float popupHeight = PopupHeight * PopupScale;
        float minX = _canvasRect.rect.xMin + 8f;
        float maxX = _canvasRect.rect.xMax - popupWidth - 8f;
        bottomPosition.x = maxX >= minX
            ? Mathf.Clamp(bottomPosition.x, minX, maxX)
            : minX;

        float minY = _canvasRect.rect.yMin + popupHeight + 8f;
        float maxY = _canvasRect.rect.yMax - 8f;
        bottomPosition.y = maxY >= minY
            ? Mathf.Clamp(bottomPosition.y, minY, maxY)
            : maxY;

        _popupRect.pivot = new Vector2(0f, 1f);
        _popupRect.anchoredPosition = bottomPosition;
    }

    /// <summary>Restores <see cref="DefaultValue"/> and commits. No-op when null.</summary>
    /// <param name="notify">Fire the change callback.</param>
    public void Reset(bool notify = true) {
        if (!DefaultValue.HasValue) {
            return;
        }
        Set(DefaultValue.Value, notify);
        if (notify) {
            OnComplete?.Invoke(Value);
        }
    }

    /// <summary>Sets the color (clamped to 0..1).</summary>
    /// <param name="value">New color.</param>
    /// <param name="notify">Fire the change callback.</param>
    public void Set(Color value, bool notify = true) {
        if (IsDisposed) {
            return;
        }

        value.r = Mathf.Clamp01(value.r);
        value.g = Mathf.Clamp01(value.g);
        value.b = Mathf.Clamp01(value.b);
        value.a = Mathf.Clamp01(value.a);
        Color32 prev = Value;
        Color32 next = value;
        bool changed = prev.r != next.r || prev.g != next.g || prev.b != next.b || prev.a != next.a;
        Value = value;
        Color.RGBToHSV(value, out _hue, out _saturation, out _brightness);
        SyncOklchFromValue();
        UpdateVisuals();
        if (notify) {
            OnChanged?.Invoke(Value);
        }
        FlashIfExternal(changed, notify);
    }

    /// <summary>Switches channel sliders between RGB and HSV.</summary>
    /// <param name="useHsv">True for HSV + alpha, false for RGB + alpha.</param>
    public void SetMode(bool useHsv) => SetMode(useHsv ? O5ColorMode.Hsv : O5ColorMode.Rgb);

    /// <summary>Switches channel sliders between RGB, HSV, and OKLCH.</summary>
    public void SetMode(O5ColorMode mode) {
        if (IsDisposed) {
            return;
        }

        var theme = Ctx.Theme;
        _mode = mode;
        ApplyModeTabColors(theme);

        string[] labels;
        double[] mins;
        double[] maxs;
        string[] formats;
        float[] defaults;
        Color[] colors = GetModeSliderColors(theme);

        Color baseColor = DefaultValue ?? Value;
        switch (mode) {
            case O5ColorMode.Hsv: {
                labels = ["H", "S", "V", "A"];
                mins = [0d, 0d, 0d, 0d];
                maxs = [1d, 1d, 1d, 1d];
                formats = ["F2", "F2", "F2", "F2"];
                Color.RGBToHSV(baseColor, out float defaultHue, out float defaultSaturation, out float defaultBrightness);
                defaults = [defaultHue, defaultSaturation, defaultBrightness, baseColor.a];
                break;
            }
            case O5ColorMode.Oklch: {
                labels = ["L", "C", "H", "A"];
                mins = [0d, 0d, 0d, 0d];
                maxs = [1d, OklchMaxChroma, 360d, 1d];
                formats = ["F2", "F3", "F0", "F2"];
                O5Palette.RgbToOklch(baseColor.r, baseColor.g, baseColor.b, out float defL, out float defC, out float defH);
                defaults = [defL, Math.Min(defC, OklchMaxChroma), defH * 360f, baseColor.a];
                break;
            }
            default: {
                labels = ["R", "G", "B", "A"];
                mins = [0d, 0d, 0d, 0d];
                maxs = [1d, 1d, 1d, 1d];
                formats = ["F2", "F2", "F2", "F2"];
                defaults = [baseColor.r, baseColor.g, baseColor.b, baseColor.a];
                break;
            }
        }

        for (int i = 0; i < _sliders.Length; i++) {
            _sliders[i].Label.text = labels[i];
            _sliders[i].Min = mins[i];
            _sliders[i].Max = maxs[i];
            _sliders[i].Format = formats[i];
            _sliders[i].AccentColor = colors[i];
            _sliders[i].SetDefaultValue(DefaultValue.HasValue ? defaults[i] : null, true);
        }

        UpdateSliderValues();
        UpdateTexture();
        UpdateHandles();
    }

    /// <summary>Writes one channel (0..3) in the active color mode.</summary>
    /// <param name="channel">Channel index.</param>
    /// <param name="value">Channel value in the active mode's range.</param>
    public void SetChannel(int channel, double value) {
        if (_mode == O5ColorMode.Rgb) {
            double clamped = MathCompat.Clamp(value, 0d, 1d);
            Color color = Value;
            switch (channel) {
                case 0:
                    color.r = (float)clamped;
                    break;
                case 1:
                    color.g = (float)clamped;
                    break;
                case 2:
                    color.b = (float)clamped;
                    break;
                default:
                    color.a = (float)clamped;
                    break;
            }
            Set(color);
            return;
        }

        if (_mode == O5ColorMode.Hsv) {
            double clamped = MathCompat.Clamp(value, 0d, 1d);
            switch (channel) {
                case 0:
                    _hue = (float)clamped;
                    break;
                case 1:
                    _saturation = (float)clamped;
                    break;
                case 2:
                    _brightness = (float)clamped;
                    break;
                case 3:
                    Value = new Color(Value.r, Value.g, Value.b, (float)clamped);
                    UpdateVisuals();
                    OnChanged?.Invoke(Value);
                    return;
            }

            Color rgb = Color.HSVToRGB(_hue, _saturation, _brightness);
            rgb.a = Value.a;
            Value = rgb;
            SyncOklchFromValue();
            UpdateVisuals();
            OnChanged?.Invoke(Value);
            return;
        }

        switch (channel) {
            case 0:
                _oklchL = (float)MathCompat.Clamp(value, 0d, 1d);
                break;
            case 1:
                _oklchC = (float)MathCompat.Clamp(value, 0d, OklchMaxChroma);
                break;
            case 2:
                _oklchH = (float)MathCompat.Clamp(value / 360d, 0d, 1d);
                break;
            case 3:
                Value = new Color(Value.r, Value.g, Value.b, (float)MathCompat.Clamp(value, 0d, 1d));
                UpdateVisuals();
                OnChanged?.Invoke(Value);
                return;
        }

        Color oklchRgb = O5Palette.OklchToRgb(_oklchL, _oklchC, _oklchH, Value.a);
        Value = oklchRgb;
        Color.RGBToHSV(oklchRgb, out _hue, out _saturation, out _brightness);
        UpdateVisuals();
        OnChanged?.Invoke(Value);
    }

    /// <summary>Starts a wheel drag. Picks the hue ring or the triangle by hit position.</summary>
    /// <param name="data">Pointer event.</param>
    public void BeginPointer(BaseEventData data) {
        if (!TryGetPointerPosition(data, out Vector2 normalized)) {
            return;
        }

        if (_mode == O5ColorMode.Oklch) {
            float u = normalized.x + 0.5f;
            float v = normalized.y + 0.5f;
            if (u < -0.02f || u > 1.02f || v < -0.02f || v > 1.02f) {
                return;
            }

            _dragTarget = v <= (OklchHueBarYMax + OklchChartYMin) * 0.5f
                ? DragTarget.Hue
                : DragTarget.Triangle;
            ApplyPointer(normalized);
            return;
        }

        float distance = normalized.magnitude;
        if (distance > 0.56f) {
            return;
        }

        _dragTarget = distance >= RingInner - 0.03f ? DragTarget.Hue : DragTarget.Triangle;
        ApplyPointer(normalized);
    }

    /// <summary>Continues the active wheel drag.</summary>
    /// <param name="data">Pointer event.</param>
    public void DragPointer(BaseEventData data) {
        if (_dragTarget == DragTarget.None || !TryGetPointerPosition(data, out Vector2 normalized)) {
            return;
        }

        ApplyPointer(normalized);
    }

    /// <summary>Ends the wheel drag and commits the color.</summary>
    /// <param name="data">Pointer event.</param>
    public void EndPointer(BaseEventData data) {
        if (_dragTarget == DragTarget.None) {
            return;
        }

        _dragTarget = DragTarget.None;
        OnComplete?.Invoke(Value);
    }

    private bool TryGetPointerPosition(BaseEventData data, out Vector2 normalized) {
        PointerEventData? pointer = UnityUtils.AsPointerEventData(data);
        normalized = default;
        if (pointer == null || pointer.button != PointerEventData.InputButton.Left) {
            return false;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _wheelRect, pointer.position, pointer.pressEventCamera, out Vector2 local
        )) {
            return false;
        }

        float radius = Math.Min(_wheelRect.rect.width, _wheelRect.rect.height);
        if (radius <= 0f) {
            return false;
        }

        normalized = local / radius;
        return true;
    }

    private void ApplyPointer(Vector2 normalized) {
        if (_mode == O5ColorMode.Oklch) {
            float u = normalized.x + 0.5f;
            float v = normalized.y + 0.5f;
            float tX = Mathf.Clamp01((u - OklchPadX) / (1f - (OklchPadX * 2f)));
            if (_dragTarget == DragTarget.Hue) {
                _oklchH = tX;
            } else {
                _oklchL = tX;
                float rawC = Mathf.Clamp01((v - OklchChartYMin) / (OklchChartYMax - OklchChartYMin)) * OklchMaxChroma;
                _oklchC = Math.Min(rawC, O5Palette.MaxSrgbChroma(_oklchL, _oklchH));
            }

            Color pickedOklch = O5Palette.OklchToRgb(_oklchL, _oklchC, _oklchH, Value.a);
            Value = pickedOklch;
            Color.RGBToHSV(pickedOklch, out _hue, out _saturation, out _brightness);
            UpdateVisuals();
            OnChanged?.Invoke(Value);
            return;
        }

        if (_dragTarget == DragTarget.Hue) {
            _hue = Mathf.Repeat(Mathf.Atan2(normalized.y, normalized.x) / ((float)Math.PI * 2f), 1f);
            Color rgb = Color.HSVToRGB(_hue, _saturation, _brightness);
            rgb.a = Value.a;
            Value = rgb;
            SyncOklchFromValue();
            UpdateVisuals();
            OnChanged?.Invoke(Value);
            return;
        }

        Vector2 huePoint = Direction(_hue) * TriangleRadius;
        Vector2 whitePoint = Direction(_hue + (1f / 3f)) * TriangleRadius;
        Vector2 blackPoint = Direction(_hue - (1f / 3f)) * TriangleRadius;
        Vector2 point = ClosestPointOnTriangle(normalized, huePoint, whitePoint, blackPoint);
        if (!Barycentric(point, huePoint, whitePoint, blackPoint, out Vector3 weights)) {
            return;
        }

        _saturation = weights.x / Math.Max(weights.x + weights.y, 0.0001f);
        _brightness = Mathf.Clamp01(weights.x + weights.y);
        Color picked = Color.HSVToRGB(_hue, _saturation, _brightness);
        picked.a = Value.a;
        Value = picked;
        SyncOklchFromValue();
        UpdateVisuals();
        OnChanged?.Invoke(Value);
    }

    /// <summary>Live-validates hex text, tinting the preview and outline by state.</summary>
    /// <param name="text">Hex text with or without <c>#</c>.</param>
    public void ValidateHex(string text) {
        if (_suppressHex) {
            return;
        }

        var theme = Ctx.Theme;
        string candidate = text.StartsWith("#") ? text : "#" + text;
        bool validLength = candidate.Length is 7 or 9;
        Color parsed = default;
        bool valid = validLength &&
#if IL2CPP
            O5Kit.Compat.O5ColorCompat.TryParseHtmlString(candidate, out parsed);
#else
            ColorUtility.TryParseHtmlString(candidate, out parsed);
#endif
        _pendingHexColor = valid ? parsed : null;
        _preview.color = valid ? parsed : Value;
        UpdateLabelColor(_preview.color);

        Color stateColor = valid
            ? theme.MathOk
            : IsPartialHex(text)
                ? theme.MathWarn
                : theme.MathErr;
        SetHexValidationColor(stateColor);
    }

    /// <summary>Commits valid hex text, or restores the field when invalid.</summary>
    /// <param name="text">Hex text with or without <c>#</c>.</param>
    public void CompleteHex(string text) {
        ValidateHex(text);
        if (_pendingHexColor.HasValue) {
            Set(_pendingHexColor.Value);
            OnComplete?.Invoke(Value);
        } else {
            SetHexText();
        }

        _pendingHexColor = null;
        SetHexValidationColor(Ctx.Theme.ObjectActive, true);
    }

    private void UpdateVisuals() {
        _preview.color = Value;
        UpdateLabelColor(Value);
        UpdateSliderValues();
        SetHexText();
        UpdateTexture();
        UpdateHandles();
    }

    private void UpdateLabelColor(Color background) {
        if (!_previewLabel) {
            return;
        }

        Color composite = Color.Lerp(Ctx.Theme.PanelBG, background, background.a);
        float luminance = RelativeLuminance(composite);
        float blackContrast = (luminance + 0.05f) / 0.05f;
        float whiteContrast = 1.05f / (luminance + 0.05f);
        _previewLabel.color = whiteContrast >= blackContrast ? Color.white : Color.black;
    }

    private static float RelativeLuminance(Color color) {
        static float Linearize(float channel) => channel <= 0.03928f
            ? channel / 12.92f
            : Mathf.Pow((channel + 0.055f) / 1.055f, 2.4f);

        return 0.2126f * Linearize(color.r)
            + 0.7152f * Linearize(color.g)
            + 0.0722f * Linearize(color.b);
    }

    private void SyncOklchFromValue() {
        O5Palette.RgbToOklch(Value.r, Value.g, Value.b, out _oklchL, out _oklchC, out float newH);
        if (_oklchC >= 1e-4f) {
            _oklchH = newH;
        }
    }

    private void ApplyModeTabColors(O5Theme theme) {
        if (_rgbModeBackground) {
            _rgbModeBackground.color = _mode == O5ColorMode.Rgb ? theme.ObjectActive : Color.clear;
        }
        if (_hsvModeBackground) {
            _hsvModeBackground.color = _mode == O5ColorMode.Hsv ? theme.ObjectActive : Color.clear;
        }
        if (_oklchModeBackground) {
            _oklchModeBackground.color = _mode == O5ColorMode.Oklch ? theme.ObjectActive : Color.clear;
        }
        if (_rgbModeLabel) {
            _rgbModeLabel.color = _mode == O5ColorMode.Rgb ? theme.Text : theme.TextDim;
        }
        if (_hsvModeLabel) {
            _hsvModeLabel.color = _mode == O5ColorMode.Hsv ? theme.Text : theme.TextDim;
        }
        if (_oklchModeLabel) {
            _oklchModeLabel.color = _mode == O5ColorMode.Oklch ? theme.Text : theme.TextDim;
        }
    }

    private Color[] GetModeSliderColors(O5Theme theme) {
        switch (_mode) {
            case O5ColorMode.Hsv:
                return [
                    Color.HSVToRGB(_hue, 1f, 1f),
                    theme.ChannelS,
                    theme.ChannelV,
                    theme.ChannelA
                ];
            case O5ColorMode.Oklch:
                Color huePreview = O5Palette.OklchToRgb(0.7f, 0.18f, _oklchH);
                return [
                    new Color(0.85f, 0.85f, 0.85f, 1f),
                    huePreview,
                    huePreview,
                    theme.ChannelA
                ];
            default:
                return [
                    theme.ChannelR,
                    theme.ChannelG,
                    theme.ChannelB,
                    theme.ChannelA
                ];
        }
    }

    private void UpdateSliderValues() {
        double[] values = _mode switch {
            O5ColorMode.Hsv => [_hue, _saturation, _brightness, Value.a],
            O5ColorMode.Oklch => [_oklchL, _oklchC, _oklchH * 360f, Value.a],
            _ => [Value.r, Value.g, Value.b, Value.a],
        };
        for (int i = 0; i < _sliders.Length; i++) {
            _sliders[i].Set(values[i], false);
        }

        if (_mode == O5ColorMode.Hsv) {
            _sliders[0].AccentColor = Color.HSVToRGB(_hue, 1f, 1f);
        } else if (_mode == O5ColorMode.Oklch) {
            Color huePreview = O5Palette.OklchToRgb(0.7f, 0.18f, _oklchH);
            _sliders[1].AccentColor = huePreview;
            _sliders[2].AccentColor = huePreview;
        }
    }

    private void SetHexText() {
#if IL2CPP
        string value = O5Kit.Compat.O5ColorCompat.ToHtmlStringRGBA(Value);
#else
        string value = ColorUtility.ToHtmlStringRGBA(Value);
#endif
        _suppressHex = true;
        _hexInput.Set(value, false);
        _suppressHex = false;
    }

    private void SetHexValidationColor(Color color, bool resetText = false) {
        if (_hexOutline) {
            _validationTween?.Kill();
            float alpha = 1f;
            if (resetText) {
                RectTransform? header = _hexOutline.rectTransform.parent.AsRectTransform();
                alpha = header && RectTransformUtility.RectangleContainsScreenPoint(
                    header, O5Input.MousePosition, _canvasCamera
                ) ? 1f : 0f;
            }

            var outline = _hexOutline;
            _validationTween = Ctx.Tween.TweenColor(
                () => outline.color,
                v => {
                    if (outline) {
                        outline.color = v;
                    }
                },
                O5Palette.WithAlpha(color, alpha), 0.2f);
        }

        _hexInput.InputField.textComponent.color = Ctx.Theme.Text;
    }

    private static bool IsPartialHex(string text) {
        if (string.IsNullOrEmpty(text)) {
            return true;
        }

        int start = text[0] == '#' ? 1 : 0;
        int length = text.Length - start;
        if (length > 8) {
            return false;
        }

        for (int i = start; i < text.Length; i++) {
            char c = text[i];
            bool isHex = c is (>= '0' and <= '9') or (>= 'a' and <= 'f') or (>= 'A' and <= 'F');
            if (!isHex) {
                return false;
            }
        }

        return true;
    }

    private void UpdateHandles() {
        float size = Math.Min(_wheelRect.rect.width, _wheelRect.rect.height);
        if (_mode == O5ColorMode.Oklch) {
            float spanX = 1f - (OklchPadX * 2f);
            float hueU = OklchPadX + (_oklchH * spanX);
            float hueV = (OklchHueBarYMin + OklchHueBarYMax) * 0.5f;
            _hueHandle.anchoredPosition = new Vector2((hueU - 0.5f) * size, (hueV - 0.5f) * size);

            float chartU = OklchPadX + (Mathf.Clamp01(_oklchL) * spanX);
            float chartV = Mathf.Lerp(OklchChartYMin, OklchChartYMax, Mathf.Clamp01(_oklchC / OklchMaxChroma));
            _colorHandle.anchoredPosition = new Vector2((chartU - 0.5f) * size, (chartV - 0.5f) * size);
            return;
        }

        _hueHandle.anchoredPosition = Direction(_hue) * (size * 0.43f);

        float hueWeight = _saturation * _brightness;
        float whiteWeight = (1f - _saturation) * _brightness;
        float blackWeight = 1f - _brightness;
        Vector2 position = Direction(_hue) * hueWeight;
        position += Direction(_hue + (1f / 3f)) * whiteWeight;
        position += Direction(_hue - (1f / 3f)) * blackWeight;
        _colorHandle.anchoredPosition = position * (size * TriangleRadius);
    }

    private static Color32[]? _hsvRingBase;

    private static Color32[] GetHsvRingBase() {
        if (_hsvRingBase != null) {
            return _hsvRingBase;
        }
        var ring = new Color32[TextureSize * TextureSize];
        for (int y = 0; y < TextureSize; y++) {
            float py = ((y + 0.5f) / TextureSize) - 0.5f;
            int row = y * TextureSize;
            for (int x = 0; x < TextureSize; x++) {
                float px = ((x + 0.5f) / TextureSize) - 0.5f;
                float distSq = (px * px) + (py * py);
                if (distSq >= RingInner * RingInner && distSq <= RingOuter * RingOuter) {
                    float angle = (float)(Math.Atan2(py, px) / (Math.PI * 2.0));
                    if (angle < 0f) angle += 1f;
                    ring[row + x] = HsvToColor32(angle, 1f, 1f);
                }
            }
        }
        _hsvRingBase = ring;
        return ring;
    }

    private static Color32 HsvToColor32(float h, float s, float v) {
        float h6 = (h - (float)Math.Floor(h)) * 6f;
        int sector = (int)h6;
        float f = h6 - sector;
        float p = v * (1f - s);
        float q = v * (1f - (s * f));
        float t = v * (1f - (s * (1f - f)));
        float r, g, b;
        switch (sector) {
            case 0: r = v; g = t; b = p; break;
            case 1: r = q; g = v; b = p; break;
            case 2: r = p; g = v; b = t; break;
            case 3: r = p; g = q; b = v; break;
            case 4: r = t; g = p; b = v; break;
            default: r = v; g = p; b = q; break;
        }
        return new Color32(
            (byte)Math.Round(Mathf.Clamp01(r) * 255f),
            (byte)Math.Round(Mathf.Clamp01(g) * 255f),
            (byte)Math.Round(Mathf.Clamp01(b) * 255f),
            255
        );
    }

    private void UpdateTexture() {
        if (!Expanded) {
            return;
        }
        bool isOklch = _mode == O5ColorMode.Oklch;
        float activeHue = isOklch ? _oklchH : _hue;
        float hueThreshold = isOklch ? 0.25f : 0.5f;
        if (_renderedHue >= 0f && _renderedOklch == isOklch && Math.Abs(Mathf.DeltaAngle(_renderedHue * 360f, activeHue * 360f)) < hueThreshold) {
            return;
        }

        _renderedHue = activeHue;
        _renderedOklch = isOklch;
        if (isOklch) {
            RenderOklchTexture();
        } else {
            RenderHsvTexture();
        }
    }

    private void RenderHsvTexture() {
        Color32[] ringBase = GetHsvRingBase();
        Array.Copy(ringBase, _pixels, _pixels.Length);

        Color32 hueC32 = HsvToColor32(_hue, 1f, 1f);
        float hr = hueC32.r;
        float hg = hueC32.g;
        float hb = hueC32.b;
        Vector2 huePoint = Direction(_hue) * TriangleRadius;
        Vector2 whitePoint = Direction(_hue + (1f / 3f)) * TriangleRadius;
        Vector2 blackPoint = Direction(_hue - (1f / 3f)) * TriangleRadius;

        int minIdx = Math.Max(0, (int)Math.Floor((0.5f - TriangleRadius) * TextureSize) - 1);
        int maxIdx = Math.Min(TextureSize - 1, (int)Math.Ceiling((0.5f + TriangleRadius) * TextureSize) + 1);

        for (int y = minIdx; y <= maxIdx; y++) {
            float py = ((y + 0.5f) / TextureSize) - 0.5f;
            int row = y * TextureSize;
            for (int x = minIdx; x <= maxIdx; x++) {
                float px = ((x + 0.5f) / TextureSize) - 0.5f;
                if (Barycentric(new Vector2(px, py), huePoint, whitePoint, blackPoint, out Vector3 weights)) {
                    float wx = weights.x;
                    float wy = weights.y * 255f;
                    _pixels[row + x] = new Color32(
                        (byte)Math.Min(255, Math.Max(0, (int)((hr * wx) + wy + 0.5f))),
                        (byte)Math.Min(255, Math.Max(0, (int)((hg * wx) + wy + 0.5f))),
                        (byte)Math.Min(255, Math.Max(0, (int)((hb * wx) + wy + 0.5f))),
                        255
                    );
                }
            }
        }

        _texture.SetPixels32(_pixels);
        _texture.Apply(false, false);
    }

    private void RenderOklchTexture() {
        Array.Clear(_pixels, 0, _pixels.Length);

        const float uMin = OklchPadX;
        const float uMax = 1f - OklchPadX;
        const float spanX = uMax - uMin;
        const float chartSpanY = OklchChartYMax - OklchChartYMin;
        const float pixelStep = 1f / TextureSize;
        const float aaMask = pixelStep * 1.25f;
        const float bgR = 20f;
        const float bgG = 20f;
        const float bgB = 26f;
        const float bgA = 165f;

        double rad = _oklchH * 2.0 * Math.PI;
        float cosH = (float)Math.Cos(rad);
        float sinH = (float)Math.Sin(rad);

        for (int x = 0; x < TextureSize; x++) {
            float u = (x + 0.5f) * pixelStep;
            float l = Mathf.Clamp01((u - uMin) / spanX);
            _maxChromaByX[x] = O5Palette.MaxSrgbChroma(l, cosH, sinH);
        }

        float aaC = (OklchMaxChroma / (chartSpanY * TextureSize)) * 1.5f;

        for (int y = 0; y < TextureSize; y++) {
            float v = (y + 0.5f) * pixelStep;
            bool inChartBand = v >= OklchChartYMin - pixelStep && v <= OklchChartYMax + pixelStep;
            bool inHueBand = !inChartBand && v >= OklchHueBarYMin - pixelStep && v <= OklchHueBarYMax + pixelStep;
            if (!inChartBand && !inHueBand) {
                continue;
            }

            int rowOffset = y * TextureSize;
            if (inHueBand) {
                for (int x = 0; x < TextureSize; x++) {
                    float u = (x + 0.5f) * pixelStep;
                    float mask = RoundedRectAlpha(u, v, uMin, uMax, OklchHueBarYMin, OklchHueBarYMax, OklchHueCorner, aaMask);
                    if (mask <= 0f) {
                        continue;
                    }
                    Color32 strip = OklchHueStripLut[x];
                    strip.a = (byte)Mathf.RoundToInt(255f * mask);
                    _pixels[rowOffset + x] = strip;
                }
                continue;
            }

            float rawC = Mathf.Clamp01((v - OklchChartYMin) / chartSpanY) * OklchMaxChroma;
            for (int x = 0; x < TextureSize; x++) {
                float u = (x + 0.5f) * pixelStep;
                float mask = RoundedRectAlpha(u, v, uMin, uMax, OklchChartYMin, OklchChartYMax, OklchChartCorner, aaMask);
                if (mask <= 0f) {
                    continue;
                }

                float maxC = _maxChromaByX[x];
                if (rawC > maxC + aaC) {
                    _pixels[rowOffset + x] = new Color32((byte)bgR, (byte)bgG, (byte)bgB, (byte)Mathf.RoundToInt(bgA * mask));
                    continue;
                }

                float l = Mathf.Clamp01((u - uMin) / spanX);
                float evalC = Math.Min(rawC, maxC);
                O5Palette.TryOklchToLinearRgb(l, evalC, cosH, sinH, out float lr, out float lg, out float lb);
                byte r = LinearToSrgbByte(lr);
                byte g = LinearToSrgbByte(lg);
                byte b = LinearToSrgbByte(lb);

                float edgeDist = Math.Abs(rawC - maxC);
                if (edgeDist < aaC) {
                    float edge = (1f - (edgeDist / aaC)) * 0.30f;
                    r = (byte)Mathf.RoundToInt(r + ((255f - r) * edge));
                    g = (byte)Mathf.RoundToInt(g + ((255f - g) * edge));
                    b = (byte)Mathf.RoundToInt(b + ((255f - b) * edge));
                }

                if (rawC <= maxC) {
                    _pixels[rowOffset + x] = new Color32(r, g, b, (byte)Mathf.RoundToInt(255f * mask));
                } else {
                    float cov = Mathf.Clamp01(1f - ((rawC - maxC) / aaC));
                    byte cr = (byte)Mathf.RoundToInt(bgR + ((r - bgR) * cov));
                    byte cg = (byte)Mathf.RoundToInt(bgG + ((g - bgG) * cov));
                    byte cb = (byte)Mathf.RoundToInt(bgB + ((b - bgB) * cov));
                    float ca = bgA + ((255f - bgA) * cov);
                    _pixels[rowOffset + x] = new Color32(cr, cg, cb, (byte)Mathf.RoundToInt(ca * mask));
                }
            }
        }

        _texture.SetPixels32(_pixels);
        _texture.Apply(false, false);
    }

    private static float RoundedRectAlpha(float u, float v, float xMin, float xMax, float yMin, float yMax, float radius, float aa) {
        float cx = (xMin + xMax) * 0.5f;
        float cy = (yMin + yMax) * 0.5f;
        float hx = ((xMax - xMin) * 0.5f) - radius;
        float hy = ((yMax - yMin) * 0.5f) - radius;
        float dx = Math.Max(Math.Abs(u - cx) - hx, 0f);
        float dy = Math.Max(Math.Abs(v - cy) - hy, 0f);
        float dist = (float)Math.Sqrt((dx * dx) + (dy * dy)) - radius;
        return Mathf.Clamp01(0.5f - (dist / aa));
    }

    private static byte LinearToSrgbByte(float c) => LinearToSrgbLut[(int)(Mathf.Clamp01(c) * SrgbLutSize + 0.5f)];

    private static byte[] BuildLinearToSrgbLut() {
        var lut = new byte[SrgbLutSize + 1];
        for (int i = 0; i <= SrgbLutSize; i++) {
            float c = (float)i / SrgbLutSize;
            float srgb = c <= 0.0031308f
                ? 12.92f * c
                : (float)((1.055 * Math.Pow(c, 1.0 / 2.4)) - 0.055);
            lut[i] = (byte)Mathf.RoundToInt(Mathf.Clamp01(srgb) * 255f);
        }
        return lut;
    }

    private static Color32[] BuildOklchHueStripLut() {
        var lut = new Color32[TextureSize];
        float spanX = 1f - (OklchPadX * 2f);
        for (int x = 0; x < TextureSize; x++) {
            float u = (x + 0.5f) / TextureSize;
            float h = Mathf.Clamp01((u - OklchPadX) / spanX);
            lut[x] = O5Palette.OklchToRgb(0.70f, 0.17f, h);
        }
        return lut;
    }

    private static Vector2 Direction(float turns) {
        float radians = turns * (float)Math.PI * 2f;
        return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
    }

    private static bool Barycentric(Vector2 point, Vector2 a, Vector2 b, Vector2 c, out Vector3 weights) {
        Vector2 v0 = b - a;
        Vector2 v1 = c - a;
        Vector2 v2 = point - a;
        float denominator = (v0.x * v1.y) - (v1.x * v0.y);
        if (Math.Abs(denominator) < 0.00001f) {
            weights = default;
            return false;
        }

        float y = ((v2.x * v1.y) - (v1.x * v2.y)) / denominator;
        float z = ((v0.x * v2.y) - (v2.x * v0.y)) / denominator;
        float x = 1f - y - z;
        weights = new Vector3(x, y, z);
        return x >= 0f && y >= 0f && z >= 0f;
    }

    private static Vector2 ClosestPointOnTriangle(Vector2 point, Vector2 a, Vector2 b, Vector2 c) {
        if (Barycentric(point, a, b, c, out _)) {
            return point;
        }

        Vector2 ab = ClosestPointOnSegment(point, a, b);
        Vector2 bc = ClosestPointOnSegment(point, b, c);
        Vector2 ca = ClosestPointOnSegment(point, c, a);
        float abDistance = (point - ab).sqrMagnitude;
        float bcDistance = (point - bc).sqrMagnitude;
        float caDistance = (point - ca).sqrMagnitude;
        if (abDistance <= bcDistance && abDistance <= caDistance) {
            return ab;
        }

        return bcDistance <= caDistance ? bc : ca;
    }

    private static Vector2 ClosestPointOnSegment(Vector2 point, Vector2 a, Vector2 b) {
        Vector2 segment = b - a;
        float length = segment.sqrMagnitude;
        if (length <= 0.00001f) {
            return a;
        }

        return a + (segment * Mathf.Clamp01(Vector2.Dot(point - a, segment) / length));
    }

    /// <inheritdoc/>
    protected override void OnThemeChanged(O5Theme prev, O5Theme next) {
        if (_body) {
            var bodyBg = _body.GetComponent<Image>();
            if (bodyBg) {
                bodyBg.color = next.PanelBG;
            }
        }
        ApplyModeTabColors(next);
        Color[] sliderColors = GetModeSliderColors(next);
        for (int i = 0; i < _sliders.Length; i++) {
            _sliders[i].AccentColor = sliderColors[i];
        }
        if (_hexInput != null && _hexInput.InputField && _hexInput.InputField.textComponent) {
            _hexInput.InputField.textComponent.color = next.Text;
        }
    }

    /// <inheritdoc/>
    public override void Dispose() {
        if (IsDisposed) {
            return;
        }

        _validationTween?.Kill();
        _popupTween?.Kill();
        _popupFadeTween?.Kill();
        _validationTween = _popupTween = _popupFadeTween = null;
        foreach (O5Slider slider in _sliders) {
            slider.Dispose();
        }

        _hexInput.Dispose();
        UnityEngine.Object.Destroy(_textureSprite);
        UnityEngine.Object.Destroy(_texture);
        base.Dispose();
    }
}
