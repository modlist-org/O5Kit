// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Collections.Generic;
using UnityEngine;

namespace O5Kit.Core;

/// <summary>Owns a canvas and the windows on it. Tracks structure and z-order only; visibility and animation belong to the consumer.</summary>
public sealed class O5WindowManager : IDisposable {
    /// <summary>Shared overlay canvas.</summary>
    public O5Canvas Canvas { get; }

    private readonly List<O5Window> _windows = new();
    private bool _disposed;

    private O5WindowManager(O5Canvas canvas) {
        Canvas = canvas;
    }

    /// <summary>Creates a manager with its own overlay canvas under <paramref name="parent"/>.</summary>
    /// <param name="parent">Scene or mod root to parent under.</param>
    /// <param name="name">Canvas object name.</param>
    public static O5WindowManager Create(Transform parent, string name = "O5Windows") {
        return new O5WindowManager(O5Canvas.Create(parent, name));
    }

    /// <summary>Tracked windows (alive, visible or not).</summary>
    public IReadOnlyList<O5Window> Windows => _windows;

    /// <summary>Builds and tracks a window. Visibility is untouched; show it yourself.</summary>
    /// <param name="options">Appearance and behaviour.</param>
    public O5Window Create(O5WindowOptions options) {
        var window = O5Window.Create(Canvas.Root.transform, options);
        window.Focused += BringToFront;
        window.OnDisposed += () => {
            window.Focused -= BringToFront;
            _windows.Remove(window);
        };
        _windows.Add(window);
        return window;
    }

    /// <summary>Moves a window above its siblings.</summary>
    /// <param name="window">Window to front.</param>
    public void BringToFront(O5Window window) {
        if (_disposed || window.IsDisposed) {
            return;
        }

        if (window.Options.BringToFrontOnFocus) {
            window.BringToFront();
        }
    }

    /// <summary>Disposes a tracked window.</summary>
    /// <param name="window">Window to destroy.</param>
    public void Destroy(O5Window window) => window.Dispose();

    /// <summary>Disposes every window and the canvas.</summary>
    public void Dispose() {
        if (_disposed) {
            return;
        }

        _disposed = true;
        foreach (var window in _windows.ToArray()) {
            window.Dispose();
        }

        _windows.Clear();
        Canvas.Dispose();
    }
}
