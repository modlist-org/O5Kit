// SPDX-License-Identifier: LGPL-3.0-or-later

using UnityEngine;

#if IL2CPP
using Il2CppInterop.Runtime.InteropTypes.Arrays;
#endif

namespace O5Kit.Compat;

/// <summary>Texture helpers with IL2CPP-safe overloads.</summary>
public static class O5Texture {
    /// <summary>Loads PNG/JPG bytes into a texture, marshalling for IL2CPP when needed.</summary>
    /// <param name="tex">Target texture.</param>
    /// <param name="data">Encoded image bytes.</param>
    /// <param name="markNonReadable">Free CPU memory after upload.</param>
    public static bool LoadImage(Texture2D tex, byte[] data, bool markNonReadable = false) {
#if IL2CPP
        Il2CppStructArray<byte> il2cppData = new(data.Length);
        for (int i = 0; i < data.Length; i++) {
            il2cppData[i] = data[i];
        }

        return tex.LoadImage(il2cppData, markNonReadable);
#else
        return tex.LoadImage(data, markNonReadable);
#endif
    }
}
