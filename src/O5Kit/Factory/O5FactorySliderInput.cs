// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using O5Kit.Behaviour;
using O5Kit.Control;
using O5Kit.Core;
using O5Kit.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UnityEngine.EventSystems.PointerEventData;

namespace O5Kit.Factory;

public static partial class O5Factory {
    /// <summary>Builds a slider with fill bar, formula box and infinite horizontal drag.</summary>
    /// <param name="parent">Parent transform.</param>
    /// <param name="defaultValue">Reset target.</param>
    /// <param name="min">Range lower bound.</param>
    /// <param name="max">Range upper bound.</param>
    /// <param name="value">Initial value.</param>
    /// <param name="format">Display format, e.g. <c>"F2"</c>.</param>
    /// <param name="clampMode">Where clamping applies.</param>
    /// <param name="filter">Optional value transform.</param>
    /// <param name="onChanged">Change callback.</param>
    /// <param name="onComplete">Drag/edit commit callback.</param>
    /// <param name="text">Label text.</param>
    /// <param name="id">Stable identifier.</param>
    /// <param name="showFill">Show the fill bar.</param>
    /// <param name="dragStep">Value per pixel. Null scales by control width.</param>
    /// <param name="blockHoverWhileDragging">Cover the canvas with a drag blocker.</param>
    public static O5Slider Slider(
        Transform parent,
        float defaultValue,
        float min,
        float max,
        float value,
        string format,
        ClampMode clampMode,
        Func<float, float>? filter,
        Action<float>? onChanged,
        Action<float>? onComplete,
        string text,
        string id,
        bool showFill = true,
        float? dragStep = null,
        bool blockHoverWhileDragging = false
    ) {
        RectTransform rect = ControlBackground(parent);
        rect.SetParent(parent, false);

        GameObject change = AddSmallChangedCircle(rect);
        Image changeImg = change.GetComponent<Image>();

        GameObject fill = new("Fill");
        fill.transform.SetParent(rect, false);

        RectTransform fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.anchoredPosition = Vector2.zero;
        fillRect.sizeDelta = Vector2.zero;

        TMPro.TextMeshProUGUI label = ControlText(rect, O5Boot.Theme.FontSizeBody);
        label.text = text;
        label.alignment = TMPro.TextAlignmentOptions.Left;

        GameObject inputObj = new("ValueInput");
        inputObj.transform.SetParent(rect, false);
        inputObj.SetActive(false);

        CanvasGroup inputCanvasGroup = inputObj.AddComponent<CanvasGroup>();
        inputCanvasGroup.blocksRaycasts = false;
        RectTransform inputRect = inputObj.AddComponent<RectTransform>();
        inputRect.anchorMin = Vector2.zero;
        inputRect.anchorMax = Vector2.one;
        inputRect.offsetMin = Vector2.zero;
        inputRect.offsetMax = Vector2.zero;
        inputObj.AddComponent<RectMask2D>();

        TMPro.TextMeshProUGUI previewLabel = ControlText(inputObj.transform, O5Boot.Theme.FontSizeBody);
        previewLabel.rectTransform.anchorMin = Vector2.zero;
        previewLabel.rectTransform.anchorMax = Vector2.one;
        previewLabel.rectTransform.pivot = new Vector2(1f, 0.5f);
        previewLabel.rectTransform.offsetMin = Vector2.zero;
        previewLabel.rectTransform.offsetMax = new Vector2(-14f, 0f);
        previewLabel.alignment = TMPro.TextAlignmentOptions.Right;
        previewLabel.verticalAlignment = TMPro.VerticalAlignmentOptions.Middle;
        previewLabel.font = O5Boot.Fonts.Monospace;
        previewLabel.color = new Color(1f, 1f, 1f, 0.6f);

        TMPro.TMP_InputField inputField = inputObj.AddComponent<TMPro.TMP_InputField>();
        var textComp = ControlText(inputObj.transform, O5Boot.Theme.FontSizeBody);
        textComp.rectTransform.anchorMin = Vector2.zero;
        textComp.rectTransform.anchorMax = Vector2.one;
        textComp.rectTransform.pivot = new Vector2(1f, 0.5f);
        textComp.rectTransform.offsetMin = Vector2.zero;
        textComp.rectTransform.offsetMax = Vector2.zero;
        textComp.alignment = TMPro.TextAlignmentOptions.Right;
        textComp.verticalAlignment = TMPro.VerticalAlignmentOptions.Middle;
        textComp.font = O5Boot.Fonts.Monospace;

        inputField.textComponent = textComp;
        inputField.textViewport = inputRect;

        Image fillImg = fill.AddComponent<Image>();
        fillImg.sprite = O5Boot.Sprites.RoundedControl;
        fillImg.type = Image.Type.Sliced;
        fill.AddComponent<Mask>().showMaskGraphic = true;

        GameObject changeUp = AddSmallChangedCircle(fillRect);
        Image changeUpImg = changeUp.GetComponent<Image>();

        var trigger = rect.gameObject.AddComponent<EventTrigger>();

        O5Slider slider = new(
            id, rect, fillRect, fillImg, label, inputField, previewLabel,
            changeImg, changeUpImg, O5Effects.HoverOutline(rect.gameObject, trigger), defaultValue, min, max,
            value, format, clampMode, filter, onChanged, onComplete
        ) {
            ShowFill = showFill
        };
        fill.SetActive(showFill);
        inputObj.SetActive(true);

        Transform? canvasRoot = parent.GetComponentInParent<Canvas>()?.transform;
        RectTransform? dragBlocker = blockHoverWhileDragging && canvasRoot != null
            ? CreateSliderDragBlocker(canvasRoot)
            : null;
        if (dragBlocker) {
            dragBlocker.gameObject.SetActive(false);
        }

        var ovent = rect.gameObject.AddComponent<OventHandler>();
        ovent.OnClick += e => {
            switch (e) {
                case InputButton.Middle:
                    if (!O5Boot.Config.MiddleClickToDefault) {
                        break;
                    }

                    slider.Set(Apply(slider.DefaultValue));
                    slider.OnComplete?.Invoke(slider.Value);
                    break;
            }
        };

        float Apply(float v) {
            v = filter != null ? filter(v) : v;
            return slider.ClampMode == ClampMode.None ? v : Math.Clamp(v, min, max);
        }

        bool isDragging = false;
        float cachedValue = 0f;
        Vector2Int resetPos = Vector2Int.zero;
        Vector2 previousMousePos = Vector2.zero;
        bool justWarped = false;

        UnityUtils.AddEvents(trigger,
            (EventTriggerType.BeginDrag, (e) => {
                if (!O5Input.GetMouseButton(0)) {
                    return;
                }

                isDragging = true;
                cachedValue = slider.Value;

                if (EventSystem.current) {
                    EventSystem.current.SetSelectedGameObject(null);
                }

                resetPos = Vector2Int.RoundToInt(O5Input.OSMousePosition);
                previousMousePos = O5Input.MousePosition;
                justWarped = false;
                if (dragBlocker) {
                    dragBlocker.gameObject.SetActive(true);
                    dragBlocker.SetAsLastSibling();
                }
            }
        ),
            (EventTriggerType.Drag, (e) => {
                if (isDragging && O5Input.GetMouseButton(0)) {
                    Vector2 currentMousePos = O5Input.MousePosition;
                    Vector2 mousePixelDelta = currentMousePos - previousMousePos;
                    previousMousePos = currentMousePos;

                    if (justWarped) {
                        mousePixelDelta = Vector2.zero;
                        justWarped = false;
                    }

                    if (dragStep.HasValue) {
                        cachedValue += mousePixelDelta.x * dragStep.Value * O5Boot.Config.SliderSensitivity;
                    } else {
                        float finalPixelWidth = inputRect.rect.width * (canvasRoot?.GetComponent<Canvas>()?.scaleFactor ?? 1f);
                        cachedValue += mousePixelDelta.x * (slider.Max - slider.Min) * O5Boot.Config.SliderSensitivity / finalPixelWidth;
                    }

                    if (slider.ClampMode != ClampMode.None) {
                        cachedValue = Math.Clamp(cachedValue, min, max);
                    }

                    slider.Set(Apply(cachedValue));

                    Cursor.visible = false;

                    Vector2Int currentOSPos = O5Input.OSMousePosition;
                    int screenWidth = Screen.currentResolution.width;
                    int padding = 5;

                    if (currentOSPos.x <= padding) {
                        currentOSPos.x = screenWidth - padding - 1;
                        O5Input.OSMousePosition = new Vector2Int(currentOSPos.x, currentOSPos.y);
                        previousMousePos = new Vector2(Screen.width - padding - 1, currentMousePos.y);
                        justWarped = true;
                    } else if (currentOSPos.x >= screenWidth - padding) {
                        currentOSPos.x = padding + 1;
                        O5Input.OSMousePosition = new Vector2Int(currentOSPos.x, currentOSPos.y);
                        previousMousePos = new Vector2(padding + 1, currentMousePos.y);
                        justWarped = true;
                    }
                } else {
                    isDragging = false;
                }
            }
        ),
            (EventTriggerType.EndDrag, (e) => {
                if (isDragging) {
                    isDragging = false;
                    slider.OnComplete?.Invoke(slider.Value);

                    O5Input.OSMousePosition = resetPos;
                    Cursor.visible = true;
                    if (dragBlocker) {
                        dragBlocker.gameObject.SetActive(false);
                    }
                }
            }
        ),
            (EventTriggerType.Cancel, (e) => {
                if (!isDragging) {
                    return;
                }

                isDragging = false;
                O5Input.OSMousePosition = resetPos;
                Cursor.visible = true;
                if (dragBlocker) {
                    dragBlocker.gameObject.SetActive(false);
                }

                slider.OnComplete?.Invoke(slider.Value);
            }
        ),
            (EventTriggerType.PointerUp, (e) => {
                if (isDragging) {
                    return;
                }

                var ped =
#if IL2CPP
                    e.TryCast<PointerEventData>();
#else
                    e as PointerEventData;
#endif
                if (ped != null && ped.button != InputButton.Left) {
                    return;
                }

                if (EventSystem.current) {
                    EventSystem.current.SetSelectedGameObject(null);
                }

                inputField.Select();
                inputField.ActivateInputField();
            }
        )
        );

        slider.OnDisposed += () => {
            if (isDragging) {
                isDragging = false;
                O5Input.OSMousePosition = resetPos;
                Cursor.visible = true;
            }

            if (dragBlocker) {
                UnityEngine.Object.Destroy(dragBlocker.gameObject);
            }
        };

        slider.Set(Apply(value), false);

        return slider;
    }

