// SPDX-License-Identifier: LGPL-2.1-or-later

using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace O5Kit.Input.OS.Impl;

/// <summary>Linux cursor access via X11. No-op on Wayland without XWayland.</summary>
public sealed class LinuxOsApi : OsApi {
    [DllImport("libX11.so.6")]
    private static extern IntPtr XOpenDisplay(string? displayStr);
    [DllImport("libX11.so.6")]
    private static extern IntPtr XDefaultRootWindow(IntPtr display);
    [DllImport("libX11.so.6")]
    private static extern int XWarpPointer(IntPtr display, IntPtr srcWindow, IntPtr dstWindow, int srcX, int srcY, uint srcWidth, uint srcHeight, int dstX, int dstY);
    [DllImport("libX11.so.6")]
    private static extern int XFlush(IntPtr display);
    [DllImport("libX11.so.6")]
    private static extern int XCloseDisplay(IntPtr display);

    [DllImport("libX11.so.6")]
    private static extern bool XQueryPointer(IntPtr display, IntPtr window, out IntPtr rootReturn, out IntPtr childReturn, out int rootXReturn, out int rootYReturn, out int winXReturn, out int winYReturn, out uint maskReturn);

    public override void SetCursorPosition(int x, int y) {
        IntPtr display = IntPtr.Zero;
        try {
            display = XOpenDisplay(null);
            if (display == IntPtr.Zero) {
                return;
            }

            IntPtr rootWindow = XDefaultRootWindow(display);
            XWarpPointer(display, IntPtr.Zero, rootWindow, 0, 0, 0, 0, x, y);
            XFlush(display);
        } finally {
            if (display != IntPtr.Zero) {
                XCloseDisplay(display);
            }
        }
    }

    public override Vector2Int GetCursorPosition() {
        IntPtr display = IntPtr.Zero;
        try {
            display = XOpenDisplay(null);
            if (display != IntPtr.Zero) {
                IntPtr rootWindow = XDefaultRootWindow(display);

                XQueryPointer(display, rootWindow, out _, out _, out int rootX, out int rootY, out _, out _, out _);
                return new Vector2Int(rootX, rootY);
            }
        } finally {
            if (display != IntPtr.Zero) {
                XCloseDisplay(display);
            }
        }

        return Vector2Int.zero;
    }
}
