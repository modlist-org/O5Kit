// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Collections.Generic;
using UnityEngine;

namespace O5Kit.Input;

/// <summary>Main key plus required modifier keys.</summary>
public readonly struct O5KeyCombo {
    /// <summary>Main key.</summary>
    public KeyCode Main { get; }

    /// <summary>Modifiers that must all be held.</summary>
    public KeyCode[] Modifiers { get; }

    /// <summary>Creates a combo. Empty modifiers means the bare key.</summary>
    /// <param name="main">Main key.</param>
    /// <param name="modifiers">Required modifier keys.</param>
    public O5KeyCombo(KeyCode main, params KeyCode[] modifiers) {
        Main = main;
        Modifiers = modifiers ?? [];
    }

    /// <summary>True while every key is held.</summary>
    public bool IsHeld() => ModifiersHeld() && O5Input.GetKey(Main);

    /// <summary>True on the frame the main key goes down while modifiers are held.</summary>
    public bool WasPressed() => ModifiersHeld() && O5Input.GetKeyDown(Main);

    /// <summary>True on the frame the main key goes up.</summary>
    public bool WasReleased() => O5Input.GetKeyUp(Main);

    internal bool ModifiersHeld() {
        foreach (var modifier in Modifiers) {
            if (!O5Input.GetKey(modifier)) {
                return false;
            }
        }

        return true;
    }
}

/// <summary>Named hotkey with an optional hold action.</summary>
public sealed class O5Shortcut {
    /// <summary>Stable identifier.</summary>
    public string Id { get; }

    /// <summary>Key combo. Mutable to rebind at runtime.</summary>
    public O5KeyCombo Combo { get; set; }

    /// <summary>Hold time in seconds for <see cref="OnHeld"/>. Zero disables holding.</summary>
    public float HoldSeconds { get; set; }

    /// <summary>Fired on press.</summary>
    public Action<O5Shortcut>? OnPressed { get; set; }

    /// <summary>Fired once after holding past <see cref="HoldSeconds"/>.</summary>
    public Action<O5Shortcut>? OnHeld { get; set; }

    /// <summary>Fired on main-key release after a press.</summary>
    public Action<O5Shortcut>? OnReleased { get; set; }

    internal bool Pressed;
    internal float PressTime;
    internal bool HeldFired;

    /// <summary>Creates a hotkey. Prefer <see cref="O5ShortcutManager.Register"/>.</summary>
    /// <param name="id">Stable identifier.</param>
    /// <param name="combo">Key combo.</param>
    /// <param name="holdSeconds">Hold time for <see cref="OnHeld"/>. Zero disables holding.</param>
    /// <param name="onPressed">Press callback.</param>
    /// <param name="onHeld">Hold callback.</param>
    /// <param name="onReleased">Release callback.</param>
    public O5Shortcut(string id, O5KeyCombo combo, float holdSeconds = 0f, Action<O5Shortcut>? onPressed = null, Action<O5Shortcut>? onHeld = null, Action<O5Shortcut>? onReleased = null) {
        Id = id;
        Combo = combo;
        HoldSeconds = holdSeconds;
        OnPressed = onPressed;
        OnHeld = onHeld;
        OnReleased = onReleased;
    }
}

/// <summary>Polled hotkey registry. Register at init, pump via <see cref="HandleUpdate"/>.</summary>
public static class O5ShortcutManager {
    private static readonly Dictionary<string, O5Shortcut> _shortcuts = new();

    /// <summary>When true, shortcuts reset silently. Defaults to the O5Kit input-focus check. Replace to combine with your own blocker.</summary>
    public static Func<bool>? IsSuspended { get; set; } = () => Control.O5InputBlocker.IsEditing;

    /// <summary>Registers (or replaces) a hotkey.</summary>
    /// <param name="id">Stable identifier.</param>
    /// <param name="combo">Key combo.</param>
    /// <param name="holdSeconds">Hold time for the hold callback. Zero disables holding.</param>
    /// <param name="onPressed">Press callback.</param>
    /// <param name="onHeld">Hold callback.</param>
    /// <param name="onReleased">Release callback.</param>
    public static O5Shortcut Register(string id, O5KeyCombo combo, float holdSeconds = 0f, Action<O5Shortcut>? onPressed = null, Action<O5Shortcut>? onHeld = null, Action<O5Shortcut>? onReleased = null) {
        var shortcut = new O5Shortcut(id, combo, holdSeconds, onPressed, onHeld, onReleased);
        _shortcuts[id] = shortcut;
        return shortcut;
    }

    /// <summary>Looks up a hotkey for runtime rebinding.</summary>
    /// <param name="id">Stable identifier.</param>
    public static O5Shortcut? Get(string id)
        => _shortcuts.TryGetValue(id, out var shortcut) ? shortcut : null;

    /// <summary>Removes a hotkey. False when absent.</summary>
    /// <param name="id">Stable identifier.</param>
    public static bool Unregister(string id) => _shortcuts.Remove(id);

    /// <summary>Removes every hotkey.</summary>
    public static void Clear() => _shortcuts.Clear();

    /// <summary>Polls all hotkeys. Call once per frame.</summary>
    public static void HandleUpdate() {
        foreach (var shortcut in new List<O5Shortcut>(_shortcuts.Values)) {
            if (IsSuspended?.Invoke() == true) {
                Reset(shortcut);
                continue;
            }

            if (shortcut.Combo.WasReleased()) {
                if (shortcut.Pressed) {
                    shortcut.OnReleased?.Invoke(shortcut);
                }

                Reset(shortcut);
                continue;
            }

            if (!shortcut.Pressed) {
                if (shortcut.Combo.WasPressed()) {
                    shortcut.Pressed = true;
                    shortcut.PressTime = Time.unscaledTime;
                    shortcut.HeldFired = false;
                    shortcut.OnPressed?.Invoke(shortcut);
                }

                continue;
            }

            if (!shortcut.Combo.IsHeld()) {
                Reset(shortcut);
                continue;
            }

            if (!shortcut.HeldFired && shortcut.HoldSeconds > 0f &&
                Time.unscaledTime - shortcut.PressTime >= shortcut.HoldSeconds) {
                shortcut.HeldFired = true;
                shortcut.OnHeld?.Invoke(shortcut);
            }
        }
    }

    private static void Reset(O5Shortcut shortcut) {
        shortcut.Pressed = false;
        shortcut.HeldFired = false;
        shortcut.PressTime = 0f;
    }
}
