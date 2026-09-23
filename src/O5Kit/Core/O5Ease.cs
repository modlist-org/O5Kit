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
}
