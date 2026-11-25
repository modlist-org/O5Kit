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

/// <summary>Compact single-select for 2-4 options. A rounded segmented button group; lighter than a dropdown (no expanding list).</summary>
public class O5Segmented<T> : O5Object {
    /// <summary>Value to reset to on middle-click. Null disables reset and hides the dot.</summary>
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

    /// <summary>Resting background color for unselected segments.</summary>
    public Color NormalColor { get; set; }

    /// <summary>Resting background color for the selected segment.</summary>
    public Color ActiveColor { get; set; }

    /// <summary>Outline color.</summary>
    public Color BorderColor {
        get;
        set {
            field = value;
            if (OutlineImage != null && OutlineImage) {
                OutlineImage.color = value;
            }
        }
    }

    /// <summary>Segment row container (factory-built mask + layout).</summary>
    public RectTransform Container { get; }

    /// <summary>Outline overlay image.</summary>
    public Image OutlineImage { get; }

    /// <summary>Dot shown while the value differs from default.</summary>
    public Image ChangedImage { get; }

    /// <summary>Per-segment background images, in <see cref="Values"/> order.</summary>
    public IReadOnlyList<Image> SegmentBackgrounds => _backgrounds;

    /// <summary>Per-segment label texts, in <see cref="Values"/> order.</summary>
    public IReadOnlyList<TMPro.TextMeshProUGUI> SegmentLabels => _labels;

    /// <summary>Label font size. Applies to all segments immediately.</summary>
    public float FontSize {
        get => _fontSize;
        set {
            _fontSize = value;
            foreach (var label in _labels) {
                if (label) {
                    label.fontSize = value;
                }
            }
        }
    }

    private readonly List<Image> _backgrounds = new();
    private readonly List<TMPro.TextMeshProUGUI> _labels = new();
    private readonly List<ITweenHandle?> _hoverTweens = new();
    private readonly List<bool> _hovered = new();
    private readonly List<bool> _pressed = new();
    private float _fontSize;

    /// <summary>Creates a segmented control over a factory-built container. Builds segment rows itself (like dropdown rows).</summary>
    /// <param name="ctx">Owning kit context.</param>
    /// <param name="id">Stable identifier.</param>
    /// <param name="rect">Root rect.</param>
    /// <param name="container">Segment row container with a horizontal layout.</param>
    /// <param name="outlineImage">Outline overlay image.</param>
    /// <param name="changedImage">Changed-dot image.</param>
    /// <param name="fontSize">Label font size.</param>
    /// <param name="options">Behaviour. Null uses defaults.</param>
    public O5Segmented(
        O5Context ctx,
        string id,
        RectTransform rect,
        RectTransform container,
        Image outlineImage,
        Image changedImage,
        float fontSize,
        O5SegmentedOptions<T>? options = null
    ) : base(ctx, id, rect) {
        options ??= new O5SegmentedOptions<T>();
        Container = container;
        OutlineImage = outlineImage;
        ChangedImage = changedImage;
        _fontSize = fontSize;
        Values = options.Values;
        Display = options.Display;
        Value = options.Value;
        DefaultValue = options.DefaultValue;
        OnChanged = options.OnChanged;
        NormalColor = options.NormalColor ?? Ctx.Theme.ObjectButton;
        ActiveColor = options.ActiveColor ?? Ctx.Theme.ObjectActive;
        BorderColor = options.BorderColor ?? Ctx.Theme.ControlOutlineIdle;

        BuildSegments();
        UpdateVisual(true);
    }

    /// <summary>Builds a segmented control under <paramref name="parent"/>. Owns all GameObjects; the caller only passes behaviour.</summary>
    /// <param name="ctx">Owning kit context.</param>
    /// <param name="parent">Parent transform.</param>
    /// <param name="id">Stable identifier.</param>
    /// <param name="options">Behaviour. Null uses defaults.</param>
    public static O5Segmented<T> Create(O5Context ctx, Transform parent, string id, O5SegmentedOptions<T>? options = null)
        => O5Factory.Segmented(ctx, parent, id, options);

    /// <summary>Builds a segmented control under <paramref name="parent"/> with inline configuration.</summary>
    /// <param name="ctx">Owning kit context.</param>
    /// <param name="parent">Parent transform.</param>
    /// <param name="id">Stable identifier.</param>
    /// <param name="configure">Mutates a fresh <see cref="O5SegmentedOptions{T}"/>.</param>
    public static O5Segmented<T> Create(O5Context ctx, Transform parent, string id, Action<O5SegmentedOptions<T>> configure) {
        var options = new O5SegmentedOptions<T>();
        configure?.Invoke(options);
        return Create(ctx, parent, id, options);
    }

