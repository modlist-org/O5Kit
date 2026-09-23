// SPDX-License-Identifier: LGPL-3.0-or-later

using O5Kit.Core;
using UnityEngine;

namespace O5Kit.Eval;

/// <summary>Theme colors for formula parse states.</summary>
public static class MathVisuals {
    /// <summary>Returns the theme color for a parse state.</summary>
    /// <param name="state">Parse state.</param>
    public static Color GetStateColor(EvalState state) => state switch {
        EvalState.Ok => O5Boot.Theme.MathOk,
        EvalState.Error => O5Boot.Theme.MathErr,
        EvalState.Same => O5Boot.Theme.ObjectActive,
        EvalState.OverRange => O5Boot.Theme.MathWarn,
        EvalState.UnderRange => O5Boot.Theme.MathWarn,
        _ => O5Boot.Theme.ObjectActive,
    };
}
