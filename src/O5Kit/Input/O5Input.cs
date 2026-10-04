// SPDX-License-Identifier: LGPL-2.1-or-later
// * This program is free software: you can redistribute it and/or modify
// * it under the terms of the GNU Lesser General Public License as published by
// * the Free Software Foundation, either version 2.1 of the License, or
// * (at your option) any later version.
// *
// * This program is distributed in the hope that it will be useful,
// * but WITHOUT ANY WARRANTY; without even the implied warranty of
// * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// * GNU Lesser General Public License for more details.
// *
// * You should have received a copy of the GNU Lesser General Public License
// * along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Reflection;
using O5Kit.Input.OS;
using O5Kit.Input.OS.Impl;
using UnityEngine;

namespace O5Kit.Input;

/// <summary>
/// Unified keyboard/mouse/OS-cursor access. Prefers the new Input System
/// (via reflection, so O5Kit doesn't hard-depend on the InputSystem package)
/// and falls back to legacy <see cref="UnityEngine.Input"/>.
/// Input stack derived from UniverseLib (LGPL-2.1-or-later, see header above).
/// </summary>
public static class O5Input {
    private static Type? t_Keyboard, t_Key, t_ButtonControl, t_Mouse, t_Pointer;
    private static PropertyInfo? p_kbCurrent, p_kbIndexer, p_btnIsPressed, p_btnWasPressed, p_btnWasReleased;
    private static PropertyInfo? p_mouseCurrent, p_mousePosition, p_mouseScroll, p_mouseDelta;
    private static PropertyInfo? p_leftBtn, p_rightBtn, p_middleBtn;
    private static MethodInfo? m_ReadV2;
    private static OsApi? _osAPI;
    private static bool _initialized;

    private static void EnsureInitialized() {
        if (_initialized) {
            return;
        }

        t_Keyboard = Type.GetType("UnityEngine.InputSystem.Keyboard, Unity.InputSystem");
        t_Key = Type.GetType("UnityEngine.InputSystem.Key, Unity.InputSystem");
        t_ButtonControl = Type.GetType("UnityEngine.InputSystem.Controls.ButtonControl, Unity.InputSystem");
        t_Mouse = Type.GetType("UnityEngine.InputSystem.Mouse, Unity.InputSystem");
        t_Pointer = Type.GetType("UnityEngine.InputSystem.Pointer, Unity.InputSystem");

        if (t_Keyboard != null) {
            p_kbCurrent = t_Keyboard.GetProperty("current", BindingFlags.Public | BindingFlags.Static);
            p_kbIndexer = t_Keyboard.GetProperty("Item", [t_Key!]);
            p_btnIsPressed = t_ButtonControl!.GetProperty("isPressed");
            p_btnWasPressed = t_ButtonControl.GetProperty("wasPressedThisFrame");
            p_btnWasReleased = t_ButtonControl.GetProperty("wasReleasedThisFrame");
        }

        if (t_Mouse != null) {
            p_mouseCurrent = t_Mouse.GetProperty("current", BindingFlags.Public | BindingFlags.Static);
            p_leftBtn = t_Mouse.GetProperty("leftButton");
            p_rightBtn = t_Mouse.GetProperty("rightButton");
            p_middleBtn = t_Mouse.GetProperty("middleButton");
            p_mouseScroll = t_Mouse.GetProperty("scroll");
            p_mousePosition = t_Pointer!.GetProperty("position");
            p_mouseDelta = t_Pointer.GetProperty("delta");
            m_ReadV2 = t_Pointer.Assembly.GetType("UnityEngine.InputSystem.InputControl`1")!
                .MakeGenericType(typeof(Vector2)).GetMethod("ReadValue");
        }

        _osAPI = Application.platform switch {
            RuntimePlatform.WindowsPlayer or RuntimePlatform.WindowsEditor => new WinOsApi(),
            RuntimePlatform.LinuxPlayer or RuntimePlatform.LinuxEditor => new LinuxOsApi(),
            RuntimePlatform.OSXPlayer or RuntimePlatform.OSXEditor => new MacOsApi(),
            _ => null,
        };
        _initialized = true;
    }

    /// <summary>Whether a keyboard key is held.</summary>
    /// <param name="key">Key to query.</param>
    public static bool GetKey(KeyCode key) {
        EnsureInitialized();
        return t_Keyboard != null ? TryInvoke(p_btnIsPressed, GetKeyControl(key)) : UnityEngine.Input.GetKey(key);
    }

    /// <summary>Whether a keyboard key went down this frame.</summary>
    /// <param name="key">Key to query.</param>
    public static bool GetKeyDown(KeyCode key) {
        EnsureInitialized();
        return t_Keyboard != null ? TryInvoke(p_btnWasPressed, GetKeyControl(key)) : UnityEngine.Input.GetKeyDown(key);
    }

    /// <summary>Whether a keyboard key went up this frame.</summary>
    /// <param name="key">Key to query.</param>
    public static bool GetKeyUp(KeyCode key) {
        EnsureInitialized();
        return t_Keyboard != null ? TryInvoke(p_btnWasReleased, GetKeyControl(key)) : UnityEngine.Input.GetKeyUp(key);
    }

