// SPDX-License-Identifier: LGPL-3.0-or-later

using System;

namespace O5Kit.Control;

/// <summary>Slider appearance and behaviour. Replaces the old 21-arg factory/ctor signatures.</summary>
public sealed class O5SliderOptions {
    /// <summary>Drag range lower bound.</summary>
    public double Min { get; set; }

    /// <summary>Drag range upper bound.</summary>
    public double Max { get; set; } = 100d;

    /// <summary>Initial value.</summary>
    public double Value { get; set; }

    /// <summary>Value to reset to on middle-click. Null disables reset and hides the dot.</summary>
    public double? DefaultValue { get; set; }

    /// <summary>Display format, e.g. <c>"F2"</c>.</summary>
    public string Format { get; set; } = "F2";

    /// <summary>Label text next to the fill bar.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Clamp drags/code-sets into <c>[Min, Max]</c>. Old <c>ClampMode.Slider | All</c>.</summary>
    public bool ClampDrag { get; set; } = true;

    /// <summary>Clamp text input into the text-input range. Old <c>ClampMode.Input | All</c>.</summary>
    public bool ClampInput { get; set; } = true;

    /// <summary>Text-input lower bound. Null falls back to <see cref="Min"/>.</summary>
    public double? InputMin { get; set; }

    /// <summary>Text-input upper bound. Null falls back to <see cref="Max"/>.</summary>
    public double? InputMax { get; set; }

    /// <summary>Value transform applied on drag/code-set. Null means identity.</summary>
    public Func<double, double>? DragFilter { get; set; }

    /// <summary>Value transform applied on text input. Null means identity (free input).</summary>
    public Func<double, double>? InputFilter { get; set; }

    /// <summary>Fill/preview mapping. Null means linear.</summary>
    public IValueScale? Scale { get; set; }

    /// <summary>Fired on every value change.</summary>
    public Action<double>? OnChanged { get; set; }

    /// <summary>Fired when a drag or text edit commits.</summary>
    public Action<double>? OnComplete { get; set; }

    /// <summary>Whether the fill bar is shown.</summary>
    public bool ShowFill { get; set; } = true;

    /// <summary>Value per pixel. Null scales by control width.</summary>
    public double? DragStep { get; set; }

    /// <summary>Cover the canvas with a drag blocker while dragging.</summary>
    public bool BlockHoverWhileDragging { get; set; }
}
