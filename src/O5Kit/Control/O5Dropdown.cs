// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using O5Kit.Behaviour;
using O5Kit.Core;
using O5Kit.Factory;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace O5Kit.Control;

/// <summary>Single-select dropdown with an animated expanding list.</summary>
public class O5Dropdown<T> : O5Object {
    /// <summary>Value to reset to on middle-click.</summary>
    public T? DefaultValue { get; set; }

    /// <summary>Current value.</summary>
    public T? Value { get; private set; }

    /// <summary>Available options. Replace via <see cref="SetValues"/>.</summary>
    public IReadOnlyList<T> Values { get; private set; }

    /// <summary>Renders an option as text.</summary>
    public Func<T, string> Display { get; set; }

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
    /// <param name="ctx">Owning kit context.</param>
    /// <param name="id">Stable identifier.</param>
    /// <param name="rect">Root rect.</param>
    /// <param name="label">Header label.</param>
    /// <param name="triangleImage">Foldout triangle image.</param>
    /// <param name="triangleRect">Foldout triangle rect.</param>
    /// <param name="changedImage">Changed-dot image.</param>
    /// <param name="listObject">List container.</param>
    /// <param name="listRect">List layout rect.</param>
    /// <param name="listCanvasGroup">List fade group.</param>
    /// <param name="options">Behaviour. Null uses defaults.</param>
    public O5Dropdown(
        O5Context ctx,
        string id,
        RectTransform rect,
        TMPro.TextMeshProUGUI label,
        Image triangleImage,
        RectTransform triangleRect,
        Image changedImage,
        GameObject listObject,
        RectTransform listRect,
        CanvasGroup listCanvasGroup,
        O5DropdownOptions<T>? options = null
    ) : base(ctx, id, rect) {
        options ??= new O5DropdownOptions<T>();
        Label = label;

        TriangleImage = triangleImage;
        TriangleRect = triangleRect;

        ChangedImage = changedImage;

        ListObject = listObject;
        ListRect = listRect;
        ListCanvasGroup = listCanvasGroup;

        Values = options.Values;

        Display = options.Display;
        DefaultValue = options.DefaultValue;

        Value = options.Value;

        OnChanged = options.OnChanged;

        Label.text = Value != null ? Display(Value) : "";

        RebuildList();
        UpdateVisual(true);
    }

    /// <summary>Selects a value.</summary>
    /// <param name="value">New value.</param>
    /// <param name="notify">Fire the change callback.</param>
    public void Set(T? value, bool notify = true) {
        if (IsDisposed) {
            return;
        }

        Value = value;

        if (Label != null && Label) {
            Label.text = Value != null ? Display(Value) : "";
        }

        if (notify) {
            OnChanged?.Invoke(value);
        }

        UpdateVisual(true);

        OnLayoutChanged?.Invoke();
    }

    /// <summary>Builds a dropdown under <paramref name="parent"/>. Owns all GameObjects; the caller only passes behaviour.</summary>
    /// <param name="ctx">Owning kit context.</param>
    /// <param name="parent">Parent transform. Needs a <see cref="UnityEngine.UI.LayoutElement"/> for height animation.</param>
    /// <param name="id">Stable identifier.</param>
    /// <param name="options">Behaviour. Null uses defaults.</param>
    public static O5Dropdown<T> Create(O5Context ctx, Transform parent, string id, O5DropdownOptions<T>? options = null)
        => O5Factory.DropDown(ctx, parent, id, options);

    /// <summary>Builds a dropdown under <paramref name="parent"/> with inline configuration.</summary>
    /// <param name="ctx">Owning kit context.</param>
    /// <param name="parent">Parent transform.</param>
    /// <param name="id">Stable identifier.</param>
    /// <param name="configure">Mutates a fresh <see cref="O5DropdownOptions{T}"/>.</param>
    public static O5Dropdown<T> Create(O5Context ctx, Transform parent, string id, Action<O5DropdownOptions<T>> configure) {
        var options = new O5DropdownOptions<T>();
        configure?.Invoke(options);
        return Create(ctx, parent, id, options);
    }

