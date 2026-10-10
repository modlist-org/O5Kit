using System;
using System.Collections.Generic;
namespace GTweens.Extensions;

public static class MathExtensions {
    public const float Deg2Rad = 0.01745329f;
    public const float Rad2Deg = 57.29578f;

    public static float SafeDivide(float v1, float v2) {
        if(v2 == 0f) {
            return 0f;
        }

        return v1 / v2;
    }

    /// <summary>
    /// Loops the value t, so that it is never larger than length and never smaller than 0.
    /// </summary>
    public static float Repeat(float t, float length) => Clamp(t - ((float)Math.Floor(t / length) * length), 0.0f, length);

    // NOTE (O5Kit embed): Math.Clamp needs netstandard2.1+; this is its exact equivalent.
    private static float Clamp(float value, float min, float max) => value < min ? min : value > max ? max : value;
}