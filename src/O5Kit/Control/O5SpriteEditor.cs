// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Collections.Generic;
using O5Kit.Behaviour;
using O5Kit.Core;
using O5Kit.Factory;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UnityEngine.EventSystems.PointerEventData;

namespace O5Kit.Control;

/// <summary>Text and sizing for <see cref="O5SpriteEditor"/>. All strings are consumer-provided (no translator in O5Kit).</summary>
public sealed class O5SpriteEditorOptions {
    /// <summary>Values hint format: {0}=L {1}=R {2}=B {3}=T.</summary>
    public string ValuesFormat { get; set; } = "L {0}  R {1}  B {2}  T {3}";

    /// <summary>Hint shown without a texture.</summary>
    public string HintEmpty { get; set; } = "Drag the green guides to set 9-slice borders.";

    /// <summary>Left slider label.</summary>
    public string LeftLabel { get; set; } = "Left";

    /// <summary>Right slider label.</summary>
    public string RightLabel { get; set; } = "Right";

    /// <summary>Bottom slider label.</summary>
    public string BottomLabel { get; set; } = "Bottom";

    /// <summary>Top slider label.</summary>
    public string TopLabel { get; set; } = "Top";

    /// <summary>Cancel button label.</summary>
    public string CancelLabel { get; set; } = "Cancel";

    /// <summary>Apply button label.</summary>
    public string ApplyLabel { get; set; } = "Apply Settings";

    /// <summary>Window size. Clamped to the canvas on open.</summary>
    public Vector2 Size { get; set; } = new(760f, 580f);
}

/// <summary>9-slice border guides.</summary>
public enum O5SpriteGuide {
    /// <summary>Left border.</summary>
    Left,
    /// <summary>Right border.</summary>
    Right,
    /// <summary>Bottom border.</summary>
    Bottom,
    /// <summary>Top border.</summary>
    Top,
}

/// <summary>9-slice border editor: preview workspace with draggable guides plus channel sliders. Domain I/O stays consumer-side via <see cref="Applied"/>.</summary>
public sealed class O5SpriteEditor : O5Object {
    /// <summary>Current border in pixels (x=left, y=bottom, z=right, w=top).</summary>
    public Vector4 Border => _border;

    /// <summary>Editor window.</summary>
    public O5Window Window => _window;

    /// <summary>Fired with the normalized border when Apply is pressed.</summary>
    public event Action<Vector4>? Applied;

    /// <summary>Fired when the editor closes.</summary>
    public event Action? Closed;

    private readonly O5SpriteEditorOptions _options;
    private readonly RectTransform _canvasRect;
    private readonly GameObject _blocker;
    private readonly O5Window _window;
    private readonly RawImage _image;
    private readonly RectTransform _preview;
    private readonly RectTransform _overlay;
    private readonly TMPro.TextMeshProUGUI _hint;
    private readonly RectTransform _fieldRow;
    private readonly RectTransform _guideLeft;
    private readonly RectTransform _guideRight;
    private readonly RectTransform _guideBottom;
    private readonly RectTransform _guideTop;
    private readonly List<O5Slider> _sliders = new();
    private Texture2D? _texture;
    private Vector4 _border;

    private O5SpriteEditor(
        O5Window window,
        RectTransform canvasRect,
        O5SpriteEditorOptions options,
        GameObject blocker,
        RawImage image,
        RectTransform preview,
        RectTransform overlay,
        TMPro.TextMeshProUGUI hint,
        RectTransform fieldRow,
        RectTransform guideLeft,
        RectTransform guideRight,
        RectTransform guideBottom,
        RectTransform guideTop
    ) : base("sprite_editor", window.Rect) {
        _window = window;
        _canvasRect = canvasRect;
        _options = options;
        _blocker = blocker;
        _image = image;
        _preview = preview;
        _overlay = overlay;
        _hint = hint;
        _fieldRow = fieldRow;
        _guideLeft = guideLeft;
        _guideRight = guideRight;
        _guideBottom = guideBottom;
        _guideTop = guideTop;
    }

