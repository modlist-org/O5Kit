// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using UnityEngine;

namespace O5Kit.Core;

/// <summary>Alpha-only and single-hue monochrome color derivations so a base hue change propagates cleanly.</summary>
public static class O5Palette {
    /// <summary>Default Overlayer base color (H=243°, matching Overlayer's original purple-slate / periwinkle family).</summary>
    public static readonly Color DefaultBaseColor = new(126f / 255f, 120f / 255f, 240f / 255f, 1f);

    /// <summary>Default Overlayer base hex (RRGGBB).</summary>
    public const string DefaultBaseHex = "7E78F0";

    /// <summary>Returns <paramref name="c"/> with its alpha replaced by <paramref name="a"/>.</summary>
    public static Color WithAlpha(Color c, float a) => new(c.r, c.g, c.b, a);

    /// <summary>Secondary text from a base text color.</summary>
    public static Color Dim(Color text, float alpha) => WithAlpha(text, alpha);

    /// <summary>Faint text/placeholder from a base text color.</summary>
    public static Color Faint(Color text, float alpha) => WithAlpha(text, alpha);

    /// <summary>Off-state accent from the on-state accent.</summary>
    public static Color Inactive(Color active, float alpha) => WithAlpha(active, alpha);

    /// <summary>Fully transparent variant (e.g. hidden dots, idle hover rings).</summary>
    public static Color Transparent(Color c) => WithAlpha(c, 0f);

    /// <summary>Pure-managed RGB to HSV conversion (works without Unity native runtime).</summary>
    public static void RgbToHsv(float r, float g, float b, out float h, out float s, out float v) {
        float max = Math.Max(r, Math.Max(g, b));
        float min = Math.Min(r, Math.Min(g, b));
        float delta = max - min;
        v = max;
        s = max > 1e-5f ? delta / max : 0f;

        if (delta <= 1e-5f) {
            h = 0f;
            return;
        }

        if (max == r) {
            h = (g - b) / delta;
            if (h < 0f) {
                h += 6f;
            }
        } else if (max == g) {
            h = 2f + ((b - r) / delta);
        } else {
            h = 4f + ((r - g) / delta);
        }

        h /= 6f;
    }

    /// <summary>Pure-managed HSV to RGB conversion (works without Unity native runtime).</summary>
    public static Color HsvToRgb(float h, float s, float v, float a = 1f) {
        h %= 1f;
        if (h < 0f) {
            h += 1f;
        }
        s = Clamp01(s);
        v = Clamp01(v);

        if (s <= 1e-5f) {
            return new Color(v, v, v, a);
        }

        float sector = h * 6f;
        int i = (int)Math.Floor(sector) % 6;
        float f = sector - (float)Math.Floor(sector);
        float p = v * (1f - s);
        float q = v * (1f - (s * f));
        float t = v * (1f - (s * (1f - f)));

        return i switch {
            0 => new Color(v, t, p, a),
            1 => new Color(q, v, p, a),
            2 => new Color(p, v, t, a),
            3 => new Color(p, q, v, a),
            4 => new Color(t, p, v, a),
            _ => new Color(v, p, q, a),
        };
    }

    /// <summary>
    /// Derives a UI color from a single base color's hue using target saturation and value.
    /// Low-saturation base colors smoothly desaturate the palette toward neutral charcoal/slate.
    /// When <paramref name="adaptLuminance"/> is true, naturally bright hues (yellow, cyan, green)
    /// are gently deepened so white foreground text remains legible.
    /// </summary>
    public static Color Shade(Color baseColor, float saturation, float value, float alpha = 1f, bool adaptLuminance = false) {
        RgbToHsv(Clamp01(baseColor.r), Clamp01(baseColor.g), Clamp01(baseColor.b), out float h, out float baseS, out float baseV);
        float chroma = baseS > 0.001f ? Clamp01(baseS / 0.35f) : 0f;
        float effS = Clamp01(saturation * chroma);
        float effV = Clamp01(value);

        if (adaptLuminance) {
            Color sample = HsvToRgb(h, 0.50f * chroma, 1f, 1f);
            float hueLum = (0.2126f * sample.r) + (0.7152f * sample.g) + (0.0722f * sample.b);
            float lumScale = Math.Min(1f, 0.56f / Math.Max(0.56f, hueLum));
            effV *= 0.65f + (0.35f * lumScale);
        }

        // Let baseColor's brightness (V) subtly deepen or lift surfaces and accents
        // while keeping text legible and preserving the button highlight ordering.
        const float neutralV = 0.90f;
        if (adaptLuminance) {
            if (baseV <= neutralV) {
                effV *= 0.56f + (0.44f * (baseV / neutralV));
            } else {
                float up = (baseV - neutralV) / (1f - neutralV);
                effV += (1f - effV) * (up * 0.22f);
            }
        } else if (value < 0.35f) {
            if (baseV <= neutralV) {
                effV *= 0.55f + (0.45f * (baseV / neutralV));
            } else {
                float up = (baseV - neutralV) / (1f - neutralV);
                effV *= 1f + (up * 0.14f);
            }
        }

        return HsvToRgb(h, effS, effV, alpha);
    }

