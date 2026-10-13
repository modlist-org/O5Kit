const expr = @import("expr.zig");
const fmt = @import("fmt.zig");
const ring = @import("ring.zig");
const tag = @import("tag.zig");

export fn o5_fmt_f64(
    val: f64,
    fmt_ptr: [*]const u16,
    fmt_len: usize,
    out_ptr: [*]u16,
    max_out: usize,
) usize {
    return fmt.o5_fmt_f64(val, fmt_ptr, fmt_len, out_ptr, max_out);
}

export fn o5_fmt_f64_utf8(
    val: f64,
    fmt_ptr: [*]const u8,
    fmt_len: usize,
    out_ptr: [*]u8,
    max_out: usize,
) usize {
    return fmt.o5_fmt_f64_utf8(val, fmt_ptr, fmt_len, out_ptr, max_out);
}

export fn o5_tag_scan(
    src: [*]const u16,
    len: usize,
    out_tags: [*]tag.TagSpan,
    max_tags: usize,
) usize {
    return tag.scan(src[0..len], out_tags[0..max_tags]);
}

export fn o5_expr_compile(src: [*]const u8, len: usize) i64 {
    return expr.exprCompile(src[0..len]);
}

export fn o5_expr_eval(h: i64) f64 {
    return expr.exprEval(h);
}

export fn o5_expr_eval_t(h: i64, t: f64) f64 {
    return expr.exprEvalT(h, t);
}

export fn o5_expr_eval_vars(h: i64, t: f64, vars: ?[*]const f64, var_count: usize) f64 {
    const v_slice = if (vars) |ptr| ptr[0..var_count] else &[_]f64{};
    return expr.exprEvalVars(h, t, v_slice);
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
