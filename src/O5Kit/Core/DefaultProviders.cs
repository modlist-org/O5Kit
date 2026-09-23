// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using O5Kit.Resource;
using UnityEngine;

namespace O5Kit.Core;

/// <summary>Bundled-asset sprite source (see <c>Asset/NOTICE.md</c>). Needs no game assets.</summary>
public sealed class DefaultSpriteProvider : ISpriteProvider, IDisposable {
    private readonly O5Resources _resources = O5Resources.Bundled();
    private readonly Dictionary<string, Sprite> _cache = new();
    private bool _disposed;

    /// <inheritdoc/>
    public Sprite RoundedPanel => GetSliced(O5Asset.Panel, 56f);

    /// <inheritdoc/>
    public Sprite RoundedControl => GetSliced(O5Asset.Control, 40f);

    /// <inheritdoc/>
    public Sprite TopBar => GetSliced(O5Asset.TopBar, new Vector4(56f, 0f, 56f, 56f));

    /// <inheritdoc/>
    public Sprite RoundedOutline => GetSliced(O5Asset.Outline, 56f);

    /// <inheritdoc/>
    public Sprite Circle => GetSimple(O5Asset.Circle);

    /// <inheritdoc/>
    public Sprite? Icon(string name) => name switch {
        "toggle-on" => GetSimple(O5Asset.Circle),
        "toggle-off" => GetSimple(O5Asset.Ring),
        "Triangle128" or "triangle" => GetSimple(O5Asset.Triangle),
        "X128" or "x" => GetSimple(O5Asset.X),
        _ => null,
    };

    private Sprite GetSliced(O5Asset asset, float border)
        => GetSliced(asset, new Vector4(border, border, border, border));

    private Sprite GetSliced(O5Asset asset, Vector4 border) {
        string key = $"sliced:{asset}:{border}";
        if (_cache.TryGetValue(key, out Sprite? cached) && cached) {
            return cached;
        }

        Texture2D? texture = _resources.GetTexture(asset);
        if (texture == null) {
            throw new InvalidOperationException($"O5Kit: bundled asset missing: {asset}");
        }

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f, 0, SpriteMeshType.FullRect, border);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        _cache[key] = sprite;
        return sprite;
    }

    private Sprite GetSimple(O5Asset asset) {
        string key = $"simple:{asset}";
        if (_cache.TryGetValue(key, out Sprite? cached) && cached) {
            return cached;
        }

        Texture2D? texture = _resources.GetTexture(asset);
        if (texture == null) {
            throw new InvalidOperationException($"O5Kit: bundled asset missing: {asset}");
        }

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        _cache[key] = sprite;
        return sprite;
    }

    /// <summary>Destroys created sprites and textures.</summary>
    public void Dispose() {
        if (_disposed) {
            return;
        }

        _disposed = true;
        foreach (Sprite sprite in _cache.Values) {
            if (sprite) {
                UnityEngine.Object.Destroy(sprite);
            }
        }

        _cache.Clear();
        _resources.Dispose();
    }
}

/// <summary>Bundled SUIT/JetBrains Mono fonts with system-font fallback. Needs no game assets.</summary>
public sealed class DefaultFontProvider : IFontProvider, IDisposable {
    private readonly O5Resources _resources = O5Resources.Bundled();
    private TMPro.TMP_FontAsset? _regular;
    private TMPro.TMP_FontAsset? _medium;
    private TMPro.TMP_FontAsset? _mono;
    private bool _disposed;

    /// <inheritdoc/>
    public TMPro.TMP_FontAsset Regular
        => _regular ??= LoadEmbedded("Font.SUIT-Regular.otf", "SUIT-Regular.otf") ?? LoadSystemFont();

    /// <inheritdoc/>
    public TMPro.TMP_FontAsset Medium
        => _medium ??= LoadEmbedded("Font.SUIT-Medium.otf", "SUIT-Medium.otf") ?? LoadSystemFont();

    /// <inheritdoc/>
    public TMPro.TMP_FontAsset Monospace
        => _mono ??= LoadEmbedded("Font.JetBrainsMonoNL-Medium.ttf", "JetBrainsMonoNL-Medium.ttf") ?? LoadSystemFont();

    private TMPro.TMP_FontAsset? LoadEmbedded(string path, string name) {
        byte[]? data = _resources.Load(path);
        return data == null ? null : _resources.LoadFontAsset(data, name);
    }

    private static TMPro.TMP_FontAsset LoadSystemFont() {
        Font? font = Resources.GetBuiltinResource<Font>("Arial.ttf")
            ?? Resources.FindObjectsOfTypeAll<Font>().FirstOrDefault();
        if (font == null) {
            throw new InvalidOperationException("O5Kit: no runtime Font found for DefaultFontProvider.");
        }

        return TMPro.TMP_FontAsset.CreateFontAsset(font);
    }

    /// <summary>Destroys loaded font assets.</summary>
    public void Dispose() {
        if (_disposed) {
            return;
        }

        _disposed = true;
        _regular = _medium = _mono = null;
        _resources.Dispose();
    }
}
