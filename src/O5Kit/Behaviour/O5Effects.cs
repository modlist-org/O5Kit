// SPDX-License-Identifier: LGPL-3.0-or-later

using O5Kit.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace O5Kit.Behaviour;

/// <summary>Shared hover-outline effect for rows and buttons.</summary>
public static class O5Effects {
    /// <summary>Adds a resting outline plus a hover ring that fades in on hover and out on exit. Returns the hover-ring image.</summary>
    /// <param name="ctx">Owning kit context.</param>
    /// <param name="obj">Control root to attach under.</param>
    /// <param name="trigger">Event trigger receiving hover events.</param>
    /// <param name="restingOutline">True to add a separate resting ring. Disabled by default.</param>
    public static Image HoverOutline(O5Context ctx, GameObject obj, EventTrigger trigger, bool restingOutline = false) {
        ITweenHandle? hoverTween = null;

        if (restingOutline) {
            var resting = new GameObject("Outline");
            resting.transform.SetParent(obj.transform, false);
            resting.transform.SetAsFirstSibling();

            var restingRect = resting.AddComponent<RectTransform>();
            restingRect.anchorMin = Vector2.zero;
            restingRect.anchorMax = Vector2.one;
            restingRect.pivot = new Vector2(0.5f, 0.5f);
            restingRect.offsetMin = Vector2.zero;
            restingRect.offsetMax = Vector2.zero;

            var restingImage = resting.AddComponent<Image>();
            restingImage.sprite = ctx.Sprites.RoundedOutline;
            restingImage.type = Image.Type.Sliced;
            restingImage.color = ctx.Theme.ControlOutline;
            restingImage.raycastTarget = false;
        }

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

        Color baseColor = ctx.Theme.ObjectActive;
        Color idleColor = O5Palette.WithAlpha(Color.white, 0f);
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
                    }, baseColor, 0.1f);
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