    /// <summary>Whether a mouse button is held.</summary>
    /// <param name="btn">0 = left, 1 = right, 2 = middle.</param>
    public static bool GetMouseButton(int btn) {
        EnsureInitialized();
        return TryReadMouseButton(p_btnIsPressed, btn, out bool pressed)
            ? pressed
            : TryReadLegacyMouseButton(btn, 0);
    }

    /// <summary>Whether a mouse button went down this frame.</summary>
    /// <param name="btn">0 = left, 1 = right, 2 = middle.</param>
    public static bool GetMouseButtonDown(int btn) {
        EnsureInitialized();
        return TryReadMouseButton(p_btnWasPressed, btn, out bool pressed)
            ? pressed
            : TryReadLegacyMouseButton(btn, 1);
    }

    /// <summary>Whether a mouse button went up this frame.</summary>
    /// <param name="btn">0 = left, 1 = right, 2 = middle.</param>
    public static bool GetMouseButtonUp(int btn) {
        EnsureInitialized();
        return TryReadMouseButton(p_btnWasReleased, btn, out bool released)
            ? released
            : TryReadLegacyMouseButton(btn, 2);
    }

    /// <summary>Cursor position in screen pixels (Unity space, origin bottom-left).</summary>
    public static Vector2 MousePosition {
        get {
            EnsureInitialized();
            if (TryReadMouseVector(p_mousePosition, out Vector2 position)) {
                return position;
            }

            try {
                return UnityEngine.Input.mousePosition;
            } catch {
                return Vector2.zero;
            }
        }
    }

    /// <summary>OS-level cursor position (native space, origin top-left). Settable for drag warping.</summary>
    public static Vector2Int OSMousePosition {
        get {
            EnsureInitialized();
            if (_osAPI == null) {
                return Vector2Int.zero;
            }

            Vector2Int nativePos = _osAPI.GetCursorPosition();
            return new Vector2Int(nativePos.x, nativePos.y);
        }
        set {
            EnsureInitialized();
            _osAPI?.SetCursorPosition(value.x, value.y);
        }
    }

    /// <summary>Cursor movement since last frame, in pixels.</summary>
    public static Vector2 MouseDelta {
        get {
            EnsureInitialized();
            if (t_Mouse != null) {
                try {
                    return (Vector2)m_ReadV2!.Invoke(p_mouseDelta!.GetValue(p_mouseCurrent!.GetValue(null)), null)!;
                } catch {
                    return Vector2.zero;
                }
            }

            return Vector2.zero;
        }
    }

    /// <summary>Scroll wheel delta this frame.</summary>
    public static Vector2 MouseScrollDelta {
        get {
            EnsureInitialized();
            if (TryReadMouseVector(p_mouseScroll, out Vector2 delta)) {
                return delta;
            }

            try {
                return UnityEngine.Input.mouseScrollDelta;
            } catch {
                return Vector2.zero;
            }
        }
    }

    private static object? GetKeyControl(KeyCode key) {
        EnsureInitialized();
        string s = key.ToString();
        if (s == "BackQuote") {
            s = "Backquote";
        }

        s = s.Replace("Alpha", "Digit").Replace("Control", "Ctrl").Replace("Return", "Enter");
        try {
            return p_kbIndexer!.GetValue(p_kbCurrent!.GetValue(null), [Enum.Parse(t_Key!, s)]);
        } catch {
            return null;
        }
    }

    private static bool TryReadMouseButton(PropertyInfo? property, int btn, out bool value) {
        value = false;
        if (property == null || p_mouseCurrent == null) {
            return false;
        }

        try {
            // The Input System assembly can be present even when it has no active
            // mouse device (for example, when the game is using the legacy backend).
            object? mouse = p_mouseCurrent.GetValue(null);
            PropertyInfo? buttonProperty = btn switch {
                0 => p_leftBtn,
                1 => p_rightBtn,
                2 => p_middleBtn,
                _ => null,
            };
            object? button = buttonProperty?.GetValue(mouse);
            if (button == null) {
                return false;
            }

            value = (bool)property.GetValue(button)!;
            return true;
        } catch {
            return false;
        }
    }

    private static bool TryReadMouseVector(PropertyInfo? controlProperty, out Vector2 value) {
        value = Vector2.zero;
        if (controlProperty == null || p_mouseCurrent == null || m_ReadV2 == null) {
            return false;
        }

        try {
            object? mouse = p_mouseCurrent.GetValue(null);
            object? control = controlProperty.GetValue(mouse);
            if (control == null) {
                return false;
            }

            value = (Vector2)m_ReadV2.Invoke(control, null)!;
            return true;
        } catch {
            return false;
        }
    }

    private static bool TryReadLegacyMouseButton(int btn, int state) {
        try {
            return state switch {
                0 => UnityEngine.Input.GetMouseButton(btn),
                1 => UnityEngine.Input.GetMouseButtonDown(btn),
                2 => UnityEngine.Input.GetMouseButtonUp(btn),
                _ => false,
            };
        } catch {
            return false;
        }
    }

    private static bool TryInvoke(PropertyInfo? prop, object? target) {
        try {
            return target != null && prop != null && (bool)prop.GetValue(target)!;
        } catch {
            return false;
        }
    }
}
