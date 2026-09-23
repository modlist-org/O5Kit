// SPDX-License-Identifier: LGPL-3.0-or-later

using UnityEngine;

namespace O5Kit.Core;

/// <summary>Swappable visual theme. Derive variants with <c>with</c> expressions, apply via <see cref="O5Boot.SetTheme"/>.</summary>
public sealed record O5Theme {
    /// <summary>Main window background.</summary>
    public Color PanelBG { get; init; } = new(0.145f, 0.141f, 0.180f, 1f);

    /// <summary>Window top bar background.</summary>
    public Color TopBar { get; init; } = new(0.255f, 0.259f, 0.333f, 1f);

    /// <summary>Side menu background.</summary>
    public Color MenuBG { get; init; } = new(0.42f, 0.431f, 0.545f, 1f);

    /// <summary>Default control background.</summary>
    public Color ObjectBG { get; init; } = new(0.235f, 0.227f, 0.294f, 1f);

    /// <summary>Button background.</summary>
    public Color ObjectButton { get; init; } = new(0.478f, 0.514f, 0.875f, 1f);

    /// <summary>Accent for on/hover/selected states.</summary>
    public Color ObjectActive { get; init; } = new(0.569f, 0.604f, 1f, 1f);

    /// <summary>Brighter accent for button hover.</summary>
    public Color ObjectActiveBright { get; init; } = new(0.812f, 0.827f, 1f, 1f);

    /// <summary>Dimmed accent for off states.</summary>
    public Color ObjectInactive { get; init; } = new(0.569f, 0.604f, 1f, 0.4f);

    /// <summary>Selection highlight (e.g. text selection).</summary>
    public Color MenuHover { get; init; } = new(0.635f, 0.655f, 0.878f, 0.4f);

    /// <summary>Card header background.</summary>
    public Color CardHeader { get; init; } = new Color32(76, 77, 102, 255);

    /// <summary>Card content background.</summary>
    public Color CardPanel { get; init; } = new Color32(47, 46, 58, 255);

    /// <summary>Semantic red (delete, errors).</summary>
    public Color SoftRed { get; init; } = new(0.886f, 0.404f, 0.427f, 1f);

    /// <summary>Formula input: valid result.</summary>
    public Color MathOk { get; init; } = new(0.588f, 1f, 0.569f, 1f);

    /// <summary>Formula input: clamped or partial result.</summary>
    public Color MathWarn { get; init; } = new(1f, 0.898f, 0.569f, 1f);

    /// <summary>Formula input: invalid expression.</summary>
    public Color MathErr { get; init; } = new(1f, 0.569f, 0.569f, 1f);

    /// <summary>Default corner radius hint for rounded sprites.</summary>
    public float CornerRadius { get; init; } = 12f;

    /// <summary>Default outline width hint.</summary>
    public float OutlineWidth { get; init; } = 2f;

    /// <summary>Default control row height.</summary>
    public float ControlHeight { get; init; } = 50f;

    /// <summary>Default body font size.</summary>
    public float FontSizeBody { get; init; } = 24f;

    /// <summary>Default heading font size.</summary>
    public float FontSizeH1 { get; init; } = 32f;

    /// <summary>Default dark theme.</summary>
    public static O5Theme Dark { get; } = new();

    /// <summary>Original Overlayer palette.</summary>
    public static O5Theme Overlayer { get; } = new() {
        PanelBG = new Color(0.165f, 0.161f, 0.196f, 1f),
    };

    /// <summary>Light theme alternative.</summary>
    public static O5Theme Light { get; } = new() {
        PanelBG = new Color(0.93f, 0.93f, 0.95f, 1f),
        TopBar = new Color(0.82f, 0.83f, 0.90f, 1f),
        MenuBG = new Color(0.78f, 0.79f, 0.88f, 1f),
        ObjectBG = new Color(0.88f, 0.88f, 0.92f, 1f),
        ObjectButton = new Color(0.42f, 0.46f, 0.85f, 1f),
        ObjectActive = new Color(0.35f, 0.40f, 0.90f, 1f),
        ObjectActiveBright = new Color(0.20f, 0.25f, 0.75f, 1f),
        ObjectInactive = new Color(0.35f, 0.40f, 0.90f, 0.4f),
    };
}
