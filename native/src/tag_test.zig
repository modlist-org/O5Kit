const std = @import("std");
const tag = @import("tag.zig");

fn toUtf16(comptime str: []const u8) [str.len]u16 {
    var result: [str.len]u16 = undefined;
    for (str, 0..) |c, i| {
        result[i] = c;
    }
    return result;
}

fn sliceUtf16(src: []const u16, start: u32, len: u32) []const u16 {
    return src[start .. start + len];
}

fn expectUtf16EqualsAscii(src: []const u16, expected: []const u8) !void {
    try std.testing.expectEqual(expected.len, src.len);
    for (src, 0..) |c, i| {
        try std.testing.expectEqual(@as(u16, expected[i]), c);
    }
}

test "plain text returns no tags" {
    const input = toUtf16("dropped the beat and died");
    var out: [16]tag.TagSpan = undefined;
    const count = tag.scan(&input, &out);
    try std.testing.expectEqual(@as(usize, 0), count);
}

test "simple tag {Bpm}" {
    const input = toUtf16("{Bpm}");
    var out: [16]tag.TagSpan = undefined;
    const count = tag.scan(&input, &out);
    try std.testing.expectEqual(@as(usize, 1), count);
    try std.testing.expectEqual(@as(u32, 0), out[0].raw_start);
    try std.testing.expectEqual(@as(u32, 5), out[0].raw_len);
    try expectUtf16EqualsAscii(sliceUtf16(&input, out[0].expr_start, out[0].expr_len), "Bpm");
    try std.testing.expectEqual(@as(u32, 0), out[0].fmt_len);
}

test "formatted tag {CurHit:F2}" {
    const input = toUtf16("{CurHit:F2}");
    var out: [16]tag.TagSpan = undefined;
    const count = tag.scan(&input, &out);
    try std.testing.expectEqual(@as(usize, 1), count);
    try expectUtf16EqualsAscii(sliceUtf16(&input, out[0].expr_start, out[0].expr_len), "CurHit");
    try expectUtf16EqualsAscii(sliceUtf16(&input, out[0].fmt_start, out[0].fmt_len), "F2");
}

test "expression tag {Bpm * 1.5:F1}" {
    const input = toUtf16("{Bpm * 1.5:F1}");
    var out: [16]tag.TagSpan = undefined;
    const count = tag.scan(&input, &out);
    try std.testing.expectEqual(@as(usize, 1), count);
    try expectUtf16EqualsAscii(sliceUtf16(&input, out[0].expr_start, out[0].expr_len), "Bpm * 1.5");
    try expectUtf16EqualsAscii(sliceUtf16(&input, out[0].fmt_start, out[0].fmt_len), "F1");
}

test "ternary operator with parens {(Combo > 100 ? 2 : 1):F0}" {
    const input = toUtf16("{(Combo > 100 ? 2 : 1):F0}");
    var out: [16]tag.TagSpan = undefined;
    const count = tag.scan(&input, &out);
    try std.testing.expectEqual(@as(usize, 1), count);
    try expectUtf16EqualsAscii(sliceUtf16(&input, out[0].expr_start, out[0].expr_len), "(Combo > 100 ? 2 : 1)");
    try expectUtf16EqualsAscii(sliceUtf16(&input, out[0].fmt_start, out[0].fmt_len), "F0");
}

test "time format preserves colons {Time:HH:mm:ss}" {
    const input = toUtf16("{Time:HH:mm:ss}");
    var out: [16]tag.TagSpan = undefined;
    const count = tag.scan(&input, &out);
    try std.testing.expectEqual(@as(usize, 1), count);
    try expectUtf16EqualsAscii(sliceUtf16(&input, out[0].expr_start, out[0].expr_len), "Time");
    try expectUtf16EqualsAscii(sliceUtf16(&input, out[0].fmt_start, out[0].fmt_len), "HH:mm:ss");
}

test "arithmetic expression {2 + 3 * 4}" {
    const input = toUtf16("{2 + 3 * 4}");
    var out: [16]tag.TagSpan = undefined;
    const count = tag.scan(&input, &out);
    try std.testing.expectEqual(@as(usize, 1), count);
    try expectUtf16EqualsAscii(sliceUtf16(&input, out[0].expr_start, out[0].expr_len), "2 + 3 * 4");
    try std.testing.expectEqual(@as(u32, 0), out[0].fmt_len);
}

test "multiple tags" {
    const input = toUtf16("{FullCombo} nice {Miss:fail}");
    var out: [16]tag.TagSpan = undefined;
    const count = tag.scan(&input, &out);
    try std.testing.expectEqual(@as(usize, 2), count);
    try expectUtf16EqualsAscii(sliceUtf16(&input, out[0].expr_start, out[0].expr_len), "FullCombo");
    try expectUtf16EqualsAscii(sliceUtf16(&input, out[1].expr_start, out[1].expr_len), "Miss");
    try expectUtf16EqualsAscii(sliceUtf16(&input, out[1].fmt_start, out[1].fmt_len), "fail");
}
