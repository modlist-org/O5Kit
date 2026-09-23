// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using O5Kit.Behaviour;
using O5Kit.Core;
using O5Kit.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace O5Kit.Control;

/// <summary>HSV wheel + RGB/HSV channel sliders + hex field, in an auto-placed popup.</summary>
public sealed class O5ColorPicker : O5Object {
    private const int TextureSize = 256;
    private const float PopupWidth = 360f;
    private const float PopupHeight = 578f;
    private const float PopupScale = 0.9f;
    private const float RingInner = 0.37f;
    private const float RingOuter = 0.49f;
    private const float TriangleRadius = 0.32f;

    /// <summary>Value to reset to on middle-click.</summary>
    public Color DefaultValue { get; }

    /// <summary>Current color.</summary>
    public Color Value { get; private set; }

    /// <summary>Whether the popup is open.</summary>
    public bool Expanded { get; private set; }

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
    private readonly TMPro.TextMeshProUGUI _rgbModeLabel;
    private readonly TMPro.TextMeshProUGUI _hsvModeLabel;
    private readonly Action<Color>? _onChanged;
    private readonly Action<Color>? _onComplete;
    private readonly Texture2D _texture;
    private readonly Sprite _textureSprite;

    private float _hue;
    private float _saturation;
    private float _brightness;
    private float _renderedHue = -1f;
    private bool _suppressHex;
    private Color? _pendingHexColor;
    private bool _hsvMode;
    private DragTarget _dragTarget;
    private ITweenHandle? _popupTween, _popupFadeTween, _validationTween;

    private enum DragTarget { None, Hue, Triangle }

    /// <summary>Creates a color picker over factory-built visuals.</summary>
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
    /// <param name="defaultValue">Reset target.</param>
    /// <param name="value">Initial color.</param>
    /// <param name="onChanged">Change callback.</param>
    /// <param name="onComplete">Commit callback.</param>
    public O5ColorPicker(
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
        Color defaultValue,
        Color value,
        Action<Color>? onChanged,
        Action<Color>? onComplete
    ) : base(id, rect) {
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
        _hexInput = hexInput;
        _hexOutline = sharedOutline;
        _onChanged = onChanged;
        _onComplete = onComplete;
        DefaultValue = defaultValue;

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

        Set(value, false);
        SetMode(false);
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
            _popupBlocker.transform.SetAsLastSibling();
            _popup.transform.SetAsLastSibling();
            _popupRect.localScale = new Vector3(PopupScale * 0.96f, PopupScale * 0.96f, 1f);
            _popupCanvas.alpha = 0f;
            _popupCanvas.interactable = true;
            _popupCanvas.blocksRaycasts = true;
            _body.SetActive(true);
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_bodyRect);
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

