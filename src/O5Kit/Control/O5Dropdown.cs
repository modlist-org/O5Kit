// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using O5Kit.Behaviour;
using O5Kit.Core;
using O5Kit.Factory;
using O5Kit.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace O5Kit.Control;

/// <summary>Global UI event hooks invoked by O5Kit controls (e.g. for UI audio).</summary>
public static class O5UiEvents {
    /// <summary>Fired when any dropdown opens (true) or closes (false).</summary>
    public static Action<bool>? OnDropdownToggled { get; set; }

    /// <summary>Fired when a dropdown item row is clicked by the user.</summary>
    public static Action? OnDropdownPicked { get; set; }

    /// <summary>Fired when a bool toggle changes via user click (true = on, false = off).</summary>
    public static Action<bool>? OnToggleChanged { get; set; }
}

/// <summary>Wheel-selection state shared by every <see cref="O5Dropdown{T}"/>, whatever its option type.</summary>
public static class O5Dropdown {
    internal static O5Object? Hovered;

    /// <summary>Optional gate enabling mouse-wheel selection on hovered, closed dropdowns.</summary>
    public static Func<bool>? EnableWheelAdjust { get; set; }

    /// <summary>Fired whenever a dropdown value changes via mouse wheel.</summary>
    public static Action? OnAnyWheelChanged { get; set; }

    /// <summary>True when a closed dropdown that the wheel would change is under the pointer.</summary>
    public static bool AnyWheelTarget => Hovered is IWheelSelectable w && w.AcceptsWheel;

    internal interface IWheelSelectable {
        bool AcceptsWheel { get; }
    }

    internal const float IndicatorLeftPadding = 16f;
    internal const float IndicatorBottomPadding = 4f;
    internal const float IndicatorHeight = 4f;
    internal const float SelectedPillWidth = 18f;
    internal const float UnselectedPillWidth = 8f;
    internal const float PillGap = 4f;
    internal const float BarDotWidth = 14f;
    internal const float IndicatorTweenDuration = 0.3f;
    internal const O5Ease IndicatorTweenEase = O5Ease.OutQuad;

    /// <summary>Total width required to render <paramref name="count"/> pills in multi-dot mode.</summary>
    public static float RequiredDotsWidth(int count)
        => count <= 0 ? 0f : SelectedPillWidth + ((count - 1) * (UnselectedPillWidth + PillGap));

    /// <summary>Returns true when <paramref name="count"/> items exceed <paramref name="maxWidth"/> and must fall back to bar + single-dot mode.</summary>
    public static bool ShouldUseBarMode(int count, float maxWidth)
        => count > 1 && maxWidth > 0f && RequiredDotsWidth(count) > maxWidth;

    /// <summary>Computes the left-edge x offset for the single dot in bar mode.</summary>
    public static float BarDotOffsetX(int index, int count, float maxWidth) {
        if (count <= 1 || index < 0 || maxWidth <= 0f) {
            return 0f;
        }
        float dotWidth = Math.Min(BarDotWidth, maxWidth);
        float t = Math.Max(0f, Math.Min(1f, index / (float)(count - 1)));
        return t * Math.Max(0f, maxWidth - dotWidth);
    }
}

/// <summary>Single-select dropdown with an animated expanding list.</summary>
public class O5Dropdown<T> : O5Object, O5Dropdown.IWheelSelectable {
    /// <summary>Value to reset to on middle-click.</summary>
    public T? DefaultValue { get; set; }

    /// <summary>Current value.</summary>
    public T? Value { get; private set; }

    /// <inheritdoc/>
    public override string? ShownValue => Value?.ToString() ?? string.Empty;

    /// <summary>Available options. Replace via <see cref="SetValues"/>.</summary>
    public IReadOnlyList<T> Values { get; private set; }

    /// <summary>Renders an option as text.</summary>
    public Func<T, string> Display { get; set; }

    /// <summary>Fired when the value changes.</summary>
    public Action<T?>? OnChanged { get; set; }

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

    /// <summary>Optional container for the selected-index indicator.</summary>
    public RectTransform? IndicatorRect { get; }

    /// <summary>Whether the selected-index indicator is enabled.</summary>
    public bool ShowIndexIndicator { get; }

    /// <summary>Whether the list is open.</summary>
    public bool Expanded { get; private set; }

    /// <summary>Fired when expansion changes so the factory can animate layout.</summary>
    public Action? OnLayoutChanged;

