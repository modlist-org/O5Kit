// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using O5Kit.Behaviour;
using O5Kit.Core;
using O5Kit.Factory;
using O5Kit.Resource;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace O5Kit.Control;

/// <summary>Flag-style multi-select dropdown with checkmark rows and a summary label.</summary>
public class O5MultiDropdown<T> : O5Object where T : struct, Enum {
    /// <summary>Value to reset to on middle-click.</summary>
    public T? DefaultValue { get; set; }
    internal bool HasDefault => DefaultValue != null;

    /// <summary>Current combined flags value.</summary>
    public T Value { get; private set; }

    /// <inheritdoc/>
    public override string? ShownValue => Value.ToString();

    /// <summary>Available flags. Replace via <see cref="SetValues"/>.</summary>
    public IReadOnlyList<T> Values { get; private set; }

    /// <summary>Renders a flag as row text.</summary>
    public Func<T, string> Display { get; set; }

    /// <summary>Renders the combined value as header text.</summary>
    public Func<T, string> Summary { get; set; }

    /// <summary>Fired when the value changes.</summary>
    public Action<T>? OnChanged { get; set; }

    /// <summary>Header label.</summary>
    public TMPro.TextMeshProUGUI Label { get; }

    /// <summary>Foldout triangle icon.</summary>
    public TMPro.TextMeshProUGUI TriangleIcon { get; }

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
    private readonly List<TMPro.TextMeshProUGUI> _selectionIcons = new();

    /// <summary>Creates a multi dropdown over factory-built visuals.</summary>
    /// <param name="ctx">Owning kit context.</param>
    /// <param name="id">Stable identifier.</param>
    /// <param name="rect">Root rect.</param>
    /// <param name="label">Header label.</param>
    /// <param name="triangleIcon">Foldout triangle icon.</param>
    /// <param name="triangleRect">Foldout triangle rect.</param>
    /// <param name="changedImage">Changed-dot image.</param>
    /// <param name="listObject">List container.</param>
    /// <param name="listRect">List layout rect.</param>
    /// <param name="listCanvasGroup">List fade group.</param>
    /// <param name="options">Behaviour. Null uses defaults.</param>
    public O5MultiDropdown(
        O5Context ctx,
        string id,
        RectTransform rect,
        TMPro.TextMeshProUGUI label,
        TMPro.TextMeshProUGUI triangleIcon,
        RectTransform triangleRect,
        Image changedImage,
        GameObject listObject,
        RectTransform listRect,
        CanvasGroup listCanvasGroup,
        O5MultiDropdownOptions<T>? options = null
    ) : base(ctx, id, rect) {
        options ??= new O5MultiDropdownOptions<T>();
        Label = label;
        TriangleIcon = triangleIcon;
        TriangleRect = triangleRect;
        ChangedImage = changedImage;
        ListObject = listObject;
        ListRect = listRect;
        ListCanvasGroup = listCanvasGroup;
        Values = options.Values;
        Display = options.Display;
        Summary = options.Summary;
        DefaultValue = options.DefaultValue;
        Value = options.Value;
        OnChanged = options.OnChanged;

        Label.text = Summary(Value);
        RebuildList();
        UpdateVisual(true);
    }

    /// <summary>Sets the combined value.</summary>
    /// <param name="value">New flags value.</param>
    /// <param name="notify">Fire the change callback.</param>
    public void Set(T value, bool notify = true) {
        if (IsDisposed) {
            return;
        }

        bool changed = !EqualityComparer<T>.Default.Equals(Value, value);
        Value = value;
        if (Label != null && Label) {
            Label.text = Summary(Value);
        }
        UpdateSelectionVisuals();

        if (notify) {
            OnChanged?.Invoke(value);
        }

        UpdateVisual(true);
        FlashIfExternal(changed, notify);
    }

    /// <summary>XORs a flag into the current value.</summary>
    /// <param name="value">Flag to flip.</param>
    /// <param name="notify">Fire the change callback.</param>
    public void Toggle(T value, bool notify = true) {
        ulong current = Convert.ToUInt64(Value);
        ulong flag = Convert.ToUInt64(value);
        Set((T)Enum.ToObject(typeof(T), current ^ flag), notify);
    }

    /// <summary>Builds a multi dropdown under <paramref name="parent"/>. Owns all GameObjects; the caller only passes behaviour.</summary>
    /// <param name="ctx">Owning kit context.</param>
    /// <param name="parent">Parent transform. Needs a <see cref="UnityEngine.UI.LayoutElement"/> for height animation.</param>
    /// <param name="id">Stable identifier.</param>
    /// <param name="options">Behaviour. Null uses defaults.</param>
    public static O5MultiDropdown<T> Create(O5Context ctx, Transform parent, string id, O5MultiDropdownOptions<T>? options = null)
        => O5Factory.MultiDropDown(ctx, parent, id, options);

