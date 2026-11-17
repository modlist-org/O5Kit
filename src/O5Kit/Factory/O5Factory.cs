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
    /// <param name="ctx">Owning kit context.</param>
    /// <param name="parent">Parent transform.</param>
    /// <param name="height">Row height. Defaults to the theme control height.</param>
    public static RectTransform ControlBackground(O5Context ctx, Transform parent, float? height = null) {
        var theme = ctx.Theme;
        var obj = new GameObject("Bg");
        obj.transform.SetParent(parent, false);

        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var layout = obj.AddComponent<LayoutElement>();
        float heightPx = height ?? theme.ControlHeightFor(theme.FontSizeBody);
        layout.preferredHeight = layout.minHeight = heightPx;

        var img = obj.AddComponent<Image>();
        img.color = theme.ObjectBG;
        img.sprite = ctx.Sprites.RoundedControl;
        img.type = Image.Type.Sliced;

        return rect;
    }

    /// <summary>Themed body text with left padding.</summary>
    /// <param name="ctx">Owning kit context.</param>
    /// <param name="parent">Parent transform.</param>
    /// <param name="fontSize">Font size.</param>
    /// <param name="noPad">Remove the left padding.</param>
    public static TMPro.TextMeshProUGUI ControlText(O5Context ctx, Transform parent, float fontSize, bool noPad = false) {
        var obj = new GameObject("Text");
        obj.transform.SetParent(parent, false);

        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(noPad ? 0f : 16f, 0f);
        rect.offsetMax = Vector2.zero;

        var tmp = obj.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.font = ctx.Fonts.Regular;
        tmp.fontSize = fontSize;
        tmp.color = ctx.Theme.Text;
        tmp.alignment = TMPro.TextAlignmentOptions.Left;
        tmp.verticalAlignment = TMPro.VerticalAlignmentOptions.Middle;
        tmp.characterSpacing = -3f;

        return tmp;
    }

    /// <summary>Themed heading text without padding.</summary>
    /// <param name="ctx">Owning kit context.</param>
    /// <param name="parent">Parent transform.</param>
    public static TMPro.TextMeshProUGUI ControlTextH1(O5Context ctx, Transform parent) {
        var tmp = ControlText(ctx, parent, ctx.Theme.FontSizeH1, true);
        tmp.font = ctx.Fonts.Medium;
        return tmp;
    }
    /// <summary>Builds a button. Set <see cref="O5ButtonOptions.Text"/> for a text button or <see cref="O5ButtonOptions.Icon"/> for an icon button.</summary>
    /// <param name="ctx">Owning kit context.</param>
    /// <param name="parent">Parent transform.</param>
    /// <param name="id">Stable identifier.</param>
    /// <param name="options">Behaviour. Null uses defaults.</param>
    public static O5Button Button(O5Context ctx, Transform parent, string id, O5ButtonOptions? options = null) {
        options ??= new O5ButtonOptions();
        var theme = ctx.Theme;
        if (options.Icon != null) {
            return BuildIconButton(ctx, parent, id, options);
        }

        var rect = ControlBackground(ctx, parent, options.Height);
        rect.gameObject.name = "O5Button";

        var bg = rect.GetComponent<Image>();
        bg.color = theme.ObjectButton;

        var tmp = ControlText(ctx, rect, theme.FontSizeBody);
        tmp.text = options.Text ?? string.Empty;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.rectTransform.offsetMin = Vector2.zero;
        tmp.rectTransform.offsetMax = Vector2.zero;

        var button = new O5Button(ctx, id, rect, tmp, bg, options);

        var trigger = rect.gameObject.AddComponent<EventTrigger>();
        O5Effects.HoverOutline(ctx, rect.gameObject, trigger);
        UnityUtils.AddEvents(trigger,
            (EventTriggerType.PointerEnter, _ => button.OnHoverEnter()),
            (EventTriggerType.PointerExit, _ => button.OnHoverExit()),
            (EventTriggerType.PointerDown, e => {
                if (IsLeftPress(e)) {
                    button.OnPressEnter();
                }
            }),
            (EventTriggerType.PointerUp, _ => button.OnPressExit())
        );

        var ovent = rect.gameObject.AddComponent<OventHandler>();
        ovent.OnClick += btn => {
            if (btn == PointerEventData.InputButton.Left) {
                button.Press();
            }
        };

        return button;
    }

    /// <summary>Builds a button under <paramref name="parent"/> with inline configuration.</summary>
    /// <param name="ctx">Owning kit context.</param>
    /// <param name="parent">Parent transform.</param>
    /// <param name="id">Stable identifier.</param>
    /// <param name="configure">Mutates a fresh <see cref="O5ButtonOptions"/>.</param>
    public static O5Button Button(O5Context ctx, Transform parent, string id, Action<O5ButtonOptions> configure) {
        var options = new O5ButtonOptions();
        configure?.Invoke(options);
        return Button(ctx, parent, id, options);
    }

    private static O5Button BuildIconButton(O5Context ctx, Transform parent, string id, O5ButtonOptions options) {
        var theme = ctx.Theme;
        var rect = ControlBackground(ctx, parent, options.Height);
        rect.gameObject.name = "O5IconButton";

        var iconObj = new GameObject("Icon");
        var iconRect = iconObj.AddComponent<RectTransform>();
        var iconImage = iconObj.AddComponent<Image>();

        iconRect.SetParent(rect, false);
        iconImage.sprite = options.Icon;
        iconImage.raycastTarget = false;
        iconImage.preserveAspect = true;

        iconRect.anchorMin = Vector2.zero;
        iconRect.anchorMax = Vector2.one;
        iconRect.offsetMin = new Vector2(options.IconPadding, options.IconPadding);
        iconRect.offsetMax = new Vector2(-options.IconPadding, -options.IconPadding);

        var bg = rect.GetComponent<Image>();
        bg.color = theme.ObjectButton;

        var button = new O5Button(ctx, id, rect, iconImage, bg, options);

        var trigger = rect.gameObject.AddComponent<EventTrigger>();
        O5Effects.HoverOutline(ctx, rect.gameObject, trigger);

        var ovent = rect.gameObject.AddComponent<OventHandler>();
        ovent.OnClick += btn => {
            if (btn == PointerEventData.InputButton.Left) {
                button.Press();
            }
        };

        UnityUtils.AddEvents(trigger,
            (EventTriggerType.PointerEnter, _ => button.OnHoverEnter()),
            (EventTriggerType.PointerExit, _ => button.OnHoverExit()),
            (EventTriggerType.PointerDown, e => {
                if (IsLeftPress(e)) {
                    button.OnPressEnter();
                }
            }),
            (EventTriggerType.PointerUp, _ => button.OnPressExit())
        );

        return button;
    }

    private static bool IsLeftPress(BaseEventData e) {
        PointerEventData? ped = e as PointerEventData;
        return ped != null && ped.button == PointerEventData.InputButton.Left;
    }
}