    /// <summary>Builds a cohesive dark UI theme derived strictly from a single base hue/color.</summary>
    public static O5Theme FromMonochrome(Color baseColor) {
        Color active = Shade(baseColor, 0.51f, 0.785f, 1f, adaptLuminance: true);
        Color activeBright = Shade(baseColor, 0.34f, 0.945f, 1f, adaptLuminance: true);
        Color button = active;
        Color buttonHover = Shade(baseColor, 0.43f, 0.895f, 1f, adaptLuminance: true);
        Color buttonPressed = Shade(baseColor, 0.28f, 0.975f, 1f, adaptLuminance: true);
        Color text = Shade(baseColor, 0.03f, 0.985f, 1f);

        return new O5Theme {
            PanelBG = Shade(baseColor, 0.17f, 0.120f),
            TopBar = Shade(baseColor, 0.21f, 0.168f),
            MenuBG = Shade(baseColor, 0.19f, 0.150f),
            ObjectBG = Shade(baseColor, 0.21f, 0.190f),
            ObjectButton = button,
            ObjectActive = active,
            ObjectActiveBright = activeBright,
            MenuHover = Shade(baseColor, 0.48f, 0.720f, 0.22f, adaptLuminance: true),
            CardHeader = Shade(baseColor, 0.24f, 0.235f),
            CardPanel = Shade(baseColor, 0.18f, 0.145f),
            ButtonHover = buttonHover,
            ButtonPressed = buttonPressed,
            Text = text,
            TooltipBG = Shade(baseColor, 0.18f, 0.085f, 0.94f),
            Outline = Shade(baseColor, 0.22f, 0.88f, 0.20f),
            ControlOutlineIdle = new Color(1f, 1f, 1f, 0f),
            ControlOutlineHover = active,
        };
    }

    /// <summary>Component-wise approximate equality for matching theme colors.</summary>
    public static bool ApproxEquals(Color a, Color b, float epsilon = 0.006f)
        => Math.Abs(a.r - b.r) <= epsilon
        && Math.Abs(a.g - b.g) <= epsilon
        && Math.Abs(a.b - b.b) <= epsilon
        && Math.Abs(a.a - b.a) <= epsilon;

    /// <summary>RGB-only approximate equality (ignoring alpha).</summary>
    public static bool ApproxRgbEquals(Color a, Color b, float epsilon = 0.006f)
        => Math.Abs(a.r - b.r) <= epsilon
        && Math.Abs(a.g - b.g) <= epsilon
        && Math.Abs(a.b - b.b) <= epsilon;

    /// <summary>Formats RGB channels as uppercase 6-char hex.</summary>
    public static string ToHexRgb(Color c) {
        int r = (int)Math.Round(Clamp01(c.r) * 255f);
        int g = (int)Math.Round(Clamp01(c.g) * 255f);
        int b = (int)Math.Round(Clamp01(c.b) * 255f);
        return $"{r:X2}{g:X2}{b:X2}";
    }

    /// <summary>Parses a 6- or 8-char hex string (with optional leading #) into an opaque RGB color.</summary>
    public static bool TryParseHexRgb(string? hex, out Color color) {
        color = DefaultBaseColor;
        if (string.IsNullOrWhiteSpace(hex)) {
            return false;
        }
        string s = hex!.Trim();
        if (s.StartsWith("#", StringComparison.Ordinal)) {
            s = s.Substring(1);
        }
        if (s.Length != 6 && s.Length != 8) {
            return false;
        }
        if (!TryHexByte(s, 0, out byte r) || !TryHexByte(s, 2, out byte g) || !TryHexByte(s, 4, out byte b)) {
            return false;
        }
        color = new Color(r / 255f, g / 255f, b / 255f, 1f);
        return true;
    }

    private static bool TryHexByte(string s, int offset, out byte value) {
        value = 0;
        int hi = HexNibble(s[offset]);
        int lo = HexNibble(s[offset + 1]);
        if (hi < 0 || lo < 0) {
            return false;
        }
        value = (byte)((hi << 4) | lo);
        return true;
    }

    private static int HexNibble(char c) => c switch {
        >= '0' and <= '9' => c - '0',
        >= 'a' and <= 'f' => 10 + (c - 'a'),
        >= 'A' and <= 'F' => 10 + (c - 'A'),
        _ => -1,
    };

    private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
}
