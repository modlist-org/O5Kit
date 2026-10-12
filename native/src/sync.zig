const std = @import("std");

pub const SpinLock = struct {
    flag: std.atomic.Value(u8) = .{ .raw = 0 },

    pub fn lock(s: *SpinLock) void {
        while (s.flag.cmpxchgWeak(0, 1, .acquire, .monotonic) != null) {
            std.atomic.spinLoopHint();
        }
    }

    pub fn unlock(s: *SpinLock) void {
        s.flag.store(0, .release);
    }
};
