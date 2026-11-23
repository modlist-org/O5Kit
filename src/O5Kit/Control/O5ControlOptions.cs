// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Collections.Generic;
using UnityEngine;

namespace O5Kit.Control;

/// <summary>Button appearance and behaviour.</summary>
public sealed class O5ButtonOptions {
    /// <summary>Text label. Null for icon buttons.</summary>
    public string? Text { get; set; }

    /// <summary>Icon sprite. Null for text buttons.</summary>
    public Sprite? Icon { get; set; }

    /// <summary>Icon inset. Text buttons ignore this.</summary>
    public float IconPadding { get; set; } = 5f;

    /// <summary>Explicit height. Null uses the theme control height.</summary>
    public float? Height { get; set; }

    /// <summary>Click callback.</summary>
    public Action? OnClick { get; set; }
}

/// <summary>Toggle appearance and behaviour.</summary>
public sealed class O5ToggleOptions {
    /// <summary>Value to reset to on middle-click. Null disables reset and hides the dot.</summary>
    public bool? DefaultValue { get; set; }

    /// <summary>Initial value.</summary>
    public bool Value { get; set; }

    /// <summary>Label text.</summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>Fired when the value changes.</summary>
    public Action<bool>? OnChanged { get; set; }
}

/// <summary>Single-select dropdown behaviour.</summary>
public sealed class O5DropdownOptions<T> {
    /// <summary>Value to reset to on middle-click.</summary>
    public T? DefaultValue { get; set; }

    /// <summary>Initial value.</summary>
    public T? Value { get; set; }

    /// <summary>Available options.</summary>
    public IReadOnlyList<T> Values { get; set; } = Array.Empty<T>();

    /// <summary>Renders an option as text.</summary>
    public Func<T, string> Display { get; set; } = v => v?.ToString() ?? string.Empty;

    /// <summary>Fired when the value changes.</summary>
    public Action<T?>? OnChanged { get; set; }

    /// <summary>Show the selected-index indicator under the header. Set false for action/button-style dropdowns.</summary>
    public bool ShowIndexIndicator { get; set; } = true;
}

/// <summary>Flag-style multi-select dropdown behaviour.</summary>
public sealed class O5MultiDropdownOptions<T> where T : struct, Enum {
    /// <summary>Value to reset to on middle-click.</summary>
    public T? DefaultValue { get; set; }

    /// <summary>Initial combined flags value.</summary>
    public T Value { get; set; }

    /// <summary>Available flags.</summary>
    public IReadOnlyList<T> Values { get; set; } = Array.Empty<T>();

    /// <summary>Renders a flag as row text.</summary>
    public Func<T, string> Display { get; set; } = v => v.ToString();

    /// <summary>Renders the combined value as header text.</summary>
    public Func<T, string> Summary { get; set; } = v => v.ToString();

    /// <summary>Fired when the value changes.</summary>
    public Action<T>? OnChanged { get; set; }
}

/// <summary>Text field behaviour.</summary>
public sealed class O5InputOptions {
    /// <summary>Value to reset to on middle-click. Null disables reset.</summary>
    public string? DefaultValue { get; set; }

    /// <summary>Initial text.</summary>
    public string? Value { get; set; }

    /// <summary>Placeholder text.</summary>
    public string Placeholder { get; set; } = string.Empty;

    /// <summary>Trailing icon. Null hides it.</summary>
    public Sprite? Icon { get; set; }

    /// <summary>Change callback.</summary>
    public Action<string>? OnChanged { get; set; }

    /// <summary>End-edit callback.</summary>
    public Action<string>? OnEndEdit { get; set; }

    /// <summary>Multi-line mode.</summary>
    public bool Multiline { get; set; }

    /// <summary>Monospace font.</summary>
    public bool Monospace { get; set; }

    /// <summary>Custom field builder (e.g. a code editor field). Null uses <see cref="TMPro.TMP_InputField"/>.</summary>
    public Func<GameObject, TMPro.TMP_InputField>? FieldFactory { get; set; }
}

/// <summary>Color picker behaviour.</summary>
public sealed class O5ColorPickerOptions {
    /// <summary>Popup coordinate space (usually the canvas rect). Required.</summary>
    public RectTransform? CanvasRect { get; set; }

    /// <summary>Camera for hover checks. Null for overlay canvases.</summary>
    public Camera? CanvasCamera { get; set; }

    /// <summary>Value to reset to on middle-click.</summary>
    public Color? DefaultValue { get; set; }

    /// <summary>Initial color.</summary>
    public Color Value { get; set; } = Color.white;

    /// <summary>Preview label. Empty hides it.</summary>
    public string? Label { get; set; }

    /// <summary>Fired on every change.</summary>
    public Action<Color>? OnChanged { get; set; }

    /// <summary>Fired on commit.</summary>
    public Action<Color>? OnComplete { get; set; }
}

/// <summary>Segmented single-select behaviour (compact alternative to a dropdown for 2-4 options).</summary>
public sealed class O5SegmentedOptions<T> {
    /// <summary>Value to reset to on middle-click. Null disables reset and hides the dot.</summary>
    public T? DefaultValue { get; set; }

    /// <summary>Initial value.</summary>
    public T? Value { get; set; }

    /// <summary>Available options.</summary>
    public IReadOnlyList<T> Values { get; set; } = Array.Empty<T>();

    /// <summary>Renders an option as text.</summary>
    public Func<T, string> Display { get; set; } = v => v?.ToString() ?? string.Empty;

    /// <summary>Explicit height. Null uses the theme control height.</summary>
    public float? Height { get; set; }

    /// <summary>Label font size. Null uses the theme body size.</summary>
    public float? FontSize { get; set; }

    /// <summary>Resting segment color. Null uses the theme button color.</summary>
    public Color? NormalColor { get; set; }

    /// <summary>Selected segment color. Null uses the theme active color.</summary>
    public Color? ActiveColor { get; set; }

    /// <summary>Outline color. Null uses the theme idle outline color.</summary>
    public Color? BorderColor { get; set; }

    /// <summary>Fired when the value changes.</summary>
    public Action<T?>? OnChanged { get; set; }
}
