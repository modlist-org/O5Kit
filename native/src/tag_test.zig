const std = @import("std");
const tag = @import("tag.zig");

fn toUtf16(comptime str: []const u8) [str.len]u16 {
    var result: [str.len]u16 = undefined;
    for (str, 0..) |c, i| {
        result[i] = c;
    }
    return result;
}

test "plain text returns no tags" {
    const input = toUtf16("dropped the beat and died");
    var out: [16]tag.TagSpan = undefined;
    const count = tag.scan(&input, &out);
    try std.testing.expectEqual(@as(usize, 0), count);
}

test "simple tag" {
    const input = toUtf16("{Bpm}");
    var out: [16]tag.TagSpan = undefined;
    const count = tag.scan(&input, &out);
    try std.testing.expectEqual(@as(usize, 1), count);
    try std.testing.expectEqual(tag.TagType.simple, out[0].tag_type);
    try std.testing.expectEqual(@as(u32, 0), out[0].raw_start);
    try std.testing.expectEqual(@as(u32, 5), out[0].raw_len);
    try std.testing.expectEqual(@as(u32, 1), out[0].name_start);
    try std.testing.expectEqual(@as(u32, 3), out[0].name_len);
    try std.testing.expectEqual(@as(u32, 0), out[0].args_len);
}

test "colon tag" {
    const input = toUtf16("{Stream:200, 174}");
    var out: [16]tag.TagSpan = undefined;
    const count = tag.scan(&input, &out);
    try std.testing.expectEqual(@as(usize, 1), count);
    try std.testing.expectEqual(tag.TagType.colon, out[0].tag_type);
    try std.testing.expectEqual(@as(u32, 0), out[0].raw_start);
    try std.testing.expectEqual(@as(u32, 17), out[0].raw_len);
    try std.testing.expectEqual(@as(u32, 1), out[0].name_start);
    try std.testing.expectEqual(@as(u32, 6), out[0].name_len);
    try std.testing.expectEqual(@as(u32, 8), out[0].args_start);
    try std.testing.expectEqual(@as(u32, 8), out[0].args_len); // "200, 174"
}

test "func tag" {
    const input = toUtf16("{Stream(200,174)}");
    var out: [16]tag.TagSpan = undefined;
    const count = tag.scan(&input, &out);
    try std.testing.expectEqual(@as(usize, 1), count);
    try std.testing.expectEqual(tag.TagType.func, out[0].tag_type);
    try std.testing.expectEqual(@as(u32, 0), out[0].raw_start);
    try std.testing.expectEqual(@as(u32, 17), out[0].raw_len);
    try std.testing.expectEqual(@as(u32, 1), out[0].name_start);
    try std.testing.expectEqual(@as(u32, 6), out[0].name_len);
    try std.testing.expectEqual(@as(u32, 8), out[0].args_start);
    try std.testing.expectEqual(@as(u32, 7), out[0].args_len); // "200,174"
}

test "jsexpr balanced braces" {
    const input = toUtf16("{JSExpr:{gauge: 100}}");
    var out: [16]tag.TagSpan = undefined;
    const count = tag.scan(&input, &out);
    try std.testing.expectEqual(@as(usize, 1), count);
    try std.testing.expectEqual(tag.TagType.js_expr, out[0].tag_type);
    try std.testing.expectEqual(@as(u32, 0), out[0].raw_start);
    try std.testing.expectEqual(@as(u32, 21), out[0].raw_len);
    try std.testing.expectEqual(@as(u32, 1), out[0].name_start);
    try std.testing.expectEqual(@as(u32, 6), out[0].name_len);
    try std.testing.expectEqual(@as(u32, 8), out[0].args_start);
    try std.testing.expectEqual(@as(u32, 12), out[0].args_len); // "{gauge: 100}"
}

test "multiple tags" {
    const input = toUtf16("{FullCombo} nice {Miss:fail}");
    var out: [16]tag.TagSpan = undefined;
    const count = tag.scan(&input, &out);
    try std.testing.expectEqual(@as(usize, 2), count);
    try std.testing.expectEqual(@as(u32, 0), out[0].raw_start);
    try std.testing.expectEqual(@as(u32, 11), out[0].raw_len);
    try std.testing.expectEqual(@as(u32, 17), out[1].raw_start);
    try std.testing.expectEqual(@as(u32, 11), out[1].raw_len);
}
