// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Collections.Generic;
using O5Kit.Behaviour;
using O5Kit.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static UnityEngine.EventSystems.PointerEventData;

namespace O5Kit.Factory;

public static partial class O5Factory {
    /// <summary>Builds a collapsible card with active-toggle, title foldout and delete button.</summary>
    /// <param name="parent">Parent transform.</param>
    /// <param name="title">Header title.</param>
    /// <param name="activeValue">Initial active state.</param>
    /// <param name="onActiveChanged">Active toggle callback.</param>
    /// <param name="onDeleteClick">Delete button callback.</param>
    /// <param name="showDeleteButton">Show the delete button.</param>
    /// <param name="showActiveToggle">Show the active checkbox.</param>
    /// <returns>Card and content rects. Fill the content rect with controls.</returns>
    public static (RectTransform cardRect, RectTransform contentRect) Card(
        Transform parent,
        string title,
        bool activeValue,
        Action<bool>? onActiveChanged,
        Action? onDeleteClick,
        bool showDeleteButton = true,
        bool showActiveToggle = true
    ) {
        var theme = O5Boot.Theme;

        GameObject cardGo = new("ComponentCard_" + title);
        cardGo.transform.SetParent(parent, false);

        RectTransform cardRect = cardGo.AddComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0f, 1f);
        cardRect.anchorMax = new Vector2(1f, 1f);
        cardRect.pivot = new Vector2(0.5f, 1f);
        cardRect.sizeDelta = new Vector2(0f, 0f);

        var cardLayout = cardGo.AddComponent<VerticalLayoutGroup>();
        cardLayout.spacing = 0f;
        cardLayout.childControlWidth = true;
        cardLayout.childControlHeight = true;
        cardLayout.childForceExpandWidth = true;
        cardLayout.childForceExpandHeight = false;

        GameObject headerGo = new("Header");
        headerGo.transform.SetParent(cardGo.transform, false);
        RectTransform headerRect = headerGo.AddComponent<RectTransform>();

        var headerLayout = headerGo.AddComponent<LayoutElement>();
        headerLayout.preferredHeight = 38f;
        headerLayout.minHeight = 38f;

        var headerImg = headerGo.AddComponent<Image>();
        headerImg.sprite = O5Boot.Sprites.RoundedControl;
        headerImg.type = Image.Type.Sliced;
        headerImg.color = theme.CardHeader;

        var headerHLayout = headerGo.AddComponent<HorizontalLayoutGroup>();
        headerHLayout.padding = new RectOffset {
            left = 10,
            right = 10,
            top = 0,
            bottom = 0
        };
        headerHLayout.spacing = 8f;
        headerHLayout.childControlWidth = true;
        headerHLayout.childControlHeight = true;
        headerHLayout.childForceExpandWidth = false;
        headerHLayout.childForceExpandHeight = false;
        headerHLayout.childAlignment = TextAnchor.MiddleLeft;

        GameObject checkboxGo = new("Checkbox");
        checkboxGo.transform.SetParent(headerGo.transform, false);
        RectTransform checkboxRect = checkboxGo.AddComponent<RectTransform>();
        checkboxRect.sizeDelta = new Vector2(30f, 30f);
        var checkboxLE = checkboxGo.AddComponent<LayoutElement>();
        checkboxLE.preferredWidth = 30f;
        checkboxLE.preferredHeight = 30f;

        checkboxGo.AddComponent<EmptyGraphic>().raycastTarget = true;
        checkboxGo.SetActive(showActiveToggle);

        GameObject toggleVisualGo = new("ToggleCircle");
        toggleVisualGo.transform.SetParent(checkboxGo.transform, false);
        RectTransform toggleVisualRect = toggleVisualGo.AddComponent<RectTransform>();
        toggleVisualRect.anchorMin = new Vector2(0.5f, 0.5f);
        toggleVisualRect.anchorMax = new Vector2(0.5f, 0.5f);
        toggleVisualRect.pivot = new Vector2(0.5f, 0.5f);
        toggleVisualRect.sizeDelta = new Vector2(26f, 26f);
        var toggleVisualImage = toggleVisualGo.AddComponent<Image>();
        toggleVisualImage.raycastTarget = false;
        ITweenHandle? toggleTween = null;

