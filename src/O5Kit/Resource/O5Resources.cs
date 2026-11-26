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
using Il2CppInterop.Runtime;
using MelonLoader;
using UnityEngine.TextCore.LowLevel;
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

    /// <summary>Loads an embedded icon TTF font with content-hashed caching.</summary>
    public TMPro.TMP_FontAsset? LoadIconFontAsset(string name = "O5KitIcons.ttf") {
        byte[]? fontData = Load($"Font.{name}");
        return fontData == null ? null : LoadFontAssetCore(fontData, name);
    }

#if IL2CPP
    private delegate void InternalCreateFontFromPathDelegate(IntPtr self, IntPtr path);
    private static bool _il2CppFontReflectionReady;
    private static ConstructorInfo? _fontPathCtor;
    private static MethodInfo? _fontInternalCreateFromPath;
    private static InternalCreateFontFromPathDelegate? _fontCreateFromPathIcall;
    private static MethodInfo? _tmpCreateFromPath9;
    private static MethodInfo? _tmpCreateFromPath7;
#else
    private static bool _monoFontReflectionReady;
    private static MethodInfo? _monoCreateFromFont8;
    private static object? _monoGlyphRenderModeSdfaa;

    private static void EnsureMonoFontReflection() {
        if (_monoFontReflectionReady) {
            return;
        }
        _monoFontReflectionReady = true;
        // GlyphRenderMode lives in UnityEngine.TextCoreModule on Unity 2020.3 and
        // UnityEngine.TextCoreFontEngineModule on Unity 2021+/6; resolve via the
        // TMP_FontAsset overload's own parameter type to avoid a compile-time ref.
        foreach (MethodInfo m in typeof(TMPro.TMP_FontAsset).GetMethods(BindingFlags.Public | BindingFlags.Static)) {
            if (m.Name != "CreateFontAsset") {
                continue;
            }
            ParameterInfo[] ps = m.GetParameters();
            if (ps.Length == 8
                && ps[0].ParameterType == typeof(Font)
                && ps[1].ParameterType == typeof(int)
                && ps[2].ParameterType == typeof(int)
                && ps[3].ParameterType.IsEnum
                && ps[4].ParameterType == typeof(int)
                && ps[5].ParameterType == typeof(int)
                && ps[6].ParameterType == typeof(TMPro.AtlasPopulationMode)
                && ps[7].ParameterType == typeof(bool)) {
                _monoCreateFromFont8 = m;
                try {
                    _monoGlyphRenderModeSdfaa = Enum.Parse(ps[3].ParameterType, "SDFAA");
                } catch {
                    _monoGlyphRenderModeSdfaa = Enum.ToObject(ps[3].ParameterType, 4169);
                }
                break;
            }
        }
    }
#endif

    /// <summary>
    /// Creates a dynamic SDFAA <see cref="TMPro.TMP_FontAsset"/> from a Unity <see cref="Font"/>
    /// across both Unity 2020.3 (TextCoreModule) and Unity 2021+/6 (TextCoreFontEngineModule).
    /// </summary>
    public static TMPro.TMP_FontAsset? CreateFontAsset(
        Font font,
        int samplingPointSize = 90,
        int atlasPadding = 9,
        int atlasWidth = 1024,
        int atlasHeight = 1024,
        bool enableMultiAtlasSupport = true
    ) {
        if (font == null) {
            return null;
        }
#if IL2CPP
        return TMPro.TMP_FontAsset.CreateFontAsset(
            font,
            samplingPointSize,
            atlasPadding,
            GlyphRenderMode.SDFAA,
            atlasWidth,
            atlasHeight,
            TMPro.AtlasPopulationMode.Dynamic,
            enableMultiAtlasSupport
        );
#else
        EnsureMonoFontReflection();
        if (_monoCreateFromFont8 != null && _monoGlyphRenderModeSdfaa != null) {
            return _monoCreateFromFont8.Invoke(
                null,
                [font, samplingPointSize, atlasPadding, _monoGlyphRenderModeSdfaa, atlasWidth, atlasHeight, TMPro.AtlasPopulationMode.Dynamic, enableMultiAtlasSupport]
            ) as TMPro.TMP_FontAsset;
        }
        return TMPro.TMP_FontAsset.CreateFontAsset(font);
#endif
    }

    /// <summary>
    /// Creates a dynamic <see cref="TMPro.TMP_FontAsset"/> from a TTF/OTF file path on both Mono and IL2CPP.
    /// On IL2CPP, tries <c>Font(string)</c> via reflection first, then <c>TMP_FontAsset.CreateFontAsset(string, ...)</c>
    /// so stripped games without <c>Font(string)</c> still load fonts when FreeType/TMP supports it. Returns null on failure.
    /// </summary>
    public static TMPro.TMP_FontAsset? TryCreateFontAssetFromPath(
        string path,
        int samplingPointSize = 90,
        int atlasPadding = 9,
        int atlasWidth = 1024,
        int atlasHeight = 1024,
        bool enableMultiAtlasSupport = true
    ) {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) {
            return null;
        }