    /// <summary>Builds a hidden editor under a canvas root.</summary>
    /// <param name="canvasRoot">Canvas transform. Must carry a RectTransform.</param>
    /// <param name="options">Text, sizing and labels.</param>
    public static O5SpriteEditor Create(Transform canvasRoot, O5SpriteEditorOptions options) {
        var theme = O5Boot.Theme;
        var sprites = O5Boot.Sprites;
        var canvasRect = canvasRoot as RectTransform;

        var blocker = new GameObject("SpriteEditorBlocker");
        blocker.transform.SetParent(canvasRoot, false);
        var blockerRect = blocker.AddComponent<RectTransform>();
        blockerRect.anchorMin = Vector2.zero;
        blockerRect.anchorMax = Vector2.one;
        blockerRect.offsetMin = Vector2.zero;
        blockerRect.offsetMax = Vector2.zero;
        var blockerImage = blocker.AddComponent<Image>();
        blockerImage.color = new Color(0f, 0f, 0f, 0.58f);
        blockerImage.raycastTarget = true;
        blocker.SetActive(false);

        var window = O5Window.Create(canvasRoot, new O5WindowOptions {
            Id = "sprite_editor",
            Title = options.ApplyLabel,
            Size = options.Size,
            Padding = new RectOffset(0, 0, 0, 0),
        });
        var content = window.Content;

        var workspaceObj = new GameObject("Workspace");
        workspaceObj.transform.SetParent(content, false);
        var workspace = workspaceObj.AddComponent<RectTransform>();
        workspace.anchorMin = Vector2.zero;
        workspace.anchorMax = Vector2.one;
        workspace.offsetMin = new Vector2(22f, 184f);
        workspace.offsetMax = new Vector2(-22f, -6f);
        var workspaceBg = workspaceObj.AddComponent<Image>();
        workspaceBg.sprite = sprites.RoundedControl;
        workspaceBg.type = Image.Type.Sliced;
        workspaceBg.color = theme.ObjectBG;
        workspaceBg.raycastTarget = true;

        var previewObj = new GameObject("Preview");
        previewObj.transform.SetParent(workspace, false);
        var preview = previewObj.AddComponent<RectTransform>();
        preview.anchorMin = new Vector2(0.5f, 0.5f);
        preview.anchorMax = new Vector2(0.5f, 0.5f);
        preview.pivot = new Vector2(0.5f, 0.5f);
        preview.sizeDelta = new Vector2(520f, 340f);

        var image = previewObj.AddComponent<RawImage>();
        image.color = Color.white;
        image.raycastTarget = false;

        var overlayObj = new GameObject("GuideOverlay");
        overlayObj.transform.SetParent(workspace, false);
        var overlay = overlayObj.AddComponent<RectTransform>();
        overlay.anchorMin = new Vector2(0.5f, 0.5f);
        overlay.anchorMax = new Vector2(0.5f, 0.5f);
        overlay.pivot = new Vector2(0.5f, 0.5f);
        overlay.sizeDelta = preview.sizeDelta;

        O5SpriteEditor? editor = null;
        var guideLeft = CreateGuide(overlay, O5SpriteGuide.Left, (g, d) => editor?.DragGuide(g, d));
        var guideRight = CreateGuide(overlay, O5SpriteGuide.Right, (g, d) => editor?.DragGuide(g, d));
        var guideBottom = CreateGuide(overlay, O5SpriteGuide.Bottom, (g, d) => editor?.DragGuide(g, d));
        var guideTop = CreateGuide(overlay, O5SpriteGuide.Top, (g, d) => editor?.DragGuide(g, d));

        var fieldRowObj = new GameObject("BorderFields");
        fieldRowObj.transform.SetParent(content, false);
        var fieldRow = fieldRowObj.AddComponent<RectTransform>();
        fieldRow.anchorMin = Vector2.zero;
        fieldRow.anchorMax = new Vector2(1f, 0f);
        fieldRow.offsetMin = new Vector2(22f, 90f);
        fieldRow.offsetMax = new Vector2(-22f, 176f);

        var hint = O5Factory.ControlText(content, 15f);
        hint.alignment = TMPro.TextAlignmentOptions.Center;
        hint.rectTransform.anchorMin = new Vector2(0f, 0f);
        hint.rectTransform.anchorMax = new Vector2(1f, 0f);
        hint.rectTransform.pivot = new Vector2(0.5f, 0f);
        hint.rectTransform.offsetMin = new Vector2(22f, 60f);
        hint.rectTransform.offsetMax = new Vector2(-22f, 82f);
        hint.color = new Color(1f, 1f, 1f, 0.65f);

        var cancel = O5Factory.Button(content, null, options.CancelLabel, "sprite_editor_cancel");
        cancel.Rect.anchorMin = new Vector2(1f, 0f);
        cancel.Rect.anchorMax = new Vector2(1f, 0f);
        cancel.Rect.pivot = new Vector2(1f, 0f);
        cancel.Rect.anchoredPosition = new Vector2(-152f, 12f);
        cancel.Rect.sizeDelta = new Vector2(130f, 42f);

        var apply = O5Factory.Button(content, null, options.ApplyLabel, "sprite_editor_apply");
        apply.Rect.anchorMin = new Vector2(1f, 0f);
        apply.Rect.anchorMax = new Vector2(1f, 0f);
        apply.Rect.pivot = new Vector2(1f, 0f);
        apply.Rect.anchoredPosition = new Vector2(-12f, 12f);
        apply.Rect.sizeDelta = new Vector2(130f, 42f);

        editor = new O5SpriteEditor(window, canvasRect, options, blocker, image, preview, overlay, hint, fieldRow, guideLeft, guideRight, guideBottom, guideTop);
        window.CloseRequested += _ => editor.Close();

        var blockerOvent = blocker.AddComponent<OventHandler>();
        blockerOvent.OnClick += button => {
            if (button == InputButton.Left) {
                editor.Close();
            }
        };

        cancel.OnClick = editor.Close;
        apply.OnClick = () => editor.ApplyPressed();

        return editor;
    }

