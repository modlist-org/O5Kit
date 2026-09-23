// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using O5Kit.Behaviour;
using O5Kit.Control;
using O5Kit.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UnityEngine.EventSystems.PointerEventData;

namespace O5Kit.Factory;

public static partial class O5Factory {
    /// <summary>Builds a color picker: header preview, hex field, wheel popup and channel sliders.</summary>
    /// <param name="parent">Parent transform.</param>
    /// <param name="canvasRect">Popup coordinate space (usually the canvas rect).</param>
    /// <param name="canvasCamera">Camera for hover checks. Null for overlay canvases.</param>
    /// <param name="defaultValue">Reset target.</param>
    /// <param name="value">Initial color.</param>
    /// <param name="onChanged">Change callback.</param>
    /// <param name="onComplete">Commit callback.</param>
    /// <param name="id">Stable identifier.</param>
    /// <param name="label">Preview label. Empty hides it.</param>
    public static O5ColorPicker ColorPicker(
        Transform parent,
        RectTransform canvasRect,
        Camera? canvasCamera,
        Color defaultValue,
        Color value,
        Action<Color>? onChanged,
        Action<Color>? onComplete,
        string id,
        string? label = null
    ) {
        GameObject rootObject = new("ColorPicker");
        rootObject.transform.SetParent(parent, false);
        RectTransform root = rootObject.AddComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;

        RectTransform header = ControlBackground(root);
        header.name = "Header";
        header.anchorMin = new Vector2(0f, 1f);
        header.anchorMax = new Vector2(1f, 1f);
        header.pivot = new Vector2(0.5f, 1f);
        header.offsetMin = new Vector2(0f, -50f);
        header.offsetMax = Vector2.zero;

        GameObject previewObject = new("Preview");
        previewObject.transform.SetParent(header, false);
        RectTransform previewRect = previewObject.AddComponent<RectTransform>();
        previewRect.anchorMin = new Vector2(0f, 0f);
        previewRect.anchorMax = new Vector2(0.5f, 1f);
        previewRect.offsetMin = new Vector2(9f, 8f);
        previewRect.offsetMax = new Vector2(-5f, -8f);
        Image preview = previewObject.AddComponent<Image>();
        preview.sprite = O5Boot.Sprites.RoundedControl;
        preview.type = Image.Type.Sliced;
        TMPro.TextMeshProUGUI previewLabel = ControlText(previewRect, 16f);
        previewLabel.name = "ColorLabel";
        previewLabel.text = label ?? string.Empty;
        previewLabel.alignment = TMPro.TextAlignmentOptions.Center;
        previewLabel.verticalAlignment = TMPro.VerticalAlignmentOptions.Middle;
        previewLabel.raycastTarget = false;

        O5ColorPicker? picker = null;
        O5InputField hexInput = Input(
            header,
            ColorUtility.ToHtmlStringRGBA(defaultValue),
            ColorUtility.ToHtmlStringRGBA(value),
            text => picker?.ValidateHex(text),
            string.Empty,
            null,
            id + "_hex",
            text => picker?.CompleteHex(text),
            monospace: true
        );
        RectTransform hexRect = hexInput.Rect;
        hexRect.name = "HexInput";
        hexRect.anchorMin = new Vector2(0.62f, 0f);
        hexRect.anchorMax = new Vector2(1f, 1f);
        hexRect.offsetMin = new Vector2(0f, 4f);
        hexRect.offsetMax = new Vector2(-10f, -4f);
        hexRect.GetComponent<Image>().color = Color.clear;
        hexInput.ChangedImage.gameObject.SetActive(false);
        TMPro.TMP_Text hexText = hexInput.InputField.textComponent;
        hexText.fontSize = 19f;
        hexText.alignment = TMPro.TextAlignmentOptions.Right;
        hexInput.InputField.onFocusSelectAll = false;
        hexInput.InputField.characterLimit = 9;
        Transform? hexHover = hexRect.Find("Hover");
        if (hexHover) {
            hexHover.gameObject.SetActive(false);
        }

        GameObject bodyObject = new("Body");
        bodyObject.transform.SetParent(root, false);
        RectTransform body = bodyObject.AddComponent<RectTransform>();
        body.anchorMin = new Vector2(0f, 1f);
        body.anchorMax = new Vector2(1f, 1f);
        body.pivot = new Vector2(0.5f, 1f);
        body.offsetMin = new Vector2(12f, -566f);
        body.offsetMax = new Vector2(-262f, -62f);
        Image bodyBackground = bodyObject.AddComponent<Image>();
        bodyBackground.sprite = O5Boot.Sprites.RoundedControl;
        bodyBackground.type = Image.Type.Sliced;
        bodyBackground.color = O5Boot.Theme.PanelBG;
        CanvasGroup bodyCanvas = bodyObject.AddComponent<CanvasGroup>();

        VerticalLayoutGroup bodyLayout = bodyObject.AddComponent<VerticalLayoutGroup>();
        bodyLayout.padding = new RectOffset { left = 14, right = 14, top = 12, bottom = 12 };
        bodyLayout.spacing = 6f;
        bodyLayout.childControlWidth = true;
        bodyLayout.childControlHeight = true;
        bodyLayout.childForceExpandWidth = true;
        bodyLayout.childForceExpandHeight = false;

        GameObject wheelObject = new("Wheel");
        wheelObject.transform.SetParent(body, false);
        RectTransform wheel = wheelObject.AddComponent<RectTransform>();
        LayoutElement wheelLayout = wheelObject.AddComponent<LayoutElement>();
        wheelLayout.preferredHeight = 280f;
        wheelLayout.minHeight = 280f;
        Image wheelImage = wheelObject.AddComponent<Image>();
        wheelImage.preserveAspect = true;
        wheelImage.color = Color.white;

        RectTransform hueHandle = CreateHandle(wheel, "HueHandle", new Vector2(15f, 15f));
        RectTransform colorHandle = CreateHandle(wheel, "ColorHandle", new Vector2(13f, 13f));

        RectTransform modeRow = Row(body, 30f);
        HorizontalLayoutGroup modeLayout = modeRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        modeLayout.spacing = 4f;
        modeLayout.childControlWidth = true;
        modeLayout.childControlHeight = true;
        modeLayout.childForceExpandWidth = true;
        modeLayout.childForceExpandHeight = true;
        var (rgbModeBackground, rgbModeLabel) = CreateModeButton(modeRow, "RGB", () => picker?.SetMode(false));
        var (hsvModeBackground, hsvModeLabel) = CreateModeButton(modeRow, "HSV", () => picker?.SetMode(true));

        O5Slider[] sliders = new O5Slider[4];
        string[] names = ["R", "G", "B", "A"];
        Color[] colors = [
            new Color(1f, 0.42f, 0.44f, 1f),
            new Color(0.48f, 0.82f, 0.48f, 1f),
            new Color(0.56f, 0.56f, 0.9f, 1f),
            new Color(0.45f, 0.45f, 0.45f, 1f)
        ];
        for (int i = 0; i < sliders.Length; i++) {
            int channel = i;
            RectTransform row = Row(body, 36f);
            sliders[i] = Slider(
                row, defaultValue[i], 0f, 1f, value[i], "F2", ClampMode.All, null,
                next => picker?.SetChannel(channel, next),
                _ => onComplete?.Invoke(picker!.Value),
                names[i], id + "_" + names[i].ToLowerInvariant()
            );
            sliders[i].Rect.offsetMax = Vector2.zero;
            sliders[i].FillImage.color = colors[i];
            sliders[i].Label.fontSize = 18f;
        }

        Image sharedOutline = O5Effects.HoverOutline(header.gameObject, header.gameObject.AddComponent<EventTrigger>());
        picker = new O5ColorPicker(
            id, root, canvasRect, canvasCamera, bodyObject, bodyCanvas, preview, previewLabel,
            wheel, hueHandle, colorHandle, hexInput, sharedOutline, sliders,
            rgbModeBackground, rgbModeLabel, hsvModeBackground, hsvModeLabel,
            defaultValue, value, onChanged, onComplete
        );

        EventTrigger wheelTrigger = wheelObject.AddComponent<EventTrigger>();
        UnityUtils.AddEvents(wheelTrigger,
            (EventTriggerType.PointerDown, picker.BeginPointer),
            (EventTriggerType.Drag, picker.DragPointer),
            (EventTriggerType.PointerUp, picker.EndPointer),
            (EventTriggerType.EndDrag, picker.EndPointer),
            (EventTriggerType.Cancel, picker.EndPointer)
        );

        GameObject toggleArea = new("ToggleArea");
        toggleArea.transform.SetParent(header, false);
        RectTransform toggleRect = toggleArea.AddComponent<RectTransform>();
        toggleRect.anchorMin = Vector2.zero;
        toggleRect.anchorMax = new Vector2(0.6f, 1f);
        toggleRect.offsetMin = Vector2.zero;
        toggleRect.offsetMax = Vector2.zero;
        Image toggleTarget = toggleArea.AddComponent<Image>();
        toggleTarget.color = Color.clear;
        var toggleOvent = toggleArea.AddComponent<OventHandler>();
        toggleOvent.OnClick += button => {
            switch (button) {
                case InputButton.Left:
                    picker.ToggleExpanded();
                    break;

                case InputButton.Middle:
                    if (O5Boot.Config.MiddleClickToDefault) {
                        picker.Reset();
                    }

                    break;
            }
        };

        return picker;
    }

