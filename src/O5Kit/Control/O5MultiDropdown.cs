// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Collections.Generic;
using O5Kit.Behaviour;
using O5Kit.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace O5Kit.Control;

/// <summary>Flag-style multi-select dropdown with checkmark rows and a summary label.</summary>
public class O5MultiDropdown<T> : O5Object where T : struct, Enum {
    /// <summary>Value to reset to on middle-click.</summary>
    public T DefaultValue { get; }

    /// <summary>Current combined flags value.</summary>
    public T Value { get; private set; }

    /// <summary>Available flags. Replace via <see cref="Set"/> + <see cref="RebuildList"/>.</summary>
    public IReadOnlyList<T> Values { get; private set; }

    /// <summary>Renders a flag as row text.</summary>
    public Func<T, string> Display { get; }

    /// <summary>Renders the combined value as header text.</summary>
    public Func<T, string> Summary { get; }

    /// <summary>Fired when the value changes.</summary>
    public Action<T>? OnChanged { get; }

    /// <summary>Header label.</summary>
    public TMPro.TextMeshProUGUI Label { get; }

    /// <summary>Foldout triangle image.</summary>
    public Image TriangleImage { get; }

    /// <summary>Foldout triangle rect.</summary>
    public RectTransform TriangleRect { get; }

    /// <summary>Dot shown while the value differs from default.</summary>
    public Image ChangedImage { get; }

    /// <summary>List container object.</summary>
    public GameObject ListObject { get; }

    /// <summary>List layout rect.</summary>
    public RectTransform ListRect { get; }

    /// <summary>List fade group.</summary>
    public CanvasGroup ListCanvasGroup { get; }

    /// <summary>Whether the list is open.</summary>
    public bool Expanded { get; private set; }

    /// <summary>Fired when expansion changes so the factory can animate layout.</summary>
    public Action? OnLayoutChanged;

    private ITweenHandle? _triangleTween, _triangleTintTween, _changeTween;
    /// <summary>Layout height tween owned by the factory. Killed on dispose.</summary>
    public ITweenHandle? LayoutSeq { get; set; }
    private readonly List<Image> _selectionImages = new();

    /// <summary>Creates a multi dropdown over factory-built visuals.</summary>
    /// <param name="id">Stable identifier.</param>
    /// <param name="rect">Root rect.</param>
    /// <param name="label">Header label.</param>
    /// <param name="triangleImage">Foldout triangle image.</param>
    /// <param name="triangleRect">Foldout triangle rect.</param>
    /// <param name="changedImage">Changed-dot image.</param>
    /// <param name="listObject">List container.</param>
    /// <param name="listRect">List layout rect.</param>
    /// <param name="listCanvasGroup">List fade group.</param>
    /// <param name="values">Available flags.</param>
    /// <param name="display">Flag renderer.</param>
    /// <param name="summary">Combined-value renderer.</param>
    /// <param name="defaultValue">Reset target.</param>
    /// <param name="value">Initial value.</param>
    /// <param name="onChanged">Change callback.</param>
    public O5MultiDropdown(
        string id,
        RectTransform rect,
        TMPro.TextMeshProUGUI label,
        Image triangleImage,
        RectTransform triangleRect,
        Image changedImage,
        GameObject listObject,
        RectTransform listRect,
        CanvasGroup listCanvasGroup,
        IReadOnlyList<T> values,
        Func<T, string> display,
        Func<T, string> summary,
        T defaultValue,
        T value,
        Action<T>? onChanged
    ) : base(id, rect) {
        Label = label;
        TriangleImage = triangleImage;
        TriangleRect = triangleRect;
        ChangedImage = changedImage;
        ListObject = listObject;
        ListRect = listRect;
        ListCanvasGroup = listCanvasGroup;
        Values = values;
        Display = display;
        Summary = summary;
        DefaultValue = defaultValue;
        Value = value;
        OnChanged = onChanged;

        Label.text = Summary(Value);
        RebuildList();
        UpdateVisual(true);
    }

