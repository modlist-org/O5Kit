// SPDX-License-Identifier: LGPL-3.0-or-later

namespace O5Kit.Core;

/// <summary>
/// Math helper polyfills for Mono / legacy .NET runtimes lacking .NET Standard 2.1 methods.
/// </summary>
public static class MathCompat {
    public static int Clamp(int value, int min, int max) => value < min ? min : (value > max ? max : value);
    public static float Clamp(float value, float min, float max) => value < min ? min : (value > max ? max : value);
    public static double Clamp(double value, double min, double max) => value < min ? min : (value > max ? max : value);
    public static long Clamp(long value, long min, long max) => value < min ? min : (value > max ? max : value);
}
