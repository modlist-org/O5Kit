// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace O5Kit.Core;

/// <summary>Rounded 9-slice shape in canvas units.</summary>
/// <param name="Radius">Corner radius in canvas units.</param>
/// <param name="Stroke">Outline stroke in canvas units. 0 = filled.</param>
/// <param name="FlatBottom">Rounds only the top corners (top-bar art).</param>
public readonly record struct O5Shape(float Radius, float Stroke = 0f, bool FlatBottom = false) {
    /// <summary>Converts a texture-based slice (border texels at PPU) to canvas units.</summary>
    /// <param name="slice">Source slice.</param>
    /// <param name="strokeTexels">Outline stroke of the source art in texels. 0 = filled.</param>
    public static O5Shape FromSlice(O5Slice slice, float strokeTexels = 0f) {
        float k = 100f / slice.Ppu;
        return new O5Shape(slice.Border.x * k, strokeTexels * k, slice.Border.y <= 0f);
    }
}

/// <summary>Rasterizes <see cref="O5Shape"/> sprites at the live canvas scale so one texel maps to one screen pixel. Call <see cref="Tick"/> every frame with the canvas scale factor; tracked roots get their shape sprites swapped once the scale settles.</summary>
public sealed class O5ShapeSprites : IDisposable {
    private const float ReferencePpu = 100f;
    private const int SettleFrames = 4;

    private readonly Dictionary<(O5Shape, int), Sprite> _cache = new();
    private readonly Dictionary<int, O5Shape> _shapeOf = new();
    private readonly List<Transform> _roots = new();
    private float _scale = 1f;
    private float _pending = -1f;
    private int _pendingFrames;
    private bool _seeded;
    private bool _disposed;

    /// <summary>Scale factor the current sprites are rasterized for.</summary>
    public float ScaleFactor => _scale;

    /// <summary>Sprite for <paramref name="shape"/> at the current scale.</summary>
    /// <param name="shape">Shape in canvas units.</param>
    public Sprite Get(O5Shape shape) {
        int key = Mathf.RoundToInt(_scale * 1000f);
        if (_cache.TryGetValue((shape, key), out Sprite? cached) && cached) {
            return cached;
        }

        Sprite sprite = Rasterize(shape, key / 1000f);
        _cache[(shape, key)] = sprite;
        _shapeOf[sprite.GetInstanceID()] = shape;
        return sprite;
    }

    /// <summary>Swaps shape sprites under <paramref name="root"/> on scale changes.</summary>
    /// <param name="root">Canvas root (inactive children included).</param>
    public void Track(Transform root) {
        if (root && !_roots.Contains(root)) {
            _roots.Add(root);
        }
    }

    /// <summary>Feeds the live canvas scale. Applies the first value immediately, later changes once stable for a few frames (skips mid-tween rebuilds).</summary>
    /// <param name="scaleFactor">Canvas.scaleFactor of the tracked canvas.</param>
    public void Tick(float scaleFactor) {
        if (_disposed || scaleFactor <= 0f) {
            return;
        }

        if (!_seeded) {
            _seeded = true;
            Apply(scaleFactor);
            return;
        }

        if (Mathf.Abs(scaleFactor - _scale) < 0.0005f) {
            _pending = -1f;
            return;
        }

        if (Mathf.Abs(scaleFactor - _pending) >= 0.0005f) {
            _pending = scaleFactor;
            _pendingFrames = 0;
            return;
        }

        if (++_pendingFrames >= SettleFrames) {
            _pending = -1f;
            Apply(scaleFactor);
        }
    }

    private void Apply(float scaleFactor) {
        _scale = scaleFactor;
        _roots.RemoveAll(r => !r);
        foreach (Transform root in _roots) {
            foreach (Image img in root.GetComponentsInChildren<Image>(true)) {
                Sprite current = img.sprite;
                if (current && _shapeOf.TryGetValue(current.GetInstanceID(), out O5Shape shape)) {
                    Sprite next = Get(shape);
                    if (current != next) {
                        img.sprite = next;
                    }
                }
            }
        }

        EvictStale();
    }

    // Drops other-scale sprites no Image still uses; untracked canvases keep theirs until released.
    private void EvictStale() {
        int key = Mathf.RoundToInt(_scale * 1000f);
        List<(O5Shape, int)>? stale = null;
        foreach (var entry in _cache) {
            if (entry.Key.Item2 != key) {
                (stale ??= new()).Add(entry.Key);
            }
        }
        if (stale == null) {
            return;
        }

        var inUse = new HashSet<int>();
        try {
            foreach (Image img in Resources.FindObjectsOfTypeAll<Image>()) {
                Sprite s = img.sprite;
                if (s) {
                    inUse.Add(s.GetInstanceID());
                }
            }
        } catch {
            return;
        }

        foreach (var k in stale) {
            Sprite sprite = _cache[k];
            if (sprite) {
                int id = sprite.GetInstanceID();
                if (inUse.Contains(id)) {
                    continue;
                }
                _shapeOf.Remove(id);
                Object.Destroy(sprite.texture);
                Object.Destroy(sprite);
            }
            _cache.Remove(k);
        }
    }

    private static Sprite Rasterize(O5Shape shape, float scale) {
        int r = Math.Max(1, Mathf.RoundToInt(shape.Radius * scale));
        int stroke = shape.Stroke > 0f ? Mathf.Clamp(Mathf.RoundToInt(shape.Stroke * scale), 1, r) : 0;
        int w = 2 * r + 2;
        int h = shape.FlatBottom ? r + 2 : w;

        var pixels = new Color32[w * h];
        for (int y = 0; y < h; y++) {
            float py = y + 0.5f;
            float cy = shape.FlatBottom ? Math.Min(py, h - r) : Mathf.Clamp(py, r, h - r);
            for (int x = 0; x < w; x++) {
                float px = x + 0.5f;
                float cx = Mathf.Clamp(px, r, w - r);
                float dx = px - cx;
                float dy = py - cy;
                float inside = r - Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(inside + 0.5f);
                if (stroke > 0) {
                    a -= Mathf.Clamp01(inside - stroke + 0.5f);
                }

                pixels[y * w + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
            }
        }

        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false, false) {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave,
        };
        tex.SetPixels32(pixels);
        tex.Apply(false, true);

        Sprite sprite = Sprite.Create(
            tex,
            new Rect(0f, 0f, w, h),
            new Vector2(0.5f, 0.5f),
            ReferencePpu * scale, 0, SpriteMeshType.FullRect,
            new Vector4(r, shape.FlatBottom ? 0f : r, r, r));
        sprite.hideFlags = HideFlags.HideAndDontSave;
        return sprite;
    }

    /// <summary>Destroys all rasterized sprites and textures.</summary>
    public void Dispose() {
        if (_disposed) {
            return;
        }

        _disposed = true;
        foreach (Sprite sprite in _cache.Values) {
            if (sprite) {
                Object.Destroy(sprite.texture);
                Object.Destroy(sprite);
            }
        }

        _cache.Clear();
        _shapeOf.Clear();
        _roots.Clear();
    }
}
