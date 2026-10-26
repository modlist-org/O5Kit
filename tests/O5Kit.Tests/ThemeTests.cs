// SPDX-License-Identifier: LGPL-3.0-or-later

using O5Kit.Core;

namespace O5Kit.Tests;

/// <summary>O5Theme defaults and derived math. Value types only (Color field reads).</summary>
internal static class ThemeTests {
    static void ColorEq(UnityEngine.Color expected, UnityEngine.Color actual, string name) {
        bool ok = expected.r == actual.r && expected.g == actual.g
            && expected.b == actual.b && expected.a == actual.a;
        T.Check(ok, name, ok ? null : $"expected=({expected.r},{expected.g},{expected.b},{expected.a}) actual=({actual.r},{actual.g},{actual.b},{actual.a})");
    }

    public static void Run() {
        var t = new O5Theme();
        T.Approx(50f, t.ControlHeight, "theme ControlHeight");
        T.Approx(8f, t.ControlPaddingY, "theme ControlPaddingY");
        T.Approx(1.35f, t.LineHeight, "theme LineHeight");
        T.Approx(24f, t.FontSizeBody, "theme FontSizeBody");
        T.Approx(32f, t.FontSizeH1, "theme FontSizeH1");
        T.Approx(0.4f, t.InactiveAlpha, "theme InactiveAlpha");
        T.Approx(0.6f, t.TextDimAlpha, "theme TextDimAlpha");
        T.Approx(0.2f, t.TextFaintAlpha, "theme TextFaintAlpha");
        T.Approx(12f, t.CornerRadius, "theme CornerRadius");
        T.Approx(2f, t.OutlineWidth, "theme OutlineWidth");

        // floor + scaling
        T.Approx(50f, t.ControlHeightFor(24f), "height floor");
        T.Approx(50f, t.ControlHeightFor(0f), "height zero");
        T.Approx(151f, t.ControlHeightFor(100f), "height scaled");

        // derived colors track their bases
        ColorEq(new UnityEngine.Color(t.Text.r, t.Text.g, t.Text.b, t.TextDimAlpha), t.TextDim, "TextDim derives");
        ColorEq(new UnityEngine.Color(t.Text.r, t.Text.g, t.Text.b, t.TextFaintAlpha), t.TextFaint, "TextFaint derives");
        ColorEq(new UnityEngine.Color(t.ObjectActive.r, t.ObjectActive.g, t.ObjectActive.b, t.InactiveAlpha), t.ObjectInactive, "ObjectInactive derives");
        ColorEq(new UnityEngine.Color(t.Text.r, t.Text.g, t.Text.b, t.WorkspaceAlpha), t.WorkspaceEmpty, "WorkspaceEmpty derives");

        // presets differ where they must
        T.Check(O5Theme.Light.Text.r < 0.5f && O5Theme.Dark.Text.r > 0.5f, "light/dark text polarity");
        T.Check(O5Theme.Light.PanelBG.r > 0.5f && O5Theme.Dark.PanelBG.r < 0.5f, "light/dark panel polarity");

        // with-expressions (the documented customization path)
        var big = O5Theme.Dark with { FontSizeBody = 30f };
        T.Approx(56.5f, big.ControlHeightFor(30f), "with FontSizeBody");
        T.Approx(24f, O5Theme.Dark.FontSizeBody, "original untouched");
        var custom = O5Theme.Dark with { PanelBG = new UnityEngine.Color(0.1f, 0.1f, 0.1f, 1f) };
        T.Approx(0.1f, custom.PanelBG.r, "with PanelBG");

        // Overlayer preset keeps its panel override
        T.Check(O5Theme.Overlayer.PanelBG.r != O5Theme.Dark.PanelBG.r, "overlayer panel differs");
    }
}
