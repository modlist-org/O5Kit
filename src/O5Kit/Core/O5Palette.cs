// SPDX-License-Identifier: LGPL-3.0-or-later

using UnityEngine;

namespace O5Kit.Core;

/// <summary>Alpha-only color derivations. Base RGB lives in <see cref="O5Theme"/>, alpha variants are derived here so a hue change propagates.</summary>
public static class O5Palette {
    /// <summary>Returns <paramref name="c"/> with its alpha replaced by <paramref name="a"/>.</summary>
    public static Color WithAlpha(Color c, float a) => new(c.r, c.g, c.b, a);

    /// <summary>Secondary text from a base text color.</summary>
    public static Color Dim(Color text, float alpha) => WithAlpha(text, alpha);

    /// <summary>Faint text/placeholder from a base text color.</summary>
    public static Color Faint(Color text, float alpha) => WithAlpha(text, alpha);

    /// <summary>Off-state accent from the on-state accent.</summary>
    public static Color Inactive(Color active, float alpha) => WithAlpha(active, alpha);

    /// <summary>Fully transparent variant (e.g. hidden dots, idle hover rings).</summary>
    public static Color Transparent(Color c) => WithAlpha(c, 0f);
}
