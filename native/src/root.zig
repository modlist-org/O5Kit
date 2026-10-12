const expr = @import("expr.zig");
const ring = @import("ring.zig");

export fn o5_expr_compile(src: [*]const u8, len: usize) i64 {
    return expr.exprCompile(src[0..len]);
}

export fn o5_expr_eval(h: i64) f64 {
    return expr.exprEval(h);
}

export fn o5_expr_free(h: i64) void {
    expr.exprFree(h);
}

export fn o5_ring_create(cap: usize) i64 {
    return ring.ringCreate(cap);
}

export fn o5_ring_destroy(h: i64) void {
    ring.ringDestroy(h);
}

export fn o5_ring_push(h: i64, t: f64) void {
    ring.ringPush(h, t);
}

export fn o5_ring_prune(h: i64, now: f64, window: f64) void {
    ring.ringPrune(h, now, window);
}

export fn o5_ring_decay_sum(h: i64, now: f64) f64 {
    return ring.ringDecaySum(h, now);
}

export fn o5_ring_first(h: i64) f64 {
    return ring.ringFirst(h);
}

export fn o5_ring_last(h: i64) f64 {
    return ring.ringLast(h);
}

export fn o5_ring_len(h: i64) usize {
    return ring.ringLen(h);
}
