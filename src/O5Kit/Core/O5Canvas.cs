// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using UnityEngine;
using UnityEngine.UI;

namespace O5Kit.Core;

/// <summary>Screen-space overlay root. Structure and scaling only; visibility belongs to the consumer.</summary>
public sealed class O5Canvas : IDisposable {
    /// <summary>Canvas root object.</summary>
    public GameObject Root { get; }

    /// <summary>Overlay canvas (sort order 32767).</summary>
    public Canvas Canvas { get; }

    /// <summary>Resolution scaler driven by <see cref="ApplyScale"/>.</summary>
    public CanvasScaler Scaler { get; }

    private bool _disposed;

    private O5Canvas(GameObject root, Canvas canvas, CanvasScaler scaler) {
        Root = root;
        Canvas = canvas;
        Scaler = scaler;
        ApplyScale(O5Boot.Config.UIScale);
    }

    /// <summary>Creates an overlay canvas under <paramref name="parent"/>. Visibility is untouched.</summary>
    /// <param name="parent">Scene or mod root to parent under.</param>
    /// <param name="name">GameObject name.</param>
    public static O5Canvas Create(Transform parent, string name = "O5Canvas") {
        var root = new GameObject(name);
        root.transform.SetParent(parent, false);

        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767;

        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        root.AddComponent<GraphicRaycaster>();

        return new O5Canvas(root, canvas, scaler);
    }

    /// <summary>Recomputes the reference resolution from a UI scale multiplier.</summary>
    /// <param name="scale">Scale multiplier (1 = 1920x1080 reference).</param>
    public void ApplyScale(float scale) {
        Scaler.referenceResolution = new Vector2(1920f, 1080f) / Math.Max(scale, 0.01f);
    }

    /// <summary>Destroys the canvas root. Idempotent.</summary>
    public void Dispose() {
        if (_disposed) {
            return;
        }

        _disposed = true;
        UnityEngine.Object.Destroy(Root);
    }
}
