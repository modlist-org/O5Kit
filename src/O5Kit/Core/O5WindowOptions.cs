// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using UnityEngine;

namespace O5Kit.Core;

/// <summary>Window appearance and behaviour. Every field is customizable; defaults match Overlayer.</summary>
public sealed class O5WindowOptions {
    /// <summary>Stable identifier. Defaults to the title.</summary>
    public string? Id { get; set; }

    /// <summary>Top bar title.</summary>
    public string Title { get; set; } = "Window";

    /// <summary>Initial panel size.</summary>
    public Vector2 Size { get; set; } = new(640f, 480f);

    /// <summary>Initial anchored position. Null centers the window.</summary>
    public Vector2? Position { get; set; }

    /// <summary>Header icon. Null hides the icon slot.</summary>
    public Sprite? Icon { get; set; }

    /// <summary>Header icon size. Defaults to 40.</summary>
    public float IconSize { get; set; } = 40f;

    /// <summary>Show the close button.</summary>
    public bool ShowCloseButton { get; set; } = true;

    /// <summary>Allow dragging by the top bar.</summary>
    public bool Draggable { get; set; } = true;

    /// <summary>Add edge/corner resize handles.</summary>
    public bool Resizable { get; set; }

    /// <summary>Show the outline border.</summary>
    public bool ShowOutline { get; set; } = true;

    /// <summary>Clip children to the panel rect.</summary>
    public bool ClipContent { get; set; }

    /// <summary>Top bar height. Defaults to 60.</summary>
    public float TopBarHeight { get; set; } = 60f;

    /// <summary>Content padding (left, right, top, bottom). Defaults to 12 on all sides.</summary>
    public RectOffset Padding { get; set; } = new(12, 12, 12, 12);

    /// <summary>Panel background. Null uses the theme.</summary>
    public Color? PanelColor { get; set; }

    /// <summary>Top bar background. Null uses the theme.</summary>
    public Color? TopBarColor { get; set; }

    /// <summary>Title font size. Null uses the theme body size.</summary>
    public float? TitleFontSize { get; set; }

    /// <summary>Title text color. Null uses white.</summary>
    public Color? TitleColor { get; set; }

    /// <summary>Bring to front when focused.</summary>
    public bool BringToFrontOnFocus { get; set; } = true;
}
