const std = @import("std");
const ring = @import("ring.zig");

test "push len first last" {
    const h = ring.ringCreate(4);
    try std.testing.expect(h != 0);
    defer ring.ringDestroy(h);
    try std.testing.expectEqual(@as(usize, 0), ring.ringLen(h));
    ring.ringPush(h, 1.0);
    ring.ringPush(h, 2.0);
    ring.ringPush(h, 3.0);
    try std.testing.expectEqual(@as(usize, 3), ring.ringLen(h));
    try std.testing.expectEqual(1.0, ring.ringFirst(h));
    try std.testing.expectEqual(3.0, ring.ringLast(h));
}

test "overflow drops oldest" {
    const h = ring.ringCreate(3);
    try std.testing.expect(h != 0);
    defer ring.ringDestroy(h);
    ring.ringPush(h, 1.0);
    ring.ringPush(h, 2.0);
    ring.ringPush(h, 3.0);
    ring.ringPush(h, 4.0);
    ring.ringPush(h, 5.0);
    try std.testing.expectEqual(@as(usize, 3), ring.ringLen(h));
    try std.testing.expectEqual(3.0, ring.ringFirst(h));
    try std.testing.expectEqual(5.0, ring.ringLast(h));
}

test "prune window" {
    const h = ring.ringCreate(8);
    try std.testing.expect(h != 0);
    defer ring.ringDestroy(h);
    ring.ringPush(h, 90.0);
    ring.ringPush(h, 95.0);
    ring.ringPush(h, 99.0);
    ring.ringPush(h, 100.0);
    ring.ringPrune(h, 100.0, 10.0);
    try std.testing.expectEqual(@as(usize, 4), ring.ringLen(h));
    ring.ringPrune(h, 100.0, 5.0);
    try std.testing.expectEqual(@as(usize, 3), ring.ringLen(h));
    try std.testing.expectEqual(95.0, ring.ringFirst(h));
    ring.ringPrune(h, 200.0, 10.0);
    try std.testing.expectEqual(@as(usize, 0), ring.ringLen(h));
}

test "prune boundary is strict" {
    const h = ring.ringCreate(4);
    try std.testing.expect(h != 0);
    defer ring.ringDestroy(h);
    ring.ringPush(h, 90.0);
    ring.ringPrune(h, 100.0, 10.0);
    try std.testing.expectEqual(@as(usize, 1), ring.ringLen(h));
}

test "decay sum skips future" {
    const h = ring.ringCreate(8);
    try std.testing.expect(h != 0);
    defer ring.ringDestroy(h);
    ring.ringPush(h, 99.0);
    ring.ringPush(h, 100.0);
    ring.ringPush(h, 101.0);
    const got = ring.ringDecaySum(h, 100.0);
    const want = std.math.exp(-1.0) + 1.0;
    try std.testing.expectApproxEqAbs(want, got, 1e-12);
}

test "decay sum empty" {
    const h = ring.ringCreate(4);
    try std.testing.expect(h != 0);
    defer ring.ringDestroy(h);
    try std.testing.expectEqual(0.0, ring.ringDecaySum(h, 100.0));
}

test "empty first last nan" {
    const h = ring.ringCreate(4);
    try std.testing.expect(h != 0);
    defer ring.ringDestroy(h);
    try std.testing.expect(std.math.isNan(ring.ringFirst(h)));
    try std.testing.expect(std.math.isNan(ring.ringLast(h)));
}

test "bad handles" {
    try std.testing.expectEqual(@as(usize, 0), ring.ringLen(0));
    try std.testing.expectEqual(0.0, ring.ringDecaySum(-5, 1.0));
    ring.ringPush(0, 1.0);
    ring.ringPrune(0, 1.0, 1.0);
    ring.ringDestroy(0);
    try std.testing.expectEqual(@as(i64, 0), ring.ringCreate(0));
}

test "wraparound order" {
    const h = ring.ringCreate(4);
    try std.testing.expect(h != 0);
    defer ring.ringDestroy(h);
    var t: f64 = 0.0;
    while (t < 10.0) : (t += 1.0) {
        ring.ringPush(h, t);
    }
    try std.testing.expectEqual(@as(usize, 4), ring.ringLen(h));
    try std.testing.expectEqual(6.0, ring.ringFirst(h));
    try std.testing.expectEqual(9.0, ring.ringLast(h));
    ring.ringPrune(h, 9.0, 2.0);
    try std.testing.expectEqual(@as(usize, 3), ring.ringLen(h));
    try std.testing.expectEqual(7.0, ring.ringFirst(h));
    const got = ring.ringDecaySum(h, 9.0);
    const want = std.math.exp(-2.0) + std.math.exp(-1.0) + 1.0;
    try std.testing.expectApproxEqAbs(want, got, 1e-12);
}