    /// <summary>Selects a value.</summary>
    /// <param name="value">New value.</param>
    /// <param name="notify">Fire the change callback.</param>
    public void Set(T? value, bool notify = true) {
        if (IsDisposed) {
            return;
        }

        if (EqualityComparer<T>.Default.Equals(Value, value)) {
            return;
        }

        Value = value;
        UpdateVisual();

        if (notify) {
            OnChanged?.Invoke(Value);
        }
        FlashIfExternal(true, notify);
    }

    /// <summary>Restores <see cref="DefaultValue"/>. No-op when null.</summary>
    /// <param name="notify">Fire the change callback.</param>
    public void Reset(bool notify = true) {
        if (DefaultValue != null) {
            Set(DefaultValue, notify);
        }
    }

    /// <summary>Replaces the option list. Falls back to the first option when the current value is gone.</summary>
    /// <param name="values">New options.</param>
    /// <param name="notify">Fire the change callback if the value falls back.</param>
    public void SetValues(IReadOnlyList<T> values, bool notify = false) {
        if (IsDisposed) {
            return;
        }

        Values = values;
        BuildSegments();

        if (Values.Count > 0 && (Value == null || !Values.Contains(Value))) {
            Value = Values[0];
            UpdateVisual();
            if (notify) {
                OnChanged?.Invoke(Value);
            }
        } else {
            UpdateVisual(true);
        }
    }

