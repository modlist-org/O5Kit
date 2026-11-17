// SPDX-License-Identifier: LGPL-3.0-or-later

using O5Kit.Core;

namespace O5Kit.Tests;

/// <summary>MathCompat clamps: the Mono-compat surface must match Math.Clamp.</summary>
internal static class CompatTests {
    public static void Run() {
        T.Eq(5, MathCompat.Clamp(5, 0, 10), "int inside");
        T.Eq(0, MathCompat.Clamp(-3, 0, 10), "int low");
        T.Eq(10, MathCompat.Clamp(99, 0, 10), "int high");
        T.Eq(0, MathCompat.Clamp(0, 0, 10), "int edge low");
        T.Eq(10, MathCompat.Clamp(10, 0, 10), "int edge high");

        T.Approx(5.0, MathCompat.Clamp(5.0, 0.0, 10.0), "double inside");
        T.Approx(0.0, MathCompat.Clamp(-0.5, 0.0, 10.0), "double low");
        T.Approx(10.0, MathCompat.Clamp(10.5, 0.0, 10.0), "double high");

        T.Approx(5f, MathCompat.Clamp(5f, 0f, 10f), "float inside");
        T.Approx(0f, MathCompat.Clamp(-1f, 0f, 10f), "float low");
        T.Approx(10f, MathCompat.Clamp(11f, 0f, 10f), "float high");

        T.Eq(5L, MathCompat.Clamp(5L, 0L, 10L), "long inside");
        T.Eq(0L, MathCompat.Clamp(-5L, 0L, 10L), "long low");
        T.Eq(10L, MathCompat.Clamp(15L, 0L, 10L), "long high");
    }
}
