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
        return t_Mouse != null ? TryInvoke(p_btnIsPressed, GetMouseBtnControl(btn)) : UnityEngine.Input.GetMouseButton(btn);
    }

    /// <summary>Whether a mouse button went down this frame.</summary>
    /// <param name="btn">0 = left, 1 = right, 2 = middle.</param>
    public static bool GetMouseButtonDown(int btn) {
        EnsureInitialized();
        return t_Mouse != null ? TryInvoke(p_btnWasPressed, GetMouseBtnControl(btn)) : UnityEngine.Input.GetMouseButtonDown(btn);
    }

    /// <summary>Whether a mouse button went up this frame.</summary>
    /// <param name="btn">0 = left, 1 = right, 2 = middle.</param>
    public static bool GetMouseButtonUp(int btn) {
        EnsureInitialized();
        return t_Mouse != null ? TryInvoke(p_btnWasReleased, GetMouseBtnControl(btn)) : UnityEngine.Input.GetMouseButtonUp(btn);
    }

    /// <summary>Cursor position in screen pixels (Unity space, origin bottom-left).</summary>
    public static Vector2 MousePosition {
        get {
            EnsureInitialized();
            if (t_Mouse != null) {
                try {
                    return (Vector2)m_ReadV2!.Invoke(p_mousePosition!.GetValue(p_mouseCurrent!.GetValue(null)), null)!;
                } catch {
                    return Vector2.zero;
                }
            }

            return UnityEngine.Input.mousePosition;
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
            if (t_Mouse != null) {
                try {
                    return (Vector2)m_ReadV2!.Invoke(p_mouseScroll!.GetValue(p_mouseCurrent!.GetValue(null)), null)!;
                } catch {
                    return Vector2.zero;
                }
            }

            return UnityEngine.Input.mouseScrollDelta;
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

    private static object? GetMouseBtnControl(int btn) {
        EnsureInitialized();
        var mouse = p_mouseCurrent!.GetValue(null);
        return btn switch {
            0 => p_leftBtn!.GetValue(mouse),
            1 => p_rightBtn!.GetValue(mouse),
            2 => p_middleBtn!.GetValue(mouse),
            _ => null,
        };
    }

    private static bool TryInvoke(PropertyInfo? prop, object? target) {
        try {
            return target != null && prop != null && (bool)prop.GetValue(target)!;
        } catch {
            return false;
        }
    }
}