    /// <summary>Builds a multi dropdown under <paramref name="parent"/> with inline configuration.</summary>
    /// <param name="ctx">Owning kit context.</param>
    /// <param name="parent">Parent transform.</param>
    /// <param name="id">Stable identifier.</param>
    /// <param name="configure">Mutates a fresh <see cref="O5MultiDropdownOptions{T}"/>.</param>
    public static O5MultiDropdown<T> Create(O5Context ctx, Transform parent, string id, Action<O5MultiDropdownOptions<T>> configure) {
        var options = new O5MultiDropdownOptions<T>();
        configure?.Invoke(options);
        return Create(ctx, parent, id, options);
    }

    /// <summary>Replaces the flag list. Falls back to the first flag when the current value is gone.</summary>
    /// <param name="values">New flags.</param>
    /// <param name="notify">Fire the change callback if the value falls back.</param>
    public void SetValues(IReadOnlyList<T> values, bool notify = false) {
        if (IsDisposed) {
            return;
        }

        Values = values;
        RebuildList();

        if (Values.Count > 0 && !Values.Contains(Value)) {
            Set(Values[0], notify);
        }
    }

    /// <summary>Restores <see cref="DefaultValue"/>. No-op when null.</summary>
    /// <param name="notify">Fire the change callback.</param>
    public void Reset(bool notify = true) {
        if (DefaultValue is T d) {
            Set(d, notify);
        }
    }

    /// <summary>Opens or closes the list.</summary>
    /// <param name="expanded">True to open.</param>
    public void SetExpanded(bool expanded) {
        if (IsDisposed || Expanded == expanded) {
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
        O5UiEvents.OnDropdownToggled?.Invoke(expanded);
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
        bool isDefault = DefaultValue is not T d || EqualityComparer<T>.Default.Equals(d, Value);
        float targetRot = Expanded ? 180f : 0f;
        Color targetColor = Expanded ? theme.ObjectActive : theme.ObjectInactive;

        if (noAnimate) {
            if (TriangleRect) {
                TriangleRect.localRotation = Quaternion.Euler(0f, 0f, targetRot);
            }
            if (TriangleIcon) {
                TriangleIcon.color = targetColor;
            }

            if (ChangedImage) {
                Color c = ChangedImage.color;
                c.a = isDefault ? 0f : 1f;
                ChangedImage.color = c;
            }
            UpdateSelectionVisuals();
            return;
        }

        var triRect = TriangleRect;
        if (!triRect) {
            UpdateSelectionVisuals();
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

        var triImg = TriangleIcon;
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
        UpdateSelectionVisuals();
    }

    /// <summary>Rebuilds flag rows from <see cref="Values"/>.</summary>
    public void RebuildList() {
        if (IsDisposed || ListObject == null) {
            return;
        }

        _selectionIcons.Clear();

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
            rowText.rectTransform.offsetMax = new Vector2(-48f, 0f);
            rowText.raycastTarget = false;

            var selectedIcon = O5FactoryIcons.CreateIcon(
                row.transform,
                "Selected",
                Ctx.Fonts.Icons,
                O5Icons.ToggleCircle,
                theme.ObjectInactive,
                fixedFontSize: 26f
            );
            RectTransform selectedRect = selectedIcon.rectTransform;
            selectedRect.anchorMin = new Vector2(1f, 0.5f);
            selectedRect.anchorMax = new Vector2(1f, 0.5f);
            selectedRect.pivot = new Vector2(0.5f, 0.5f);
            selectedRect.anchoredPosition = new Vector2(-23f, 0f);
            selectedRect.sizeDelta = new Vector2(26f, 26f);
            _selectionIcons.Add(selectedIcon);

            EventTrigger trigger = row.AddComponent<EventTrigger>();
            O5Effects.HoverOutline(Ctx, row, trigger);

            UnityUtils.AddEvents(trigger,
                (EventTriggerType.PointerClick, (e) => {
                    PointerEventData? pointerData = UnityUtils.AsPointerEventData(e);
                    if (pointerData == null || pointerData.button != PointerEventData.InputButton.Left) {
                        return;
                    }

                    O5UiEvents.OnDropdownPicked?.Invoke();
                    Toggle(item);
                }
            )
            );
        }

        UpdateSelectionVisuals();
    }

    private void UpdateSelectionVisuals() {
        var theme = Ctx.Theme;
        var font = Ctx.Fonts.Icons;
        for (int i = 0; i < _selectionIcons.Count && i < Values.Count; i++) {
            bool has = HasFlag(Value, Values[i]);
            O5FactoryIcons.ApplyGlyph(_selectionIcons[i], font, has ? O5Icons.Circle : O5Icons.ToggleCircle);
            _selectionIcons[i].color = has ? theme.ObjectActive : theme.ObjectInactive;
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

    private static bool HasFlag(T value, T flag) {
        ulong valueBits = Convert.ToUInt64(value);
        ulong flagBits = Convert.ToUInt64(flag);
        return (valueBits & flagBits) == flagBits;
    }

    /// <inheritdoc/>
    protected override void OnThemeChanged(O5Theme prev, O5Theme next) {
        if (ChangedImage) {
            ChangedImage.color = O5Palette.WithAlpha(next.ObjectActive, ChangedImage.color.a);
        }
        UpdateVisual(true);
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
        _selectionIcons.Clear();
        base.Dispose();
    }
}