    private static RectTransform CreateSliderDragBlocker(Transform parent) {
        GameObject blockerObject = new("NumericDragBlocker");
        blockerObject.transform.SetParent(parent, false);
        RectTransform blocker = blockerObject.AddComponent<RectTransform>();
        blocker.anchorMin = Vector2.zero;
        blocker.anchorMax = Vector2.one;
        blocker.offsetMin = Vector2.zero;
        blocker.offsetMax = Vector2.zero;
        Image image = blockerObject.AddComponent<Image>();
        image.color = Color.clear;
        return blocker;
    }

    /// <summary>Builds a text field with icon, placeholder and middle-click reset.</summary>
    /// <param name="parent">Parent transform.</param>
    /// <param name="defaultValue">Reset target. Null disables reset.</param>
    /// <param name="value">Initial text.</param>
    /// <param name="onChanged">Change callback.</param>
    /// <param name="placeholder">Placeholder text.</param>
    /// <param name="icon">Trailing icon. Null hides it.</param>
    /// <param name="id">Stable identifier.</param>
    /// <param name="onEndEdit">End-edit callback.</param>
    /// <param name="multiline">Multi-line mode.</param>
    /// <param name="monospace">Monospace font.</param>
    /// <param name="fieldFactory">Custom field builder (e.g. a code editor field). Null uses <see cref="TMPro.TMP_InputField"/>.</param>
    public static O5InputField Input(
        Transform parent,
        string? defaultValue,
        string? value,
        Action<string>? onChanged,
        string placeholder,
        Sprite? icon,
        string id,
        Action<string>? onEndEdit = null,
        bool multiline = false,
        bool monospace = false,
        Func<GameObject, TMPro.TMP_InputField>? fieldFactory = null
    ) {
        RectTransform rect = ControlBackground(parent);
        rect.SetParent(parent, false);

        GameObject change = AddSmallChangedCircle(rect);
        Image changeImg = change.GetComponent<Image>();

        GameObject iconObj = new("Icon");
        iconObj.transform.SetParent(rect, false);

        RectTransform circleRect = iconObj.AddComponent<RectTransform>();
        circleRect.anchorMin = new Vector2(1f, 0.5f);
        circleRect.anchorMax = new Vector2(1f, 0.5f);
        circleRect.pivot = new Vector2(0.5f, 0.5f);
        circleRect.anchoredPosition = new Vector2(-23f, 0f);
        circleRect.sizeDelta = new Vector2(26f, 26f);

        Image iconImage = iconObj.AddComponent<Image>();
        iconImage.sprite = icon;
        if (icon == null) {
            iconImage.enabled = false;
        } else {
            iconImage.color = new Color(1f, 1f, 1f, 0.2f);
        }

        GameObject inputObj = new("Input");
        inputObj.transform.SetParent(rect, false);
        inputObj.SetActive(false);

        RectTransform inputRect = inputObj.AddComponent<RectTransform>();
        inputRect.anchorMin = Vector2.zero;
        inputRect.anchorMax = Vector2.one;
        inputRect.offsetMin = Vector2.zero;
        inputRect.offsetMax = Vector2.zero;

        Image inputTarget = inputObj.AddComponent<Image>();
        inputTarget.color = Color.clear;
        inputTarget.raycastTarget = true;

        TMPro.TMP_InputField inputField = fieldFactory?.Invoke(inputObj) ?? inputObj.AddComponent<TMPro.TMP_InputField>();
        inputField.targetGraphic = inputTarget;

        GameObject viewportObj = new("TextViewport");
        viewportObj.transform.SetParent(inputObj.transform, false);
        RectTransform viewportRect = viewportObj.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = multiline ? new Vector2(16f, 12f) : new Vector2(16f, 4f);
        viewportRect.offsetMax = multiline ? new Vector2(-12f, -12f) : new Vector2(-12f, -4f);
        viewportObj.AddComponent<RectMask2D>();

        var text = ControlText(viewportObj.transform, O5Boot.Theme.FontSizeBody);
        text.font = monospace ? O5Boot.Fonts.Monospace : O5Boot.Fonts.Medium;
        text.text = value ?? string.Empty;
        text.alignment = multiline ? TMPro.TextAlignmentOptions.TopLeft : TMPro.TextAlignmentOptions.Left;
        text.textWrappingMode = multiline ? TMPro.TextWrappingModes.Normal : TMPro.TextWrappingModes.NoWrap;
        text.extraPadding = true;
        text.raycastTarget = false;

        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var placeholderText = ControlText(viewportObj.transform, O5Boot.Theme.FontSizeBody);
        placeholderText.font = monospace ? O5Boot.Fonts.Monospace : O5Boot.Fonts.Medium;
        placeholderText.text = placeholder;
        placeholderText.alignment = multiline ? TMPro.TextAlignmentOptions.TopLeft : TMPro.TextAlignmentOptions.Left;
        placeholderText.textWrappingMode = multiline ? TMPro.TextWrappingModes.Normal : TMPro.TextWrappingModes.NoWrap;
        placeholderText.color = new Color(1f, 1f, 1f, 0.2f);
        placeholderText.extraPadding = true;
        placeholderText.raycastTarget = false;

        RectTransform placeholderRect = placeholderText.rectTransform;
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = Vector2.zero;
        placeholderRect.offsetMax = Vector2.zero;

        inputField.textViewport = viewportRect;
        inputField.textComponent = text;
        inputField.placeholder = placeholderText;
        inputField.fontAsset = text.font;

        inputField.lineType = multiline
            ? TMPro.TMP_InputField.LineType.MultiLineNewline
            : TMPro.TMP_InputField.LineType.SingleLine;
        inputField.characterValidation = TMPro.TMP_InputField.CharacterValidation.None;
        inputField.richText = false;

        var input = new O5InputField(
            id,
            rect,
            inputField,
            placeholderText,
            iconImage,
            changeImg,
            defaultValue,
            value,
            onChanged,
            onEndEdit,
            multiline
        );
        inputObj.SetActive(true);

        var trigger = rect.gameObject.AddComponent<EventTrigger>();
        O5Effects.HoverOutline(rect.gameObject, trigger);

        var ovent = rect.gameObject.AddComponent<OventHandler>();
        ovent.OnClick += btn => {
            switch (btn) {
                case InputButton.Middle:
                    if (O5Boot.Config.MiddleClickToDefault &&
                        input.DefaultValue != null &&
                        input.Value != input.DefaultValue
                    ) {
                        input.Reset();
                        onEndEdit?.Invoke(input.Value);
                    }

                    break;
            }
        };

        UnityUtils.AddEvents(trigger,
            (EventTriggerType.PointerUp,
                e => {
                    PointerEventData? ped =
#if IL2CPP
                        e.TryCast<PointerEventData>();
#else
                        e as PointerEventData;
#endif
                    if (ped == null || ped.button != InputButton.Left) {
                        return;
                    }

                    if (EventSystem.current) {
                        EventSystem.current.SetSelectedGameObject(null);
                    }

                    inputField.Select();
                    inputField.ActivateInputField();
                }
        )
        );

        return input;
    }
}
