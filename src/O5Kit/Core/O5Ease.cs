// SPDX-License-Identifier: LGPL-3.0-or-later

namespace O5Kit.Core;

/// <summary>Easing curves for <see cref="ITweenRunner"/>. Mirrors the LitMotion set O5Kit ports rely on.</summary>
public enum O5Ease {
    /// <summary>No easing.</summary>
    Linear,
    /// <summary>Slow start.</summary>
    InSine,
    /// <summary>Calm stop. Default for UI micro-animation.</summary>
    OutSine,
    /// <summary>Slow both ends.</summary>
    InOutSine,
    /// <summary>Accelerating.</summary>
    InQuad,
    /// <summary>Snappy stop.</summary>
    OutQuad,
    /// <summary>Accelerate then snap.</summary>
    InOutQuad,
    /// <summary>Strong acceleration.</summary>
    InCubic,
    /// <summary>Strong stop.</summary>
    OutCubic,
    /// <summary>Strong both ends.</summary>
    InOutCubic,
    /// <summary>Very fast start, sudden settle. Fill bars and page slides.</summary>
    OutExpo,
    /// <summary>Circular stop.</summary>
    OutCirc,
    /// <summary>Overshooting stop. Foldouts and popups.</summary>
    OutBack,
    // --- Restored GTween set (compat: overlay texts predate the O5Ease trim) ---
    /// <summary>Quartic acceleration.</summary>
    InQuart,
    /// <summary>Quartic stop.</summary>
    OutQuart,
    /// <summary>Quartic both ends.</summary>
    InOutQuart,
    /// <summary>Quintic acceleration.</summary>
    InQuint,
    /// <summary>Quintic stop.</summary>
    OutQuint,
    /// <summary>Quintic both ends.</summary>
    InOutQuint,
    /// <summary>Exponential start.</summary>
    InExpo,
    /// <summary>Exponential both ends.</summary>
    InOutExpo,
    /// <summary>Circular start.</summary>
    InCirc,
    /// <summary>Circular both ends.</summary>
    InOutCirc,
    /// <summary>Anticipatory start.</summary>
    InBack,
    /// <summary>Overshoot both ends.</summary>
    InOutBack,
    /// <summary>Springy start.</summary>
    InElastic,
    /// <summary>Springy stop.</summary>
    OutElastic,
    /// <summary>Springy both ends.</summary>
    InOutElastic,
    /// <summary>Bouncing start.</summary>
    InBounce,
    /// <summary>Bouncing stop.</summary>
    OutBounce,
    /// <summary>Bouncing both ends.</summary>
    InOutBounce,
}
