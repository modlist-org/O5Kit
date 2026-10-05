// SPDX-License-Identifier: LGPL-3.0-or-later

namespace O5Kit.Core;

/// <summary>UI-wide runtime options. Owned by an <see cref="O5Context"/>; pass yours to its constructor.</summary>
public sealed class O5Config {
    /// <summary>Global UI scale multiplier. Applied to canvas scaling and tooltip offsets.</summary>
    public float UIScale { get; set; } = 1f;

    /// <summary>Whether <see cref="O5Tooltip"/> may show at all.</summary>
    public bool TooltipEnabled { get; set; } = true;

    /// <summary>Whether middle-click resets a control to its default value.</summary>
    public bool MiddleClickToDefault { get; set; } = true;

    /// <summary>Horizontal drag distance multiplier for sliders.</summary>
    public double SliderSensitivity { get; set; } = 1d;

    /// <summary>Global animation speed multiplier (KDE-style). 1 is normal, higher is faster,
    /// 0 or below collapses every duration to near-instant. Applied by the per-context
    /// tween decorator, so it covers the built-in runners and custom <see cref="ITweenRunner"/> implementations alike. Completions still fire asynchronously.</summary>
    public float AnimationSpeed { get; set; } = 1f;

    /// <summary>Toggle hotkey. Default matches Overlayer: Alt+BackQuote (Ctrl on Linux).</summary>
    public UnityEngine.KeyCode ToggleKey { get; set; } = UnityEngine.KeyCode.BackQuote;

    /// <summary>Shared default instance. Prefer passing your own to the <see cref="O5Context"/> constructor.</summary>
    public static O5Config Default { get; } = new();
}
