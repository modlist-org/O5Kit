// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using O5Kit.Native;

namespace O5Kit.Eval;

/// <summary>Parse result states for <see cref="Evaluator{T}"/>.</summary>
public enum EvalState {
    /// <summary>Valid new value.</summary>
    Ok,
    /// <summary>Unparsable expression.</summary>
    Error,
    /// <summary>Value equals the current one.</summary>
    Same,
    /// <summary>Clamped down to <c>max</c>.</summary>
    OverRange,
    /// <summary>Clamped up to <c>min</c>.</summary>
    UnderRange
}

/// <summary>Shared constants and culture for expression parsing.</summary>
public static class EvaluatorConstants {
    /// <summary>Named constants available in expressions (<c>PI</c>, <c>E</c>).</summary>
    public static readonly Dictionary<string, double> Constants = new() {
        { "PI", System.Math.PI },
        { "E", System.Math.E }
    };

    public const NumberStyles NumStyle = NumberStyles.Float | NumberStyles.AllowThousands;
    public static readonly CultureInfo Culture = CultureInfo.InvariantCulture;
}
/// <summary>Parses plain numbers and math expressions with optional range clamping.</summary>
/// <typeparam name="T">Numeric result type.</typeparam>
public static class Evaluator<T> where T : struct, IComparable<T>, IConvertible {

    /// <summary>Evaluates an expression, falling back to the current value on failure.</summary>
    /// <param name="exprStr">Number or expression, e.g. <c>"1+2*3"</c>.</param>
    /// <param name="currentVal">Fallback and same-check baseline.</param>
    /// <param name="min">Optional lower clamp.</param>
    /// <param name="max">Optional upper clamp.</param>
    public static (T result, EvalState state) Evaluate(string exprStr, T currentVal, T? min = null, T? max = null) {
        if (string.IsNullOrWhiteSpace(exprStr)) {
            return (currentVal, EvalState.Error);
        }

        if (double.TryParse(exprStr, EvaluatorConstants.NumStyle, EvaluatorConstants.Culture, out double parsedDirect)) {
            T directResult = (T)Convert.ChangeType(parsedDirect, typeof(T), EvaluatorConstants.Culture);
            return ValidateAndReturn(directResult, currentVal, min, max);
        }

        if (O5Expr.IsSupported) {
            long handle = O5Expr.Compile(exprStr);
            if (handle != 0) {
                try {
                    if (O5Expr.TryEval(handle, out double nativeVal) && !double.IsNaN(nativeVal) && !double.IsInfinity(nativeVal)) {
                        T result = (T)Convert.ChangeType(nativeVal, typeof(T), EvaluatorConstants.Culture);
                        return ValidateAndReturn(result, currentVal, min, max);
                    }
                } finally {
                    O5Expr.Free(handle);
                }
            }
        }

        if (TryEvalFallback(exprStr, out double fallbackVal)) {
            T result = (T)Convert.ChangeType(fallbackVal, typeof(T), EvaluatorConstants.Culture);
            return ValidateAndReturn(result, currentVal, min, max);
        }

        return (currentVal, EvalState.Error);
    }

    private static bool TryEvalFallback(string s, out double result) {
        result = 0;
        int pos = 0;
        try {
            double ParseExpr() {
                double val = ParseTerm();
                while (pos < s.Length) {
                    char c = s[pos];
                    if (c == '+') { pos++; val += ParseTerm(); }
                    else if (c == '-') { pos++; val -= ParseTerm(); }
                    else break;
                }
                return val;
            }
            double ParseTerm() {
                double val = ParseFactor();
                while (pos < s.Length) {
                    char c = s[pos];
                    if (c == '*') { pos++; val *= ParseFactor(); }
                    else if (c == '/') { pos++; val /= ParseFactor(); }
                    else if (c == '%') { pos++; val %= ParseFactor(); }
                    else break;
                }
                return val;
            }
            double ParseFactor() {
                SkipWs();
                if (pos >= s.Length) throw new InvalidOperationException();
                if (s[pos] == '+') { pos++; return ParseFactor(); }
                if (s[pos] == '-') { pos++; return -ParseFactor(); }
                if (s[pos] == '(') {
                    pos++;
                    double val = ParseExpr();
                    SkipWs();
                    if (pos < s.Length && s[pos] == ')') pos++;
                    return val;
                }
                int start = pos;
                if (char.IsLetter(s[pos])) {
                    while (pos < s.Length && char.IsLetterOrDigit(s[pos])) pos++;
                    string id = s.Substring(start, pos - start);
                    if (id.Equals("PI", StringComparison.OrdinalIgnoreCase)) return Math.PI;
                    if (id.Equals("E", StringComparison.OrdinalIgnoreCase)) return Math.E;
                    throw new InvalidOperationException();
                }
                while (pos < s.Length && (char.IsDigit(s[pos]) || s[pos] == '.')) pos++;
                if (double.TryParse(s.Substring(start, pos - start), NumberStyles.Float, CultureInfo.InvariantCulture, out double num))
                    return num;
                throw new InvalidOperationException();
            }
            void SkipWs() { while (pos < s.Length && char.IsWhiteSpace(s[pos])) pos++; }
            s = s.Trim();
            result = ParseExpr();
            SkipWs();
            return pos == s.Length && !double.IsNaN(result) && !double.IsInfinity(result);
        } catch {
            return false;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static (T result, EvalState state) ValidateAndReturn(T result, T currentVal, T? min, T? max) {
        if (min.HasValue && result.CompareTo(min.Value) < 0) {
            return (min.Value, EvalState.UnderRange);
        }

        if (max.HasValue && result.CompareTo(max.Value) > 0) {
            return (max.Value, EvalState.OverRange);
        }

        if (EqualityComparer<T>.Default.Equals(result, currentVal)) {
            return (result, EvalState.Same);
        }

        return (result, EvalState.Ok);
    }
}
