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
    /// <summary>Layout row with a fixed height for stacking controls.</summary>
    /// <param name="parent">Parent transform.</param>
    /// <param name="height">Row height.</param>
    public static RectTransform Row(Transform parent, float height = 50f) {
        GameObject obj = new("Row");
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();

        LayoutElement le = obj.AddComponent<LayoutElement>();
        le.preferredHeight = height;
        le.minHeight = height;

        return rect;
    }

    /// <summary>Small dot marking a control whose value differs from default.</summary>
    /// <param name="parent">Control rect.</param>
    public static GameObject AddSmallChangedCircle(RectTransform parent) {
        GameObject obj = new("Changed");
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(6f, -6f);
        rect.sizeDelta = new Vector2(8f, 8f);

        Image img = obj.AddComponent<Image>();
        img.sprite = O5Boot.Sprites.Circle;
        Color c = O5Boot.Theme.ObjectActive;
        c.a = 0f;
        img.color = c;

        return obj;
    }

    /// <summary>Builds a labeled toggle. Middle-click resets to default when enabled in config.</summary>
    /// <param name="parent">Parent transform.</param>
    /// <param name="defaultValue">Reset target.</param>
    /// <param name="value">Initial value.</param>
    /// <param name="onChanged">Change callback.</param>
    /// <param name="text">Label text.</param>
    /// <param name="id">Stable identifier.</param>
    public static O5Toggle Toggle(
        Transform parent,
        bool defaultValue,
        bool value,
        Action<bool>? onChanged,
        string text,
        string id
    ) {
        RectTransform rect = ControlBackground(parent);
        rect.SetParent(parent, false);

        TMPro.TextMeshProUGUI tmp = ControlText(rect, O5Boot.Theme.FontSizeBody);
        tmp.text = text;

        GameObject change = AddSmallChangedCircle(rect);
        Image changeImg = change.GetComponent<Image>();

        GameObject toggleCircle = new("ToggleCircle");
        toggleCircle.transform.SetParent(rect, false);

        RectTransform circleRect = toggleCircle.AddComponent<RectTransform>();
        circleRect.anchorMin = new Vector2(1f, 0.5f);
        circleRect.anchorMax = new Vector2(1f, 0.5f);
        circleRect.pivot = new Vector2(0.5f, 0.5f);
        circleRect.anchoredPosition = new Vector2(-23f, 0f);
        circleRect.sizeDelta = new Vector2(26f, 26f);

        Image circleImage = toggleCircle.AddComponent<Image>();

        O5Toggle toggle = new(
            id,
            rect,
            tmp,
            circleImage,
            circleRect,
            changeImg,
            defaultValue,
            value,
            onChanged
        );

        var trigger = rect.gameObject.AddComponent<EventTrigger>();
        O5Effects.HoverOutline(rect.gameObject, trigger);

        var ovent = rect.gameObject.AddComponent<OventHandler>();
        ovent.OnClick += btn => {
            switch (btn) {
                case InputButton.Left:
                    toggle.Toggle();
                    break;

                case InputButton.Middle:
                    if (O5Boot.Config.MiddleClickToDefault && toggle.Value != toggle.DefaultValue) {
                        toggle.Reset();
                    }

                    break;
            }
        };

        return toggle;
    }

    /// <summary>Attaches a static hover tooltip.</summary>
    /// <param name="parent">Hover target.</param>
    /// <param name="tip">Tooltip text.</param>
    public static Transform AddToolTip(this Transform parent, string tip)
        => parent.AddToolTipInternal(() => tip);

    /// <summary>Attaches a dynamic hover tooltip.</summary>
    /// <param name="parent">Hover target.</param>
    /// <param name="getText">Text provider evaluated on hover.</param>
    public static Transform AddToolTip(this Transform parent, Func<string> getText)
        => parent.AddToolTipInternal(getText);

    private static Transform AddToolTipInternal(this Transform parent, Func<string> getText) {
        EventTrigger trigger = parent.gameObject.GetComponent<EventTrigger>()
            ?? parent.gameObject.AddComponent<EventTrigger>();

        UnityUtils.AddEvents(trigger,
            (EventTriggerType.PointerEnter, () => O5Tooltip.Show(getText())),
            (EventTriggerType.PointerExit, O5Tooltip.Hide)
        );

        return parent;
    }
}
