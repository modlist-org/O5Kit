// SPDX-License-Identifier: LGPL-3.0-or-later

using UnityEngine;

namespace O5Kit.Compat;

/// <summary>Texture helpers with IL2CPP-safe overloads.</summary>
public static class O5Texture {
    /// <summary>Loads PNG/JPG bytes into a texture, marshalling for IL2CPP when needed.</summary>
    /// <param name="tex">Target texture.</param>
    /// <param name="data">Encoded image bytes.</param>
    /// <param name="markNonReadable">Free CPU memory after upload.</param>
    public static bool LoadImage(Texture2D tex, byte[] data, bool markNonReadable = false) {
        // Instance-style extension binding is unavailable on stripped dummies;
        // the static form is identical (verified against Unity API surface).
        // Il2CppInterop marshals managed byte[] automatically.
        return UnityEngine.ImageConversion.LoadImage(tex, data, markNonReadable);
    }
}
