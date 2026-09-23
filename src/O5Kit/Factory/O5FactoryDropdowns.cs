// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Collections.Generic;
using O5Kit.Behaviour;
using O5Kit.Control;
using O5Kit.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UnityEngine.EventSystems.PointerEventData;

namespace O5Kit.Factory;

public static partial class O5Factory {
    /// <summary>Builds a single-select dropdown with an animated list.</summary>
    /// <param name="parent">Parent transform. Needs a <see cref="UnityEngine.UI.LayoutElement"/> for height animation.</param>
    /// <param name="defaultValue">Reset target.</param>
    /// <param name="value">Initial value.</param>
    /// <param name="values">Available options.</param>
    /// <param name="display">Option renderer.</param>
    /// <param name="onChanged">Change callback.</param>
    /// <param name="id">Stable identifier.</param>
    public static O5Dropdown<T> DropDown<T>(
        Transform parent,
        T? defaultValue,
        T? value,
        IReadOnlyList<T> values,
        Func<T, string> display,
        Action<T?>? onChanged,
        string id
    ) {
        GameObject root = new("Dropdown");
        root.transform.SetParent(parent, false);

        RectTransform rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 0f);
        rootRect.anchorMax = new Vector2(1f, 1f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        RectTransform rect = ControlBackground(root.transform);
        rect.pivot = new Vector2(rect.pivot.x, 1f);
        rect.anchorMin = new Vector2(rect.anchorMin.x, 1f);
        rect.anchorMax = new Vector2(rect.anchorMax.x, 1f);
        rect.sizeDelta = new Vector2(0f, 50f);
        rect.anchoredPosition = Vector2.zero;

        TMPro.TextMeshProUGUI tmp = ControlText(rect, O5Boot.Theme.FontSizeBody);
        tmp.text = value != null ? display(value) : "";
        tmp.rectTransform.offsetMax = new Vector2(-48f, 0f);
        tmp.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        tmp.overflowMode = TMPro.TextOverflowModes.Ellipsis;

        GameObject change = AddSmallChangedCircle(rect);
        Image changeImg = change.GetComponent<Image>();

        GameObject triangle = new("Triangle");
        triangle.transform.SetParent(rect, false);

        RectTransform triangleRect = triangle.AddComponent<RectTransform>();
        triangleRect.anchorMin = new Vector2(1f, 0.5f);
        triangleRect.anchorMax = new Vector2(1f, 0.5f);
        triangleRect.pivot = new Vector2(0.5f, 0.5f);
        triangleRect.anchoredPosition = new Vector2(-23f, 0f);
        triangleRect.sizeDelta = new Vector2(26f, 26f);

        Image triangleImage = triangle.AddComponent<Image>();
        triangleImage.sprite = O5Boot.Sprites.Icon("Triangle128");

        GameObject list = new("List");
        list.transform.SetParent(root.transform, false);

        RectTransform listRect = list.AddComponent<RectTransform>();
        listRect.anchorMin = new Vector2(0f, 1f);
        listRect.anchorMax = new Vector2(1f, 1f);
        listRect.pivot = new Vector2(0.5f, 1f);
        listRect.offsetMin = new Vector2(0f, -62f);
        listRect.offsetMax = new Vector2(0f, -62f);

        Image listBg = list.AddComponent<Image>();
        listBg.sprite = O5Boot.Sprites.RoundedControl;
        listBg.type = Image.Type.Sliced;
        listBg.color = O5Boot.Theme.ObjectBG;

        VerticalLayoutGroup layout = list.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 0f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = list.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        CanvasGroup listCg = list.AddComponent<CanvasGroup>();
        listCg.alpha = 0f;

        list.SetActive(false);

        O5Dropdown<T> dropdown = new(
            id,
            rootRect,
            tmp,
            triangleImage,
            triangleRect,
            changeImg,
            list,
            listRect,
            listCg,
            values,
            display,
            defaultValue,
            value,
            onChanged
        );

        LayoutElement? parentLayout = parent.GetComponent<LayoutElement>();
        List<ITweenHandle> layoutTweens = new();
        void UpdateHeight() {
            if (dropdown.IsDisposed) {
                return;
            }

            float rowHeight = 50f;
            float spacing = layout.spacing;

            float listHeight = (dropdown.Values.Count * rowHeight) + spacing;

            float targetHeight = dropdown.Expanded ? (62f + listHeight) : 50f;
            float targetAlpha = dropdown.Expanded ? 1f : 0f;

            foreach (var h in layoutTweens) {
                h.Kill();
            }

            layoutTweens.Clear();
            dropdown.LayoutSeq?.Kill();

            if (parentLayout) {
                float startHeight = parentLayout.preferredHeight;
                var pl = parentLayout;
                var rr = rootRect;
                var heightTween = O5Boot.Tween.TweenFloat(
                    () => 0f,
                    t => {
                        if (pl) {
                            pl.preferredHeight = Mathf.LerpUnclamped(startHeight, targetHeight, t);
                        }

                        if (rr) {
                            LayoutRebuilder.ForceRebuildLayoutImmediate(rr);
                        }
                    },
                    1f, 0.14f, ease: O5Ease.OutBack);
                layoutTweens.Add(heightTween);
                dropdown.LayoutSeq = heightTween;
            }

            var cg = listCg;
            float startAlpha = cg.alpha;
            layoutTweens.Add(O5Boot.Tween.TweenFloat(
                () => 0f,
                t => {
                    if (cg) {
                        cg.alpha = Mathf.LerpUnclamped(startAlpha, targetAlpha, t);
                    }
                },
                1f, 0.16f));
        }

        dropdown.OnLayoutChanged = UpdateHeight;

        var headerTrigger = rect.gameObject.AddComponent<EventTrigger>();
        O5Effects.HoverOutline(rect.gameObject, headerTrigger);

        var ovent = rect.gameObject.AddComponent<OventHandler>();
        ovent.OnClick += btn => {
            switch (btn) {
                case InputButton.Left:
                    dropdown.ToggleExpanded();
                    UpdateHeight();
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
                    break;

                case InputButton.Middle:
                    if (O5Boot.Config.MiddleClickToDefault && dropdown.DefaultValue != null &&
                        !EqualityComparer<T>.Default.Equals(dropdown.Value, dropdown.DefaultValue)
                    ) {
                        dropdown.Reset();
                    }

                    break;
            }
        };

        dropdown.RebuildList();
        UpdateHeight();
        return dropdown;
    }

