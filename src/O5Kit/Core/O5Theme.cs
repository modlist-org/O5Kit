// SPDX-License-Identifier: LGPL-3.0-or-later

using UnityEngine;

namespace O5Kit.Core;

/// <summary>Swappable visual theme. Derive variants with <c>with</c> expressions, apply via <see cref="O5Context.SetTheme"/>.</summary>
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

    /// <summary>Dimmed accent for off states. Derived from <see cref="ObjectActive"/>.</summary>
    public Color ObjectInactive => O5Palette.Inactive(ObjectActive, InactiveAlpha);

    /// <summary>Off-state alpha applied to <see cref="ObjectActive"/>.</summary>
    public float InactiveAlpha { get; init; } = 0.4f;

    /// <summary>Selection highlight (e.g. text selection).</summary>
    public Color MenuHover { get; init; } = new(0.635f, 0.655f, 0.878f, 0.4f);

    /// <summary>Card header background.</summary>
    public Color CardHeader { get; init; } = new Color32(76, 77, 102, 255);

    /// <summary>Card content background.</summary>
    public Color CardPanel { get; init; } = new Color32(47, 46, 58, 255);

    /// <summary>Semantic red (delete, errors).</summary>
    public Color SoftRed { get; init; } = new(0.886f, 0.404f, 0.427f, 1f);

    /// <summary>Button background on hover. Slightly brighter than <see cref="ObjectButton"/>.</summary>
    public Color ButtonHover { get; init; } = new(0.612f, 0.639f, 0.925f, 1f);

    /// <summary>Button background while pressed. Strongly brighter; settles back to hover/normal on release.</summary>
    public Color ButtonPressed { get; init; } = new(0.812f, 0.827f, 1f, 1f);

    /// <summary>Primary text.</summary>
    public Color Text { get; init; } = new(1f, 1f, 1f, 1f);

    /// <summary>Secondary text (previews, hints, inactive labels). Derived from <see cref="Text"/>.</summary>
    public Color TextDim => O5Palette.Dim(Text, TextDimAlpha);

    /// <summary>Secondary-text alpha applied to <see cref="Text"/>.</summary>
    public float TextDimAlpha { get; init; } = 0.6f;

    /// <summary>Faint text (placeholders, ghost icons). Derived from <see cref="Text"/>.</summary>
    public Color TextFaint => O5Palette.Faint(Text, TextFaintAlpha);

    /// <summary>Faint-text alpha applied to <see cref="Text"/>.</summary>
    public float TextFaintAlpha { get; init; } = 0.2f;

    /// <summary>Tooltip background.</summary>
    public Color TooltipBG { get; init; } = new(0f, 0f, 0f, 0.6f);

    /// <summary>Full-screen dim behind modal editors.</summary>
    public Color OverlayScrim { get; init; } = new(0f, 0f, 0f, 0.58f);

    /// <summary>Window/panel outline ring tint.</summary>
    public Color Outline { get; init; } = new(1f, 1f, 1f, 1f);

    /// <summary>Resting control outline ring (buttons, inputs, toggles, dropdowns). Transparent by default; the hover ring uses <see cref="ObjectActive"/>.</summary>
    public Color ControlOutline { get; init; } = new(1f, 1f, 1f, 0f);

    /// <summary>Formula input: valid result.</summary>
    public Color MathOk { get; init; } = new(0.588f, 1f, 0.569f, 1f);

    /// <summary>Formula input: clamped or partial result.</summary>
    public Color MathWarn { get; init; } = new(1f, 0.898f, 0.569f, 1f);

    /// <summary>Formula input: invalid expression.</summary>
    public Color MathErr { get; init; } = new(1f, 0.569f, 0.569f, 1f);

    /// <summary>Sprite-editor guide accent (9-slice guides, handles).</summary>
    public Color EditorGuide { get; init; } = new(0.15f, 1f, 0.25f, 1f);

    /// <summary>Dark plate under guide lines/handles for contrast.</summary>
    public Color EditorGuideShadow { get; init; } = new(0f, 0f, 0f, 0.9f);

    /// <summary>Disabled content alpha (e.g. inactive card body).</summary>
    public float DisabledContentAlpha { get; init; } = 0.42f;

    /// <summary>Sprite-editor guide alphas applied to <see cref="EditorGuide"/>.</summary>
    public float GuideIdleAlpha { get; init; } = 0.12f;

    /// <summary>Sprite-editor guide hover alpha applied to <see cref="EditorGuide"/>.</summary>
    public float GuideHoverAlpha { get; init; } = 0.32f;

    /// <summary>Empty sprite-editor workspace wash. Derived from <see cref="Text"/>.</summary>
    public Color WorkspaceEmpty => O5Palette.WithAlpha(Text, WorkspaceAlpha);

    /// <summary>Workspace-wash alpha applied to <see cref="Text"/>.</summary>
    public float WorkspaceAlpha { get; init; } = 0.08f;

    /// <summary>Color-picker red channel.</summary>
    public Color ChannelR { get; init; } = new(1f, 0.42f, 0.44f, 1f);

    /// <summary>Color-picker green channel.</summary>
    public Color ChannelG { get; init; } = new(0.48f, 0.82f, 0.48f, 1f);

    /// <summary>Color-picker blue channel.</summary>
    public Color ChannelB { get; init; } = new(0.56f, 0.56f, 0.9f, 1f);

    /// <summary>Color-picker alpha channel.</summary>
    public Color ChannelA { get; init; } = new(0.45f, 0.45f, 0.45f, 1f);

    /// <summary>Color-picker saturation channel.</summary>
    public Color ChannelS { get; init; } = new(0.38f, 0.78f, 1f, 1f);

    /// <summary>Color-picker value channel.</summary>
    public Color ChannelV { get; init; } = new(1f, 0.82f, 0.35f, 1f);

    /// <summary>Default corner radius hint for rounded sprites.</summary>
    public float CornerRadius { get; init; } = 12f;

    /// <summary>Default outline width hint.</summary>
    public float OutlineWidth { get; init; } = 2f;

    /// <summary>Default control row height. Floor for <see cref="ControlHeightFor"/>.</summary>
    public float ControlHeight { get; init; } = 50f;

    /// <summary>Vertical padding inside a control row, used to derive height from font size.</summary>
    public float ControlPaddingY { get; init; } = 8f;

    /// <summary>Line-height multiplier used to derive control height from font size.</summary>
    public float LineHeight { get; init; } = 1.35f;

    /// <summary>Row height for a font size. Never below <see cref="ControlHeight"/>.</summary>
    /// <param name="fontSize">Font size driving the content.</param>
    public float ControlHeightFor(float fontSize)
        => System.Math.Max(ControlHeight, (fontSize * LineHeight) + (ControlPaddingY * 2f));

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
        ButtonHover = new Color(0.332f, 0.376f, 0.81f, 1f),
        ButtonPressed = new Color(0.20f, 0.25f, 0.75f, 1f),
        Text = new Color(0.1f, 0.1f, 0.14f, 1f),
        TooltipBG = new Color(0.95f, 0.95f, 0.98f, 0.95f),
        Outline = new Color(0.1f, 0.1f, 0.14f, 0.5f),
        EditorGuide = new Color(0f, 0.55f, 0.2f, 1f),
        EditorGuideShadow = new Color(1f, 1f, 1f, 0.9f),
        WorkspaceAlpha = 0.06f,
    };
}
