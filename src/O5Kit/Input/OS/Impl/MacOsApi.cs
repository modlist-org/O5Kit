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
    private static extern IntPtr CGEventCreate(IntPtr source);

    [DllImport("/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices")]
    private static extern CGPoint CGEventGetLocation(IntPtr eventRef);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    private static extern void CFRelease(IntPtr cf);

    public override void SetCursorPosition(int x, int y) {
        try {
            CGWarpMouseCursorPosition(new CGPoint(x, y));
        } catch {
        }
    }

    public override Vector2Int GetCursorPosition() {
        try {
            IntPtr eventRef = CGEventCreate(IntPtr.Zero);

            if (eventRef == IntPtr.Zero) {
                return Vector2Int.zero;
            }

            try {
                CGPoint p = CGEventGetLocation(eventRef);
                return new Vector2Int(
                    Mathf.RoundToInt((float)p.x),
                    Mathf.RoundToInt((float)p.y)
                );
            } finally {
                CFRelease(eventRef);
            }
        } catch {
        }

        return Vector2Int.zero;
    }
}
