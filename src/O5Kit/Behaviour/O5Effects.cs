// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using O5Kit.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace O5Kit.Behaviour;

/// <summary>Shared hover-outline effect for rows and buttons.</summary>
public static class O5Effects {
    /// <summary>Adds an outline that switches between idle and hover colors. Returns the outline image.</summary>
    /// <param name="ctx">Owning kit context.</param>
    /// <param name="obj">Control root to attach under.</param>
    /// <param name="trigger">Event trigger receiving hover events.</param>
    public static Image HoverOutline(O5Context ctx, GameObject obj, EventTrigger trigger)
        => HoverOutline(ctx, obj, trigger, null);

    /// <summary>Adds an outline that switches between idle and dynamic hover colors. Returns the outline image.</summary>
    /// <param name="ctx">Owning kit context.</param>
    /// <param name="obj">Control root to attach under.</param>
    /// <param name="trigger">Event trigger receiving hover events.</param>
    /// <param name="hoverColor">Optional dynamic hover color provider; null uses theme hover outline.</param>
    public static Image HoverOutline(O5Context ctx, GameObject obj, EventTrigger trigger, Func<Color>? hoverColor) {
        ITweenHandle? hoverTween = null;

        var hover = new GameObject("Hover");
        hover.transform.SetParent(obj.transform, false);
        hover.transform.SetAsFirstSibling();

        var hoverRect = hover.AddComponent<RectTransform>();
        hoverRect.anchorMin = Vector2.zero;
        hoverRect.anchorMax = Vector2.one;
        hoverRect.pivot = new Vector2(0.5f, 0.5f);
        hoverRect.offsetMin = Vector2.zero;
        hoverRect.offsetMax = Vector2.zero;

        var hoverImage = hover.AddComponent<Image>();
        hoverImage.sprite = ctx.Sprites.RoundedOutline;
        hoverImage.type = Image.Type.Sliced;

        hoverImage.color = ctx.Theme.ControlOutlineIdle;

        UnityUtils.AddEvents(trigger,
            (EventTriggerType.PointerEnter, () => {
                hoverTween?.Kill();
                var img = hoverImage;
                Color start = img ? img.color : ctx.Theme.ControlOutlineIdle;
                hoverTween = ctx.Tween.TweenFloat(
                    () => 0f,
                    t => {
                        if (img) {
                            Color target = hoverColor != null ? hoverColor() : ctx.Theme.ControlOutlineHover;
                            img.color = Color.Lerp(start, target, t);
                        }
                    }, 1f, 0.1f);
            }
        ),
            (EventTriggerType.PointerExit, () => {
                hoverTween?.Kill();
                var img = hoverImage;
                Color start = img ? img.color : ctx.Theme.ControlOutlineIdle;
                hoverTween = ctx.Tween.TweenFloat(
                    () => 0f,
                    t => {
                        if (img) {
                            img.color = Color.Lerp(start, ctx.Theme.ControlOutlineIdle, t);
                        }
                    }, 1f, 0.1f);
            }
        )
        );

        return hoverImage;
    }
}