        bool isCurrentActive = activeValue;
        CanvasGroup? contentCanvasGroup = null;
        void UpdateComponentToggle(bool animate) {
            var onSprite = O5Boot.Sprites.Icon("toggle-on");
            var offSprite = O5Boot.Sprites.Icon("toggle-off");
            if (isCurrentActive && onSprite != null) {
                toggleVisualImage.sprite = onSprite;
            } else if (!isCurrentActive && offSprite != null) {
                toggleVisualImage.sprite = offSprite;
            }

            if (!animate) {
                toggleVisualRect.sizeDelta = new Vector2(26f, 26f);
                toggleVisualImage.color = isCurrentActive ? theme.ObjectActive : theme.ObjectInactive;
                return;
            }

            toggleVisualRect.sizeDelta = new Vector2(30f, 30f);
            var tvr = toggleVisualRect;
            var tvi = toggleVisualImage;
            var target = isCurrentActive ? theme.ObjectActive : theme.ObjectInactive;
            Color startColor = tvi.color;
            toggleTween?.Kill();
            toggleTween = O5Boot.Tween.TweenFloat(
                () => 0f,
                t => {
                    if (tvr) {
                        tvr.sizeDelta = Vector2.LerpUnclamped(new Vector2(30f, 30f), new Vector2(26f, 26f), t);
                    }

                    if (tvi) {
                        tvi.color = Color.LerpUnclamped(startColor, target, t);
                    }
                },
                1f, 0.3f, ease: O5Ease.OutQuad);
        }

        UpdateComponentToggle(false);

        var checkboxOvent = checkboxGo.AddComponent<OventHandler>();
        checkboxOvent.OnClick += btn => {
            if (btn == InputButton.Left) {
                isCurrentActive = !isCurrentActive;
                UpdateComponentToggle(true);
                if (contentCanvasGroup != null) {
                    contentCanvasGroup.alpha = isCurrentActive ? 1f : 0.42f;
                    contentCanvasGroup.interactable = isCurrentActive;
                    contentCanvasGroup.blocksRaycasts = isCurrentActive;
                }

                onActiveChanged?.Invoke(isCurrentActive);
            }
        };

        GameObject titleTriggerGo = new("TitleTrigger");
        titleTriggerGo.transform.SetParent(headerGo.transform, false);
        RectTransform titleTriggerRect = titleTriggerGo.AddComponent<RectTransform>();
        titleTriggerGo.AddComponent<EmptyGraphic>().raycastTarget = true;
        var titleTriggerLE = titleTriggerGo.AddComponent<LayoutElement>();
        titleTriggerLE.flexibleWidth = 1f;

        var triggerHLayout = titleTriggerGo.AddComponent<HorizontalLayoutGroup>();
        triggerHLayout.padding = new RectOffset { right = 38 };
        triggerHLayout.childControlWidth = true;
        triggerHLayout.childControlHeight = true;
        triggerHLayout.childForceExpandWidth = false;
        triggerHLayout.childForceExpandHeight = true;
        triggerHLayout.childAlignment = TextAnchor.MiddleLeft;

        GameObject foldoutGo = new("Triangle");
        foldoutGo.transform.SetParent(titleTriggerGo.transform, false);
        var foldoutRect = foldoutGo.AddComponent<RectTransform>();
        foldoutRect.anchorMin = new Vector2(1f, 0.5f);
        foldoutRect.anchorMax = new Vector2(1f, 0.5f);
        foldoutRect.pivot = new Vector2(0.5f, 0.5f);
        foldoutRect.anchoredPosition = new Vector2(-23f, 0f);
        foldoutRect.sizeDelta = new Vector2(26f, 26f);
        foldoutRect.localRotation = Quaternion.Euler(0f, 0f, 180f);
        var foldoutImage = foldoutGo.AddComponent<Image>();
        foldoutImage.sprite = O5Boot.Sprites.Icon("Triangle128");
        foldoutImage.color = theme.ObjectActive;
        foldoutImage.raycastTarget = false;
        foldoutGo.AddComponent<LayoutElement>().ignoreLayout = true;
        List<ITweenHandle> foldoutTweens = new();

        GameObject titleGo = new("TitleText");
        titleGo.transform.SetParent(titleTriggerGo.transform, false);
        var titleText = titleGo.AddComponent<TMPro.TextMeshProUGUI>();
        titleText.font = O5Boot.Fonts.Medium;
        titleText.fontSize = 18f;
        titleText.text = title;
        titleText.color = Color.white;
        titleText.alignment = TMPro.TextAlignmentOptions.Left;
        titleText.verticalAlignment = TMPro.VerticalAlignmentOptions.Middle;
        titleText.raycastTarget = false;
        var titleLayout = titleGo.AddComponent<LayoutElement>();
        titleLayout.flexibleWidth = 1f;