    /// <summary>Opens the editor for a texture with an initial border and title.</summary>
    /// <param name="texture">Texture to preview. Null clears the editor.</param>
    /// <param name="border">Initial border in pixels (x=left, y=bottom, z=right, w=top).</param>
    /// <param name="title">Window title.</param>
    public void Open(Texture2D? texture, Vector4 border, string title) {
        if (IsDisposed) {
            return;
        }

        _window.Rect.sizeDelta = new Vector2(
            Mathf.Min(_options.Size.x, _canvasRect.rect.width - 32f),
            Mathf.Min(_options.Size.y, _canvasRect.rect.height - 32f)
        );
        _blocker.SetActive(true);
        _window.Rect.gameObject.SetActive(true);
        _window.BringToFront();
        _blocker.transform.SetAsLastSibling();
        _window.Rect.transform.SetAsLastSibling();
        _window.SetTitle(title);
        Canvas.ForceUpdateCanvases();
        int width = texture ? texture.width : 0;
        int height = texture ? texture.height : 0;
        RebuildSliders(width, height);
        SetEditor(texture, border);
    }

    /// <summary>Hides the editor and clears the preview.</summary>
    public void Close() {
        if (IsDisposed) {
            return;
        }

        _blocker.SetActive(false);
        _window.Rect.gameObject.SetActive(false);
        SetEditor(null, Vector4.zero);
        Closed?.Invoke();
    }

    private void ApplyPressed() {
        if (IsDisposed || _texture == null) {
            return;
        }

        Applied?.Invoke(NormalizeBorder(_border, _texture.width, _texture.height));
    }

    private void RebuildSliders(int width, int height) {
        foreach (var slider in _sliders) {
            var rect = slider.Rect;
            slider.Dispose();
            if (rect) {
                UnityEngine.Object.Destroy(rect.gameObject);
            }
        }

        _sliders.Clear();

        _sliders.Add(CreateBorderSlider(_fieldRow, _options.LeftLabel, O5SpriteGuide.Left, false, true, width, "sprite_border_left"));
        _sliders.Add(CreateBorderSlider(_fieldRow, _options.RightLabel, O5SpriteGuide.Right, true, true, width, "sprite_border_right"));
        _sliders.Add(CreateBorderSlider(_fieldRow, _options.BottomLabel, O5SpriteGuide.Bottom, false, false, height, "sprite_border_bottom"));
        _sliders.Add(CreateBorderSlider(_fieldRow, _options.TopLabel, O5SpriteGuide.Top, true, false, height, "sprite_border_top"));
    }

