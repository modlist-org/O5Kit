// SPDX-License-Identifier: LGPL-3.0-or-later

using O5Kit.Control;

namespace O5Kit.Tests;

/// <summary>O5ValueScale math: no Unity types involved.</summary>
internal static class ScaleTests {
    public static void Run() {
        T.Check(O5ValueScale.Linear != null, "linear singleton");
        T.Check(O5ValueScale.Log != null, "log singleton");

        // --- linear ---
        T.Approx(0.5, O5ValueScale.LinearNormalize(0, 5, 10), "linear mid");
        T.Approx(0.0, O5ValueScale.LinearNormalize(0, 0, 10), "linear min");
        T.Approx(1.0, O5ValueScale.LinearNormalize(0, 10, 10), "linear max");
        T.Approx(0.0, O5ValueScale.LinearNormalize(5, 99, 5), "linear zero span");
        T.Approx(2.5, O5ValueScale.LinearDenormalize(0, 0.25, 10), "linear denorm");
        T.Approx(-5.0, O5ValueScale.LinearDenormalize(-10, 0.25, 10), "linear denorm negative");

        // --- log ---
        T.Approx(0.5, O5ValueScale.LogNormalize(1, 10, 100), "log mid");
        T.Approx(0.0, O5ValueScale.LogNormalize(1, 1, 100), "log min");
        T.Approx(1.0, O5ValueScale.LogNormalize(1, 100, 100), "log max");
        T.Approx(10.0, O5ValueScale.LogDenormalize(1, 0.5, 100), "log denorm");
        // out-of-domain falls back to linear
        T.Approx(O5ValueScale.LinearNormalize(0, 5, 10), O5ValueScale.LogNormalize(0, 5, 10), "log min<=0 fallback");
        T.Approx(O5ValueScale.LinearNormalize(1, 0, 100), O5ValueScale.LogNormalize(1, 0, 100), "log value<=0 fallback");
        T.Approx(O5ValueScale.LinearDenormalize(0, 0.5, 100), O5ValueScale.LogDenormalize(0, 0.5, 100), "log denorm fallback");

        // --- roundtrips through the interface ---
        double[] spans = [0.5, 1.0, 7.3, 100.0];
        foreach (IValueScale s in new IValueScale[] { O5ValueScale.Linear!, O5ValueScale.Log! }) {
            foreach (double v in spans) {
                double t = s.Normalize(1, v, 1000);
                T.Approx(v, s.Denormalize(1, t, 1000), $"roundtrip {v}");
            }
        }
        // linear roundtrip over a negative span
        double tn = O5ValueScale.Linear!.Normalize(-50, -10, 50);
        T.Approx(-10.0, O5ValueScale.Linear!.Denormalize(-50, tn, 50), "linear neg roundtrip");
    }
}