        if (showDeleteButton) {
            GameObject deleteGo = new("DeleteBtn");
            deleteGo.transform.SetParent(headerGo.transform, false);
            RectTransform deleteRect = deleteGo.AddComponent<RectTransform>();
            deleteRect.sizeDelta = new Vector2(22f, 22f);
            var deleteLE = deleteGo.AddComponent<LayoutElement>();
            deleteLE.preferredWidth = 22f;
            deleteLE.preferredHeight = 22f;

            var deleteImg = deleteGo.AddComponent<Image>();
            deleteImg.sprite = O5Boot.Sprites.Icon("X128");
            deleteImg.color = theme.SoftRed;

            var deleteTrigger = deleteGo.AddComponent<EventTrigger>();
            O5Effects.HoverOutline(deleteGo, deleteTrigger);
            var deleteOvent = deleteGo.AddComponent<OventHandler>();
            deleteOvent.OnClick += btn => {
                if (btn == InputButton.Left) {
                    onDeleteClick?.Invoke();
                }
            };
        }

        GameObject contentGo = new("Content");
        contentGo.transform.SetParent(cardGo.transform, false);
        RectTransform contentRect = contentGo.AddComponent<RectTransform>();
        var contentElement = contentGo.AddComponent<LayoutElement>();
        contentElement.minHeight = 0f;
        contentElement.flexibleHeight = 0f;
        contentElement.enabled = false;

        var contentLayout = contentGo.AddComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset {
            left = 10,
            right = 10,
            top = 8,
            bottom = 8
        };
        contentLayout.spacing = 8f;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        var contentImg = contentGo.AddComponent<Image>();
        contentImg.sprite = O5Boot.Sprites.RoundedControl;
        contentImg.type = Image.Type.Sliced;
        contentImg.color = theme.CardPanel;
        contentGo.AddComponent<RectMask2D>();

        contentCanvasGroup = contentGo.AddComponent<CanvasGroup>();
        contentCanvasGroup.alpha = activeValue ? 1f : 0.42f;
        contentCanvasGroup.interactable = activeValue;
        contentCanvasGroup.blocksRaycasts = activeValue;

        bool isExpanded = true;
        float expandedContentHeight = -1f;

        void RebuildCardLayout() {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
            LayoutRebuilder.ForceRebuildLayoutImmediate(cardRect);
            Transform? parentTransform = cardGo.transform.parent;
            while (parentTransform != null) {
                var parentRect = parentTransform.GetComponent<RectTransform>();
                if (parentRect != null) {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
                }

                parentTransform = parentTransform.parent;
            }
        }

        var titleOvent = titleTriggerGo.AddComponent<OventHandler>();
        titleOvent.OnClick += btn => {
            if (btn != InputButton.Left) {
                return;
            }

            if (isExpanded) {
                RebuildCardLayout();
                Canvas.ForceUpdateCanvases();
                expandedContentHeight = Math.Max(0f, contentRect.rect.height);
            }

            isExpanded = !isExpanded;
            float currentHeight = contentElement.enabled
                ? contentElement.preferredHeight
                : (isExpanded ? 0f : expandedContentHeight);
            float targetHeight = isExpanded ? expandedContentHeight : 0f;
            contentElement.enabled = true;
            contentElement.preferredHeight = currentHeight;
            if (isExpanded) {
                contentGo.SetActive(true);
            }

            if (contentCanvasGroup != null) {
                contentCanvasGroup.alpha = isCurrentActive ? 1f : 0.42f;
            }

            if (isExpanded) {
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
            }

            var ce = contentElement;
            var cg = contentGo;
            var cr = contentRect;
            var fr = foldoutRect;
            var fi = foldoutImage;
            bool expanding = isExpanded;
            float foldStart = fr.localRotation.eulerAngles.z;
            if (foldStart > 180f) {
                foldStart -= 360f;
            }

            float foldTarget = expanding ? 180f : 0f;
            Color foldColorTarget = expanding ? theme.ObjectActive : theme.ObjectInactive;
            Color foldStartColor = fi.color;
            foreach (var h in foldoutTweens) {
                h.Kill();
            }

            foldoutTweens.Clear();
            foldoutTweens.Add(O5Boot.Tween.TweenFloat(
                () => 0f,
                t => {
                    if (ce) {
                        ce.preferredHeight = Math.Max(0f, Mathf.LerpUnclamped(currentHeight, targetHeight, t));
                    }

                    RebuildCardLayout();
                },
                1f, 0.18f,
                onComplete: () => {
                    if (expanding && ce) {
                        ce.enabled = false;
                    } else if (!expanding && cg) {
                        cg.SetActive(false);
                    }

                    RebuildCardLayout();
                },
                ease: O5Ease.OutCubic));
            foldoutTweens.Add(O5Boot.Tween.TweenFloat(
                () => 0f,
                t => {
                    if (fr) {
                        fr.localRotation = Quaternion.Euler(0f, 0f, Mathf.LerpUnclamped(foldStart, foldTarget, t));
                    }

                    if (fi) {
                        fi.color = Color.LerpUnclamped(foldStartColor, foldColorTarget, t);
                    }
                },
                1f, 0.4f, ease: O5Ease.OutBack));
        };

        return (cardRect, contentRect);
    }
}