    private O5Slider CreateBorderSlider(Transform parent, string label, O5SpriteGuide guide, bool right, bool top, float max, string id) {
        O5Slider input = O5Factory.Slider(
            parent,
            0f,
            0f,
            max,
            0f,
            "F0",
            ClampMode.All,
            value => FilterSlider(guide, value),
            value => SetBorderFromSlider(guide, value),
            null,
            label,
            id
        );
        input.Rect.anchorMin = new Vector2(right ? 0.5f : 0f, top ? 0.5f : 0f);
        input.Rect.anchorMax = new Vector2(right ? 1f : 0.5f, top ? 1f : 0.5f);
        input.Rect.offsetMin = new Vector2(right ? 5f : 0f, top ? 3f : 0f);
        input.Rect.offsetMax = new Vector2(right ? 0f : -5f, top ? 0f : -3f);
        input.Label.fontSize = 16f;
        input.PreviewLabel.fontSize = 16f;
        input.InputCore.InputField.textComponent.fontSize = 16f;
        return input;
    }

    private static RectTransform CreateGuide(RectTransform parent, O5SpriteGuide guide, Action<O5SpriteGuide, BaseEventData> onDrag) {
        GameObject guideObject = new(guide.ToString());
        guideObject.transform.SetParent(parent, false);
        RectTransform rect = guideObject.AddComponent<RectTransform>();
        bool vertical = guide is O5SpriteGuide.Left or O5SpriteGuide.Right;
        (rect.anchorMin, rect.anchorMax) = guide switch {
            O5SpriteGuide.Left => (new Vector2(0f, 0f), new Vector2(0f, 1f)),
            O5SpriteGuide.Right => (new Vector2(1f, 0f), new Vector2(1f, 1f)),
            O5SpriteGuide.Bottom => (new Vector2(0f, 0f), new Vector2(1f, 0f)),
            _ => (new Vector2(0f, 1f), new Vector2(1f, 1f))
        };
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = vertical ? new Vector2(24f, 0f) : new Vector2(0f, 24f);

        Image hitArea = guideObject.AddComponent<Image>();
        Color idleColor = new(0.15f, 1f, 0.25f, 0.12f);
        Color hoverColor = new(0.15f, 1f, 0.25f, 0.32f);
        hitArea.color = idleColor;
        hitArea.raycastTarget = true;

        CreateGuideLine(guideObject.transform, vertical, 6f, new Color(0f, 0f, 0f, 0.9f));
        CreateGuideLine(guideObject.transform, vertical, 3f, new Color(0.15f, 1f, 0.25f, 1f));

        GameObject handleObject = new("Handle");
        handleObject.transform.SetParent(guideObject.transform, false);
        RectTransform handle = handleObject.AddComponent<RectTransform>();
        handle.anchorMin = new Vector2(0.5f, 0.5f);
        handle.anchorMax = new Vector2(0.5f, 0.5f);
        handle.sizeDelta = vertical ? new Vector2(24f, 42f) : new Vector2(42f, 24f);
        Image handleImage = handleObject.AddComponent<Image>();
        handleImage.sprite = O5Boot.Sprites.RoundedControl;
        handleImage.type = Image.Type.Sliced;
        handleImage.color = new Color(0f, 0f, 0f, 0.9f);
        handleImage.raycastTarget = false;

        GameObject handleFillObject = new("Fill");
        handleFillObject.transform.SetParent(handleObject.transform, false);
        RectTransform handleFill = handleFillObject.AddComponent<RectTransform>();
        handleFill.anchorMin = Vector2.zero;
        handleFill.anchorMax = Vector2.one;
        handleFill.offsetMin = new Vector2(3f, 3f);
        handleFill.offsetMax = new Vector2(-3f, -3f);
        Image handleFillImage = handleFillObject.AddComponent<Image>();
        handleFillImage.sprite = O5Boot.Sprites.RoundedControl;
        handleFillImage.type = Image.Type.Sliced;
        handleFillImage.color = new Color(0.15f, 1f, 0.25f, 1f);
        handleFillImage.raycastTarget = false;

        UnityUtils.AddEvents(
            guideObject.AddComponent<EventTrigger>(),
            (EventTriggerType.PointerEnter, _ => hitArea.color = hoverColor),
            (EventTriggerType.PointerExit, _ => hitArea.color = idleColor),
            (EventTriggerType.PointerDown, data => onDrag(guide, data)),
            (EventTriggerType.BeginDrag, data => onDrag(guide, data)),
            (EventTriggerType.Drag, data => onDrag(guide, data))
        );
        return rect;
    }

