// SPDX-License-Identifier: LGPL-3.0-or-later

using System;

namespace O5Kit.Control;

/// <summary>Maps a slider value to 0..1 and back. Replaces the old Normalize/Denormalize func pair (which could desync).</summary>
public interface IValueScale {
    /// <summary>Maps (min, value, max) to 0..1 for the fill bar and previews.</summary>
    double Normalize(double min, double value, double max);

    /// <summary>Inverse mapping for <see cref="O5Slider.SetNormalized"/>.</summary>
    double Denormalize(double min, double t, double max);
}

/// <summary>Built-in <see cref="IValueScale"/> implementations.</summary>
public sealed class O5ValueScale : IValueScale {
    private readonly Func<double, double, double, double> _normalize;
    private readonly Func<double, double, double, double> _denormalize;

    private O5ValueScale(Func<double, double, double, double> normalize, Func<double, double, double, double> denormalize) {
        _normalize = normalize;
        _denormalize = denormalize;
    }

    /// <inheritdoc/>
    public double Normalize(double min, double value, double max) => _normalize(min, value, max);

    /// <inheritdoc/>
    public double Denormalize(double min, double t, double max) => _denormalize(min, t, max);

    /// <summary>Linear mapping. Default when <see cref="O5SliderOptions.Scale"/> is null.</summary>
    public static IValueScale Linear { get; } = new O5ValueScale(LinearNormalize, LinearDenormalize);

    /// <summary>Logarithmic mapping. Requires min &gt; 0; falls back to linear otherwise.</summary>
    public static IValueScale Log { get; } = new O5ValueScale(LogNormalize, LogDenormalize);

    /// <summary>Default linear (min, value, max) to 0..1 mapping.</summary>
    public static double LinearNormalize(double min, double value, double max) {
        double span = max - min;
        if (span == 0d) {
            return 0d;
        }

        return (value - min) / span;
    }

    /// <summary>Default linear (min, t, max) to value mapping.</summary>
    public static double LinearDenormalize(double min, double t, double max) => min + ((max - min) * t);

    /// <summary>Logarithmic (min, value, max) to 0..1 mapping. Falls back to linear when out of domain.</summary>
    public static double LogNormalize(double min, double value, double max) {
        if (min <= 0d || max <= 0d || value <= 0d || max == min) {
            return LinearNormalize(min, value, max);
        }

        return Math.Log(value / min) / Math.Log(max / min);
    }

    /// <summary>Inverse of <see cref="LogNormalize"/>. Falls back to linear when out of domain.</summary>
    public static double LogDenormalize(double min, double t, double max) {
        if (min <= 0d || max <= 0d || max == min) {
            return LinearDenormalize(min, t, max);
        }

        return min * Math.Pow(max / min, t);
    }
}
