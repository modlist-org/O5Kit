// SPDX-License-Identifier: LGPL-3.0-or-later

using O5Kit.Core;
using UnityEngine;

namespace O5Kit.Tests;

/// <summary>O5ColorSpace math and Linear-vs-Gamma blending parity tests (Unity-runtime-free).</summary>
internal static class ColorSpaceTests {
    public static void Run() {
        T.Check(!O5ColorSpace.IsLinear, "IsLinear safely returns false outside Unity");

        // Transfer function endpoints and round-trip
        T.Approx(0f, O5ColorSpace.SrgbToLinear(0f), "sRGB->Linear(0)");
        T.Approx(1f, O5ColorSpace.SrgbToLinear(1f), "sRGB->Linear(1)");
        T.Approx(0f, O5ColorSpace.LinearToSrgb(0f), "Linear->sRGB(0)");
        T.Approx(1f, O5ColorSpace.LinearToSrgb(1f), "Linear->sRGB(1)");

        foreach (float x in new[] { 0.01f, 0.04045f, 0.1f, 0.25f, 0.5f, 0.78f, 0.95f }) {
            float back = O5ColorSpace.LinearToSrgb(O5ColorSpace.SrgbToLinear(x));
            T.Approx(x, back, $"sRGB<->Linear roundtrip {x}", 1e-5);
        }

        // Opaque and clear colors need zero compensation
        var opaqueTint = O5ColorSpace.ComputeLinearBlendTint(new Color(0.5f, 0.5f, 0.9f, 1f), 0.1f, true);
        var clearTint = O5ColorSpace.ComputeLinearBlendTint(new Color(0.5f, 0.5f, 0.9f, 0f), 0.1f, true);
        T.Check(O5Palette.ApproxEquals(Color.white, opaqueTint, 1e-5f), "opaque fg yields white tint");
        T.Check(O5Palette.ApproxEquals(Color.white, clearTint, 1e-5f), "clear fg yields white tint");

        // Equal luminance fg/bg needs zero compensation
        var isoTint = O5ColorSpace.ComputeLinearBlendTint(new Color(0.2f, 0.2f, 0.2f, 0.5f), 0.2f, true);
        T.Check(O5Palette.ApproxEquals(Color.white, isoTint, 1e-5f), "iso-luma fg yields white tint");

        // Compositing semi-transparent ancestors onto an opaque base
        float panelY = O5ColorSpace.RelativeLuminance(new Color(15f / 255f, 16f / 255f, 21f / 255f, 1f));
        T.Approx(panelY, O5ColorSpace.CompositeBackgroundLuminance(panelY, null), "composite null layers");
        T.Approx(panelY, O5ColorSpace.CompositeBackgroundLuminance(panelY, Array.Empty<(float, float)>()), "composite empty layers");

        float tutCardCompositedY = O5ColorSpace.CompositeBackgroundLuminance(panelY, new[] { (1f, 0.06f) });
        float expectedTutCardY = (0.06f * 1f) + (0.94f * panelY);
        T.Approx(expectedTutCardY, tutCardCompositedY, "composite TutorialCard over PanelBG");

        // Multi-layer (inner-to-outer): e.g. translucent ghost button or highlight over done card (0.03f) over PanelBG
        float multiLayerY = O5ColorSpace.CompositeBackgroundLuminance(panelY, new[] { (1f, 0.05f), (1f, 0.03f) });
        float afterDone = (0.03f * 1f) + (0.97f * panelY);
        float expectedMulti = (0.05f * 1f) + (0.95f * afterDone);
        T.Approx(expectedMulti, multiLayerY, "composite multi-layer inner-to-outer over PanelBG");

        Color panelBgColor = new(15f / 255f, 16f / 255f, 21f / 255f, 1f);
        Color tutCardCompositeBg = new(
            (0.06f * 1f) + (0.94f * panelBgColor.r),
            (0.06f * 1f) + (0.94f * panelBgColor.g),
            (0.06f * 1f) + (0.94f * panelBgColor.b),
            1f);

        // Simulate Unity 8-bit Color32 vertex quantization + Linear GPU blending across real Overlayer/O5Kit UI roles
        var cases = new (string Name, Color Fg, Color Bg, int MaxChannelErr)[] {
            ("RowEvenBg",             new(1f, 1f, 1f, 0.018f),                        new(20f/255f, 20f/255f, 27f/255f, 1f), 3),
            ("RowOddBg",              new(1f, 1f, 1f, 0.042f),                        new(20f/255f, 20f/255f, 27f/255f, 1f), 3),
            ("TableHeader",           new(1f, 1f, 1f, 0.055f),                        new(20f/255f, 20f/255f, 27f/255f, 1f), 3),
            ("GhostBtn",              new(1f, 1f, 1f, 0.050f),                        panelBgColor,                            3),
            ("TutorialCard",          new(1f, 1f, 1f, 0.060f),                        panelBgColor,                            3),
            ("TutorialCardDone",      new(1f, 1f, 1f, 0.030f),                        panelBgColor,                            3),
            ("TutorialIntro",         new(1f, 1f, 1f, 0.600f),                        panelBgColor,                            4),
            ("TutorialSummaryOnCard", new(1f, 1f, 1f, 0.580f),                        tutCardCompositeBg,                    4),
            ("DisabledCardText",      new(1f, 1f, 1f, 0.420f),                        new(20f/255f, 20f/255f, 28f/255f, 1f), 3),
            ("BorderSubtle",          new(1f, 1f, 1f, 0.080f),                        panelBgColor,                            3),
            ("NavActiveBg",           new(1f, 1f, 1f, 0.095f),                        new(14f/255f, 14f/255f, 21f/255f, 1f), 3),
            ("BtnActiveBg",           new(1f, 1f, 1f, 0.160f),                        new(20f/255f, 20f/255f, 27f/255f, 1f), 3),
            ("WindowOutline",         new(178f/255f, 180f/255f, 212f/255f, 0.200f),   new(25f/255f, 26f/255f, 36f/255f, 1f), 4),
            ("MenuHover",             new(110f/255f, 108f/255f, 195f/255f, 0.220f),   new(22f/255f, 22f/255f, 30f/255f, 1f), 6),
            ("CaptionText",           new(1f, 1f, 1f, 0.420f),                        new(20f/255f, 20f/255f, 27f/255f, 1f), 3),
            ("TextDim",               new(247f/255f, 247f/255f, 252f/255f, 0.600f),   panelBgColor,                            4),
            ("TextFaint",             new(247f/255f, 247f/255f, 252f/255f, 0.200f),   new(29f/255f, 30f/255f, 41f/255f, 1f), 5),
            ("MenuSelected",          new(120f/255f, 118f/255f, 212f/255f, 0.920f),   new(22f/255f, 22f/255f, 30f/255f, 1f), 4),
            ("PowerBg",               new(0f, 0f, 0f, 0.100f),                        new(22f/255f, 22f/255f, 30f/255f, 1f), 2),
            ("TooltipBG",             new(13f/255f, 13f/255f, 18f/255f, 0.940f),      panelBgColor,                            2),
            ("OverlayScrim",          new(0f, 0f, 0f, 0.580f),                        new(0.50f, 0.50f, 0.50f, 1f),          3),
            ("LightOutline",          new(0.10f, 0.10f, 0.14f, 0.500f),               new(0.93f, 0.93f, 0.95f, 1f),          4),
            ("LightWork",             new(0.10f, 0.10f, 0.14f, 0.060f),               new(0.93f, 0.93f, 0.95f, 1f),          3),
        };

        foreach (var c in cases) {
            Color tint = O5ColorSpace.ComputeLinearBlendTint(c.Fg, O5ColorSpace.RelativeLuminance(c.Bg), allowRgbBoost: true);
            int gr = ByteRound((c.Fg.a * c.Fg.r) + ((1f - c.Fg.a) * c.Bg.r));
            int gg = ByteRound((c.Fg.a * c.Fg.g) + ((1f - c.Fg.a) * c.Bg.g));
            int gb = ByteRound((c.Fg.a * c.Fg.b) + ((1f - c.Fg.a) * c.Bg.b));

            // Unity vertex Color32 quantization (in gamma space) followed by UIGammaToLinear + HW linear blend + LinearToSrgb
            float vr = ByteRound(c.Fg.r * tint.r) / 255f;
            float vg = ByteRound(c.Fg.g * tint.g) / 255f;
            float vb = ByteRound(c.Fg.b * tint.b) / 255f;
            float va = ByteRound(c.Fg.a * tint.a) / 255f;

            int lr = ByteRound(O5ColorSpace.LinearToSrgb((va * O5ColorSpace.SrgbToLinear(vr)) + ((1f - va) * O5ColorSpace.SrgbToLinear(c.Bg.r))));
            int lg = ByteRound(O5ColorSpace.LinearToSrgb((va * O5ColorSpace.SrgbToLinear(vg)) + ((1f - va) * O5ColorSpace.SrgbToLinear(c.Bg.g))));
            int lb = ByteRound(O5ColorSpace.LinearToSrgb((va * O5ColorSpace.SrgbToLinear(vb)) + ((1f - va) * O5ColorSpace.SrgbToLinear(c.Bg.b))));

            int err = Math.Max(Math.Abs(gr - lr), Math.Max(Math.Abs(gg - lg), Math.Abs(gb - lb)));
            T.Check(err <= c.MaxChannelErr, $"linear blend parity {c.Name}", $"gamma=({gr},{gg},{gb}) linear=({lr},{lg},{lb}) err={err}");
        }
    }

    private static int ByteRound(float v) {
        if (v <= 0f) return 0;
        if (v >= 1f) return 255;
        return (int)Math.Round(v * 255f);
    }
}
