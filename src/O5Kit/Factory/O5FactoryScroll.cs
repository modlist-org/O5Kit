// SPDX-License-Identifier: LGPL-3.0-or-later

using O5Kit.Behaviour;
using O5Kit.Core;
using UnityEngine;
using UnityEngine.UI;

namespace O5Kit.Factory;

public static partial class O5Factory {
    /// <summary>Builds an Overlayer-style scroll view: full-stretch viewport (hit target + clip)
    /// with a top-anchored vertical content stack and a smooth wheel/right-drag scroller.
    /// Content taller than the viewport scrolls; the viewport clips it.</summary>
    /// <param name="parent">Parent transform (e.g. a window's content area).</param>
    /// <param name="spacing">Vertical gap between stacked rows.</param>
    /// <param name="padding">Viewport inset on all sides. Ignored when explicit offsets are given.</param>
    /// <param name="expandLayout">True when the view lives in a parent layout group: the root takes remaining height instead of stretching.</param>
    /// <param name="viewportOffsetMin">Explicit viewport offsets. Null derives from <paramref name="padding"/>.</param>
    /// <param name="viewportOffsetMax">Explicit viewport offsets. Null derives from <paramref name="padding"/>.</param>
    /// <param name="contentPadding">Content stack padding. Null leaves the default.</param>
    /// <returns>Viewport, content (fill this), and the scroll controller.</returns>
    /// <param name="ctx">Owning kit context (assigned to the scroll controller).</param>
    public static (RectTransform viewport, RectTransform content, UIScrollController scroller) ScrollView(
        O5Context ctx,
        Transform parent,
        float spacing = 12f,
        float padding = 0f,
        bool expandLayout = false,
        Vector2? viewportOffsetMin = null,
        Vector2? viewportOffsetMax = null,
        RectOffset? contentPadding = null
    ) {
        GameObject root = new("ScrollView");
        root.transform.SetParent(parent, false);

        RectTransform rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        if (expandLayout) {
            var rootLayout = root.AddComponent<LayoutElement>();
            rootLayout.flexibleHeight = 1f;
        }

        GameObject viewportObj = new("Viewport");
        viewportObj.transform.SetParent(root.transform, false);

        RectTransform viewportRect = viewportObj.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.pivot = new Vector2(0.5f, 0.5f);
        viewportRect.offsetMin = viewportOffsetMin ?? new Vector2(padding, padding);
        viewportRect.offsetMax = viewportOffsetMax ?? new Vector2(-padding, -padding);

        viewportObj.AddComponent<EmptyGraphic>().raycastTarget = true;
        viewportObj.AddComponent<RectMask2D>();

        GameObject contentObj = new("Content");
        contentObj.transform.SetParent(viewportObj.transform, false);

        RectTransform contentRect = contentObj.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        VerticalLayoutGroup layout = contentObj.AddComponent<VerticalLayoutGroup>();
        layout.spacing = spacing;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        if (contentPadding != null) {
            layout.padding = contentPadding;
        }

        ContentSizeFitter fitter = contentObj.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        UIScrollController scroller = root.AddComponent<UIScrollController>();
        scroller.Ctx = ctx;
        scroller.SetContent(contentRect, viewportRect);

        return (viewportRect, contentRect, scroller);
    }
}