    /// <summary>Fired after <see cref="RebuildList"/> recreates the dropdown rows.</summary>
    public event Action? OnListRebuilt;

    private ITweenHandle? _triangleTween, _triangleTintTween, _changeTween, _indicatorTween;

    /// <summary>Layout height tween owned by the factory. Killed on dispose.</summary>
    public ITweenHandle? LayoutSeq { get; set; }
    private readonly List<ITweenHandle> _itemHoverTweens = new();
    private readonly List<Image> _indicatorPills = new();
    private Image? _indicatorBarTrack;
    private Image? _indicatorBarDot;
    private bool _indicatorBarMode;
    private float _lastIndicatorWidth = -1f;

    /// <summary>Creates a dropdown over factory-built visuals.</summary>
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
    /// <param name="indicatorRect">Optional container for the selected-index indicator.</param>
    public O5Dropdown(
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
        O5DropdownOptions<T>? options = null,
        RectTransform? indicatorRect = null
    ) : base(ctx, id, rect) {
        options ??= new O5DropdownOptions<T>();
        Label = label;

        TriangleIcon = triangleIcon;
        TriangleRect = triangleRect;

        ChangedImage = changedImage;

        ListObject = listObject;
        ListRect = listRect;
        ListCanvasGroup = listCanvasGroup;
        IndicatorRect = indicatorRect;
        ShowIndexIndicator = options.ShowIndexIndicator;

        Values = options.Values;

        Display = options.Display;
        DefaultValue = options.DefaultValue;

        Value = options.Value;

        OnChanged = options.OnChanged;

        Label.text = Value != null ? Display(Value) : "";

        RebuildList();
        UpdateVisual(true);
        RegisterTick();
    }

    internal void SetHovered(bool hovered) {
        if (hovered) {
            O5Dropdown.Hovered = this;
        } else if (ReferenceEquals(O5Dropdown.Hovered, this)) {
            O5Dropdown.Hovered = null;
        }
    }

    // activeInHierarchy: uGUI sends no PointerExit to a deactivated object, so Hovered can go stale.
    bool O5Dropdown.IWheelSelectable.AcceptsWheel => !IsDisposed
        && Rect && Rect.gameObject.activeInHierarchy
        && !Expanded
        && Values.Count > 1
        && CanvasGroup.interactable
        && O5Dropdown.EnableWheelAdjust?.Invoke() == true;

    /// <inheritdoc/>
    public override void Tick() {
        RefreshIndicatorSizeIfChanged();

        if (!ReferenceEquals(O5Dropdown.Hovered, this) || !((O5Dropdown.IWheelSelectable)this).AcceptsWheel || O5Input.GetMouseButton(0)) {
            return;
        }

        float wheel = O5Input.MouseScrollDelta.y;
        if (Mathf.Abs(wheel) < 0.0001f) {
            return;
        }

        // Wheel up moves toward the top of the list; stops at either end.
        int index = SelectedIndex();
        int next = Mathf.Clamp(index + (wheel > 0f ? -1 : 1), 0, Values.Count - 1);
        if (next != index) {
            Set(Values[next]);
            O5Dropdown.OnAnyWheelChanged?.Invoke();
        }
    }

