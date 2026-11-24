// SPDX-License-Identifier: LGPL-3.0-or-later

using O5Kit.Control;

namespace O5Kit.Tests;

/// <summary>Options defaults and configure-lambda shape. Only options whose
/// construction never touches UnityEngine types are covered here; the rest
/// (ColorPicker/Button visuals) are exercised in-game.</summary>
internal static class OptionsTests {
    public static void Run() {
        var s = new O5SliderOptions();
        T.Eq(0d, s.Min, "slider Min");
        T.Eq(100d, s.Max, "slider Max");
        T.Eq(0d, s.Value, "slider Value");
        T.Eq(null, s.DefaultValue, "slider DefaultValue");
        T.Eq("F2", s.Format, "slider Format");
        T.Eq("", s.Label, "slider Label");
        T.Check(s.ClampDrag && s.ClampInput, "slider clamps on");
        T.Eq(null, s.InputMin, "slider InputMin");
        T.Eq(null, s.InputMax, "slider InputMax");
        T.Eq(null, s.DragFilter, "slider DragFilter");
        T.Eq(null, s.InputFilter, "slider InputFilter");
        T.Eq(null, s.Scale, "slider Scale");
        T.Eq(null, s.OnChanged, "slider OnChanged");
        T.Eq(null, s.OnComplete, "slider OnComplete");
        T.Check(s.ShowFill, "slider ShowFill");
        T.Eq(null, s.DragStep, "slider DragStep");
        T.Check(!s.BlockHoverWhileDragging, "slider BlockHover");

        var configured = new O5SliderOptions();
        Action<O5SliderOptions> cfg = o => {
            o.Min = -10;
            o.Max = 10;
            o.Value = 3;
            o.Label = "Vol";
        };
        cfg(configured);
        T.Eq(-10d, configured.Min, "slider configure Min");
        T.Eq(10d, configured.Max, "slider configure Max");
        T.Eq(3d, configured.Value, "slider configure Value");
        T.Eq("Vol", configured.Label, "slider configure Label");

        var t = new O5ToggleOptions();
        T.Eq(null, t.DefaultValue, "toggle DefaultValue");
        T.Check(!t.Value, "toggle Value");
        T.Eq("", t.Text, "toggle Text");
        T.Eq(null, t.OnChanged, "toggle OnChanged");

        var d = new O5DropdownOptions<string>();
        T.Eq(0, d.Values.Count, "dropdown Values empty");
        T.Eq(null, d.Value, "dropdown Value");
        T.Eq(null, d.DefaultValue, "dropdown DefaultValue");
        T.Eq("x", d.Display("x"), "dropdown Display");
        T.Eq(null, d.OnChanged, "dropdown OnChanged");
        T.Check(d.ShowIndexIndicator, "dropdown ShowIndexIndicator default");
        var di = new O5DropdownOptions<int>();
        T.Eq("5", di.Display(5), "dropdown int Display");

        var m = new O5MultiDropdownOptions<DayOfWeek>();
        T.Eq(0, m.Values.Count, "multidropdown Values empty");
        T.Eq("Monday", m.Display(DayOfWeek.Monday), "multidropdown Display");
        T.Eq(DayOfWeek.Monday.ToString(), m.Summary(DayOfWeek.Monday), "multidropdown Summary");

        var inp = new O5InputOptions();
        T.Eq(null, inp.DefaultValue, "input DefaultValue");
        T.Eq(null, inp.Value, "input Value");
        T.Eq("", inp.Placeholder, "input Placeholder");
        // NB: inp.Icon (O5Icon) is intentionally not touched: reading it would
        // JIT a UnityEngine/TMPro type, which is unavailable outside the game.
        T.Check(!inp.Multiline && !inp.Monospace, "input flags");

        // NB: T? with unconstrained T is NOT Nullable<T> for value types:
        // Segmented<int>.Value defaults to 0 (same shape as O5Dropdown<T>).
        // Reference-type options preserve null.
        var seg = new O5SegmentedOptions<int>();
        T.Eq(0, seg.Values.Count, "segmented Values empty");
        T.Eq(0, seg.Value, "segmented int Value default");
        T.Eq(0, seg.DefaultValue, "segmented int DefaultValue default");
        T.Eq("5", seg.Display(5), "segmented Display");
        T.Eq(null, seg.Height, "segmented Height");
        T.Eq(null, seg.FontSize, "segmented FontSize");
        var segs = new O5SegmentedOptions<string>();
        T.Eq(null, segs.Value, "segmented string Value default");
        T.Eq(null, segs.DefaultValue, "segmented string DefaultValue default");

        var b = new O5ButtonOptions();
        T.Eq(null, b.Text, "button Text");
        // NB: b.Icon (O5Icon) not touched: UnityEngine/TMPro is unavailable off-game.
        T.Approx(5f, b.IconPadding, "button IconPadding");
        T.Eq(null, b.Height, "button Height");

        // Dropdown index indicator math
        T.Approx(0f, O5Dropdown.RequiredDotsWidth(0), "indicator width 0");
        T.Approx(78f, O5Dropdown.RequiredDotsWidth(6), "indicator width 6");
        T.Check(!O5Dropdown.ShouldUseBarMode(1, 50f), "indicator bar mode off for 1");
        T.Check(!O5Dropdown.ShouldUseBarMode(6, 78f), "indicator fits exactly at 78");
        T.Check(O5Dropdown.ShouldUseBarMode(6, 77.9f), "indicator bar mode when exceeding maxWidth");
        T.Check(!O5Dropdown.ShouldUseBarMode(20, 0f), "indicator defers bar mode before layout");
        T.Approx(0f, O5Dropdown.BarDotOffsetX(0, 10, 100f), "bar dot start");
        T.Approx(86f, O5Dropdown.BarDotOffsetX(9, 10, 100f), "bar dot end (100 - 14)");
        T.Approx(43f, O5Dropdown.BarDotOffsetX(1, 3, 100f), "bar dot midpoint");
    }
}
