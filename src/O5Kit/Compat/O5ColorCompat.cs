// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using UnityEngine;

namespace O5Kit.Compat;

/// <summary>Hex color helpers for stripped runtimes (no ColorUtility in metadata).</summary>
internal static class O5ColorCompat {
    /// <summary>Formats <c>RRGGBBAA</c> (uppercase, no leading <c>#</c>).</summary>
    public static string ToHtmlStringRGBA(Color color) {
        byte r = ToByte(color.r);
        byte g = ToByte(color.g);
        byte b = ToByte(color.b);
        byte a = ToByte(color.a);
        return r.ToString("X2") + g.ToString("X2") + b.ToString("X2") + a.ToString("X2");
    }

    /// <summary>Parses <c>#RRGGBB[AA]</c> or <c>#RGB[A]</c>, with or without <c>#</c>. Named colors are not supported.</summary>
    public static bool TryParseHtmlString(string text, out Color color) {
        color = default;
        if (string.IsNullOrEmpty(text)) {
            return false;
        }

        string hex = text.StartsWith("#", StringComparison.Ordinal) ? text.Substring(1) : text;
        if (hex.Length == 3 || hex.Length == 4) {
            char[] expanded = new char[hex.Length * 2];
            for (int i = 0; i < hex.Length; i++) {
                expanded[i * 2] = hex[i];
                expanded[i * 2 + 1] = hex[i];
            }
            hex = new string(expanded);
        }

        if (hex.Length != 6 && hex.Length != 8) {
            return false;
        }

        foreach (char c in hex) {
            if (!IsHexDigit(c)) {
                return false;
            }
        }

        try {
            float r = Convert.ToInt32(hex.Substring(0, 2), 16) / 255f;
            float g = Convert.ToInt32(hex.Substring(2, 2), 16) / 255f;
            float b = Convert.ToInt32(hex.Substring(4, 2), 16) / 255f;
            float a = hex.Length == 8 ? Convert.ToInt32(hex.Substring(6, 2), 16) / 255f : 1f;
            color = new Color(r, g, b, a);
            return true;
        } catch {
            return false;
        }
    }

    private static byte ToByte(float channel) {
        if (channel <= 0f) {
            return 0;
        }
        if (channel >= 1f) {
            return 255;
        }
        return (byte)Math.Round(channel * 255d, MidpointRounding.AwayFromZero);
    }

    private static bool IsHexDigit(char c)
        => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
}