    /// <summary>Selects a value.</summary>
    /// <param name="value">New value.</param>
    /// <param name="notify">Fire the change callback.</param>
    public void Set(T? value, bool notify = true) {
        if (IsDisposed) {
            return;
        }

        bool changed = !EqualityComparer<T>.Default.Equals(Value!, value!);
        Value = value;

        if (Label != null && Label) {
            Label.text = Value != null ? Display(Value) : "";
        }

        UpdateVisual(true);
        UpdateIndicator(false);

        if (notify) {
            OnChanged?.Invoke(value);
        }

        OnLayoutChanged?.Invoke();
        FlashIfExternal(changed, notify);
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
    /// <param name="notifyAudio">True to fire <see cref="O5UiEvents.OnDropdownToggled"/>.</param>
    public void SetExpanded(bool expanded, bool notifyAudio = true) {
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
        if (notifyAudio) {
            O5UiEvents.OnDropdownToggled?.Invoke(expanded);
        }
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
            if (TriangleIcon) {
                TriangleIcon.color = targetColor;
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
                child.SetParent(null, false);
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
                        () => img.color, v => img.color = v, Ctx.Theme.ObjectActive, 0.12f);
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
                    PointerEventData? pointerData = UnityUtils.AsPointerEventData(e);
                    if (pointerData == null || pointerData.button != PointerEventData.InputButton.Left) {
                        return;
                    }

                    O5UiEvents.OnDropdownPicked?.Invoke();
                    Set(item);
                    rowImage.color = Color.clear;
                    SetExpanded(false, notifyAudio: false);
                }
            )
            );
        }

        RebuildIndicator();
        OnListRebuilt?.Invoke();
    }

    private int SelectedIndex() {
        for (int i = 0; i < Values.Count; i++) {
            if (EqualityComparer<T>.Default.Equals(Values[i], Value)) {
                return i;
            }
        }
        return -1;
    }

    private void RefreshIndicatorSizeIfChanged() {
        if (IndicatorRect == null || !IndicatorRect || Values.Count <= 1) {
            return;
        }

        float w = IndicatorRect.rect.width;
        if (Mathf.Abs(w - _lastIndicatorWidth) <= 0.5f) {
            return;
        }

        bool needBarMode = O5Dropdown.ShouldUseBarMode(Values.Count, w);
        if (needBarMode != _indicatorBarMode) {
            RebuildIndicator();
        } else {
            _lastIndicatorWidth = w;
            if (_indicatorBarMode) {
                UpdateIndicator(true);
            }
        }
    }

    private void ClearIndicatorObjects() {
        _indicatorTween?.Kill();
        _indicatorTween = null;
        _indicatorPills.Clear();
        _indicatorBarTrack = null;
        _indicatorBarDot = null;
        if (IndicatorRect == null || !IndicatorRect) {
            return;
        }

        for (int i = IndicatorRect.childCount - 1; i >= 0; i--) {
            Transform child = IndicatorRect.GetChild(i);
            if (child != null) {
                child.SetParent(null, false);
                UnityEngine.Object.Destroy(child.gameObject);
            }
        }
    }

    private Image CreateIndicatorImage(string name) {
        var obj = new GameObject(name);
        obj.transform.SetParent(IndicatorRect, false);
        var rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;

        var img = obj.AddComponent<Image>();
        img.sprite = Ctx.Sprites.RoundedControl;
        img.type = Image.Type.Sliced;
        img.raycastTarget = false;
        if (img.sprite != null && img.sprite.border.y > 0f && img.sprite.pixelsPerUnit > 0f) {
            float targetRadius = O5Dropdown.IndicatorHeight * 0.5f;
            img.pixelsPerUnitMultiplier = (img.sprite.border.y * 100f) / (img.sprite.pixelsPerUnit * targetRadius);
        }
        return img;
    }

    private void RebuildIndicator() {
        if (IndicatorRect == null || !IndicatorRect) {
            return;
        }

        ClearIndicatorObjects();
        int count = Values.Count;
        if (!ShowIndexIndicator || count <= 1) {
            IndicatorRect.gameObject.SetActive(false);
            return;
        }

        IndicatorRect.gameObject.SetActive(true);
        float maxWidth = IndicatorRect.rect.width;
        _lastIndicatorWidth = maxWidth;
        _indicatorBarMode = O5Dropdown.ShouldUseBarMode(count, maxWidth);

        if (_indicatorBarMode) {
            var track = CreateIndicatorImage("Track");
            RectTransform trackRect = track.rectTransform;
            trackRect.anchorMin = Vector2.zero;
            trackRect.anchorMax = Vector2.one;
            trackRect.offsetMin = Vector2.zero;
            trackRect.offsetMax = Vector2.zero;
            _indicatorBarTrack = track;
            _indicatorBarDot = CreateIndicatorImage("Dot");
        } else {
            for (int i = 0; i < count; i++) {
                _indicatorPills.Add(CreateIndicatorImage($"Dot_{i}"));
            }
        }

        UpdateIndicator(true);
    }

    private void UpdateIndicator(bool noAnimate) {
        if (IndicatorRect == null || !IndicatorRect || Values.Count <= 1) {
            return;
        }

        _indicatorTween?.Kill();
        _indicatorTween = null;

        var theme = Ctx.Theme;
        Color activeColor = theme.ObjectActive;
        Color inactiveColor = theme.ObjectInactive;
        int selected = SelectedIndex();
        int count = Values.Count;

        if (_indicatorBarMode) {
            if (_indicatorBarTrack) {
                _indicatorBarTrack!.color = inactiveColor;
            }
            if (!_indicatorBarDot) {
                return;
            }

            bool hasSelection = selected >= 0;
            _indicatorBarDot!.gameObject.SetActive(hasSelection);
            if (!hasSelection) {
                return;
            }

            float maxWidth = IndicatorRect.rect.width;
            float dotWidth = Mathf.Min(O5Dropdown.BarDotWidth, Math.Max(0f, maxWidth));
            RectTransform dotRect = _indicatorBarDot.rectTransform;
            dotRect.sizeDelta = new Vector2(dotWidth, 0f);
            _indicatorBarDot.color = activeColor;

            float targetX = O5Dropdown.BarDotOffsetX(selected, count, maxWidth);
            if (noAnimate) {
                dotRect.anchoredPosition = new Vector2(targetX, 0f);
            } else {
                float startX = dotRect.anchoredPosition.x;
                _indicatorTween = Ctx.Tween.TweenFloat(
                    () => 0f,
                    t => {
                        if (dotRect) {
                            dotRect.anchoredPosition = new Vector2(Mathf.LerpUnclamped(startX, targetX, t), 0f);
                        }
                    },
                    1f, O5Dropdown.IndicatorTweenDuration, ease: O5Dropdown.IndicatorTweenEase);
            }
            return;
        }

        if (_indicatorPills.Count != count) {
            return;
        }

        if (noAnimate) {
            float x = 0f;
            for (int i = 0; i < count; i++) {
                Image pill = _indicatorPills[i];
                if (!pill) {
                    continue;
                }
                bool isSelected = i == selected;
                float w = isSelected ? O5Dropdown.SelectedPillWidth : O5Dropdown.UnselectedPillWidth;
                pill.color = isSelected ? activeColor : inactiveColor;
                RectTransform pillRect = pill.rectTransform;
                pillRect.sizeDelta = new Vector2(w, 0f);
                pillRect.anchoredPosition = new Vector2(x, 0f);
                x += w + O5Dropdown.PillGap;
            }
            return;
        }

        var startWidths = new float[count];
        var targetWidths = new float[count];
        var startColors = new Color[count];
        var targetColors = new Color[count];
        for (int i = 0; i < count; i++) {
            Image pill = _indicatorPills[i];
            bool isSelected = i == selected;
            startWidths[i] = pill ? pill.rectTransform.sizeDelta.x : O5Dropdown.UnselectedPillWidth;
            targetWidths[i] = isSelected ? O5Dropdown.SelectedPillWidth : O5Dropdown.UnselectedPillWidth;
            startColors[i] = pill ? pill.color : inactiveColor;
            targetColors[i] = isSelected ? activeColor : inactiveColor;
        }

        _indicatorTween = Ctx.Tween.TweenFloat(
            () => 0f,
            t => {
                float x = 0f;
                for (int i = 0; i < _indicatorPills.Count; i++) {
                    Image pill = _indicatorPills[i];
                    if (!pill) {
                        continue;
                    }
                    float w = Mathf.LerpUnclamped(startWidths[i], targetWidths[i], t);
                    pill.color = Color.LerpUnclamped(startColors[i], targetColors[i], t);
                    RectTransform pillRect = pill.rectTransform;
                    pillRect.sizeDelta = new Vector2(w, 0f);
                    pillRect.anchoredPosition = new Vector2(x, 0f);
                    x += w + O5Dropdown.PillGap;
                }
            },
            1f, O5Dropdown.IndicatorTweenDuration, ease: O5Dropdown.IndicatorTweenEase);
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
    protected override void OnThemeChanged(O5Theme prev, O5Theme next) {
        if (ChangedImage) {
            ChangedImage.color = O5Palette.WithAlpha(next.ObjectActive, ChangedImage.color.a);
        }
        bool animatingIndicator = _indicatorTween?.IsAlive == true;
        UpdateVisual(true);
        UpdateIndicator(!animatingIndicator);
    }

    /// <inheritdoc/>
    public override void SetBlocked(bool blocked, bool noAnimate = false) {
        base.SetBlocked(blocked, noAnimate);
        if (IsDisposed || Rect == null || !Rect) {
            return;
        }
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
        _indicatorTween?.Kill();
        LayoutSeq?.Kill();
        _triangleTween = _triangleTintTween = _changeTween = _indicatorTween = LayoutSeq = null;
        _indicatorPills.Clear();
        _indicatorBarTrack = null;
        _indicatorBarDot = null;
        OnLayoutChanged = null;
        OnListRebuilt = null;
        foreach (var tween in _itemHoverTweens) {
            tween?.Kill();
        }

        _itemHoverTweens.Clear();
        SetHovered(false);
        base.Dispose();
    }
}
