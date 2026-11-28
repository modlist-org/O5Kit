// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Linq;
using O5Kit.Resource;
using UnityEngine;

namespace O5Kit.Core;

/// <summary>Bundled-asset sprite source (see <c>Asset/NOTICE.md</c>). Needs no game assets. Slice shapes come from <see cref="O5SpriteStyle"/> (Overlayer values by default); pass your own style for custom art.</summary>
public sealed class DefaultSpriteProvider : ISpriteProvider, IDisposable {
    private readonly O5Resources _resources = O5Resources.Bundled();
    private readonly O5SpriteStyle _style;
    private readonly O5ShapeSprites _shapes;
    private readonly bool _ownsShapes;
    private readonly Dictionary<string, Sprite> _cache = new();
    private bool _disposed;

    /// <summary>Creates a provider over the bundled artwork.</summary>
    /// <param name="style">Slicing style. Null uses <see cref="O5SpriteStyle.Default"/> (Overlayer values).</param>
    /// <param name="shapes">Scale-aware rasterizer for the rounded roles. Null creates an owned one.</param>
    public DefaultSpriteProvider(O5SpriteStyle? style = null, O5ShapeSprites? shapes = null) {
        _style = style ?? O5SpriteStyle.Default;
        _ownsShapes = shapes == null;
        _shapes = shapes ?? new O5ShapeSprites();
    }

    // Rounded roles are rasterized at the live canvas scale (pixel-exact);
    // the outline art strokes half its corner border.
    /// <inheritdoc/>
    public Sprite RoundedPanel => _shapes.Get(O5Shape.FromSlice(_style.Panel));

    /// <inheritdoc/>
    public Sprite RoundedControl => _shapes.Get(O5Shape.FromSlice(_style.Control));

    /// <inheritdoc/>
    public Sprite TopBar => _shapes.Get(O5Shape.FromSlice(_style.TopBar));

    /// <inheritdoc/>
    public Sprite RoundedOutline => _shapes.Get(O5Shape.FromSlice(_style.Outline, _style.Outline.Border.x * 0.5f));

    /// <inheritdoc/>
    public Sprite Circle => GetSimple(O5Asset.Circle128);

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
            _style.SimplePpu);
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
        if (_ownsShapes) {
            _shapes.Dispose();
        }

        _resources.Dispose();
    }
}

/// <summary>Bundled SUIT/JetBrains Mono fonts with system-font fallback. Needs no game assets.</summary>
public sealed class DefaultFontProvider : IFontProvider, IDisposable {
    private readonly O5Resources _resources = O5Resources.Bundled();
    private TMPro.TMP_FontAsset? _regular;
    private TMPro.TMP_FontAsset? _medium;
    private TMPro.TMP_FontAsset? _mono;
    private TMPro.TMP_FontAsset? _icons;
    private bool _iconsLoaded;
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

    /// <inheritdoc/>
    public TMPro.TMP_FontAsset? Icons {
        get {
            if (!_iconsLoaded) {
                _iconsLoaded = true;
                _icons = _resources.LoadIconFontAsset("O5KitIcons.ttf");
            }
            return _icons;
        }
    }

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
        _regular = _medium = _mono = _icons = null;
        _resources.Dispose();
    }
}
