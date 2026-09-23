// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using O5Kit.Behaviour;
using O5Kit.Core;
using O5Kit.Control;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace O5Kit.Factory;

public static partial class O5Factory {
    /// <summary>Full-width themed row background with a fixed height.</summary>
    /// <param name="parent">Parent transform.</param>
    /// <param name="height">Row height. Defaults to the theme control height.</param>
    public static RectTransform ControlBackground(Transform parent, float? height = null) {
        var theme = O5Boot.Theme;
        var obj = new GameObject("Bg");
        obj.transform.SetParent(parent, false);

        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var layout = obj.AddComponent<LayoutElement>();
        layout.preferredHeight = layout.minHeight = height ?? theme.ControlHeight;

        var img = obj.AddComponent<Image>();
        img.color = theme.ObjectBG;
        img.sprite = O5Boot.Sprites.RoundedControl;
        img.type = Image.Type.Sliced;

        return rect;
    }

    /// <summary>Themed body text with left padding.</summary>
    /// <param name="parent">Parent transform.</param>
    /// <param name="fontSize">Font size.</param>
    /// <param name="noPad">Remove the left padding.</param>
    public static TMPro.TextMeshProUGUI ControlText(Transform parent, float fontSize, bool noPad = false) {
        var obj = new GameObject("Text");
        obj.transform.SetParent(parent, false);

        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(noPad ? 0f : 16f, 0f);
        rect.offsetMax = Vector2.zero;

        var tmp = obj.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.font = O5Boot.Fonts.Regular;
        tmp.fontSize = fontSize;
        tmp.color = Color.white;
        tmp.alignment = TMPro.TextAlignmentOptions.Left;
        tmp.verticalAlignment = TMPro.VerticalAlignmentOptions.Middle;
        tmp.characterSpacing = -3f;

        return tmp;
    }

    /// <summary>Themed heading text without padding.</summary>
    /// <param name="parent">Parent transform.</param>
    public static TMPro.TextMeshProUGUI ControlTextH1(Transform parent) {
        var tmp = ControlText(parent, O5Boot.Theme.FontSizeH1, true);
        tmp.font = O5Boot.Fonts.Medium;
        return tmp;
    }
    /// <summary>Builds a centered text button.</summary>
    /// <param name="parent">Parent transform.</param>
    /// <param name="onClick">Click callback.</param>
    /// <param name="text">Label text.</param>
    /// <param name="id">Stable identifier.</param>
    public static O5Button Button(Transform parent, Action? onClick, string text, string id) {
        var theme = O5Boot.Theme;
        var rect = ControlBackground(parent);
        rect.gameObject.name = "O5Button";

        var bg = rect.GetComponent<Image>();
        bg.color = theme.ObjectButton;

        var tmp = ControlText(rect, theme.FontSizeBody);
        tmp.text = text;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.rectTransform.offsetMin = Vector2.zero;
        tmp.rectTransform.offsetMax = Vector2.zero;

        var button = new O5Button(id, rect, tmp, bg, onClick);

        var trigger = rect.gameObject.AddComponent<EventTrigger>();
        UnityUtils.AddEvents(trigger,
            (EventTriggerType.PointerEnter, button.OnHoverEnter),
            (EventTriggerType.PointerExit, button.OnHoverExit)
        );

        var ovent = rect.gameObject.AddComponent<OventHandler>();
        ovent.OnClick += btn => {
            if (btn == PointerEventData.InputButton.Left) {
                button.Click();
            }
        };

        return button;
    }

    /// <summary>Builds an icon button.</summary>
    /// <param name="parent">Parent transform.</param>
    /// <param name="onClick">Click callback.</param>
    /// <param name="iconSprite">Icon sprite.</param>
    /// <param name="id">Stable identifier.</param>
    /// <param name="iconPadding">Icon inset.</param>
    public static O5Button Button(Transform parent, Action? onClick, Sprite? iconSprite, string id, float iconPadding = 5f) {
        var theme = O5Boot.Theme;
        var rect = ControlBackground(parent);
        rect.gameObject.name = "O5IconButton";

        var iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        var iconRect = iconObj.GetComponent<RectTransform>();
        var iconImage = iconObj.GetComponent<Image>();

        iconRect.SetParent(rect, false);
        iconImage.sprite = iconSprite;
        iconImage.raycastTarget = false;
        iconImage.preserveAspect = true;

        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(iconPadding, iconPadding);
        iconRect.offsetMax = new Vector2(-iconPadding, -iconPadding);

        var bg = rect.GetComponent<Image>();
        bg.color = theme.ObjectButton;

        var button = new O5Button(id, rect, iconImage, bg, onClick);

        var trigger = rect.gameObject.AddComponent<EventTrigger>();
        O5Effects.HoverOutline(rect.gameObject, trigger);

        var ovent = rect.gameObject.AddComponent<OventHandler>();
        ovent.OnClick += btn => {
            if (btn == PointerEventData.InputButton.Left) {
                button.Click();
            }
        };

        UnityUtils.AddEvents(trigger,
            (EventTriggerType.PointerEnter, button.OnHoverEnter),
            (EventTriggerType.PointerExit, button.OnHoverExit)
        );

        return button;
    }
}