    /// <summary>Sets the combined value, optionally invoking <see cref="OnChanged"/>.</summary>
    /// <param name="value">New flags value.</param>
    /// <param name="invoke">Fire the change callback.</param>
    public void Set(T value, bool invoke = true) {
        if (IsDisposed) {
            return;
        }

        Value = value;
        Label.text = Summary(Value);
        UpdateSelectionVisuals();

        if (invoke) {
            OnChanged?.Invoke(value);
        }

        UpdateVisual();
    }

    /// <summary>XORs a flag into the current value.</summary>
    /// <param name="value">Flag to flip.</param>
    public void Toggle(T value) {
        ulong current = Convert.ToUInt64(Value);
        ulong flag = Convert.ToUInt64(value);
        Set((T)Enum.ToObject(typeof(T), current ^ flag));
    }

    /// <summary>Restores <see cref="DefaultValue"/>.</summary>
    public void Reset() => Set(DefaultValue);

    /// <summary>Opens or closes the list.</summary>
    /// <param name="expanded">True to open.</param>
    public void SetExpanded(bool expanded) {
        if (IsDisposed) {
            return;
        }

        Expanded = expanded;
        if (ListObject != null) {
            ListObject.SetActive(expanded);
            if (expanded) {
                LayoutRebuilder.ForceRebuildLayoutImmediate(ListRect);
            }
        }

        UpdateVisual();
        OnLayoutChanged?.Invoke();
    }

    /// <summary>Flips the open state.</summary>
    public void ToggleExpanded() => SetExpanded(!Expanded);

    /// <summary>Refreshes triangle, tint and changed-dot.</summary>
    /// <param name="noAnimate">Snap instead of animating.</param>
    public void UpdateVisual(bool noAnimate = false) {
        if (IsDisposed) {
            return;
        }

        _triangleTween?.Kill();
        _triangleTintTween?.Kill();
        _changeTween?.Kill();

        var theme = O5Boot.Theme;
        bool isDefault = EqualityComparer<T>.Default.Equals(DefaultValue, Value);
        float targetRot = Expanded ? 180f : 0f;
        Color targetColor = Expanded ? theme.ObjectActive : theme.ObjectInactive;

        if (noAnimate) {
            TriangleRect.localRotation = Quaternion.Euler(0f, 0f, targetRot);
            TriangleImage.color = targetColor;

            Color c = ChangedImage.color;
            c.a = isDefault ? 0f : 1f;
            ChangedImage.color = c;
            UpdateSelectionVisuals();
            return;
        }

        var triRect = TriangleRect;
        float startRot = triRect.localRotation.eulerAngles.z;
        if (startRot > 180f) {
            startRot -= 360f;
        }

        _triangleTween = O5Boot.Tween.TweenFloat(
            () => 0f,
            t => {
                if (triRect) {
                    triRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.LerpUnclamped(startRot, targetRot, t));
                }
            },
            1f, 0.4f, ease: O5Ease.OutBack);

        var triImg = TriangleImage;
        _triangleTintTween = O5Boot.Tween.TweenColor(
            () => triImg.color, v => triImg.color = v, targetColor, 0.2f);