    private static void CreateGuideLine(Transform parent, bool vertical, float thickness, Color color) {
        GameObject lineObject = new("Line");
        lineObject.transform.SetParent(parent, false);
        RectTransform line = lineObject.AddComponent<RectTransform>();
        line.anchorMin = vertical ? new Vector2(0.5f, 0f) : new Vector2(0f, 0.5f);
        line.anchorMax = vertical ? new Vector2(0.5f, 1f) : new Vector2(1f, 0.5f);
        line.sizeDelta = vertical ? new Vector2(thickness, 0f) : new Vector2(0f, thickness);
        Image lineImage = lineObject.AddComponent<Image>();
        lineImage.color = color;
        lineImage.raycastTarget = false;
    }

    private float FilterSlider(O5SpriteGuide guide, float value) {
        value = Mathf.Round(value);
        if (_texture == null) {
            return Mathf.Max(0f, value);
        }

        return guide switch {
            O5SpriteGuide.Left => Mathf.Clamp(value, 0f, _texture.width - _border.z),
            O5SpriteGuide.Right => Mathf.Clamp(value, 0f, _texture.width - _border.x),
            O5SpriteGuide.Bottom => Mathf.Clamp(value, 0f, _texture.height - _border.w),
            _ => Mathf.Clamp(value, 0f, _texture.height - _border.y)
        };
    }

    private void SetBorderFromSlider(O5SpriteGuide guide, float value) {
        if (_texture == null) {
            return;
        }

        value = FilterSlider(guide, value);
        switch (guide) {
            case O5SpriteGuide.Left:
                _border.x = value;
                break;
            case O5SpriteGuide.Right:
                _border.z = value;
                break;
            case O5SpriteGuide.Bottom:
                _border.y = value;
                break;
            case O5SpriteGuide.Top:
                _border.w = value;
                break;
        }

        UpdateGuides();
    }

    private void UpdateInputs() {
        UpdateInput(0, _border.x);
        UpdateInput(1, _border.z);
        UpdateInput(2, _border.y);
        UpdateInput(3, _border.w);
    }

    private void UpdateInput(int index, float value) {
        if (index < 0 || index >= _sliders.Count) {
            return;
        }

        _sliders[index].Set(Mathf.Round(value), false);
    }

    private void SetEditor(Texture2D? texture, Vector4 border) {
        bool hasTexture = texture != null;
        _texture = texture;
        if (hasTexture) {
            border = NormalizeBorder(border, texture!.width, texture.height);
            _border = NormalizeBorder(
                new Vector4(
                    Mathf.Round(border.x),
                    Mathf.Round(border.y),
                    Mathf.Round(border.z),
                    Mathf.Round(border.w)
                ),
                texture.width,
                texture.height
            );
        } else {
            _border = Vector4.zero;
        }

        if (_image == null) {
            return;
        }

        _image.texture = texture;
        _image.color = hasTexture ? Color.white : new Color(1f, 1f, 1f, 0.08f);
        if (hasTexture) {
            RectTransform workspace = _preview.parent as RectTransform;
            float maxWidth = Mathf.Max(120f, workspace.rect.width - 56f);
            float maxHeight = Mathf.Max(100f, workspace.rect.height - 48f);
            float scale = Mathf.Min(maxWidth / texture!.width, maxHeight / texture.height);
            _preview.sizeDelta = new Vector2(texture.width * scale, texture.height * scale);
        } else {
            _preview.sizeDelta = new Vector2(520f, 340f);
            _hint.text = _options.HintEmpty;
        }

        _overlay.sizeDelta = _preview.sizeDelta;
        _overlay.SetAsLastSibling();
        SetGuideActive(_guideLeft, hasTexture);
        SetGuideActive(_guideRight, hasTexture);
        SetGuideActive(_guideBottom, hasTexture);
        SetGuideActive(_guideTop, hasTexture);
        UpdateInputs();
        if (hasTexture) {
            Canvas.ForceUpdateCanvases();
            UpdateGuides();
        }
    }