    /// <summary>Replaces the option list. Falls back to the first option when the current value is gone.</summary>
    /// <param name="values">New options.</param>
    /// <param name="notify">Fire the change callback if the value falls back.</param>
    public void SetValues(IReadOnlyList<T> values, bool notify = false) {
        if (IsDisposed) {
            return;
        }

        Values = values;

        RebuildList();

        if (Value == null || !Values.Contains(Value)) {
            if (Values.Count > 0) {
                Set(Values[0], notify);
            }
        }
    }

    /// <summary>Restores <see cref="DefaultValue"/>. No-op when null.</summary>
    /// <param name="notify">Fire the change callback.</param>
    public void Reset(bool notify = true) {
        if (DefaultValue != null) {
            Set(DefaultValue, notify);
        }
    }

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
        if (IsDisposed || Rect == null || !Rect) {
            return;
        }

        _triangleTween?.Kill();
        _triangleTintTween?.Kill();
        _changeTween?.Kill();

        var theme = Ctx.Theme;
        bool isDefault = DefaultValue == null || EqualityComparer<T>.Default.Equals(DefaultValue, Value);
        float targetRot = Expanded ? 180f : 0f;
        Color targetColor = Expanded ? theme.ObjectActive : theme.ObjectInactive;

        if (noAnimate) {
            if (TriangleRect) {
                TriangleRect.localRotation = Quaternion.Euler(0f, 0f, targetRot);
            }
            if (TriangleImage) {
                TriangleImage.color = targetColor;
            }

            if (ChangedImage) {
                Color c = ChangedImage.color;
                c.a = isDefault ? 0f : 1f;
                ChangedImage.color = c;
            }

            return;
        }

        var triRect = TriangleRect;
        if (!triRect) {
            return;
        }

        float startRot = triRect.localRotation.eulerAngles.z;
        if (startRot > 180f) {
            startRot -= 360f;
        }

        _triangleTween = Ctx.Tween.TweenFloat(
            () => 0f,
            t => {
                if (triRect) {
                    triRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.LerpUnclamped(startRot, targetRot, t));
                }
            },
            1f, 0.4f, ease: O5Ease.OutBack);

        var triImg = TriangleImage;
        if (triImg) {
            _triangleTintTween = Ctx.Tween.TweenColor(
                () => triImg.color, v => { if (triImg) triImg.color = v; }, targetColor, 0.2f);
        }

        var changed = ChangedImage;
        if (changed) {
            float changedStart = changed.color.a;
            float changedTarget = isDefault ? 0f : 1f;
            _changeTween = Ctx.Tween.TweenFloat(
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

        var theme = Ctx.Theme;
        foreach (T item in Values) {
            GameObject row = new("Row");
            row.transform.SetParent(ListObject.transform, false);

            RectTransform rowRect = row.AddComponent<RectTransform>();
            rowRect.sizeDelta = new Vector2(0f, Ctx.Theme.ControlHeightFor(Ctx.Theme.FontSizeBody));

            Image rowImage = row.AddComponent<Image>();
            rowImage.sprite = Ctx.Sprites.RoundedControl;
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
                    hoverTween = Ctx.Tween.TweenColor(
                        () => img.color, v => img.color = v, theme.ObjectActive, 0.12f);
                    _itemHoverTweens.Add(hoverTween);
                }
            ),
                (EventTriggerType.PointerExit, (e) => {
                    hoverTween?.Kill();
                    _itemHoverTweens.Remove(hoverTween!);
                    hoverTween = Ctx.Tween.TweenColor(
                        () => img.color, v => img.color = v, Color.clear, 0.12f);
                    _itemHoverTweens.Add(hoverTween);
                }
            ),
                (EventTriggerType.PointerClick, (e) => {
                    PointerEventData? pointerData = e as PointerEventData;
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

    private TMPro.TextMeshProUGUI CreateRowText(RectTransform parent) {
        var theme = Ctx.Theme;
        var obj = new GameObject("Text");
        obj.transform.SetParent(parent, false);

        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(16f, 0f);
        rect.offsetMax = Vector2.zero;

        var tmp = obj.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.font = Ctx.Fonts.Regular;
        tmp.fontSize = theme.FontSizeBody;
        tmp.color = theme.Text;
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
