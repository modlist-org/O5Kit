// SPDX-License-Identifier: LGPL-3.0-or-later

namespace O5Kit.Tests;

/// <summary>Minimal test runner. No external packages; Unity-free logic only.</summary>
internal static class T {
    public static int Pass;
    public static int Fail;
    public static int Skip;
    private static readonly List<string> failures = new();

    public static void Check(bool cond, string name, string? detail = null) {
        if (cond) {
            Pass++;
        } else {
            Fail++;
            failures.Add(detail == null ? $"FAIL {name}" : $"FAIL {name} :: {detail}");
        }
    }

    public static void Eq<TValue>(TValue expected, TValue actual, string name) {
        bool ok = EqualityComparer<TValue>.Default.Equals(expected, actual);
        Check(ok, name, ok ? null : $"expected={expected} actual={actual}");
    }

    public static void Approx(double expected, double actual, string name, double eps = 1e-9) {
        bool ok = double.IsNaN(expected) ? double.IsNaN(actual)
            : Math.Abs(expected - actual) <= eps * Math.Max(1.0, Math.Abs(expected));
        Check(ok, name, ok ? null : $"expected={expected:R} actual={actual:R}");
    }

    public static void SkipCase(string name) {
        Skip++;
    }

    public static int Report() {
        foreach (string f in failures) {
            Console.WriteLine(f);
        }
        Console.WriteLine($"PASS {Pass}, FAIL {Fail}, SKIP {Skip}");
        return Fail == 0 ? 0 : 1;
    }
}
