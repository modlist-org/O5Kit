// SPDX-License-Identifier: LGPL-3.0-or-later

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
    public static Image HoverOutline(O5Context ctx, GameObject obj, EventTrigger trigger) {
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

        Color idleColor = ctx.Theme.ControlOutlineIdle;
        Color hoverColor = ctx.Theme.ControlOutlineHover;
        hoverImage.color = idleColor;

        UnityUtils.AddEvents(trigger,
            (EventTriggerType.PointerEnter, () => {
                hoverTween?.Kill();
                var img = hoverImage;
                hoverTween = ctx.Tween.TweenColor(() => img ? img.color : idleColor,
                    v => {
                        if (img) {
                            img.color = v;
                        }
                    }, hoverColor, 0.1f);
            }
        ),
            (EventTriggerType.PointerExit, () => {
                hoverTween?.Kill();
                var img = hoverImage;
                hoverTween = ctx.Tween.TweenColor(() => img ? img.color : idleColor,
                    v => {
                        if (img) {
                            img.color = v;
                        }
                    }, idleColor, 0.1f);
            }
        )
        );

        return hoverImage;
    }
}