    private void BuildSegments() {
        foreach (var tween in _hoverTweens) {
            tween?.Kill();
        }
        _backgrounds.Clear();
        _labels.Clear();
        _hoverTweens.Clear();
        _hovered.Clear();
        _pressed.Clear();

        for (int i = Container.transform.childCount - 1; i >= 0; i--) {
            Transform child = Container.transform.GetChild(i);
            if (child != null) {
                UnityEngine.Object.Destroy(child.gameObject);
            }
        }

        for (int i = 0; i < Values.Count; i++) {
            int index = i;
            GameObject segmentObj = new($"Segment_{i}");
            segmentObj.transform.SetParent(Container, false);

            segmentObj.AddComponent<RectTransform>();
            LayoutElement segLE = segmentObj.AddComponent<LayoutElement>();
            segLE.minWidth = 0f;
            segLE.preferredWidth = 0f;
            segLE.flexibleWidth = 1f;

            Image bgImage = segmentObj.AddComponent<Image>();
            bgImage.color = NormalColor;
            bgImage.raycastTarget = true;

            GameObject textObj = new("Text");
            textObj.transform.SetParent(segmentObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TMPro.TextMeshProUGUI tmp = textObj.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.font = Ctx.Fonts.Regular;
            tmp.fontSize = _fontSize;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.verticalAlignment = TMPro.VerticalAlignmentOptions.Middle;
            tmp.raycastTarget = false;
            tmp.text = Display(Values[index]);

            _backgrounds.Add(bgImage);
            _labels.Add(tmp);
            _hoverTweens.Add(null);
            _hovered.Add(false);
            _pressed.Add(false);

            EventTrigger trigger = segmentObj.AddComponent<EventTrigger>();
            UnityUtils.AddEvents(trigger,
                (EventTriggerType.PointerEnter, _ => OnSegmentHoverEnter(index)),
                (EventTriggerType.PointerExit, _ => OnSegmentHoverExit(index)),
                (EventTriggerType.PointerDown, e => {
                    if (UnityUtils.AsPointerEventData(e) is { } ped && ped.button == PointerEventData.InputButton.Left) {
                        OnSegmentPressEnter(index);
                    }
                }),
                (EventTriggerType.PointerUp, _ => OnSegmentPressExit(index))
            );

            OventHandler ovent = segmentObj.AddComponent<OventHandler>();
            ovent.OnClick += btn => {
                if (IsDisposed) {
                    return;
                }

                switch (btn) {
                    case PointerEventData.InputButton.Left:
                        Set(Values[index]);
                        break;

                    case PointerEventData.InputButton.Middle:
                        if (Ctx.Config.MiddleClickToDefault) {
                            Reset();
                        }

                        break;
                }
            };
        }
    }

    private void OnSegmentHoverEnter(int index) {
        if (IsDisposed || index < 0 || index >= _backgrounds.Count) {
            return;
        }

        _hovered[index] = true;
        if (_pressed[index]) {
            return;
        }

        bool selected = EqualityComparer<T>.Default.Equals(Values[index], Value);
        Color target = selected ? Ctx.Theme.ObjectActiveBright : Ctx.Theme.ButtonHover;
        Tint(index, target, 0.12f);
    }

    private void OnSegmentHoverExit(int index) {
        if (IsDisposed || index < 0 || index >= _backgrounds.Count) {
            return;
        }

        _hovered[index] = false;
        if (_pressed[index]) {
            return;
        }

        bool selected = EqualityComparer<T>.Default.Equals(Values[index], Value);
        Tint(index, selected ? ActiveColor : NormalColor, 0.12f);
    }

    private void OnSegmentPressEnter(int index) {
        if (IsDisposed || index < 0 || index >= _backgrounds.Count) {
            return;
        }

        _pressed[index] = true;
        bool selected = EqualityComparer<T>.Default.Equals(Values[index], Value);
        Tint(index, selected ? Ctx.Theme.ObjectActiveBright : Ctx.Theme.ButtonHover, 0.08f);
    }

    private void OnSegmentPressExit(int index) {
        if (IsDisposed || index < 0 || index >= _backgrounds.Count || !_pressed[index]) {
            return;
        }

        _pressed[index] = false;
        bool selected = EqualityComparer<T>.Default.Equals(Values[index], Value);
        Color settle = selected
            ? (_hovered[index] ? Ctx.Theme.ObjectActiveBright : ActiveColor)
            : (_hovered[index] ? Ctx.Theme.ButtonHover : NormalColor);
        Tint(index, settle, 0.12f);
    }

    private void Tint(int index, Color target, float duration) {
        _hoverTweens[index]?.Kill();
        var bg = _backgrounds[index];
        if (!bg) {
            return;
        }

        _hoverTweens[index] = Ctx.Tween.TweenColor(() => bg.color, v => bg.color = v, target, duration);
    }

    /// <summary>Refreshes selection tint, label style and changed-dot.</summary>
    /// <param name="noAnimate">Snap instead of animating.</param>
    public void UpdateVisual(bool noAnimate = false) {
        if (IsDisposed) {
            return;
        }

        var theme = Ctx.Theme;
        bool isDefault = DefaultValue == null || EqualityComparer<T>.Default.Equals(DefaultValue, Value);

        if (ChangedImage) {
            Color c = ChangedImage.color;
            c.a = isDefault ? 0f : 1f;
            ChangedImage.color = c;
        }

        for (int i = 0; i < _backgrounds.Count && i < Values.Count; i++) {
            _pressed[i] = false;
            bool selected = EqualityComparer<T>.Default.Equals(Values[i], Value);
            Color targetBg = selected
                ? (_hovered[i] ? theme.ObjectActiveBright : ActiveColor)
                : (_hovered[i] ? theme.ButtonHover : NormalColor);

            if (noAnimate) {
                _hoverTweens[i]?.Kill();
                if (_backgrounds[i]) {
                    _backgrounds[i].color = targetBg;
                }
            } else {
                Tint(i, targetBg, 0.15f);
            }

            if (_labels[i]) {
                _labels[i].color = selected ? theme.Text : theme.TextDim;
                _labels[i].fontStyle = selected
                    ? TMPro.FontStyles.Bold
                    : TMPro.FontStyles.Normal;
            }
        }
    }

    /// <inheritdoc/>
    protected override void OnThemeChanged(O5Theme prev, O5Theme next) {
        if (O5Palette.ApproxEquals(NormalColor, prev.ObjectButton)) {
            NormalColor = next.ObjectButton;
        }
        if (O5Palette.ApproxEquals(ActiveColor, prev.ObjectActive)) {
            ActiveColor = next.ObjectActive;
        }
        if (O5Palette.ApproxEquals(BorderColor, prev.ControlOutlineIdle)) {
            BorderColor = next.ControlOutlineIdle;
        }
        if (OutlineImage) {
            OutlineImage.color = BorderColor;
        }
        if (ChangedImage) {
            ChangedImage.color = O5Palette.WithAlpha(next.ObjectActive, ChangedImage.color.a);
        }
        UpdateVisual(true);
    }

    /// <inheritdoc/>
    public override void Dispose() {
        if (IsDisposed) {
            return;
        }

        foreach (var tween in _hoverTweens) {
            tween?.Kill();
        }
        _hoverTweens.Clear();
        OnChanged = null;
        base.Dispose();
    }
}