        var changed = ChangedImage;
        float changedStart = changed.color.a;
        float changedTarget = isDefault ? 0f : 1f;
        _changeTween = O5Boot.Tween.TweenFloat(
            () => 0f,
            t => {
                if (changed) {
                    var c = changed.color;
                    c.a = Mathf.LerpUnclamped(changedStart, changedTarget, t);
                    changed.color = c;
                }
            },
            1f, 0.2f);
        UpdateSelectionVisuals();
    }

    /// <summary>Rebuilds flag rows from <see cref="Values"/>.</summary>
    public void RebuildList() {
        if (IsDisposed || ListObject == null) {
            return;
        }

        _selectionImages.Clear();

        for (int i = ListObject.transform.childCount - 1; i >= 0; i--) {
            Transform child = ListObject.transform.GetChild(i);
            if (child != null) {
                UnityEngine.Object.Destroy(child.gameObject);
            }
        }

        var theme = O5Boot.Theme;
        foreach (T item in Values) {
            GameObject row = new("Row");
            row.transform.SetParent(ListObject.transform, false);

            RectTransform rowRect = row.AddComponent<RectTransform>();
            rowRect.sizeDelta = new Vector2(0f, 50f);

            Image rowImage = row.AddComponent<Image>();
            rowImage.sprite = O5Boot.Sprites.RoundedControl;
            rowImage.type = Image.Type.Sliced;
            rowImage.color = Color.clear;

            TMPro.TextMeshProUGUI rowText = CreateRowText(rowRect);
            rowText.text = Display(item);
            rowText.rectTransform.offsetMax = new Vector2(-48f, 0f);
            rowText.raycastTarget = false;

            GameObject selected = new("Selected");
            selected.transform.SetParent(row.transform, false);
            RectTransform selectedRect = selected.AddComponent<RectTransform>();
            selectedRect.anchorMin = new Vector2(1f, 0.5f);
            selectedRect.anchorMax = new Vector2(1f, 0.5f);
            selectedRect.pivot = new Vector2(0.5f, 0.5f);
            selectedRect.anchoredPosition = new Vector2(-23f, 0f);
            selectedRect.sizeDelta = new Vector2(26f, 26f);
            Image selectedImage = selected.AddComponent<Image>();
            selectedImage.raycastTarget = false;
            _selectionImages.Add(selectedImage);

            EventTrigger trigger = row.AddComponent<EventTrigger>();
            O5Effects.HoverOutline(row, trigger);

            UnityUtils.AddEvents(trigger,
                (EventTriggerType.PointerClick, (e) => {
                    PointerEventData? pointerData =
#if IL2CPP
                        e.TryCast<PointerEventData>();
#else
                        e as PointerEventData;
#endif
                    if (pointerData == null || pointerData.button != PointerEventData.InputButton.Left) {
                        return;
                    }

                    Toggle(item);
                }
            )
            );
        }

        UpdateSelectionVisuals();
    }

    private void UpdateSelectionVisuals() {
        var theme = O5Boot.Theme;
        var onSprite = O5Boot.Sprites.Icon("toggle-on");
        var offSprite = O5Boot.Sprites.Icon("toggle-off");
        for (int i = 0; i < _selectionImages.Count && i < Values.Count; i++) {
            bool has = HasFlag(Value, Values[i]);
            if (has && onSprite != null) {
                _selectionImages[i].sprite = onSprite;
            } else if (!has && offSprite != null) {
                _selectionImages[i].sprite = offSprite;
            }

            _selectionImages[i].color = has ? theme.ObjectActive : theme.ObjectInactive;
        }
    }

    private static TMPro.TextMeshProUGUI CreateRowText(RectTransform parent) {
        var theme = O5Boot.Theme;
        var obj = new GameObject("Text");
        obj.transform.SetParent(parent, false);

        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(16f, 0f);
        rect.offsetMax = Vector2.zero;

        var tmp = obj.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.font = O5Boot.Fonts.Regular;
        tmp.fontSize = theme.FontSizeBody;
        tmp.color = Color.white;
        tmp.alignment = TMPro.TextAlignmentOptions.Left;
        tmp.verticalAlignment = TMPro.VerticalAlignmentOptions.Middle;

        return tmp;
    }

    private static bool HasFlag(T value, T flag) {
        ulong valueBits = Convert.ToUInt64(value);
        ulong flagBits = Convert.ToUInt64(flag);
        return (valueBits & flagBits) == flagBits;
    }

    /// <inheritdoc/>
    public override void SetBlocked(bool blocked, bool noAnimate = false) {
        base.SetBlocked(blocked, noAnimate);
        SetExpanded(false);
    }

    /// <inheritdoc/>
    public override void Dispose() {
        if (IsDisposed) {
            return;
        }

        _triangleTween?.Kill();
        _triangleTintTween?.Kill();
        _changeTween?.Kill();
        LayoutSeq?.Kill();
        _triangleTween = _triangleTintTween = _changeTween = LayoutSeq = null;
        OnLayoutChanged = null;
        _selectionImages.Clear();
        base.Dispose();
    }
}
