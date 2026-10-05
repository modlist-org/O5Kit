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
    /// <param name="ctx">Owning kit context.</param>
    /// <param name="parent">Parent transform.</param>
    /// <param name="height">Row height. Null derives from the theme body font size.</param>
    public static RectTransform Row(O5Context ctx, Transform parent, float? height = null) {
        GameObject obj = new("Row");
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();

        float heightPx = height ?? ctx.Theme.ControlHeightFor(ctx.Theme.FontSizeBody);
        LayoutElement le = obj.AddComponent<LayoutElement>();
        le.preferredHeight = heightPx;
        le.minHeight = heightPx;

        return rect;
    }

    /// <summary>Small dot marking a control whose value differs from default.</summary>
    /// <param name="ctx">Owning kit context.</param>
    /// <param name="parent">Control rect.</param>
    public static GameObject AddSmallChangedCircle(O5Context ctx, RectTransform parent) {
        GameObject obj = new("Changed");
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(6f, -6f);
        rect.sizeDelta = new Vector2(8f, 8f);

        Image img = obj.AddComponent<Image>();
        img.sprite = ctx.Sprites.Circle;
        img.color = O5Palette.Transparent(ctx.Theme.ObjectActive);

        return obj;
    }

    /// <summary>Builds a labeled toggle. Middle-click resets to default when enabled in config.</summary>
    /// <param name="ctx">Owning kit context.</param>
    /// <param name="parent">Parent transform.</param>
    /// <param name="defaultValue">Reset target.</param>
    /// <param name="value">Initial value.</param>
    /// <param name="onChanged">Change callback.</param>
    /// <param name="text">Label text.</param>
    /// <param name="id">Stable identifier.</param>
    public static O5Toggle Toggle(
        O5Context ctx,
        Transform parent,
        bool? defaultValue,
        bool value,
        Action<bool>? onChanged,
        string text,
        string id
    ) {
        RectTransform rect = ControlBackground(ctx, parent);
        rect.SetParent(parent, false);

        TMPro.TextMeshProUGUI tmp = ControlText(ctx, rect, ctx.Theme.FontSizeBody);
        tmp.text = text;

        GameObject change = AddSmallChangedCircle(ctx, rect);
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
            ctx,
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
        O5Effects.HoverOutline(ctx, rect.gameObject, trigger);

        var ovent = rect.gameObject.AddComponent<OventHandler>();
        ovent.OnClick += btn => {
            switch (btn) {
                case InputButton.Left:
                    toggle.Toggle();
                    break;

                case InputButton.Middle:
                    if (ctx.Config.MiddleClickToDefault && toggle.DefaultValue.HasValue &&
                        toggle.Value != toggle.DefaultValue.Value) {
                        toggle.Reset();
                    }

                    break;
            }
        };

        return toggle;
    }

    /// <summary>Attaches a static hover tooltip.</summary>
    /// <param name="parent">Hover target.</param>
    /// <param name="ctx">Owning kit context.</param>
    /// <param name="tip">Tooltip text.</param>
    public static Transform AddToolTip(this Transform parent, O5Context ctx, string tip)
        => AddToolTipInternal(parent, ctx, () => tip);

    /// <summary>Attaches a dynamic hover tooltip.</summary>
    /// <param name="parent">Hover target.</param>
    /// <param name="ctx">Owning kit context.</param>
    /// <param name="getText">Text provider evaluated on hover.</param>
    public static Transform AddToolTip(this Transform parent, O5Context ctx, Func<string> getText)
        => AddToolTipInternal(parent, ctx, getText);

    /// <param name="ctx">Owning kit context.</param>
    private static Transform AddToolTipInternal(Transform parent, O5Context ctx, Func<string> getText) {
        EventTrigger trigger = parent.gameObject.GetComponent<EventTrigger>()
            ?? parent.gameObject.AddComponent<EventTrigger>();

        UnityUtils.AddEvents(trigger,
            (EventTriggerType.PointerEnter, () => ctx.Tooltip.Show(getText())),
            (EventTriggerType.PointerExit, ctx.Tooltip.Hide)
        );

        return parent;
    }
}
