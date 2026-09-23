// SPDX-License-Identifier: LGPL-2.1-or-later

using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace O5Kit.Input.OS.Impl;

/// <summary>Windows cursor access via user32.</summary>
public sealed class WinOsApi : OsApi {
    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT {
        public int X;
        public int Y;
    }

    public override void SetCursorPosition(int x, int y) {
        try {
            SetCursorPos(x, y);
        } catch {
        }
    }

    public override Vector2Int GetCursorPosition() {
        try {
            if (GetCursorPos(out POINT p)) {
                return new Vector2Int(p.X, p.Y);
            }
        } catch {
        }

        return Vector2Int.zero;
    }
}
