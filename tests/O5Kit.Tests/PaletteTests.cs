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
    }
}
