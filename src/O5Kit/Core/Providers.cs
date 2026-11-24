// SPDX-License-Identifier: LGPL-3.0-or-later

using UnityEngine;

namespace O5Kit.Core;

/// <summary>Rounded-rect and panel sprites. Implement this over your own assets so O5Kit ships asset-free.</summary>
public interface ISpriteProvider {
    /// <summary>Large rounded panel background (window body).</summary>
    Sprite RoundedPanel { get; }

    /// <summary>Small rounded control background (rows, list items) and mask-friendly fill.</summary>
    Sprite RoundedControl { get; }

    /// <summary>Rounded-top bar background.</summary>
    Sprite TopBar { get; }

    /// <summary>Rounded outline used for hover rings and window borders.</summary>
    Sprite RoundedOutline { get; }

    /// <summary>Plain filled circle (dots, handles).</summary>
    Sprite Circle { get; }
}

/// <summary>TMP fonts. Implement this over your own font assets.</summary>
public interface IFontProvider {
    /// <summary>Regular body font.</summary>
    TMPro.TMP_FontAsset Regular { get; }

    /// <summary>Medium/emphasis font for titles and labels.</summary>
    TMPro.TMP_FontAsset Medium { get; }

    /// <summary>Monospace font for numeric inputs and code.</summary>
    TMPro.TMP_FontAsset Monospace { get; }

    /// <summary>Vector icon glyph font (may be null on stripped runtimes).</summary>
    TMPro.TMP_FontAsset? Icons { get; }
}
