// SPDX-License-Identifier: LGPL-3.0-or-later

using O5Kit.Core;

namespace O5Kit.Tests;

/// <summary>O5Config defaults. Enum-typed ToggleKey only needs the enum type loaded.</summary>
internal static class ConfigTests {
    public static void Run() {
        var c = new O5Config();
        T.Approx(1f, c.UIScale, "config UIScale");
        T.Check(c.TooltipEnabled, "config TooltipEnabled");
        T.Check(c.MiddleClickToDefault, "config MiddleClickToDefault");
        T.Approx(1d, c.SliderSensitivity, "config SliderSensitivity");
        T.Approx(1f, c.AnimationSpeed, "config AnimationSpeed");
        T.Eq(UnityEngine.KeyCode.BackQuote, c.ToggleKey, "config ToggleKey");

        // mutation sticks (consumers tune these per game)
        c.UIScale = 2f;
        T.Approx(2f, c.UIScale, "config UIScale set");
        c.MiddleClickToDefault = false;
        T.Check(!c.MiddleClickToDefault, "config MiddleClickToDefault set");
    }
}
