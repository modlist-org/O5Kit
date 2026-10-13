const std = @import("std");
const fmt = @import("fmt.zig");

fn testFormat(val: f64, pattern: []const u8, expected: []const u8) !void {
    var out_buf: [1024]u8 = undefined;
    const spec = fmt.parseFormat(u8, pattern);
    const len = fmt.formatF64(u8, val, spec, &out_buf);
    try std.testing.expectEqualStrings(expected, out_buf[0..len]);
}

test "Fixed point F" {
    try testFormat(1234.567, "F0", "1235");
    try testFormat(1234.567, "F2", "1234.57");
    try testFormat(-1234.567, "F2", "-1234.57");
    try testFormat(0.0, "F2", "0.00");
    try testFormat(0.999, "F2", "1.00");
}

test "Number N with grouping commas" {
    try testFormat(1234567.89, "N0", "1,234,568");
    try testFormat(1234567.89, "N2", "1,234,567.89");
    try testFormat(-1234.5, "N1", "-1,234.5");
    try testFormat(123.4, "N2", "123.40");
}

test "Decimal D padding" {
    try testFormat(42.0, "D4", "0042");
    try testFormat(-42.0, "D4", "-0042");
    try testFormat(5.0, "D1", "5");
}

test "Percent P" {
    try testFormat(0.856, "P1", "85.6%");
    try testFormat(0.856, "P2", "85.60%");
    try testFormat(1.0, "P0", "100%");
}

test "Hexadecimal X" {
    try testFormat(255.0, "X", "FF");
    try testFormat(255.0, "X4", "00FF");
    try testFormat(10.0, "x2", "0a");
}

test "Exponential E" {
    try testFormat(1234.5, "E2", "1.23E+003");
    try testFormat(-1234.5, "e2", "-1.23e+003");
}

test "General G" {
    try testFormat(12.34, "G", "12.34");
    try testFormat(12.0, "G", "12");
    try testFormat(0.0, "G", "0");
}

test "Custom formats 0, #, ., ,, %" {
    try testFormat(5.0, "0.00", "5.00");
    try testFormat(5.126, "0.00", "5.13");
    try testFormat(5.0, "0.##", "5");
    try testFormat(5.1, "0.##", "5.1");
    try testFormat(5.123, "0.##", "5.12");
    try testFormat(1234567.0, "#,##0", "1,234,567");
    try testFormat(0.0, "#,##0", "0");
    try testFormat(1234.5, "#,##0.00", "1,234.50");
    try testFormat(7.0, "000", "007");
    try testFormat(0.123, "0.0%", "12.3%");
}

test "UTF-16 C ABI test" {
    var out_u16: [64]u16 = undefined;
    const fmt_str = [_]u16{ 'F', '2' };
    const len = fmt.o5_fmt_f64(123.456, &fmt_str, fmt_str.len, &out_u16, out_u16.len);
    try std.testing.expectEqual(@as(usize, 6), len);

    var res_ascii: [6]u8 = undefined;
    for (0..len) |i| {
        res_ascii[i] = @intCast(out_u16[i]);
    }
    try std.testing.expectEqualStrings("123.46", &res_ascii);
}

// test for FUCKING OVERFLOW

test "Stress: Extreme floats (double.MaxValue, MinValue, etc.)" {
    var buf: [1024]u8 = undefined;

    const values = [_]f64{
        1.7976931348623157e308, // max f64
        -1.7976931348623157e308,
        1e25,
        -1e25,
        1e20,
        1e16,
        1e-308,
        0.0,
        -0.0,
        std.math.nan(f64),
        std.math.inf(f64),
        -std.math.inf(f64),
    };

    const patterns = [_][]const u8{
        "F0",                "F2",    "F15",        "F50", "F99",
        "N0",                "N2",    "N15",        "N99", "D1",
        "D10",               "D99",   "P0",         "P2",  "P99",
        "X",                 "X8",    "X99",        "x99", "G",
        "E2",                "E15",   "E50",        "e2",  "#,##0.00",
        "0.000000000000000", "0.##%", "0000000000",
    };

    for (values) |v| {
        for (patterns) |p| {
            const spec = fmt.parseFormat(u8, p);
            const len = fmt.formatF64(u8, v, spec, &buf);
            try std.testing.expect(len <= buf.len);
            // ascii only
            for (buf[0..len]) |c| {
                try std.testing.expect(c >= 32 and c <= 126);
            }
        }
    }
}

test "Stress: Tiny buffer bounds (Zero buffer overrun guaranteed)" {
    const v: f64 = -1234567.89;
    const spec = fmt.parseFormat(u8, "N2");

    const CanaryBox = struct {
        canary_before: u8 = 0xAA,
        buf: [20]u8 = undefined,
        canary_after: u8 = 0xBB,
    };

    var i: usize = 0;
    while (i <= 20) : (i += 1) {
        var box = CanaryBox{};
        @memset(&box.buf, 0x55);

        const slice = box.buf[0..i];
        const written = fmt.formatF64(u8, v, spec, slice);

        try std.testing.expect(written <= i);
        try std.testing.expectEqual(@as(u8, 0xAA), box.canary_before);
        try std.testing.expectEqual(@as(u8, 0xBB), box.canary_after);
        for (box.buf[written..i]) |b| {
            try std.testing.expectEqual(@as(u8, 0x55), b);
        }
    }
}
