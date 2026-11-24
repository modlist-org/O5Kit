// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using O5Kit.Compat;
using UnityEngine;
using Object = UnityEngine.Object;

#if IL2CPP
using MelonLoader;
#endif

namespace O5Kit.Resource;

/// <summary>Bundled artwork keys. Only what O5Kit itself uses; game/mod icons live in the mod repo.</summary>
public enum O5Asset {
    /// <summary>Solid circle (panels, controls, dots).</summary>
    Circle128,
    /// <summary>Top-rounded bar.</summary>
    CircleHalf128,
    /// <summary>Circle outline ring, 32px stroke (panel/hover outlines).</summary>
    CircleOutline128O32,
}

/// <summary>Loads O5Kit-owned bundled assets. Mod-specific assets belong to the consuming mod and are loaded from that mod's assembly.</summary>
public sealed class O5Resources : IDisposable {
    private readonly Assembly _assembly;
    private readonly string _prefix;
    private readonly Dictionary<string, object> _cache = new();

    private static readonly Dictionary<O5Asset, string> AssetMap = new() {
        [O5Asset.Circle128] = "Image.Circle128.png",
        [O5Asset.CircleHalf128] = "Image.CircleHalf128.png",
        [O5Asset.CircleOutline128O32] = "Image.CircleOutline128O32.png",
    };

    /// <summary>Creates a loader over an assembly manifest prefix.</summary>
    /// <param name="assembly">Assembly containing the assets.</param>
    /// <param name="prefix">Manifest prefix, e.g. <c>"O5Kit.Asset."</c>.</param>
    public O5Resources(Assembly assembly, string prefix) {
        _assembly = assembly;
        _prefix = prefix;
    }

    /// <summary>Creates a loader for O5Kit's own bundled artwork.</summary>
    public static O5Resources Bundled()
        => new(typeof(O5Resources).Assembly, "O5Kit.Asset.");

    /// <summary>Reads raw bytes for a manifest path. Null when missing.</summary>
    /// <param name="path">Manifest suffix with dots, e.g. <c>"Image.Panel256.png"</c>.</param>
    public byte[]? Load(string path) {
        if (string.IsNullOrWhiteSpace(path)) {
            return null;
        }

        try {
            using Stream? stream = _assembly.GetManifestResourceStream(_prefix + path);

            if (stream == null) {
                return null;
            }

            if (stream.Length <= 0) {
                return [];
            }

            byte[] data = new byte[stream.Length];
            int offset = 0;

            while (offset < data.Length) {
                int read = stream.Read(data, offset, data.Length - offset);

                if (read <= 0) {
                    break;
                }

                offset += read;
            }

            return offset == data.Length ? data : null;
        } catch {
            return null;
        }
    }

    /// <summary>Loads (and caches) a texture. Null when missing or undecodable.</summary>
    /// <param name="path">Manifest suffix.</param>
    /// <param name="filter">Texture filter mode.</param>
    public Texture2D? LoadTexture(string path, FilterMode filter = FilterMode.Bilinear) {
        if (_cache.TryGetValue(path, out object? cached)) {
            return cached as Texture2D;
        }

        byte[]? data = Load(path);

        if (data == null || data.Length == 0) {
            return null;
        }

        Texture2D texture = new(2, 2, TextureFormat.RGBA32, false, false);

        if (!O5Texture.LoadImage(texture, data)) {
            Object.Destroy(texture);
            return null;
        }

        texture.filterMode = filter;
        texture.hideFlags = HideFlags.HideAndDontSave;
        _cache[path] = texture;
        return texture;
    }

    /// <summary>Loads a font file into a TMP asset via a content-hashed temp copy. Cached by name and hash.</summary>
    /// <param name="fontData">TTF/OTF bytes.</param>
    /// <param name="name">Cache and file name, e.g. <c>"MyFont-Medium.otf"</c>.</param>
    public TMPro.TMP_FontAsset? LoadFontAsset(byte[] fontData, string name)
        => LoadFontAssetCore(fontData, name);

    /// <summary>Loads an embedded icon TTF font with content-hashed caching (returns null on IL2CPP).</summary>
    public TMPro.TMP_FontAsset? LoadIconFontAsset(string name = "O5KitIcons.ttf") {
        byte[]? fontData = Load($"Font.{name}");
        return fontData == null ? null : LoadFontAssetCore(fontData, name);
    }

    private TMPro.TMP_FontAsset? LoadFontAssetCore(byte[] fontData, string name) {
        string hash = ComputeShortHash(fontData);
        string key = $"font:{name}:{hash}";
        if (_cache.TryGetValue(key, out object? cached)) {
            return cached as TMPro.TMP_FontAsset;
        }

#if IL2CPP
        // Stripped runtimes expose neither Font(path) nor OS-font creation:
        // custom TTF bytes cannot be turned into a Font here.
        return null;
#else
        try {
            string dir = Path.Combine(Application.temporaryCachePath, "O5Kit", "Fonts");
            Directory.CreateDirectory(dir);
            string stem = Path.GetFileNameWithoutExtension(name);
            string ext = Path.GetExtension(name);
            string tempPath = Path.Combine(dir, $"{stem}_{hash}{ext}");
            if (!File.Exists(tempPath)) {
                File.WriteAllBytes(tempPath, fontData);
            }

            Font font = new(tempPath);
            TMPro.TMP_FontAsset asset = TMPro.TMP_FontAsset.CreateFontAsset(font);
            _cache[key] = asset;
            return asset;
        } catch {
            return null;
        }
#endif
    }

    private static string ComputeShortHash(byte[] data) {
        using var sha = SHA256.Create();
        byte[] digest = sha.ComputeHash(data);
        return BitConverter.ToString(digest, 0, 4).Replace("-", string.Empty).ToLowerInvariant();
    }

    /// <summary>Loads (and caches) a texture by artwork key.</summary>
    /// <param name="asset">Artwork key.</param>
    public Texture2D? GetTexture(O5Asset asset)
        => AssetMap.TryGetValue(asset, out string? path) ? LoadTexture(path) : null;

    /// <summary>Loads a texture by manifest suffix.</summary>
    /// <param name="path">Manifest suffix.</param>
    public Texture2D? GetTexture(string path) {
        if (string.IsNullOrWhiteSpace(path)) {
            return null;
        }

        return LoadTexture(path);
    }

    /// <summary>Destroys cached textures and font assets.</summary>
    public void Dispose() {
        foreach (object item in _cache.Values) {
            if (item is Texture2D texture) {
                Object.Destroy(texture);
            } else if (item is TMPro.TMP_FontAsset font) {
                Object.Destroy(font);
            }
        }

        _cache.Clear();
    }
}
