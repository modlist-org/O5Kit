// SPDX-License-Identifier: LGPL-3.0-or-later

using O5Kit.Eval;

namespace O5Kit.Tests;

/// <summary>Evaluator&lt;double&gt; coverage. The managed fallback only handles
/// +-*/% parens unary numbers PI/E, so richer cases are native-only and skip
/// gracefully when no compatible native library is present.</summary>
internal static class EvalTests {
    public static bool NativeActive { get; private set; }

    /// <summary>Set when launched with --force-fallback: native must stay off.</summary>
    public static bool ExpectFallback { get; set; }

    public static void Detect() {
        // "1<<4" is rejected by the managed fallback but yields 16 natively.
        try {
            var (r, s) = Evaluator<double>.Evaluate("1<<4", 0.0);
            NativeActive = s == EvalState.Ok && r == 16.0;
        } catch {
            NativeActive = false;
        }
        Console.WriteLine($"eval engine: {(NativeActive ? "native" : "managed fallback")}");
        if (ExpectFallback) {
            T.Check(!NativeActive, "fallback forced: native must be off");
        }
    }

    private const double Baseline = 123456.789; // never a genuine result below

    private static void Ok(string expr, double expected, string? name = null, bool nativeOnly = false) {
        if (nativeOnly && !NativeActive) {
            T.SkipCase(name ?? expr);
            return;
        }
        try {
            var (r, s) = Evaluator<double>.Evaluate(expr, Baseline);
            T.Check(s == EvalState.Ok, name ?? $"eval {expr}",
                s != EvalState.Ok ? $"state={s} value={r:R}" : null);
            if (s == EvalState.Ok) {
                T.Approx(expected, r, (name ?? $"eval {expr}") + " value");
            }
        } catch (Exception e) {
            T.Check(false, name ?? $"eval {expr}", e.GetType().Name + ": " + e.Message);
        }
    }

    private static void Err(string expr, string? name = null, bool nativeOnly = false) {
        if (nativeOnly && !NativeActive) {
            T.SkipCase(name ?? expr);
            return;
        }
        try {
            var (_, s) = Evaluator<double>.Evaluate(expr, Baseline);
            T.Check(s == EvalState.Error, name ?? $"eval-err {expr}", $"state={s}");
        } catch (Exception e) {
            T.Check(false, name ?? $"eval-err {expr}", e.GetType().Name + ": " + e.Message);
        }
    }

