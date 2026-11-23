// SPDX-License-Identifier: LGPL-3.0-or-later

using O5Kit.Core;

namespace O5Kit.Tests;

/// <summary>O5Palette alpha derivations. Field-wise asserts only (no Unity method calls).</summary>
internal static class PaletteTests {
    public static void Run() {
        var c = new UnityEngine.Color(1f, 0.5f, 0.25f, 1f);

        var w = O5Palette.WithAlpha(c, 0.5f);
        T.Approx(1f, w.r, "withalpha r");
        T.Approx(0.5f, w.g, "withalpha g");
        T.Approx(0.25f, w.b, "withalpha b");
        T.Approx(0.5f, w.a, "withalpha a");

        T.Approx(0.3f, O5Palette.Dim(c, 0.3f).a, "dim alpha passthrough");
        T.Approx(1f, O5Palette.Dim(c, 0.3f).r, "dim keeps rgb");
        T.Approx(0.1f, O5Palette.Faint(c, 0.1f).a, "faint alpha");
        T.Approx(0.4f, O5Palette.Inactive(c, 0.4f).a, "inactive alpha");

        var t = O5Palette.Transparent(c);
        T.Approx(0f, t.a, "transparent alpha");
        T.Approx(1f, t.r, "transparent keeps r");

        // alpha extremes are exact, not clamped
        T.Approx(0f, O5Palette.WithAlpha(c, 0f).a, "alpha 0");
        T.Approx(1f, O5Palette.WithAlpha(c, 1f).a, "alpha 1");

        // hex parse & round-trip
        T.Check(O5Palette.TryParseHexRgb(O5Palette.DefaultBaseHex, out var overlayer), "parse default overlayer");
        T.Check(O5Palette.ToHexRgb(overlayer) == O5Palette.DefaultBaseHex, "roundtrip default hex");
        T.Check(O5Palette.TryParseHexRgb("#4E9EF0", out var ocean), "parse leading hash");
        T.Check(O5Palette.ToHexRgb(ocean) == "4E9EF0", "roundtrip ocean hex");
        T.Check(!O5Palette.TryParseHexRgb("ZZZZZZ", out _), "reject invalid hex");

        // OKLCH round-trip across primary, accent, and neutral colors
        foreach (var sample in new[] { overlayer, ocean, c, new UnityEngine.Color(0.5f, 0.5f, 0.5f, 1f), new UnityEngine.Color(0.2f, 0.8f, 0.4f, 1f) }) {
            O5Palette.RgbToOklch(sample.r, sample.g, sample.b, out float l, out float ch, out float h);
            var back = O5Palette.OklchToRgb(l, ch, h, sample.a);
            T.Approx(sample.r, back.r, "oklch roundtrip r", 0.002f);
            T.Approx(sample.g, back.g, "oklch roundtrip g", 0.002f);
            T.Approx(sample.b, back.b, "oklch roundtrip b", 0.002f);
        }

        // monochrome derivation preserves base OKLCH hue across surfaces and accents
        var theme = O5Palette.FromMonochrome(overlayer);
        O5Palette.RgbToOklch(overlayer.r, overlayer.g, overlayer.b, out _, out _, out float baseOklchH);
        O5Palette.RgbToOklch(theme.PanelBG.r, theme.PanelBG.g, theme.PanelBG.b, out float panelL, out float panelC, out float panelH);
        O5Palette.RgbToOklch(theme.ObjectButton.r, theme.ObjectButton.g, theme.ObjectButton.b, out float btnL, out float btnC, out float btnH);
        O5Palette.RgbToOklch(theme.ObjectActive.r, theme.ObjectActive.g, theme.ObjectActive.b, out float actL, out float actC, out float actH);
        T.Approx(baseOklchH, panelH, "panel preserves base OKLCH hue", 0.015f);
        T.Approx(baseOklchH, btnH, "button preserves base OKLCH hue", 0.015f);
        T.Approx(baseOklchH, actH, "active preserves base OKLCH hue", 0.015f);
        T.Check(panelL < 0.22f && panelC < 0.02f, "panel has HUD-editor-like dark low-chroma surface");
        T.Check(theme.PanelBG.b < theme.CardPanel.b && theme.CardPanel.b < theme.ObjectBG.b && theme.ObjectBG.b < theme.CardHeader.b, "surface elevation ladder increases");
        T.Check(O5Palette.ApproxEquals(theme.ObjectButton, theme.ObjectActive), "button matches active color");
        O5Palette.RgbToOklch(theme.ButtonHover.r, theme.ButtonHover.g, theme.ButtonHover.b, out float hoverL, out _, out _);
        O5Palette.RgbToOklch(theme.ObjectActiveBright.r, theme.ObjectActiveBright.g, theme.ObjectActiveBright.b, out float brightL, out _, out _);
        O5Palette.RgbToOklch(theme.ButtonPressed.r, theme.ButtonPressed.g, theme.ButtonPressed.b, out float pressL, out _, out _);
        T.Check(btnC > panelC && btnL > panelL && actL >= btnL, "accents step up in chroma and lightness");
        T.Check(hoverL > btnL && brightL > hoverL && pressL > brightL, "button highlight ladder steps upward");

        // grayscale base produces neutral low-chroma theme
        var grayTheme = O5Palette.FromMonochrome(new UnityEngine.Color(0.5f, 0.5f, 0.5f, 1f));
        O5Palette.RgbToHsv(grayTheme.ObjectButton.r, grayTheme.ObjectButton.g, grayTheme.ObjectButton.b, out _, out float grayBtnS, out _);
        T.Approx(0f, grayBtnS, "grayscale base yields neutral button", 0.001f);

        // baseColor V meaningfully modulates surfaces & accents while preserving highlight ladder & legible text
        float hue = 243f / 360f;
        var t20 = O5Palette.FromMonochrome(O5Palette.HsvToRgb(hue, 0.49f, 0.20f));
        var t60 = O5Palette.FromMonochrome(O5Palette.HsvToRgb(hue, 0.49f, 0.60f));
        var t94 = O5Palette.FromMonochrome(O5Palette.HsvToRgb(hue, 0.49f, 0.94f));
        var t100 = O5Palette.FromMonochrome(O5Palette.HsvToRgb(hue, 0.49f, 1.00f));
        float VOf(UnityEngine.Color col) {
            O5Palette.RgbToHsv(col.r, col.g, col.b, out _, out _, out float v);
            return v;
        }
        T.Check(VOf(t20.PanelBG) < VOf(t60.PanelBG) && VOf(t60.PanelBG) < VOf(t94.PanelBG) && VOf(t94.PanelBG) < VOf(t100.PanelBG),
            "baseV modulates surface brightness monotonically");
        T.Check(VOf(t20.ObjectButton) < VOf(t60.ObjectButton) && VOf(t60.ObjectButton) < VOf(t94.ObjectButton) && VOf(t94.ObjectButton) < VOf(t100.ObjectButton),
            "baseV modulates button/accent brightness monotonically");
        T.Check(VOf(t20.ObjectButton) < VOf(t20.ButtonHover) && VOf(t20.ButtonHover) < VOf(t20.ObjectActiveBright) && VOf(t20.ObjectActiveBright) < VOf(t20.ButtonPressed),
            "highlight ladder holds at low baseV");
        T.Check(VOf(t100.ObjectButton) < VOf(t100.ButtonHover) && VOf(t100.ButtonHover) < VOf(t100.ObjectActiveBright) && VOf(t100.ObjectActiveBright) < VOf(t100.ButtonPressed),
            "highlight ladder holds at max baseV");
        var blackTheme = O5Palette.FromMonochrome(new UnityEngine.Color(0f, 0f, 0f, 1f));
        T.Check(VOf(blackTheme.Text) > 0.95f, "primary text stays bright even at V=0");

        // static legacy preset keeps fixed colors and falls back to Overlayer for new roles
        var staticTheme = O5Theme.OverlayerStatic;
        T.Check(O5Palette.ApproxEquals(staticTheme.PanelBG, new UnityEngine.Color(0.165f, 0.161f, 0.196f, 1f)), "static PanelBG matches legacy");
        T.Check(O5Palette.ApproxEquals(staticTheme.ObjectButton, new UnityEngine.Color(0.478f, 0.514f, 0.875f, 1f)), "static ObjectButton matches legacy");
        T.Check(O5Palette.ApproxEquals(staticTheme.ObjectActive, new UnityEngine.Color(0.569f, 0.604f, 1f, 1f)), "static ObjectActive matches legacy");
        T.Check(O5Palette.ApproxEquals(staticTheme.CardHeader, new UnityEngine.Color32(76, 77, 102, 255)), "static CardHeader matches legacy");
        T.Check(O5Palette.ApproxEquals(staticTheme.CardPanel, new UnityEngine.Color32(47, 46, 58, 255)), "static CardPanel matches legacy");
        T.Check(O5Palette.ApproxEquals(staticTheme.MathOk, O5Theme.Overlayer.MathOk), "static inherits unmapped MathOk from Overlayer");
        T.Check(O5Palette.ApproxEquals(staticTheme.SoftRed, O5Theme.Overlayer.SoftRed), "static inherits unmapped SoftRed from Overlayer");

        // MaxSrgbChroma gamut boundary checks
        T.Approx(0f, O5Palette.MaxSrgbChroma(0f, 0.2f), "MaxSrgbChroma at L=0 is 0");
        T.Approx(0f, O5Palette.MaxSrgbChroma(1f, 0.2f), "MaxSrgbChroma at L=1 is 0");
        foreach (float testHue in new[] { 0f, 0.25f, 0.5f, 0.75f }) {
            float maxC = O5Palette.MaxSrgbChroma(0.65f, testHue);
            T.Check(maxC > 0.05f && maxC <= 0.37f, $"MaxSrgbChroma at L=0.65 H={testHue} in range");
            var gamutEdge = O5Palette.OklchToRgb(0.65f, maxC, testHue);
            O5Palette.RgbToOklch(gamutEdge.r, gamutEdge.g, gamutEdge.b, out float backL, out float backC, out _);
            T.Approx(0.65f, backL, $"MaxSrgbChroma roundtrip L at H={testHue}", 0.003f);
            T.Approx(maxC, backC, $"MaxSrgbChroma roundtrip C at H={testHue}", 0.003f);
        }
        float yellowHue = 110f / 360f;
        float blueHue = 264f / 360f;
        T.Check(O5Palette.MaxSrgbChroma(0.90f, yellowHue) > O5Palette.MaxSrgbChroma(0.90f, blueHue),
            "at L=0.90 yellow has wider sRGB chroma than blue");
        T.Check(O5Palette.MaxSrgbChroma(0.45f, blueHue) > O5Palette.MaxSrgbChroma(0.45f, yellowHue),
            "at L=0.45 blue has wider sRGB chroma than yellow");
    }
}
