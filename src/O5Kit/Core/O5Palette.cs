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

    /// <summary>Converts sRGB [0..1] to OKLCH (L in [0..1], C in [0..~0.4], H in [0..1]).</summary>
    public static void RgbToOklch(float r, float g, float b, out float l, out float c, out float h) {
        float lr = SrgbToLinear(Clamp01(r));
        float lg = SrgbToLinear(Clamp01(g));
        float lb = SrgbToLinear(Clamp01(b));

        float lmsL = (0.4122214708f * lr) + (0.5363325363f * lg) + (0.0514459929f * lb);
        float lmsM = (0.2119034982f * lr) + (0.6806995451f * lg) + (0.1073969566f * lb);
        float lmsS = (0.0883024619f * lr) + (0.2817188376f * lg) + (0.6299787005f * lb);

        float l_ = Cbrt(lmsL);
        float m_ = Cbrt(lmsM);
        float s_ = Cbrt(lmsS);

        l = Clamp01((0.2104542553f * l_) + (0.7936177850f * m_) - (0.0040720468f * s_));
        float labA = (1.9779984951f * l_) - (2.4285922050f * m_) + (0.4505937099f * s_);
        float labB = (0.0259040371f * l_) + (0.7827717662f * m_) - (0.8086757660f * s_);

        c = (float)Math.Sqrt((labA * labA) + (labB * labB));
        if (c < 1e-5f) {
            c = 0f;
            h = 0f;
            return;
        }

        h = (float)(Math.Atan2(labB, labA) / (2.0 * Math.PI));
        if (h < 0f) {
            h += 1f;
        }
    }

    /// <summary>Converts OKLCH (L in [0..1], C >= 0, H in [0..1]) to gamut-clipped sRGB.</summary>
    public static Color OklchToRgb(float l, float c, float h, float a = 1f) {
        l = Clamp01(l);
        if (l <= 0f) {
            return new Color(0f, 0f, 0f, a);
        }
        if (l >= 1f) {
            return new Color(1f, 1f, 1f, a);
        }

        c = Math.Max(0f, c);
        h %= 1f;
        if (h < 0f) {
            h += 1f;
        }

        double rad = h * 2.0 * Math.PI;
        float cosH = (float)Math.Cos(rad);
        float sinH = (float)Math.Sin(rad);

        OklabToLinearRgb(l, c * cosH, c * sinH, out float lr, out float lg, out float lb);
        if (!IsInLinearSrgbGamut(lr, lg, lb)) {
            float lo = 0f;
            float hi = c;
            for (int i = 0; i < 12; i++) {
                float mid = (lo + hi) * 0.5f;
                OklabToLinearRgb(l, mid * cosH, mid * sinH, out float tr, out float tg, out float tb);
                if (IsInLinearSrgbGamut(tr, tg, tb)) {
                    lo = mid;
                } else {
                    hi = mid;
                }
            }
            OklabToLinearRgb(l, lo * cosH, lo * sinH, out lr, out lg, out lb);
        }

        return new Color(
            Clamp01(LinearToSrgb(lr)),
            Clamp01(LinearToSrgb(lg)),
            Clamp01(LinearToSrgb(lb)),
            a
        );
    }

    /// <summary>Returns the maximum in-gamut sRGB chroma for the given OKLCH lightness and hue [0..1].</summary>
    public static float MaxSrgbChroma(float l, float h) {
        l = Clamp01(l);
        if (l <= 0f || l >= 1f) {
            return 0f;
        }

        h %= 1f;
        if (h < 0f) {
            h += 1f;
        }

        double rad = h * 2.0 * Math.PI;
        return MaxSrgbChroma(l, (float)Math.Cos(rad), (float)Math.Sin(rad));
    }

    internal static float MaxSrgbChroma(float l, float cosH, float sinH) {
        if (l <= 0f || l >= 1f) {
            return 0f;
        }

        float lo = 0f;
        float hi = 0.4f;
        for (int i = 0; i < 14; i++) {
            float mid = (lo + hi) * 0.5f;
            OklabToLinearRgb(l, mid * cosH, mid * sinH, out float lr, out float lg, out float lb);
            if (IsInLinearSrgbGamut(lr, lg, lb)) {
                lo = mid;
            } else {
                hi = mid;
            }
        }
        return lo;
    }

    internal static bool TryOklchToLinearRgb(float l, float c, float cosH, float sinH, out float lr, out float lg, out float lb) {
        OklabToLinearRgb(l, c * cosH, c * sinH, out lr, out lg, out lb);
        return IsInLinearSrgbGamut(lr, lg, lb);
    }

    /// <summary>
    /// Derives a perceptually uniform UI color from a single base color's OKLCH hue using target chroma and lightness.
    /// Low-chroma base colors smoothly desaturate toward neutral charcoal/slate.
    /// </summary>
    public static Color ShadeOklch(Color baseColor, float chroma, float lightness, float alpha = 1f, bool adaptBaseLightness = false) {
        RgbToOklch(Clamp01(baseColor.r), Clamp01(baseColor.g), Clamp01(baseColor.b), out float baseL, out float baseC, out float h);
        float chromaScale = baseC > 0.001f ? Clamp01(baseC / 0.11f) : 0f;
        float effC = Math.Max(0f, chroma * chromaScale);
        float effL = Clamp01(lightness);

        const float neutralL = 0.65f;
        if (adaptBaseLightness) {
            if (baseL <= neutralL) {
                effL *= 0.68f + (0.32f * (baseL / neutralL));
            } else {
                float up = (baseL - neutralL) / (1f - neutralL);
                effL += (1f - effL) * (up * 0.18f);
            }
        } else if (lightness < 0.40f) {
            if (baseL <= neutralL) {
                effL *= 0.72f + (0.28f * (baseL / neutralL));
            } else {
                float up = (baseL - neutralL) / (1f - neutralL);
                effL *= 1f + (up * 0.10f);
            }
        }

        return OklchToRgb(effL, effC, h, alpha);
    }

    /// <summary>Builds a cohesive dark UI theme derived strictly from a single base hue/color using OKLCH.</summary>
    public static O5Theme FromMonochrome(Color baseColor) {
        Color active = ShadeOklch(baseColor, 0.140f, 0.615f, 1f, adaptBaseLightness: true);
        Color activeBright = ShadeOklch(baseColor, 0.105f, 0.775f, 1f, adaptBaseLightness: true);
        Color button = active;
        Color buttonHover = ShadeOklch(baseColor, 0.128f, 0.715f, 1f, adaptBaseLightness: true);
        Color buttonPressed = ShadeOklch(baseColor, 0.088f, 0.825f, 1f, adaptBaseLightness: true);
        Color text = ShadeOklch(baseColor, 0.008f, 0.980f);

        return new O5Theme {
            PanelBG = ShadeOklch(baseColor, 0.010f, 0.175f),
            TopBar = ShadeOklch(baseColor, 0.020f, 0.222f),
            MenuBG = ShadeOklch(baseColor, 0.016f, 0.205f),
            ObjectBG = ShadeOklch(baseColor, 0.024f, 0.242f),
            ObjectButton = button,
            ObjectActive = active,
            ObjectActiveBright = activeBright,
            MenuHover = ShadeOklch(baseColor, 0.125f, 0.560f, 0.22f, adaptBaseLightness: true),
            CardHeader = ShadeOklch(baseColor, 0.032f, 0.285f),
            CardPanel = ShadeOklch(baseColor, 0.014f, 0.195f),
            ButtonHover = buttonHover,
            ButtonPressed = buttonPressed,
            Text = text,
            TooltipBG = ShadeOklch(baseColor, 0.012f, 0.155f, 0.94f),
            Outline = ShadeOklch(baseColor, 0.045f, 0.780f, 0.20f),
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

    private static float Cbrt(float x) => x >= 0f
        ? (float)Math.Pow(x, 1.0 / 3.0)
        : -(float)Math.Pow(-x, 1.0 / 3.0);

    private static float SrgbToLinear(float c) => c <= 0.04045f
        ? c / 12.92f
        : (float)Math.Pow((c + 0.055) / 1.055, 2.4);

    private static float LinearToSrgb(float c) => c <= 0.0031308f
        ? 12.92f * c
        : (float)((1.055 * Math.Pow(c, 1.0 / 2.4)) - 0.055);

    private static void OklabToLinearRgb(float l, float a, float b, out float lr, out float lg, out float lb) {
        float l_ = l + (0.3963377774f * a) + (0.2158037573f * b);
        float m_ = l - (0.1055613458f * a) - (0.0638541728f * b);
        float s_ = l - (0.0894841775f * a) - (1.2914855480f * b);

        float lmsL = l_ * l_ * l_;
        float lmsM = m_ * m_ * m_;
        float lmsS = s_ * s_ * s_;

        lr = (+4.0767416621f * lmsL) - (3.3077115913f * lmsM) + (0.2309699292f * lmsS);
        lg = (-1.2684380046f * lmsL) + (2.6097574011f * lmsM) - (0.3413193965f * lmsS);
        lb = (-0.0041960863f * lmsL) - (0.7034186147f * lmsM) + (1.7076147010f * lmsS);
    }

    private static bool IsInLinearSrgbGamut(float r, float g, float b) {
        const float eps = 1e-5f;
        return r >= -eps && r <= 1f + eps
            && g >= -eps && g <= 1f + eps
            && b >= -eps && b <= 1f + eps;
    }
}
