const std = @import("std");
const expr = @import("src/expr.zig");

test "dbg" {
    for ([_][]const u8{ "Max(2.5,(1^3))", "Min(2.5,(1^3))", "Max(2.5,2)", "+5", "2*+3" }) |s| {
        const h = expr.exprCompile(s);
        if (h == 0) {
            std.debug.print("{s} => DECLINED\n", .{s});
        } else {
            defer expr.exprFree(h);
            std.debug.print("{s} => {}\n", .{ s, expr.exprEval(h) });
        }
    }
}
