// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using O5Kit.Control;
using O5Kit.Core;
using UnityEngine;
using UnityEngine.UI;

namespace O5Kit.Factory;

public static partial class O5Factory {
    /// <summary>Builds a compact single-select segmented control. Owns all GameObjects; the caller only passes behaviour.</summary>
    /// <param name="ctx">Owning kit context.</param>
    /// <param name="parent">Parent transform.</param>
    /// <param name="id">Stable identifier.</param>
    /// <param name="options">Behaviour. Null uses defaults.</param>
    public static O5Segmented<T> Segmented<T>(
        O5Context ctx,
        Transform parent,
        string id,
        O5SegmentedOptions<T>? options = null
    ) {
        options ??= new O5SegmentedOptions<T>();
        var theme = ctx.Theme;
        float fontSize = options.FontSize ?? theme.FontSizeBody;
        float h = options.Height ?? theme.ControlHeightFor(fontSize);
        Color normalColor = options.NormalColor ?? theme.ObjectButton;
        Color borderColor = options.BorderColor ?? theme.ControlOutlineIdle;

        GameObject controlObj = new($"SegmentedControl_{id}");
        controlObj.transform.SetParent(parent, false);

        RectTransform rect = controlObj.AddComponent<RectTransform>();
        LayoutElement le = controlObj.AddComponent<LayoutElement>();
        le.minHeight = h;
        le.preferredHeight = h;
        le.flexibleWidth = 1f;

        GameObject maskObj = new("MaskContainer");
        maskObj.transform.SetParent(rect, false);

        RectTransform maskRect = maskObj.AddComponent<RectTransform>();
        maskRect.anchorMin = Vector2.zero;
        maskRect.anchorMax = Vector2.one;
        maskRect.offsetMin = Vector2.zero;
        maskRect.offsetMax = Vector2.zero;

        Image maskImg = maskObj.AddComponent<Image>();
        maskImg.sprite = ctx.Sprites.RoundedControl;
        maskImg.type = Image.Type.Sliced;
        maskImg.color = normalColor;

        Mask mask = maskObj.AddComponent<Mask>();
        mask.showMaskGraphic = true;

        HorizontalLayoutGroup layout = maskObj.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.spacing = 0f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        GameObject outlineObj = new("Outline");
        outlineObj.transform.SetParent(rect, false);

        RectTransform outlineRect = outlineObj.AddComponent<RectTransform>();
        outlineRect.anchorMin = Vector2.zero;
        outlineRect.anchorMax = Vector2.one;
        outlineRect.offsetMin = Vector2.zero;
        outlineRect.offsetMax = Vector2.zero;

        Image outlineImg = outlineObj.AddComponent<Image>();
        outlineImg.sprite = ctx.Sprites.RoundedOutline;
        outlineImg.type = Image.Type.Sliced;
        outlineImg.color = borderColor;
        outlineImg.raycastTarget = false;

        GameObject change = AddSmallChangedCircle(ctx, rect);
        Image changeImg = change.GetComponent<Image>();

        return new O5Segmented<T>(ctx, id, rect, maskRect, outlineImg, changeImg, fontSize, options);
    }

    /// <summary>Builds a segmented control with inline configuration.</summary>
    /// <param name="ctx">Owning kit context.</param>
    /// <param name="parent">Parent transform.</param>
    /// <param name="id">Stable identifier.</param>
    /// <param name="configure">Mutates a fresh <see cref="O5SegmentedOptions{T}"/>.</param>
    public static O5Segmented<T> Segmented<T>(
        O5Context ctx,
        Transform parent,
        string id,
        Action<O5SegmentedOptions<T>> configure
    ) {
        var options = new O5SegmentedOptions<T>();
        configure?.Invoke(options);
        return Segmented(ctx, parent, id, options);
    }
}