    private static void SetGuideActive(RectTransform guide, bool active) {
        if (guide) {
            guide.gameObject.SetActive(active);
        }
    }

    private void UpdateGuides() {
        if (_texture == null || _overlay == null) {
            return;
        }

        float width = _overlay.rect.width;
        float height = _overlay.rect.height;
        float scaleX = width / _texture.width;
        float scaleY = height / _texture.height;
        float inset = 3f;
        float left = Mathf.Clamp(_border.x * scaleX, inset, width - inset);
        float right = Mathf.Clamp(_border.z * scaleX, inset, width - inset);
        float bottom = Mathf.Clamp(_border.y * scaleY, inset, height - inset);
        float top = Mathf.Clamp(_border.w * scaleY, inset, height - inset);
        _guideLeft.anchoredPosition = new Vector2(left, 0f);
        _guideRight.anchoredPosition = new Vector2(-right, 0f);
        _guideBottom.anchoredPosition = new Vector2(0f, bottom);
        _guideTop.anchoredPosition = new Vector2(0f, -top);
        UpdateInputs();
        UpdateHint();
    }

    private void UpdateHint() {
        if (_hint == null || _texture == null) {
            return;
        }

        _hint.text = string.Format(_options.ValuesFormat,
            Mathf.RoundToInt(_border.x),
            Mathf.RoundToInt(_border.z),
            Mathf.RoundToInt(_border.y),
            Mathf.RoundToInt(_border.w)
        );
    }

    private void DragGuide(O5SpriteGuide guide, BaseEventData data) {
        PointerEventData? pointer =
#if IL2CPP
            data.TryCast<PointerEventData>();
#else
            data as PointerEventData;
#endif
        if (pointer == null || pointer.button != PointerEventData.InputButton.Left ||
            _texture == null || _preview == null ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _preview,
                pointer.position,
                pointer.pressEventCamera,
                out Vector2 local
            )) {
            return;
        }

        float width = _preview.rect.width;
        float height = _preview.rect.height;
        if (width <= 0f || height <= 0f) {
            return;
        }

        float x = Mathf.Round(Mathf.Clamp(local.x + width * 0.5f, 0f, width) / width * _texture.width);
        float y = Mathf.Round(Mathf.Clamp(local.y + height * 0.5f, 0f, height) / height * _texture.height);
        switch (guide) {
            case O5SpriteGuide.Left:
                _border.x = Mathf.Clamp(x, 0f, _texture.width - _border.z);
                break;
            case O5SpriteGuide.Right:
                _border.z = Mathf.Clamp(_texture.width - x, 0f, _texture.width - _border.x);
                break;
            case O5SpriteGuide.Bottom:
                _border.y = Mathf.Clamp(y, 0f, _texture.height - _border.w);
                break;
            case O5SpriteGuide.Top:
                _border.w = Mathf.Clamp(_texture.height - y, 0f, _texture.height - _border.y);
                break;
        }

        UpdateGuides();
    }

    /// <summary>Clamps a border into a valid range for a texture size.</summary>
    /// <param name="border">Border in pixels (x=left, y=bottom, z=right, w=top).</param>
    /// <param name="width">Texture width.</param>
    /// <param name="height">Texture height.</param>
    public static Vector4 NormalizeBorder(Vector4 border, int width, int height) {
        float left = Mathf.Clamp(border.x, 0f, width);
        float right = Mathf.Clamp(border.z, 0f, width - left);
        float bottom = Mathf.Clamp(border.y, 0f, height);
        float top = Mathf.Clamp(border.w, 0f, height - bottom);
        return new Vector4(left, bottom, right, top);
    }

    /// <inheritdoc/>
    public override void Dispose() {
        if (IsDisposed) {
            return;
        }

        foreach (var slider in _sliders) {
            slider.Dispose();
        }

        _sliders.Clear();
        _window.Dispose();
        if (_blocker) {
            UnityEngine.Object.Destroy(_blocker);
        }

        base.Dispose();
    }
}