    /// <summary>Builds a flag-style multi-select dropdown with checkmark rows.</summary>
    /// <param name="parent">Parent transform. Needs a <see cref="UnityEngine.UI.LayoutElement"/> for height animation.</param>
    /// <param name="defaultValue">Reset target.</param>
    /// <param name="value">Initial flags value.</param>
    /// <param name="values">Available flags.</param>
    /// <param name="display">Flag renderer.</param>
    /// <param name="summary">Combined-value renderer for the header.</param>
    /// <param name="onChanged">Change callback.</param>
    /// <param name="id">Stable identifier.</param>
    public static O5MultiDropdown<T> MultiDropDown<T>(
        Transform parent,
        T defaultValue,
        T value,
        IReadOnlyList<T> values,
        Func<T, string> display,
        Func<T, string> summary,
        Action<T>? onChanged,
        string id
    ) where T : struct, Enum {
        GameObject root = new("MultiDropdown");
        root.transform.SetParent(parent, false);

        RectTransform rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0f, 0f);
        rootRect.anchorMax = new Vector2(1f, 1f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        RectTransform rect = ControlBackground(root.transform);
        rect.pivot = new Vector2(rect.pivot.x, 1f);
        rect.anchorMin = new Vector2(rect.anchorMin.x, 1f);
        rect.anchorMax = new Vector2(rect.anchorMax.x, 1f);
        rect.sizeDelta = new Vector2(0f, 50f);
        rect.anchoredPosition = Vector2.zero;

        TMPro.TextMeshProUGUI tmp = ControlText(rect, O5Boot.Theme.FontSizeBody);
        tmp.text = summary(value);
        tmp.rectTransform.offsetMax = new Vector2(-48f, 0f);
        tmp.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        tmp.overflowMode = TMPro.TextOverflowModes.Ellipsis;

        GameObject change = AddSmallChangedCircle(rect);
        Image changeImg = change.GetComponent<Image>();

        GameObject triangle = new("Triangle");
        triangle.transform.SetParent(rect, false);

        RectTransform triangleRect = triangle.AddComponent<RectTransform>();
        triangleRect.anchorMin = new Vector2(1f, 0.5f);
        triangleRect.anchorMax = new Vector2(1f, 0.5f);
        triangleRect.pivot = new Vector2(0.5f, 0.5f);
        triangleRect.anchoredPosition = new Vector2(-23f, 0f);
        triangleRect.sizeDelta = new Vector2(26f, 26f);

        Image triangleImage = triangle.AddComponent<Image>();
        triangleImage.sprite = O5Boot.Sprites.Icon("Triangle128");

        GameObject list = new("List");
        list.transform.SetParent(root.transform, false);

        RectTransform listRect = list.AddComponent<RectTransform>();
        listRect.anchorMin = new Vector2(0f, 1f);
        listRect.anchorMax = new Vector2(1f, 1f);
        listRect.pivot = new Vector2(0.5f, 1f);
        listRect.offsetMin = new Vector2(0f, -62f);
        listRect.offsetMax = new Vector2(0f, -62f);

        Image listBg = list.AddComponent<Image>();
        listBg.sprite = O5Boot.Sprites.RoundedControl;
        listBg.type = Image.Type.Sliced;
        listBg.color = O5Boot.Theme.ObjectBG;

        VerticalLayoutGroup layout = list.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 0f;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = list.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        CanvasGroup listCg = list.AddComponent<CanvasGroup>();
        listCg.alpha = 0f;
        list.SetActive(false);

        O5MultiDropdown<T> dropdown = new(
            id,
            rootRect,
            tmp,
            triangleImage,
            triangleRect,
            changeImg,
            list,
            listRect,
            listCg,
            values,
            display,
            summary,
            defaultValue,
            value,
            onChanged
        );

        LayoutElement? parentLayout = parent.GetComponent<LayoutElement>();
        List<ITweenHandle> layoutTweens = new();
        void UpdateHeight() {
            if (dropdown.IsDisposed) {
                return;
            }

            float rowHeight = 50f;
            float spacing = layout.spacing;
            float listHeight = (dropdown.Values.Count * rowHeight) + spacing;
            float targetHeight = dropdown.Expanded ? (62f + listHeight) : 50f;
            float targetAlpha = dropdown.Expanded ? 1f : 0f;

            foreach (var h in layoutTweens) {
                h.Kill();
            }

            layoutTweens.Clear();
            dropdown.LayoutSeq?.Kill();

            if (parentLayout) {
                float startHeight = parentLayout.preferredHeight;
                var pl = parentLayout;
                var rr = rootRect;
                var heightTween = O5Boot.Tween.TweenFloat(
                    () => 0f,
                    t => {
                        if (pl) {
                            pl.preferredHeight = Mathf.LerpUnclamped(startHeight, targetHeight, t);
                        }

                        if (rr) {
                            LayoutRebuilder.ForceRebuildLayoutImmediate(rr);
                        }
                    },
                    1f, 0.14f, ease: O5Ease.OutBack);
                layoutTweens.Add(heightTween);
                dropdown.LayoutSeq = heightTween;
            }

            var cg = listCg;
            float startAlpha = cg.alpha;
            layoutTweens.Add(O5Boot.Tween.TweenFloat(
                () => 0f,
                t => {
                    if (cg) {
                        cg.alpha = Mathf.LerpUnclamped(startAlpha, targetAlpha, t);
                    }
                },
                1f, 0.16f));
        }

        dropdown.OnLayoutChanged = UpdateHeight;

        var headerTrigger = rect.gameObject.AddComponent<EventTrigger>();
        O5Effects.HoverOutline(rect.gameObject, headerTrigger);

        var ovent = rect.gameObject.AddComponent<OventHandler>();
        ovent.OnClick += btn => {
            switch (btn) {
                case InputButton.Left:
                    dropdown.ToggleExpanded();
                    UpdateHeight();
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
                    break;

                case InputButton.Middle:
                    if (O5Boot.Config.MiddleClickToDefault &&
                        !EqualityComparer<T>.Default.Equals(dropdown.Value, dropdown.DefaultValue)
                    ) {
                        dropdown.Reset();
                    }

                    break;
            }
        };

        dropdown.RebuildList();
        UpdateHeight();
        return dropdown;
    }
}