    private static (Image Background, TMPro.TextMeshProUGUI Label) CreateModeButton(
        Transform parent,
        string text,
        Action onClick
    ) {
        GameObject buttonObject = new(text);
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        Image background = buttonObject.AddComponent<Image>();
        background.sprite = O5Boot.Sprites.RoundedControl;
        background.type = Image.Type.Sliced;
        TMPro.TextMeshProUGUI label = ControlText(rect, 15f);
        label.text = text;
        label.alignment = TMPro.TextAlignmentOptions.Center;
        label.rectTransform.offsetMin = Vector2.zero;
        label.rectTransform.offsetMax = Vector2.zero;
        label.raycastTarget = false;
        var ovent = buttonObject.AddComponent<OventHandler>();
        ovent.OnClick += button => {
            if (button == InputButton.Left) {
                onClick();
            }
        };
        return (background, label);
    }

    private static RectTransform CreateHandle(RectTransform parent, string name, Vector2 size) {
        GameObject handleObject = new(name);
        handleObject.transform.SetParent(parent, false);
        RectTransform rect = handleObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        Image image = handleObject.AddComponent<Image>();
        image.sprite = O5Boot.Sprites.RoundedOutline;
        image.type = Image.Type.Sliced;
        image.color = Color.white;
        image.raycastTarget = false;
        return rect;
    }
}
