const std = @import("std");
const math = std.math;
const sync = @import("sync.zig");

const Ring = struct {
    buf: []f64,
    head: usize,
    len: usize,
    cap: usize,
};

const max_slots: usize = 256;

var ring_mtx: sync.SpinLock = .{};
var ring_slots: [max_slots]?*Ring = std.mem.zeroes([max_slots]?*Ring);
var ring_free: [max_slots]u16 = undefined;
var ring_free_count: usize = 0;

fn ringGet(h: i64) ?*Ring {
    if (h <= 0 or h > max_slots) return null;
    ring_mtx.lock();
    defer ring_mtx.unlock();
    return ring_slots[@intCast(h - 1)];
}

pub fn ringCreate(cap: usize) i64 {
    if (cap == 0) return 0;
    const buf = std.heap.c_allocator.alloc(f64, cap) catch return 0;
    const r = std.heap.c_allocator.create(Ring) catch {
        std.heap.c_allocator.free(buf);
        return 0;
    };
    r.* = .{ .buf = buf, .head = 0, .len = 0, .cap = cap };
    ring_mtx.lock();
    defer ring_mtx.unlock();
    var idx: ?u16 = null;
    if (ring_free_count > 0) {
        ring_free_count -= 1;
        idx = ring_free[ring_free_count];
    } else {
        var k: usize = 0;
        while (k < max_slots) : (k += 1) {
            if (ring_slots[k] == null) {
                idx = @intCast(k);
                break;
            }
        }
    }
    if (idx) |v| {
        ring_slots[v] = r;
        return @as(i64, v) + 1;
    }
    std.heap.c_allocator.free(buf);
    std.heap.c_allocator.destroy(r);
    return 0;
}

pub fn ringDestroy(h: i64) void {
    if (h <= 0 or h > max_slots) return;
    ring_mtx.lock();
    defer ring_mtx.unlock();
    const idx: usize = @intCast(h - 1);
    if (ring_slots[idx]) |r| {
        std.heap.c_allocator.free(r.buf);
        std.heap.c_allocator.destroy(r);
        ring_slots[idx] = null;
        if (ring_free_count < max_slots) {
            ring_free[ring_free_count] = @intCast(idx);
            ring_free_count += 1;
        }
    }
}

pub fn ringPush(h: i64, t: f64) void {
    const r = ringGet(h) orelse return;
    if (r.len < r.cap) {
        r.buf[(r.head + r.len) % r.cap] = t;
        r.len += 1;
    } else {
        r.buf[r.head] = t;
        r.head = (r.head + 1) % r.cap;
    }
}

pub fn ringPrune(h: i64, now: f64, window: f64) void {
    const r = ringGet(h) orelse return;
    while (r.len > 0 and now - r.buf[r.head] > window) {
        r.head = (r.head + 1) % r.cap;
        r.len -= 1;
    }
}

pub fn ringDecaySum(h: i64, now: f64) f64 {
    const r = ringGet(h) orelse return 0.0;
    var s: f64 = 0.0;
    var k: usize = 0;
    while (k < r.len) : (k += 1) {
        const t = r.buf[(r.head + k) % r.cap];
        const age = now - t;
        if (age >= 0.0) s += math.exp(-age);
    }
    return s;
}

pub fn ringFirst(h: i64) f64 {
    const r = ringGet(h) orelse return math.nan(f64);
    if (r.len == 0) return math.nan(f64);
    return r.buf[r.head];
}

pub fn ringLast(h: i64) f64 {
    const r = ringGet(h) orelse return math.nan(f64);
    if (r.len == 0) return math.nan(f64);
    return r.buf[(r.head + r.len - 1) % r.cap];
}

pub fn ringLen(h: i64) usize {
    const r = ringGet(h) orelse return 0;
    return r.len;
}
