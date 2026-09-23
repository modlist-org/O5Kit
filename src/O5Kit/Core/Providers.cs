// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using UnityEngine;

namespace O5Kit.Core;

/// <summary>Rounded-rect and icon sprites. Implement this over your own assets so O5Kit ships asset-free.</summary>
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

    /// <summary>Named icon sprite. Return null when unmapped; callers keep their current sprite.</summary>
    /// <param name="name">Icon key, e.g. <c>"toggle-on"</c>, <c>"toggle-off"</c>, or an asset name.</param>
    Sprite? Icon(string name);
}

/// <summary>TMP fonts. Implement this over your own font assets.</summary>
public interface IFontProvider {
    /// <summary>Regular body font.</summary>
    TMPro.TMP_FontAsset Regular { get; }

    /// <summary>Medium/emphasis font for titles and labels.</summary>
    TMPro.TMP_FontAsset Medium { get; }

    /// <summary>Monospace font for numeric inputs and code.</summary>
    TMPro.TMP_FontAsset Monospace { get; }
}

/// <summary>One-shot service locator. Configure once at startup, then build controls.</summary>
public static class O5Boot {
    /// <summary>Runtime options (scale, tooltip, click behaviour).</summary>
    public static O5Config Config { get; private set; } = O5Config.Default;

    /// <summary>Active visual theme.</summary>
    public static O5Theme Theme { get; private set; } = O5Theme.Dark;

    /// <summary>Active sprite source. Required before creating controls.</summary>
    public static ISpriteProvider Sprites { get; private set; } = null!;

    /// <summary>Active font source. Required before creating controls.</summary>
    public static IFontProvider Fonts { get; private set; } = null!;

    /// <summary>Active tween backend. Defaults to LitMotion.</summary>
#if O5KIT_NO_LITMOTION
    public static ITweenRunner Tween { get; private set; } = SimpleTweenRunner.Instance;
#else
    public static ITweenRunner Tween { get; private set; } = LitMotionRunner.Instance;
#endif

    /// <summary>Installs bundled-asset defaults for anything not configured. Call once at startup for zero-setup usage.</summary>
    /// <param name="config">Optional options override.</param>
    /// <param name="theme">Optional theme override.</param>
    public static void EnsureDefaults(O5Config? config = null, O5Theme? theme = null) {
        if (config != null) {
            Config = config;
        }

        if (theme != null) {
            Theme = theme;
        }

        Sprites ??= new DefaultSpriteProvider();
        Fonts ??= new DefaultFontProvider();
    }

    /// <summary>Swaps the active theme. Applies to subsequently created controls; rebuild UI to restyle existing ones.</summary>
    /// <param name="theme">New theme.</param>
    public static void SetTheme(O5Theme theme) {
        Theme = theme;
        ThemeChanged?.Invoke(theme);
    }

    /// <summary>Fired after <see cref="SetTheme"/>.</summary>
    public static event Action<O5Theme>? ThemeChanged;

    /// <summary>Whether sprites and fonts have been provided.</summary>
    public static bool IsConfigured => Sprites != null && Fonts != null;

    /// <summary>Installs O5Kit services. Null arguments leave the current value in place.</summary>
    /// <param name="config">Runtime options.</param>
    /// <param name="theme">Visual theme.</param>
    /// <param name="sprites">Sprite source.</param>
    /// <param name="fonts">Font source.</param>
    /// <param name="tween">Tween backend override.</param>
    public static void Configure(
        O5Config? config = null,
        O5Theme? theme = null,
        ISpriteProvider? sprites = null,
        IFontProvider? fonts = null,
        ITweenRunner? tween = null) {
        if (config != null) {
            Config = config;
        }

        if (theme != null) {
            Theme = theme;
        }

        if (sprites != null) {
            Sprites = sprites;
        }

        if (fonts != null) {
            Fonts = fonts;
        }

        if (tween != null) {
            Tween = tween;
        }
    }
}
