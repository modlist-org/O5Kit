// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using O5Kit.Behaviour;
using O5Kit.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace O5Kit.Control;

/// <summary>Single-select dropdown with an animated expanding list.</summary>
public class O5Dropdown<T> : O5Object {
    /// <summary>Value to reset to on middle-click.</summary>
    public T? DefaultValue { get; }

    /// <summary>Current value.</summary>
    public T? Value { get; private set; }

    /// <summary>Available options. Replace via <see cref="SetValues"/>.</summary>
    public IReadOnlyList<T> Values { get; private set; }

    /// <summary>Renders an option as text.</summary>
    public Func<T, string> Display { get; }

    /// <summary>Fired when the value changes.</summary>
    public Action<T?>? OnChanged { get; set; }

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
    private readonly List<ITweenHandle> _itemHoverTweens = new();

    /// <summary>Creates a dropdown over factory-built visuals.</summary>
    /// <param name="id">Stable identifier.</param>
    /// <param name="rect">Root rect.</param>
    /// <param name="label">Header label.</param>
    /// <param name="triangleImage">Foldout triangle image.</param>
    /// <param name="triangleRect">Foldout triangle rect.</param>
    /// <param name="changedImage">Changed-dot image.</param>
    /// <param name="listObject">List container.</param>
    /// <param name="listRect">List layout rect.</param>
    /// <param name="listCanvasGroup">List fade group.</param>
    /// <param name="values">Available options.</param>
    /// <param name="display">Option renderer.</param>
    /// <param name="defaultValue">Reset target.</param>
    /// <param name="value">Initial value.</param>
    /// <param name="onChanged">Change callback.</param>
    public O5Dropdown(
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
        T? defaultValue,
        T? value,
        Action<T?>? onChanged
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
        DefaultValue = defaultValue;

        Value = value;

        OnChanged = onChanged;

        Label.text = Value != null ? Display(Value) : "";

        RebuildList();
        UpdateVisual(true);
    }

    /// <summary>Selects a value, optionally invoking <see cref="OnChanged"/>.</summary>
    /// <param name="value">New value.</param>
    /// <param name="invoke">Fire the change callback.</param>
    public void Set(T? value, bool invoke = true) {
        if (IsDisposed) {
            return;
        }

        Value = value;

        Label.text = Value != null ? Display(Value) : "";

        if (invoke) {
            OnChanged?.Invoke(value);
        }

        UpdateVisual();

        OnLayoutChanged?.Invoke();
    }

    /// <summary>Replaces the option list. Falls back to the first option when the current value is gone.</summary>
    /// <param name="values">New options.</param>
    public void SetValues(IReadOnlyList<T> values) {
        if (IsDisposed) {
            return;
        }

        Values = values;

        RebuildList();

        if (Value == null || !Values.Contains(Value)) {
            if (Values.Count > 0) {
                Set(Values[0], false);
            }
        }
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
        bool isDefault = DefaultValue == null || EqualityComparer<T>.Default.Equals(DefaultValue, Value);
        float targetRot = Expanded ? 180f : 0f;
        Color targetColor = Expanded ? theme.ObjectActive : theme.ObjectInactive;

        if (noAnimate) {
            TriangleRect.localRotation = Quaternion.Euler(0f, 0f, targetRot);
            TriangleImage.color = targetColor;

            Color c = ChangedImage.color;
            c.a = isDefault ? 0f : 1f;
            ChangedImage.color = c;

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
    }

    /// <summary>Rebuilds list rows from <see cref="Values"/>.</summary>
    public void RebuildList() {
        if (IsDisposed) {
            return;
        }

        if (ListObject == null) {
            return;
        }

        foreach (var tween in _itemHoverTweens) {
            tween?.Kill();
        }

        _itemHoverTweens.Clear();

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

            EventTrigger trigger = row.AddComponent<EventTrigger>();

            ITweenHandle? hoverTween = null;
            var img = rowImage;

            UnityUtils.AddEvents(trigger,
                (EventTriggerType.PointerEnter, (e) => {
                    hoverTween?.Kill();
                    _itemHoverTweens.Remove(hoverTween!);
                    hoverTween = O5Boot.Tween.TweenColor(
                        () => img.color, v => img.color = v, theme.ObjectActive, 0.12f);
                    _itemHoverTweens.Add(hoverTween);
                }
            ),
                (EventTriggerType.PointerExit, (e) => {
                    hoverTween?.Kill();
                    _itemHoverTweens.Remove(hoverTween!);
                    hoverTween = O5Boot.Tween.TweenColor(
                        () => img.color, v => img.color = v, Color.clear, 0.12f);
                    _itemHoverTweens.Add(hoverTween);
                }
            ),
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

                    Set(item);
                    rowImage.color = Color.clear;
                    SetExpanded(false);
                }
            )
            );
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
        foreach (var tween in _itemHoverTweens) {
            tween?.Kill();
        }

        _itemHoverTweens.Clear();
        base.Dispose();
    }
}