    public static void Run() {
        // --- direct numbers (both paths) ---
        Ok("0", 0);
        Ok("  42 ", 42);
        Ok("3.5", 3.5);
        Ok(".5", 0.5);
        Ok("5.", 5.0);
        Ok("007", 7);
        Err("");
        Err("   ");
        Err("abc");
        Err("1_000");
        T.Eq(EvalState.Error, Evaluator<double>.Evaluate(null!, 0.0).state, "null is Error");

        // --- precedence / parens / unary (both paths) ---
        Ok("1+2*3", 7);
        Ok("(1+2)*3", 9);
        Ok("10-2-3", 5);
        Ok("-5+3", -2);
        Ok("+5", 5);
        Ok("--5", 5);
        Ok("-(2+3)", -5);
        Ok(" 1 + 2 ", 3);
        Ok("((2))", 2);
        Err("1+");
        Err("(1");
        Err("()");
        Err("(1)(2)");
        Err(".");
        Err("q"); // unknown name, both paths
        Err("5!");
        Err(")");

        // --- div / mod (both paths) ---
        Ok("7/2", 3.5);
        Ok("7%3", 1);
        Ok("7.5%2", 1.5);
        Ok("2*3%2", 0);
        Ok("10%3+1", 2);
        Ok(" 1 + 2 * ( 3 + 4 ) ", 15);
        Ok("0.1+0.2", 0.30000000000000004);
        Ok("2*0.5", 1);
        Ok("2*-0.5", -1);
        Err("7%0");
        Err("5/0");
        Err("0/0");

        // --- power (native: ** right-assoc; fallback rejects) ---
        Ok("2**3", 8, nativeOnly: true);
        Ok("2**3**2", 512, nativeOnly: true);
        Ok("-2**2", -4, nativeOnly: true); // tighter than unary
        Ok("1e-3*1000", 1, nativeOnly: true);
        // float equality gotcha, documented: 0.1+0.2 != 0.3 in binary
        Ok("0.1+0.2==0.3", 0, nativeOnly: true);
        Err("2**", nativeOnly: true);

        // --- hex / exponent (native only) ---
        Ok("0x10", 16, nativeOnly: true);
        Ok("0XFF", 255, nativeOnly: true);
        Ok("1e3", 1000, nativeOnly: true);
        Ok("1.5e-2", 0.015, nativeOnly: true);

        // --- shifts / bitwise (native only) ---
        Ok("1<<4", 16, nativeOnly: true);
        Ok("256>>3", 32, nativeOnly: true);
        Ok("6&3", 2, nativeOnly: true);
        Ok("6|3", 7, nativeOnly: true);
        Ok("6^3", 5, nativeOnly: true);

        // --- comparisons (native only) ---
        Ok("3>2", 1, nativeOnly: true);
        Ok("2>3", 0, nativeOnly: true);
        Ok("2<=2", 1, nativeOnly: true);
        Ok("2>=3", 0, nativeOnly: true);
        Ok("1<>2", 1, nativeOnly: true);
        Ok("2=2", 1, nativeOnly: true);
        Ok("2==2", 1, nativeOnly: true);
        Ok("1!=2", 1, nativeOnly: true);

        // --- logical (native only) ---
        Ok("1&&0", 0, nativeOnly: true);
        Ok("1||0", 1, nativeOnly: true);
        Ok("!0", 1, nativeOnly: true);
        Ok("!5", 0, nativeOnly: true);
        Ok("not 0", 1, nativeOnly: true);
        Ok("1 and 0", 0, nativeOnly: true);
        Ok("1 or 1", 1, nativeOnly: true);

        // --- ternary (native only) ---
        Ok("1?10:20", 10, nativeOnly: true);
        Ok("0?10:20", 20, nativeOnly: true);
        Ok("5<3?1:2", 2, nativeOnly: true);

        // --- constants (both for PI/E; t/v native only) ---
        Ok("PI", Math.PI);
        Ok("pi*2", Math.PI * 2);
        Ok("E", Math.E);
        Ok("t", 0, nativeOnly: true);
        Ok("t+1", 1, nativeOnly: true);
        Ok("v0", 0, nativeOnly: true);
        Ok("true", 1, nativeOnly: true);
        Ok("false", 0, nativeOnly: true);

        // --- functions (native only) ---
        Ok("sin(0)", 0, nativeOnly: true);
        Ok("cos(0)", 1, nativeOnly: true);
        Ok("tan(0)", 0, nativeOnly: true);
        Ok("SIN(0)", 0, nativeOnly: true);
        Ok("Math.Sin(0)", 0, nativeOnly: true);
        Ok("sqrt(16)", 4, nativeOnly: true);
        Ok("abs(0-5)", 5, nativeOnly: true);
        Ok("floor(2.9)", 2, nativeOnly: true);
        Ok("ceil(2.1)", 3, nativeOnly: true);
        Ok("trunc(0-2.9)", -2, nativeOnly: true);
        Ok("truncate(2.9)", 2, nativeOnly: true);
        Ok("ceiling(2.1)", 3, nativeOnly: true);
        Ok("round(2.5,0)", 2, nativeOnly: true); // bankers
        Ok("round(3.5,0)", 4, nativeOnly: true);
        Ok("round(1.125,2)", 1.12, nativeOnly: true); // exact binary: 112.5 -> even 112
        Ok("max(3,7)", 7, nativeOnly: true);
        Ok("min(3,7)", 3, nativeOnly: true);
        Ok("max(2.5,7)", 7, nativeOnly: true);
        Ok("pow(2,10)", 1024, nativeOnly: true);
        Ok("ln(1)", 0, nativeOnly: true);
        Ok("log(100,10)", 2, nativeOnly: true);
        Ok("log10(1000)", 3, nativeOnly: true);
        Ok("exp(0)", 1, nativeOnly: true);
        Ok("sign(0-9)", -1, nativeOnly: true);
        Ok("sign(9)", 1, nativeOnly: true);
        Ok("atan2(0,1)", 0, nativeOnly: true);
        Ok("asin(0)", 0, nativeOnly: true);
        Ok("acos(1)", 0, nativeOnly: true);
        Ok("atan(0)", 0, nativeOnly: true);
        // NB: the "ieeeremainder" builtin maps to truncated remainder (C fmod),
        // so ieeeremainder(5.5,2) is 1.5, unlike C# Math.IEEERemainder (-0.5).
        Ok("ieeeremainder(5.5,2)", 1.5, nativeOnly: true);
        Ok("log(10)", Math.Log(10), nativeOnly: true); // 1-arg log = ln
        Err("sqrt(0-1)", nativeOnly: true);
        Err("ln(0)", nativeOnly: true);
        Err("log(0-1,10)", nativeOnly: true);
        Err("sin(1,2)", nativeOnly: true);
        Err("sin()", nativeOnly: true);
        Err("round(1)", nativeOnly: true);
        Err("round(1,2,3)", nativeOnly: true);
        Err("unknownfn(1)", nativeOnly: true);
        Err("foo", nativeOnly: true);

        // --- Same / clamp states (both paths) ---
        T.Eq(EvalState.Same, Evaluator<double>.Evaluate("5", 5.0).state, "same value");
        var over = Evaluator<double>.Evaluate("200", 0.0, 0.0, 100.0);
        T.Eq(100.0, over.result, "over clamp value");
        T.Eq(EvalState.OverRange, over.state, "over clamp state");
        var under = Evaluator<double>.Evaluate("-5", 0.0, 0.0, 100.0);
        T.Eq(0.0, under.result, "under clamp value");
        T.Eq(EvalState.UnderRange, under.state, "under clamp state");
        var inRange = Evaluator<double>.Evaluate("5", 0.0, 0.0, 100.0);
        T.Eq(5.0, inRange.result, "in-range value");
        T.Eq(EvalState.Ok, inRange.state, "in-range state");

        // --- int / float / decimal generics still work ---
        var i = Evaluator<int>.Evaluate("1+2*3", 0);
        T.Eq(7, i.result, "int eval value");
        T.Eq(EvalState.Ok, i.state, "int eval state");
        T.Eq(EvalState.Error, Evaluator<int>.Evaluate("nope", 0).state, "int eval error");
        var neg = Evaluator<int>.Evaluate("0-7", 0);
        T.Eq(-7, neg.result, "int negative value");
        T.Eq(EvalState.Ok, neg.state, "int negative state");
        var f = Evaluator<float>.Evaluate("1.5", 0f);
        T.Approx(1.5, f.result, "float eval value");
        T.Eq(EvalState.Ok, f.state, "float eval state");
        var dec = Evaluator<decimal>.Evaluate("2.5", 0m);
        T.Eq(2.5m, dec.result, "decimal eval value");
        T.Eq(EvalState.Ok, dec.state, "decimal eval state");
    }
}
