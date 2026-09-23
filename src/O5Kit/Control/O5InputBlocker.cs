// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace O5Kit.Control;

/// <summary>Global "is any input being edited" flag. Used to suspend hotkeys while typing.</summary>
public static class O5InputBlocker {
    private static int _focusedInputCount;

    /// <summary>True while at least one input holds focus.</summary>
    public static bool IsEditing => _focusedInputCount > 0;

    /// <summary>Reports focus gain/loss. Counts nest safely.</summary>
    /// <param name="focused">True on focus gain.</param>
    /// <param name="inputObject">Selected object while focused, if any.</param>
    public static void SetFocused(bool focused, GameObject? inputObject = null) {
        _focusedInputCount = Math.Max(0, _focusedInputCount + (focused ? 1 : -1));

        if (focused && inputObject != null && EventSystem.current != null) {
            EventSystem.current.SetSelectedGameObject(inputObject);
        }
    }
}
