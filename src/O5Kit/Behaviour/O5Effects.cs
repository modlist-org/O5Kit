// SPDX-License-Identifier: LGPL-3.0-or-later

using O5Kit.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace O5Kit.Behaviour;

/// <summary>Shared hover-outline effect for rows and buttons.</summary>
public static class O5Effects {
    /// <summary>Adds an outline that fades in on hover and out on exit. Returns the outline image.</summary>
    /// <param name="obj">Control root to attach under.</param>
    /// <param name="trigger">Event trigger receiving hover events.</param>
    public static Image HoverOutline(GameObject obj, EventTrigger trigger) {
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
        hoverImage.sprite = O5Boot.Sprites.RoundedOutline;
        hoverImage.type = Image.Type.Sliced;

        Color baseColor = O5Boot.Theme.ObjectActive;
        hoverImage.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);

        UnityUtils.AddEvents(trigger,
            (EventTriggerType.PointerEnter, () => {
                hoverTween?.Kill();
                var img = hoverImage;
                hoverTween = O5Boot.Tween.TweenFloat(
                    () => img ? img.color.a : 0f,
                    v => {
                        if (img) {
                            Color c = img.color;
                            c.a = v;
                            img.color = c;
                        }
                    },
                    1f, 0.1f);
            }
        ),
            (EventTriggerType.PointerExit, () => {
                hoverTween?.Kill();
                var img = hoverImage;
                hoverTween = O5Boot.Tween.TweenFloat(
                    () => img ? img.color.a : 0f,
                    v => {
                        if (img) {
                            Color c = img.color;
                            c.a = v;
                            img.color = c;
                        }
                    },
                    0f, 0.1f);
            }
        )
        );

        return hoverImage;
    }
}