        _popupTween = O5Boot.Tween.TweenFloat(
            () => 0f,
            t => {
                if (popupRect) {
                    popupRect.localScale = Vector3.LerpUnclamped(fromScale, toScale, t);
                }
            },
            1f, 0.2f, onDone, O5Ease.OutBack);
        _popupFadeTween = O5Boot.Tween.TweenFloat(
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

    /// <summary>Restores <see cref="DefaultValue"/> and commits.</summary>
    public void Reset() {
        Set(DefaultValue);
        _onComplete?.Invoke(Value);
    }

    /// <summary>Sets the color (clamped to 0..1), optionally invoking the change callback.</summary>
    /// <param name="value">New color.</param>
    /// <param name="invoke">Fire the change callback.</param>
    public void Set(Color value, bool invoke = true) {
        if (IsDisposed) {
            return;
        }

        value.r = Mathf.Clamp01(value.r);
        value.g = Mathf.Clamp01(value.g);
        value.b = Mathf.Clamp01(value.b);
        value.a = Mathf.Clamp01(value.a);
        Value = value;
        Color.RGBToHSV(value, out _hue, out _saturation, out _brightness);
        UpdateVisuals();
        if (invoke) {
            _onChanged?.Invoke(Value);
        }
    }

    /// <summary>Switches channel sliders between RGB and HSV.</summary>
    /// <param name="useHsv">True for HSV + alpha, false for RGB + alpha.</param>
    public void SetMode(bool useHsv) {
        if (IsDisposed) {
            return;
        }

        var theme = O5Boot.Theme;
        _hsvMode = useHsv;
        _rgbModeBackground.color = useHsv ? Color.clear : theme.ObjectActive;
        _hsvModeBackground.color = useHsv ? theme.ObjectActive : Color.clear;
        _rgbModeLabel.color = useHsv ? new Color(1f, 1f, 1f, 0.55f) : Color.white;
        _hsvModeLabel.color = useHsv ? Color.white : new Color(1f, 1f, 1f, 0.55f);

        string[] labels = useHsv ? ["H", "S", "V", "A"] : ["R", "G", "B", "A"];
        Color.RGBToHSV(DefaultValue, out float defaultHue, out float defaultSaturation, out float defaultBrightness);
        float[] defaults = useHsv
            ? [defaultHue, defaultSaturation, defaultBrightness, DefaultValue.a]
            : [DefaultValue.r, DefaultValue.g, DefaultValue.b, DefaultValue.a];
        Color[] colors = useHsv
            ? [
                Color.HSVToRGB(_hue, 1f, 1f),
                new Color(0.38f, 0.78f, 1f, 1f),
                new Color(1f, 0.82f, 0.35f, 1f),
                new Color(0.45f, 0.45f, 0.45f, 1f)
            ]
            : [
                new Color(1f, 0.42f, 0.44f, 1f),
                new Color(0.48f, 0.82f, 0.48f, 1f),
                new Color(0.56f, 0.56f, 0.9f, 1f),
                new Color(0.45f, 0.45f, 0.45f, 1f)
            ];
        for (int i = 0; i < _sliders.Length; i++) {
            _sliders[i].Label.text = labels[i];
            _sliders[i].FillImage.color = colors[i];
            _sliders[i].SetDefaultValue(defaults[i], true);
        }

        UpdateSliderValues();
    }

    /// <summary>Writes one channel (0..3) in the active color mode.</summary>
    /// <param name="channel">Channel index.</param>
    /// <param name="value">Normalized channel value.</param>
    public void SetChannel(int channel, float value) {
        value = Mathf.Clamp01(value);
        if (!_hsvMode) {
            Color color = Value;
            color[channel] = value;
            Set(color);
            return;
        }

        switch (channel) {
            case 0:
                _hue = value;
                break;
            case 1:
                _saturation = value;
                break;
            case 2:
                _brightness = value;
                break;
            case 3:
                Value = new Color(Value.r, Value.g, Value.b, value);
                UpdateVisuals();
                _onChanged?.Invoke(Value);
                return;
        }

        Color rgb = Color.HSVToRGB(_hue, _saturation, _brightness);
        rgb.a = Value.a;
        Value = rgb;
        UpdateVisuals();
        _onChanged?.Invoke(Value);
    }

    /// <summary>Starts a wheel drag. Picks the hue ring or the triangle by hit position.</summary>
    /// <param name="data">Pointer event.</param>
    public void BeginPointer(BaseEventData data) {
        if (!TryGetPointerPosition(data, out Vector2 normalized)) {
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
        _onComplete?.Invoke(Value);
    }

    private bool TryGetPointerPosition(BaseEventData data, out Vector2 normalized) {
        PointerEventData? pointer =
#if IL2CPP
            data.TryCast<PointerEventData>();
#else
            data as PointerEventData;
#endif
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
        if (_dragTarget == DragTarget.Hue) {
            _hue = Mathf.Repeat(Mathf.Atan2(normalized.y, normalized.x) / (Mathf.PI * 2f), 1f);
            Color rgb = Color.HSVToRGB(_hue, _saturation, _brightness);
            rgb.a = Value.a;
            Value = rgb;
            UpdateVisuals();
            _onChanged?.Invoke(Value);
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
        UpdateVisuals();
        _onChanged?.Invoke(Value);
    }

    /// <summary>Live-validates hex text, tinting the preview and outline by state.</summary>
    /// <param name="text">Hex text with or without <c>#</c>.</param>
    public void ValidateHex(string text) {
        if (_suppressHex) {
            return;
        }

        var theme = O5Boot.Theme;
        string candidate = text.StartsWith("#") ? text : "#" + text;
        bool validLength = candidate.Length is 7 or 9;
        Color parsed = default;
        bool valid = validLength && ColorUtility.TryParseHtmlString(candidate, out parsed);
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
            _onComplete?.Invoke(Value);
        } else {
            SetHexText();
        }

        _pendingHexColor = null;
        SetHexValidationColor(O5Boot.Theme.ObjectActive, true);
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

        Color composite = Color.Lerp(O5Boot.Theme.PanelBG, background, background.a);
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

    private void UpdateSliderValues() {
        float[] values = _hsvMode
            ? [_hue, _saturation, _brightness, Value.a]
            : [Value.r, Value.g, Value.b, Value.a];
        for (int i = 0; i < _sliders.Length; i++) {
            _sliders[i].Set(values[i], false);
        }

        if (_hsvMode) {
            _sliders[0].FillImage.color = Color.HSVToRGB(_hue, 1f, 1f);
        }
    }

    private void SetHexText() {
        string value = ColorUtility.ToHtmlStringRGBA(Value);
        _suppressHex = true;
        _hexInput.Set(value, false);
        _suppressHex = false;
    }

    private void SetHexValidationColor(Color color, bool resetText = false) {
        if (_hexOutline) {
            _validationTween?.Kill();
            float alpha = 1f;
            if (resetText) {
                RectTransform? header = _hexOutline.rectTransform.parent as RectTransform;
                alpha = header && RectTransformUtility.RectangleContainsScreenPoint(
                    header, O5Input.MousePosition, _canvasCamera
                ) ? 1f : 0f;
            }

            var outline = _hexOutline;
            _validationTween = O5Boot.Tween.TweenColor(
                () => outline.color,
                v => {
                    if (outline) {
                        outline.color = v;
                    }
                },
                new Color(color.r, color.g, color.b, alpha), 0.2f);
        }

        _hexInput.InputField.textComponent.color = Color.white;
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
        _hueHandle.anchoredPosition = Direction(_hue) * (size * 0.43f);

        float hueWeight = _saturation * _brightness;
        float whiteWeight = (1f - _saturation) * _brightness;
        float blackWeight = 1f - _brightness;
        Vector2 position = Direction(_hue) * hueWeight;
        position += Direction(_hue + (1f / 3f)) * whiteWeight;
        position += Direction(_hue - (1f / 3f)) * blackWeight;
        _colorHandle.anchoredPosition = position * (size * TriangleRadius);
    }

    private void UpdateTexture() {
        if (_renderedHue >= 0f && Math.Abs(Mathf.DeltaAngle(_renderedHue * 360f, _hue * 360f)) < 0.5f) {
            return;
        }

        _renderedHue = _hue;
        Color hueColor = Color.HSVToRGB(_hue, 1f, 1f);
        Color32[] pixels = new Color32[TextureSize * TextureSize];
        Vector2 huePoint = Direction(_hue) * TriangleRadius;
        Vector2 whitePoint = Direction(_hue + (1f / 3f)) * TriangleRadius;
        Vector2 blackPoint = Direction(_hue - (1f / 3f)) * TriangleRadius;

        for (int y = 0; y < TextureSize; y++) {
            for (int x = 0; x < TextureSize; x++) {
                Vector2 point = new(
                    ((x + 0.5f) / TextureSize) - 0.5f,
                    ((y + 0.5f) / TextureSize) - 0.5f
                );
                float distance = point.magnitude;
                Color color = Color.clear;
                if (distance is >= RingInner and <= RingOuter) {
                    float angle = Mathf.Repeat(Mathf.Atan2(point.y, point.x) / (Mathf.PI * 2f), 1f);
                    color = Color.HSVToRGB(angle, 1f, 1f);
                } else if (Barycentric(point, huePoint, whitePoint, blackPoint, out Vector3 weights)) {
                    color = (hueColor * weights.x) + (Color.white * weights.y) + (Color.black * weights.z);
                    color.a = 1f;
                }

                pixels[(y * TextureSize) + x] = color;
            }
        }

        _texture.SetPixels32(pixels);
        _texture.Apply(false, false);
    }

    private static Vector2 Direction(float turns) {
        float radians = turns * Mathf.PI * 2f;
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
