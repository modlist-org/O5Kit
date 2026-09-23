// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using NCalc;

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

        try {
            var e = new Expression(exprStr, ExpressionOptions.IgnoreCaseAtBuiltInFunctions);

            foreach (var constant in EvaluatorConstants.Constants) {
                e.Parameters[constant.Key] = constant.Value;
            }

            object? evalResult = e.Evaluate();
            if (evalResult == null) {
                return (currentVal, EvalState.Error);
            }

            T result = (T)((IConvertible)evalResult).ToType(typeof(T), EvaluatorConstants.Culture);
            return ValidateAndReturn(result, currentVal, min, max);
        } catch {
            return (currentVal, EvalState.Error);
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
