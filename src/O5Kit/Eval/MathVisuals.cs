// SPDX-License-Identifier: LGPL-3.0-or-later

using O5Kit.Core;
using UnityEngine;

namespace O5Kit.Eval;

/// <summary>Theme colors for formula parse states.</summary>
public static class MathVisuals {
    /// <summary>Returns the theme color for a parse state.</summary>
    /// <param name="theme">Theme to read colors from.</param>
    /// <param name="state">Parse state.</param>
    public static Color GetStateColor(O5Theme theme, EvalState state) => state switch {
        EvalState.Ok => theme.MathOk,
        EvalState.Error => theme.MathErr,
        EvalState.Same => theme.ObjectActive,
        EvalState.OverRange => theme.MathWarn,
        EvalState.UnderRange => theme.MathWarn,
        _ => theme.ObjectActive,
    };
}
