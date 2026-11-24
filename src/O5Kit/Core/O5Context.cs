// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Collections.Generic;

namespace O5Kit.Core;

/// <summary>Owned O5Kit instance. Create one per consumer (game/mod) and pass it to every factory: no shared static state, so multiple consumers in one process cannot clobber each other's sprites, fonts, theme or config.</summary>
public sealed class O5Context : IDisposable {
    /// <summary>Runtime options.</summary>
    public O5Config Config { get; }

    /// <summary>Active visual theme. Change via <see cref="SetTheme"/>.</summary>
    public O5Theme Theme { get; private set; }

    /// <summary>Sprite slicing style handed to the default sprite provider.</summary>
    public O5SpriteStyle SpriteStyle { get; }

    /// <summary>Scale-aware rounded shape sprites. Feed <see cref="O5ShapeSprites.Tick"/> the canvas scale each frame and <see cref="O5ShapeSprites.Track"/> the canvas root.</summary>
    public O5ShapeSprites Shapes { get; }

    /// <summary>Active sprite source.</summary>
    public ISpriteProvider Sprites { get; }

    /// <summary>Active font source.</summary>
    public IFontProvider Fonts { get; }

    /// <summary>Active tween backend. Defaults to the automatic chain (LitMotion, game DOTween, PrimeTween, instant).</summary>
    public ITweenRunner Tween { get; }

    /// <summary>Floating tooltip bound to this context.</summary>
    public O5Tooltip Tooltip { get; }

    /// <summary>Fired after <see cref="SetTheme"/>.</summary>
    public event Action<O5Theme>? ThemeChanged;

    /// <summary>Fired after <see cref="SetTheme"/> with both the previous and new themes.</summary>
    public event Action<O5Theme, O5Theme>? ThemeTransitioned;

    /// <summary>Fired by <see cref="NotifyEnabledChanged"/>; drives <see cref="O5Object.EnabledWhen"/> gates.</summary>
    public event Action<bool>? EnabledChanged;

    private readonly List<IDisposable> _owned = new();
    private bool _disposed;

    /// <summary>Creates an owned instance. Null arguments fall back to defaults: fresh config, dark theme, Overlayer slicing style, bundled sprites/fonts, automatic tweens.</summary>
    /// <param name="config">Runtime options. Null creates a fresh one (the shared default is never used).</param>
    /// <param name="theme">Visual theme. Null uses <see cref="O5Theme.Dark"/>.</param>
    /// <param name="spriteStyle">Slicing style for the default sprite provider. Null uses <see cref="O5SpriteStyle.Default"/> (Overlayer values).</param>
    /// <param name="sprites">Sprite source. Null creates a <see cref="DefaultSpriteProvider"/> owned by this context.</param>
    /// <param name="fonts">Font source. Null creates a <see cref="DefaultFontProvider"/> owned by this context.</param>
    /// <param name="shapes">Shape rasterizer shared with the sprite provider. Null creates one owned by this context.</param>
    /// <param name="tween">Tween backend. Null uses the automatic chain (LitMotion, game DOTween, PrimeTween, instant).</param>
    public O5Context(
        O5Config? config = null,
        O5Theme? theme = null,
        O5SpriteStyle? spriteStyle = null,
        ISpriteProvider? sprites = null,
        IFontProvider? fonts = null,
        ITweenRunner? tween = null,
        O5ShapeSprites? shapes = null) {
        Config = config ?? new O5Config();
        Theme = theme ?? O5Theme.Dark;
        SpriteStyle = spriteStyle ?? O5SpriteStyle.Default;
        if (shapes != null) {
            Shapes = shapes;
        } else {
            Shapes = new O5ShapeSprites();
            _owned.Add(Shapes);
        }

        if (sprites != null) {
            Sprites = sprites;
        } else {
            var owned = new DefaultSpriteProvider(SpriteStyle, Shapes);
            _owned.Add(owned);
            Sprites = owned;
        }

        if (fonts != null) {
            Fonts = fonts;
        } else {
            var owned = new DefaultFontProvider();
            _owned.Add(owned);
            Fonts = owned;
        }

        Tween = new ScaledTweenRunner(tween ?? AutoTweenRunner.Instance, Config);
        Tooltip = new O5Tooltip(this);
    }

    /// <summary>Swaps the active theme. Live controls update via <see cref="ThemeTransitioned"/> and <see cref="ThemeChanged"/>.</summary>
    /// <param name="theme">New theme.</param>
    public void SetTheme(O5Theme theme) {
        var previous = Theme;
        Theme = theme;
        ThemeTransitioned?.Invoke(previous, theme);
        ThemeChanged?.Invoke(theme);
    }

    /// <summary>Broadcasts an enabled-state change to this context's gated controls.</summary>
    /// <param name="enabled">New global state.</param>
    public void NotifyEnabledChanged(bool enabled) => EnabledChanged?.Invoke(enabled);

    /// <summary>Disposes the tooltip and context-owned sprite/font providers.</summary>
    public void Dispose() {
        if (_disposed) {
            return;
        }

        _disposed = true;
        Tooltip.Dispose();
        foreach (var owned in _owned) {
            owned.Dispose();
        }

        _owned.Clear();
    }
}
