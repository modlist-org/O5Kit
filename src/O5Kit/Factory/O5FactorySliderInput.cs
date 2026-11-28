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
    /// <param name="ctx">Owning kit context.</param>
    /// <param name="parent">Parent transform.</param>
    /// <param name="id">Stable identifier.</param>
    /// <param name="options">Behaviour. Null uses defaults.</param>
    public static O5Slider Slider(
        O5Context ctx,
        Transform parent,
        string id,
        O5SliderOptions? options = null
    ) {
        options ??= new O5SliderOptions();
        RectTransform rect = ControlBackground(ctx, parent);
        rect.SetParent(parent, false);

        GameObject change = AddSmallChangedCircle(ctx, rect);
        Image changeImg = change.GetComponent<Image>();

        // Background-shaped clip; the full-width fill slides inside it (see O5Slider.SetFillT).
        GameObject fillClip = new("FillClip");
        fillClip.transform.SetParent(rect, false);
        RectTransform clipRect = fillClip.AddComponent<RectTransform>();
        clipRect.anchorMin = Vector2.zero;
        clipRect.anchorMax = Vector2.one;
        clipRect.offsetMin = Vector2.zero;
        clipRect.offsetMax = Vector2.zero;
        Image clipImg = fillClip.AddComponent<Image>();
        clipImg.sprite = ctx.Sprites.RoundedControl;
        clipImg.type = Image.Type.Sliced;
        clipImg.raycastTarget = false;
        fillClip.AddComponent<Mask>().showMaskGraphic = false;

        GameObject fill = new("Fill");
        fill.transform.SetParent(clipRect, false);

        RectTransform fillRect = fill.AddComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(-1f, 0f);
        fillRect.anchorMax = new Vector2(0f, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.anchoredPosition = Vector2.zero;
        fillRect.sizeDelta = Vector2.zero;

        TMPro.TextMeshProUGUI label = ControlText(ctx, rect, ctx.Theme.FontSizeBody);
        label.text = options.Label;
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
        inputRect.offsetMax = new Vector2(-14f, 0f);
        inputObj.AddComponent<RectMask2D>();

        TMPro.TextMeshProUGUI previewLabel = ControlText(ctx, inputObj.transform, ctx.Theme.FontSizeBody);
        previewLabel.rectTransform.anchorMin = Vector2.zero;
        previewLabel.rectTransform.anchorMax = Vector2.one;
        previewLabel.rectTransform.pivot = new Vector2(1f, 0.5f);
        previewLabel.rectTransform.offsetMin = Vector2.zero;
        previewLabel.rectTransform.offsetMax = Vector2.zero;
        previewLabel.alignment = TMPro.TextAlignmentOptions.Right;
        previewLabel.verticalAlignment = TMPro.VerticalAlignmentOptions.Middle;
        previewLabel.font = ctx.Fonts.Monospace;
        previewLabel.color = ctx.Theme.TextDim;

        TMPro.TMP_InputField inputField = inputObj.AddComponent<TMPro.TMP_InputField>();
        var textComp = ControlText(ctx, inputObj.transform, ctx.Theme.FontSizeBody);
        textComp.rectTransform.anchorMin = Vector2.zero;
        textComp.rectTransform.anchorMax = Vector2.one;
        textComp.rectTransform.pivot = new Vector2(1f, 0.5f);
        textComp.rectTransform.offsetMin = Vector2.zero;
        textComp.rectTransform.offsetMax = Vector2.zero;
        textComp.alignment = TMPro.TextAlignmentOptions.Right;
        textComp.verticalAlignment = TMPro.VerticalAlignmentOptions.Middle;
        textComp.font = ctx.Fonts.Monospace;

        inputField.textComponent = textComp;
        inputField.textViewport = inputRect;

        Image fillImg = fill.AddComponent<Image>();
        fillImg.sprite = ctx.Sprites.RoundedControl;
        fillImg.type = Image.Type.Sliced;
        fill.AddComponent<Mask>().showMaskGraphic = true;

        GameObject changeUp = AddSmallChangedCircle(ctx, fillRect);
        Image changeUpImg = changeUp.GetComponent<Image>();
        RectTransform changeUpRect = changeUp.GetComponent<RectTransform>();
        changeUpRect.anchorMin = changeUpRect.anchorMax = new Vector2(1f, 1f);

        var trigger = rect.gameObject.AddComponent<EventTrigger>();

        O5Slider? sliderRef = null;
        Image outlineImg = O5Effects.HoverOutline(
            ctx,
            rect.gameObject,
            trigger,
            () => sliderRef?.EffectiveHoverOutline ?? ctx.Theme.ControlOutlineHover
        );

        O5Slider slider = new(
            ctx, id, rect, fillRect, fillImg, label, inputField, previewLabel,
            changeImg, changeUpImg, outlineImg, options) {
            ShowFill = options.ShowFill
        };
        sliderRef = slider;
        inputObj.SetActive(true);

        Transform? canvasRoot = parent.GetComponentInParent<Canvas>()?.transform;

        var ovent = rect.gameObject.AddComponent<OventHandler>();
        ovent.OnDisabled += slider.ResetShake;
        ovent.OnClick += e => {
            switch (e) {
                case InputButton.Middle:
                    if (!ctx.Config.MiddleClickToDefault) {
                        break;
                    }

                    slider.Reset();
                    break;
            }
        };

        double Apply(double v) {
            v = slider.DragFilter != null ? slider.DragFilter(v) : v;
            return slider.ClampDrag ? MathCompat.Clamp(v, slider.Min, slider.Max) : v;
        }

        bool isDragging = false;
        bool blockerAcquired = false;
        double cachedValue = 0d;
        Vector2Int resetPos = Vector2Int.zero;
        Vector2 previousMousePos = Vector2.zero;
        Vector2 startMousePos = Vector2.zero;
        Vector2? warpedFrom = null;

        void EndDragCleanup(bool restoreCursor) {
            if (restoreCursor) {
                O5Input.OSMousePosition = resetPos;
                Cursor.visible = true;
            }
            if (blockerAcquired) {
                blockerAcquired = false;
                ctx.ReleaseDragBlocker();
            }
        }

        UnityUtils.AddEvents(trigger,
            (EventTriggerType.PointerEnter, _ => slider.SetHovered(true)),
            (EventTriggerType.PointerExit, _ => slider.SetHovered(false)),
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
                startMousePos = previousMousePos = O5Input.MousePosition;
                warpedFrom = null;
                if (options.BlockHoverWhileDragging && !blockerAcquired) {
                    blockerAcquired = true;
                    ctx.AcquireDragBlocker();
                }
            }
        ),
            (EventTriggerType.Drag, (e) => {
                if (isDragging && O5Input.GetMouseButton(0)) {
                    Vector2 currentMousePos = O5Input.MousePosition;

                    // Some platforms (macOS) only report the warped position on the next mouse event.
                    if (warpedFrom.HasValue && (currentMousePos - warpedFrom.Value).sqrMagnitude < 0.01f) {
                        return;
                    }

                    warpedFrom = null;
                    Vector2 mousePixelDelta = currentMousePos - previousMousePos;
                    previousMousePos = currentMousePos;

                    if (options.DragStep.HasValue) {
                        cachedValue += mousePixelDelta.x * options.DragStep.Value * ctx.Config.SliderSensitivity;
                    } else {
                        float finalPixelWidth = inputRect.rect.width * (canvasRoot?.GetComponent<Canvas>()?.scaleFactor ?? 1f);
                        cachedValue += mousePixelDelta.x * (slider.Max - slider.Min) * ctx.Config.SliderSensitivity / finalPixelWidth;
                    }

                    if (slider.ClampDrag) {
                        cachedValue = MathCompat.Clamp(cachedValue, slider.Min, slider.Max);
                    }
                    // Set() also clamps by the input range; match it so dragging back has no dead zone.
                    if (slider.ClampInput) {
                        cachedValue = MathCompat.Clamp(cachedValue, slider.EffectiveInputMin, slider.EffectiveInputMax);
                    }

                    slider.Set(Apply(cachedValue));

                    Cursor.visible = false;

                    // Infinite drag: at the window edge, jump the cursor back to the drag start.
                    // Window-space check avoids OS screen bounds (multi-monitor, Retina points vs pixels).
                    const float padding = 5f;
                    bool startInside = startMousePos.x > padding && startMousePos.x < Screen.width - padding;
                    if (startInside && (currentMousePos.x <= padding || currentMousePos.x >= Screen.width - padding)) {
                        O5Input.OSMousePosition = resetPos;
                        warpedFrom = currentMousePos;
                        previousMousePos = startMousePos;
                    }
                } else if (isDragging) {
                    isDragging = false;
                    EndDragCleanup(restoreCursor: true);
                }
            }
        ),
            (EventTriggerType.EndDrag, (e) => {
                if (isDragging) {
                    isDragging = false;
                    slider.OnComplete?.Invoke(slider.Value);
                    EndDragCleanup(restoreCursor: true);
                }
            }
        ),
            (EventTriggerType.Cancel, (e) => {
                if (!isDragging) {
                    return;
                }

                isDragging = false;
                EndDragCleanup(restoreCursor: true);
                slider.OnComplete?.Invoke(slider.Value);
            }
        ),
            (EventTriggerType.PointerUp, (e) => {
                if (isDragging) {
                    return;
                }

                var ped = UnityUtils.AsPointerEventData(e);
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
            slider.SetHovered(false);
            if (isDragging) {
                isDragging = false;
                EndDragCleanup(restoreCursor: true);
            } else {
                EndDragCleanup(restoreCursor: false);
            }
        };

        slider.Set(options.Value, false);

        return slider;
    }

    /// <summary>Builds a slider with inline configuration.</summary>
    /// <param name="ctx">Owning kit context.</param>
    /// <param name="parent">Parent transform.</param>
    /// <param name="id">Stable identifier.</param>
    /// <param name="configure">Mutates a fresh <see cref="O5SliderOptions"/>.</param>
    public static O5Slider Slider(
        O5Context ctx,
        Transform parent,
        string id,
        Action<O5SliderOptions> configure
    ) {
        var options = new O5SliderOptions();
        configure?.Invoke(options);
        return Slider(ctx, parent, id, options);
    }

    /// <summary>Builds a text field with icon, placeholder and middle-click reset.</summary>
    /// <param name="ctx">Owning kit context.</param>
    /// <param name="parent">Parent transform.</param>
    /// <param name="id">Stable identifier.</param>
    /// <param name="options">Behaviour. Null uses defaults.</param>
    public static O5InputField Input(
        O5Context ctx,
        Transform parent,
        string id,
        O5InputOptions? options = null
    ) {
        options ??= new O5InputOptions();
        RectTransform rect = ControlBackground(ctx, parent);
        rect.SetParent(parent, false);

        GameObject change = AddSmallChangedCircle(ctx, rect);
        Image changeImg = change.GetComponent<Image>();

        var iconText = O5FactoryIcons.CreateIcon(
            rect,
            "Icon",
            options.Icon ?? O5Icon.None,
            ctx.Theme.TextFaint,
            fixedFontSize: 26f
        );

        RectTransform circleRect = iconText.rectTransform;
        circleRect.anchorMin = new Vector2(1f, 0.5f);
        circleRect.anchorMax = new Vector2(1f, 0.5f);
        circleRect.pivot = new Vector2(0.5f, 0.5f);
        circleRect.anchoredPosition = new Vector2(-23f, 0f);
        circleRect.sizeDelta = new Vector2(26f, 26f);

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

        TMPro.TMP_InputField inputField = options.FieldFactory?.Invoke(inputObj) ?? inputObj.AddComponent<TMPro.TMP_InputField>();
        if (options.FieldFactory != null) {
            UnityUtils.InitInjectedTMPInputField(inputField);
        }
        inputField.targetGraphic = inputTarget;

        GameObject viewportObj = new("TextViewport");
        viewportObj.transform.SetParent(inputObj.transform, false);
        RectTransform viewportRect = viewportObj.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = options.Multiline ? new Vector2(16f, 12f) : new Vector2(16f, 4f);
        viewportRect.offsetMax = options.Multiline ? new Vector2(-12f, -12f) : new Vector2(-12f, -4f);
        viewportObj.AddComponent<RectMask2D>();

        var text = ControlText(ctx, viewportObj.transform, ctx.Theme.FontSizeBody);
        text.font = options.Monospace ? ctx.Fonts.Monospace : ctx.Fonts.Medium;
        text.text = options.Value ?? string.Empty;
        text.alignment = options.Multiline ? TMPro.TextAlignmentOptions.TopLeft : TMPro.TextAlignmentOptions.Left;
        text.enableWordWrapping = options.Multiline;
        text.extraPadding = true;
        text.raycastTarget = false;

        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var placeholderText = ControlText(ctx, viewportObj.transform, ctx.Theme.FontSizeBody);
        placeholderText.font = options.Monospace ? ctx.Fonts.Monospace : ctx.Fonts.Medium;
        placeholderText.text = options.Placeholder;
        placeholderText.alignment = options.Multiline ? TMPro.TextAlignmentOptions.TopLeft : TMPro.TextAlignmentOptions.Left;
        placeholderText.enableWordWrapping = options.Multiline;
        placeholderText.color = ctx.Theme.TextFaint;
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

        inputField.lineType = options.Multiline
            ? TMPro.TMP_InputField.LineType.MultiLineNewline
            : TMPro.TMP_InputField.LineType.SingleLine;
        inputField.characterValidation = TMPro.TMP_InputField.CharacterValidation.None;
        inputField.richText = false;

        var input = new O5InputField(
            ctx,
            id,
            rect,
            inputField,
            placeholderText,
            iconText,
            changeImg,
            options
        );
        inputObj.SetActive(true);

        var trigger = rect.gameObject.AddComponent<EventTrigger>();
        O5Effects.HoverOutline(ctx, rect.gameObject, trigger);

        var ovent = rect.gameObject.AddComponent<OventHandler>();
        ovent.OnClick += btn => {
            switch (btn) {
                case InputButton.Middle:
                    if (ctx.Config.MiddleClickToDefault &&
                        input.DefaultValue != null &&
                        input.Value != input.DefaultValue
                    ) {
                        input.Reset();
                        options.OnEndEdit?.Invoke(input.Value);
                    }

                    break;
            }
        };

        UnityUtils.AddEvents(trigger,
            (EventTriggerType.PointerUp,
                e => {
                    PointerEventData? ped = UnityUtils.AsPointerEventData(e);
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

    /// <summary>Builds a text field with inline configuration.</summary>
    /// <param name="ctx">Owning kit context.</param>
    /// <param name="parent">Parent transform.</param>
    /// <param name="id">Stable identifier.</param>
    /// <param name="configure">Mutates a fresh <see cref="O5InputOptions"/>.</param>
    public static O5InputField Input(
        O5Context ctx,
        Transform parent,
        string id,
        Action<O5InputOptions> configure
    ) {
        var options = new O5InputOptions();
        configure?.Invoke(options);
        return Input(ctx, parent, id, options);
    }
}
