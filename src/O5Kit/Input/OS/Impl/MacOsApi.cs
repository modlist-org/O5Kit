// SPDX-License-Identifier: LGPL-2.1-or-later

using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace O5Kit.Input.OS.Impl;

/// <summary>macOS cursor access via CoreGraphics.</summary>
public sealed class MacOsApi : OsApi {
    [StructLayout(LayoutKind.Sequential)]
    private struct CGPoint(double x, double y) {
        public double x = x;
        public double y = y;
    }

    [DllImport("/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices")]
    private static extern int CGWarpMouseCursorPosition(CGPoint newCursorPosition);

    [DllImport("/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices")]
    private static extern int CGAssociateMouseAndMouseCursorPosition(int connected);

    // Out-param instead of CGEventGetLocation's struct return: Unity's arm64 Mono returns garbage/zero for HFA structs.
    [DllImport("/System/Library/Frameworks/Carbon.framework/Carbon")]
    private static extern IntPtr HIGetMousePosition(uint space, IntPtr obj, out CGPoint point);

    private const uint kHICoordSpace72DPIGlobal = 1;

    public override void SetCursorPosition(int x, int y) {
        try {
            CGWarpMouseCursorPosition(new CGPoint(x, y));
            // Warping suppresses mouse input for ~250ms unless re-associated.
            CGAssociateMouseAndMouseCursorPosition(1);
        } catch {
        }
    }

    public override Vector2Int GetCursorPosition() {
        try {
            HIGetMousePosition(kHICoordSpace72DPIGlobal, IntPtr.Zero, out CGPoint p);
            return new Vector2Int(
                Mathf.RoundToInt((float)p.x),
                Mathf.RoundToInt((float)p.y)
            );
        } catch {
        }

        return Vector2Int.zero;
    }
}