#if IL2CPP
        if (!_il2CppFontReflectionReady) {
            _il2CppFontReflectionReady = true;
            _fontPathCtor = typeof(Font).GetConstructor([typeof(string)]);
            _fontInternalCreateFromPath = typeof(Font).GetMethod(
                "Internal_CreateFontFromPath",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                null,
                [typeof(Font), typeof(string)],
                null
            );
            if (_fontInternalCreateFromPath == null) {
                try {
                    _fontCreateFromPathIcall = IL2CPP.ResolveICall<InternalCreateFontFromPathDelegate>("UnityEngine.Font::Internal_CreateFontFromPath");
                } catch {
                }
            }
            Type tmpType = typeof(TMPro.TMP_FontAsset);
            _tmpCreateFromPath9 = tmpType.GetMethod(
                "CreateFontAsset",
                BindingFlags.Public | BindingFlags.Static,
                null,
                [typeof(string), typeof(int), typeof(int), typeof(int), typeof(GlyphRenderMode), typeof(int), typeof(int), typeof(TMPro.AtlasPopulationMode), typeof(bool)],
                null
            );
            _tmpCreateFromPath7 = tmpType.GetMethod(
                "CreateFontAsset",
                BindingFlags.Public | BindingFlags.Static,
                null,
                [typeof(string), typeof(int), typeof(int), typeof(int), typeof(GlyphRenderMode), typeof(int), typeof(int)],
                null
            );
        }

        if (_fontPathCtor != null) {
            try {
                if (_fontPathCtor.Invoke([path]) is Font font && font != null) {
                    var asset = CreateFontAsset(font, samplingPointSize, atlasPadding, atlasWidth, atlasHeight, enableMultiAtlasSupport);
                    if (asset != null) {
                        return asset;
                    }
                }
            } catch {
            }
        }

        // Stripped Unity 2021/2022 IL2CPP builds drop Font(string) while keeping
        // Font.Internal_CreateFontFromPath (or its engine icall): allocate a bare
        // Il2Cpp Font object and bind the file path directly.
        if (_fontInternalCreateFromPath != null || _fontCreateFromPathIcall != null) {
            try {
                IntPtr rawPtr = IL2CPP.il2cpp_object_new(Il2CppClassPointerStore<Font>.NativeClassPtr);
                if (rawPtr != IntPtr.Zero) {
                    Font font = new(rawPtr);
                    if (_fontInternalCreateFromPath != null) {
                        _fontInternalCreateFromPath.Invoke(null, [font, path]);
                    } else {
                        _fontCreateFromPathIcall!(rawPtr, IL2CPP.ManagedStringToIl2Cpp(path));
                    }
                    if (font != null) {
                        var asset = CreateFontAsset(font, samplingPointSize, atlasPadding, atlasWidth, atlasHeight, enableMultiAtlasSupport);
                        if (asset != null) {
                            return asset;
                        }
                    }
                }
            } catch {
            }
        }

        if (_tmpCreateFromPath9 != null) {
            try {
                if (_tmpCreateFromPath9.Invoke(
                    null,
                    [path, 0, samplingPointSize, atlasPadding, GlyphRenderMode.SDFAA, atlasWidth, atlasHeight, TMPro.AtlasPopulationMode.Dynamic, enableMultiAtlasSupport]
                ) is TMPro.TMP_FontAsset asset && asset != null) {
                    return asset;
                }
            } catch {
            }
        }

        if (_tmpCreateFromPath7 != null) {
            try {
                if (_tmpCreateFromPath7.Invoke(
                    null,
                    [path, 0, samplingPointSize, atlasPadding, GlyphRenderMode.SDFAA, atlasWidth, atlasHeight]
                ) is TMPro.TMP_FontAsset asset && asset != null) {
                    return asset;
                }
            } catch {
            }
        }

        return null;
#else
        try {
            Font font = new(path);
            return CreateFontAsset(
                font,
                samplingPointSize,
                atlasPadding,
                atlasWidth,
                atlasHeight,
                enableMultiAtlasSupport
            );
        } catch {
            return null;
        }
#endif
    }

    private TMPro.TMP_FontAsset? LoadFontAssetCore(byte[] fontData, string name) {
        string hash = ComputeShortHash(fontData);
        string key = $"font:{name}:{hash}";
        if (_cache.TryGetValue(key, out object? cached)) {
            return cached as TMPro.TMP_FontAsset;
        }

        try {
            string dir = Path.Combine(Application.temporaryCachePath, "O5Kit", "Fonts");
            Directory.CreateDirectory(dir);
            string stem = Path.GetFileNameWithoutExtension(name);
            string ext = Path.GetExtension(name);
            string tempPath = Path.Combine(dir, $"{stem}_{hash}{ext}");
            if (!File.Exists(tempPath)) {
                File.WriteAllBytes(tempPath, fontData);
            }

            TMPro.TMP_FontAsset? asset = TryCreateFontAssetFromPath(tempPath);
            if (asset != null) {
                _cache[key] = asset;
            }
            return asset;
        } catch {
            return null;
        }
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
