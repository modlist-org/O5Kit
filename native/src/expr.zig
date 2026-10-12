// I think this is better then C#?????
const std = @import("std");
const math = std.math;
const sync = @import("sync.zig");

extern "c" fn sin(x: f64) f64;
extern "c" fn cos(x: f64) f64;
extern "c" fn tan(x: f64) f64;
extern "c" fn asin(x: f64) f64;
extern "c" fn acos(x: f64) f64;
extern "c" fn atan(x: f64) f64;
extern "c" fn atan2(y: f64, x: f64) f64;
extern "c" fn exp(x: f64) f64;
extern "c" fn log(x: f64) f64;
extern "c" fn log10(x: f64) f64;
extern "c" fn pow(x: f64, y: f64) f64;

const Value = union(enum) {
    i: i64,
    f: f64,
    b: bool,
};

// stack machine bytecode, reads like asm
const Tag = enum(u8) {
    push_i,
    push_f,
    push_b,
    add,
    sub,
    mul,
    div, // / (always f64)
    mod, // % (int %0 is compile failure)
    pow, // ** (right associative)
    neg,
    shl, // << (u64, count &63)
    shr, // >> (u64 logical)
    band, // & (bitwise and)
    bor, // | (bitwise or)
    bxor, // ^ (bitwise xor, not power)
    eq, // == and = (equal)
    ne, // != and <> (not equal)
    lt, // < (less than)
    le, // <= (less or equal)
    gt, // > (greater than)
    ge, // >= (greater or equal)
    land, // && and (logical and)
    lor, // || or (logical or)
    lnot, // ! not (bool operand only)
    select, // ?: (no jumps, both branches evaluated)
    abs, // (always f64)
    sign, // (always int)
    floor,
    ceil,
    trunc,
    sqrt,
    exp,
    ln, // (x > 0 required)
    log10, // (x > 0 required)
    log2, // (log(x, base))
    sin,
    cos,
    tan,
    asin,
    acos,
    atan,
    atan2, // (y, x)
    round2, // (round(x, digits), bankers, digits 0..15)
    max, // (2 args, int preserved)
    min, // (2 args, int preserved)
    powf, // Pow()
    irem, // (IEEE remainder)
};

const Op = struct {
    tag: Tag,
    arg: i64,
};

const Prog = struct {
    ops: []Op,
};

const EvalError = error{
    StackUnderflow, // pop on empty stack, unreachable, trial eval rejects it
    StackOverflow, // value stack over 64 slots, decline
    OutOfOps, // over 4096 ops, decline
    DepthLimit, // parser recursion over 128, decline
    CoerceFail, // value not representable here, compile failure
    BadOp, // stack not exactly 1 value at end
};

const max_ops: usize = 4096;
const max_stack: usize = 64;
const max_depth: usize = 128;
const max_slots: usize = 1024;
const tie_half: f64 = 0.5;
const decimal_base: f64 = 10.0;
const max_round_digits: i64 = 15;
const shift_mask: u64 = 63;
const i64_max_f: f64 = @floatFromInt(math.maxInt(i64));

fn bankersRound(x: f64) f64 {
    const t = math.trunc(x);
    const d = x - t;
    const ad = if (d < 0) -d else d;
    if (ad < tie_half) return t;
    if (ad > tie_half) return t + (if (x < 0) @as(f64, -1.0) else @as(f64, 1.0));
    return if (@mod(t, 2.0) == 0.0) t else t + (if (x < 0) @as(f64, -1.0) else @as(f64, 1.0));
}

fn toF(v: *const Value) EvalError!f64 {
    return switch (v.*) {
        .i => |x| @floatFromInt(x),
        .f => |x| x,
        .b => |x| if (x) 1.0 else 0.0,
    };
}

fn toBool(v: *const Value) bool {
    return switch (v.*) {
        .i => |x| x != 0,
        .f => |x| x != 0.0,
        .b => |x| x,
    };
}

fn bankersToI64(x: f64) EvalError!i64 {
    if (!std.math.isFinite(x)) return EvalError.CoerceFail;
    const r = bankersRound(x);
    if (r < -i64_max_f or r > i64_max_f) return EvalError.CoerceFail;
    return @intFromFloat(r);
}

fn isInt(v: *const Value) bool {
    return v.* == .i;
}

fn fcmp(tag: Tag, a: f64, b: f64) bool {
    return switch (tag) {
        .eq => a == b,
        .ne => a != b,
        .lt => a < b,
        .le => a <= b,
        .gt => a > b,
        else => a >= b,
    };
}

fn bankersToI32(x: f64) EvalError!i32 {
    const v = try bankersToI64(x);
    if (v < math.minInt(i32) or v > math.maxInt(i32)) return EvalError.CoerceFail;
    return @intCast(v);
}

fn toInt(v: *const Value) EvalError!i64 {
    return switch (v.*) {
        .i => |x| x,
        .f => |x| try bankersToI64(x),
        .b => |x| if (x) @as(i64, 1) else 0,
    };
}

fn run(ops: []const Op) EvalError!f64 {
    var st: [max_stack]Value = undefined;
    var sp: usize = 0;
    var i: usize = 0;
    while (i < ops.len) : (i += 1) {
        const op = ops[i];
        switch (op.tag) {
            .push_i => {
                if (sp >= max_stack) return EvalError.StackOverflow;
                st[sp] = .{ .i = op.arg };
                sp += 1;
            },
            .push_f => {
                if (sp >= max_stack) return EvalError.StackOverflow;
                st[sp] = .{ .f = @bitCast(op.arg) };
                sp += 1;
            },
            .push_b => {
                if (sp >= max_stack) return EvalError.StackOverflow;
                st[sp] = .{ .b = op.arg != 0 };
                sp += 1;
            },
            .neg => {
                if (sp < 1) return EvalError.StackUnderflow;
                const v = st[sp - 1];
                st[sp - 1] = switch (v) {
                    .i => |x| if (x == 0) Value{ .f = -0.0 } else Value{ .i = 0 -% x },
                    .f => |x| Value{ .f = -x },
                    .b => |x| Value{ .i = if (x) -1 else 0 },
                };
            },
            .add, .sub, .mul => {
                if (sp < 2) return EvalError.StackUnderflow;
                const r = st[sp - 1];
                const l = st[sp - 2];
                sp -= 1;
                if (l == .i and r == .i) {
                    const a = l.i;
                    const b = r.i;
                    st[sp - 1] = .{ .i = switch (op.tag) {
                        .add => a +% b,
                        .sub => a -% b,
                        else => a *% b,
                    } };
                } else {
                    const a = try toF(&l);
                    const b = try toF(&r);
                    st[sp - 1] = .{ .f = switch (op.tag) {
                        .add => a + b,
                        .sub => a - b,
                        else => a * b,
                    } };
                }
            },
            .div => {
                if (sp < 2) return EvalError.StackUnderflow;
                const r = st[sp - 1];
                const l = st[sp - 2];
                sp -= 1;
                const b = try toF(&r);
                const a = try toF(&l);
                st[sp - 1] = .{ .f = a / b };
            },
            .mod => {
                if (sp < 2) return EvalError.StackUnderflow;
                const r = st[sp - 1];
                const l = st[sp - 2];
                sp -= 1;
                if (l == .i and r == .i) {
                    if (r.i == 0) {
                        st[sp - 1] = .{ .f = math.nan(f64) };
                    } else {
                        st[sp - 1] = .{ .i = @rem(l.i, r.i) };
                    }
                } else {
                    const b = try toF(&r);
                    const a = try toF(&l);
                    st[sp - 1] = .{ .f = if (b == 0.0) math.nan(f64) else @rem(a, b) };
                }
            },
            .pow => {
                if (sp < 2) return EvalError.StackUnderflow;
                const b = try toF(&st[sp - 1]);
                const a = try toF(&st[sp - 2]);
                sp -= 1;
                st[sp - 1] = .{ .f = pow(a, b) };
            },
            .shl, .shr => {
                if (sp < 2) return EvalError.StackUnderflow;
                const r = st[sp - 1];
                const l = st[sp - 2];
                sp -= 1;
                const s = try toInt(&r);
                const v = try toInt(&l);
                const amt: u6 = @truncate(@as(u64, @bitCast(s)) & shift_mask);
                st[sp - 1] = .{ .i = if (op.tag == .shl) v << amt else v >> amt };
            },
            .band, .bor, .bxor => {
                if (sp < 2) return EvalError.StackUnderflow;
                const r = st[sp - 1];
                const l = st[sp - 2];
                sp -= 1;
                const b = try toInt(&r);
                const a = try toInt(&l);
                st[sp - 1] = .{ .i = switch (op.tag) {
                    .band => a & b,
                    .bor => a | b,
                    else => a ^ b,
                } };
            },
            .eq, .ne, .lt, .le, .gt, .ge => {
                if (sp < 2) return EvalError.StackUnderflow;
                var r = st[sp - 1];
                var l = st[sp - 2];
                sp -= 1;
                var out: bool = undefined;
                if (l == .i and r == .i) {
                    const a = l.i;
                    const b = r.i;
                    out = switch (op.tag) {
                        .eq => a == b,
                        .ne => a != b,
                        .lt => a < b,
                        .le => a <= b,
                        .gt => a > b,
                        else => a >= b,
                    };
                } else {
                    const a = try toF(&l);
                    const b = try toF(&r);
                    out = switch (op.tag) {
                        .eq => a == b,
                        .ne => a != b,
                        .lt => a < b,
                        .le => a <= b,
                        .gt => a > b,
                        else => a >= b,
                    };
                }
                st[sp - 1] = .{ .b = out };
            },
            .land, .lor => {
                if (sp < 2) return EvalError.StackUnderflow;
                const b = toBool(&st[sp - 1]);
                const a = toBool(&st[sp - 2]);
                sp -= 1;
                st[sp - 1] = .{ .b = if (op.tag == .land) a and b else a or b };
            },
            .lnot => {
                if (sp < 1) return EvalError.StackUnderflow;
                const v = st[sp - 1];
                st[sp - 1] = .{ .b = !toBool(&v) };
            },
            .select => {
                if (sp < 3) return EvalError.StackUnderflow;
                const b = st[sp - 1];
                const a = st[sp - 2];
                const c = st[sp - 3];
                sp -= 2;
                st[sp - 1] = if (toBool(&c)) a else b;
            },
            .abs => {
                if (sp < 1) return EvalError.StackUnderflow;
                const x = try toF(&st[sp - 1]);
                st[sp - 1] = .{ .f = @abs(x) };
            },
            .sign => {
                if (sp < 1) return EvalError.StackUnderflow;
                const v = st[sp - 1];
                const s: i64 = switch (v) {
                    .i => |x| if (x > 0) @as(i64, 1) else if (x < 0) @as(i64, -1) else 0,
                    .f => |x| if (x > 0.0) @as(i64, 1) else if (x < 0.0) @as(i64, -1) else 0,
                    .b => |x| if (x) @as(i64, 1) else 0,
                };
                st[sp - 1] = .{ .i = @truncate(s) };
            },
            .floor => {
                if (sp < 1) return EvalError.StackUnderflow;
                const x = try toF(&st[sp - 1]);
                st[sp - 1] = .{ .f = math.floor(x) };
            },
            .ceil => {
                if (sp < 1) return EvalError.StackUnderflow;
                const x = try toF(&st[sp - 1]);
                st[sp - 1] = .{ .f = math.ceil(x) };
            },
            .trunc => {
                if (sp < 1) return EvalError.StackUnderflow;
                const x = try toF(&st[sp - 1]);
                st[sp - 1] = .{ .f = math.trunc(x) };
            },
            .sqrt => {
                if (sp < 1) return EvalError.StackUnderflow;
                const x = try toF(&st[sp - 1]);
                st[sp - 1] = .{ .f = math.sqrt(x) };
            },
            .exp => {
                if (sp < 1) return EvalError.StackUnderflow;
                const x = try toF(&st[sp - 1]);
                st[sp - 1] = .{ .f = exp(x) };
            },
            .ln => {
                if (sp < 1) return EvalError.StackUnderflow;
                const x = try toF(&st[sp - 1]);
                st[sp - 1] = .{ .f = if (x <= 0.0) math.nan(f64) else log(x) };
            },
            .log10 => {
                if (sp < 1) return EvalError.StackUnderflow;
                const x = try toF(&st[sp - 1]);
                st[sp - 1] = .{ .f = if (x <= 0.0) math.nan(f64) else log10(x) };
            },
            .log2 => {
                if (sp < 2) return EvalError.StackUnderflow;
                const b = st[sp - 1];
                const a = st[sp - 2];
                sp -= 1;
                const base = try toF(&b);
                const x = try toF(&a);
                st[sp - 1] = .{ .f = if (base <= 0.0 or base == 1.0) math.nan(f64) else log(x) / log(base) };
            },
            .sin => {
                if (sp < 1) return EvalError.StackUnderflow;
                const x = try toF(&st[sp - 1]);
                st[sp - 1] = .{ .f = sin(x) };
            },
            .cos => {
                if (sp < 1) return EvalError.StackUnderflow;
                const x = try toF(&st[sp - 1]);
                st[sp - 1] = .{ .f = cos(x) };
            },
            .tan => {
                if (sp < 1) return EvalError.StackUnderflow;
                const x = try toF(&st[sp - 1]);
                st[sp - 1] = .{ .f = tan(x) };
            },
            .asin => {
                if (sp < 1) return EvalError.StackUnderflow;
                const x = try toF(&st[sp - 1]);
                st[sp - 1] = .{ .f = asin(x) };
            },
            .acos => {
                if (sp < 1) return EvalError.StackUnderflow;
                const x = try toF(&st[sp - 1]);
                st[sp - 1] = .{ .f = acos(x) };
            },
            .atan => {
                if (sp < 1) return EvalError.StackUnderflow;
                const x = try toF(&st[sp - 1]);
                st[sp - 1] = .{ .f = atan(x) };
            },
            .atan2 => {
                if (sp < 2) return EvalError.StackUnderflow;
                const x = try toF(&st[sp - 1]);
                const y = try toF(&st[sp - 2]);
                sp -= 1;
                st[sp - 1] = .{ .f = atan2(y, x) };
            },
            .round2 => {
                if (sp < 2) return EvalError.StackUnderflow;
                const dv = st[sp - 1];
                const x = try toF(&st[sp - 2]);
                sp -= 1;
                var digits: i64 = undefined;
                if (dv == .i) {
                    digits = dv.i;
                } else if (dv == .b) {
                    digits = if (dv.b) 1 else 0;
                } else if (dv == .f and dv.f == math.trunc(dv.f) and dv.f >= -@as(f64, @floatFromInt(max_round_digits)) and dv.f <= @as(f64, @floatFromInt(max_round_digits))) {
                    digits = @intFromFloat(dv.f);
                } else {
                    return EvalError.CoerceFail;
                }
                if (digits < 0 or digits > max_round_digits) return EvalError.CoerceFail;
                const m = pow(decimal_base, @floatFromInt(digits));
                st[sp - 1] = .{ .f = bankersRound(x * m) / m };
            },
            .max, .min => {
                if (sp < 2) return EvalError.StackUnderflow;
                const b = st[sp - 1];
                const a = st[sp - 2];
                sp -= 1;
                if (a == .i and b == .i) {
                    st[sp - 1] = .{ .i = if (op.tag == .max) @max(a.i, b.i) else @min(a.i, b.i) };
                } else {
                    const x = try toF(&a);
                    const y = try toF(&b);
                    if (std.math.isNan(x) or std.math.isNan(y)) {
                        st[sp - 1] = .{ .f = std.math.nan(f64) };
                    } else {
                        st[sp - 1] = .{ .f = if (op.tag == .max) @max(x, y) else @min(x, y) };
                    }
                }
            },
            .powf => {
                if (sp < 2) return EvalError.StackUnderflow;
                const e = try toF(&st[sp - 1]);
                const base = try toF(&st[sp - 2]);
                sp -= 1;
                st[sp - 1] = .{ .f = pow(base, e) };
            },
            .irem => {
                if (sp < 2) return EvalError.StackUnderflow;
                const d = try toF(&st[sp - 1]);
                const n = try toF(&st[sp - 2]);
                sp -= 1;
                st[sp - 1] = .{ .f = if (d == 0.0) math.nan(f64) else @rem(n, d) };
            },
        }
    }
    if (sp != 1) return EvalError.BadOp;
    const last = st[0];
    return switch (last) {
        .i => |x| @floatFromInt(x),
        .f => |x| x,
        .b => |x| if (x) 1.0 else 0.0,
    };
}

const Builtin = struct {
    name: []const u8,
    min_args: u8,
    max_args: u8,
    tag: Tag,
};

const builtins = [_]Builtin{
    .{ .name = "abs", .min_args = 1, .max_args = 1, .tag = .abs },
    .{ .name = "acos", .min_args = 1, .max_args = 1, .tag = .acos },
    .{ .name = "asin", .min_args = 1, .max_args = 1, .tag = .asin },
    .{ .name = "atan", .min_args = 1, .max_args = 1, .tag = .atan },
    .{ .name = "atan2", .min_args = 2, .max_args = 2, .tag = .atan2 },
    .{ .name = "ceil", .min_args = 1, .max_args = 1, .tag = .ceil },
    .{ .name = "ceiling", .min_args = 1, .max_args = 1, .tag = .ceil },
    .{ .name = "cos", .min_args = 1, .max_args = 1, .tag = .cos },
    .{ .name = "exp", .min_args = 1, .max_args = 1, .tag = .exp },
    .{ .name = "floor", .min_args = 1, .max_args = 1, .tag = .floor },
    .{ .name = "ieeeremainder", .min_args = 2, .max_args = 2, .tag = .irem },
    .{ .name = "ln", .min_args = 1, .max_args = 1, .tag = .ln },
    .{ .name = "log", .min_args = 1, .max_args = 2, .tag = .log2 },
    .{ .name = "log10", .min_args = 1, .max_args = 1, .tag = .log10 },
    .{ .name = "max", .min_args = 2, .max_args = 2, .tag = .max },
    .{ .name = "min", .min_args = 2, .max_args = 2, .tag = .min },
    .{ .name = "pow", .min_args = 2, .max_args = 2, .tag = .powf },
    .{ .name = "round", .min_args = 2, .max_args = 2, .tag = .round2 },
    .{ .name = "sign", .min_args = 1, .max_args = 1, .tag = .sign },
    .{ .name = "sin", .min_args = 1, .max_args = 1, .tag = .sin },
    .{ .name = "sqrt", .min_args = 1, .max_args = 1, .tag = .sqrt },
    .{ .name = "tan", .min_args = 1, .max_args = 1, .tag = .tan },
    .{ .name = "trunc", .min_args = 1, .max_args = 1, .tag = .trunc },
    .{ .name = "truncate", .min_args = 1, .max_args = 1, .tag = .trunc },
};

fn findBuiltin(name: []const u8) ?Builtin {
    for (builtins) |b| {
        if (std.ascii.eqlIgnoreCase(b.name, name)) return b;
    }
    return null;
}

const ParseError = error{
    Syntax, // malformed input, decline
    DepthLimit, // parser recursion over 128, decline
    OutOfOps, // over 4096 ops, decline
    UnknownName, // not PI/E/bool/builtin, decline
    BadArity, // wrong arg count, decline
};

const Parser = struct {
    src: []const u8,
    pos: usize,
    ops: []Op,
    count: usize,
    depth: usize,

    fn peek(p: *Parser) u8 {
        if (p.pos >= p.src.len) return 0;
        return p.src[p.pos];
    }

    fn skipWs(p: *Parser) void {
        while (p.pos < p.src.len) {
            const c = p.src[p.pos];
            if (c == ' ' or c == '\t' or c == '\r' or c == '\n') {
                p.pos += 1;
            } else break;
        }
    }

    fn emit(p: *Parser, tag: Tag, arg: i64) ParseError!void {
        if (p.count >= p.ops.len) return ParseError.OutOfOps;
        p.ops[p.count] = .{ .tag = tag, .arg = arg };
        p.count += 1;
    }

    fn eat(p: *Parser, s: []const u8) bool {
        if (p.pos + s.len > p.src.len) return false;
        if (!std.mem.eql(u8, p.src[p.pos .. p.pos + s.len], s)) return false;
        p.pos += s.len;
        return true;
    }

    fn eatIdent(p: *Parser, s: []const u8) bool {
        const save = p.pos;
        if (p.pos + s.len > p.src.len) return false;
        for (s, 0..) |c, k| {
            if (std.ascii.toLower(p.src[p.pos + k]) != std.ascii.toLower(c)) {
                p.pos = save;
                return false;
            }
        }
        const after = p.pos + s.len;
        if (after < p.src.len) {
            const c = p.src[after];
            if (std.ascii.isAlphabetic(c) or c == '_' or std.ascii.isDigit(c)) {
                p.pos = save;
                return false;
            }
        }
        p.pos += s.len;
        return true;
    }

    fn parseTernary(p: *Parser) ParseError!void {
        try p.parseOr();
        p.skipWs();
        if (p.peek() == '?') {
            p.pos += 1;
            try p.parseTernary();
            p.skipWs();
            if (p.peek() != ':') return ParseError.Syntax;
            p.pos += 1;
            try p.parseTernary();
            try p.emit(.select, 0);
        }
    }

    fn parseOr(p: *Parser) ParseError!void {
        try p.parseAnd();
        while (true) {
            p.skipWs();
            if (p.eat("||")) {
                try p.parseAnd();
                try p.emit(.lor, 0);
            } else if (p.eatIdent("or")) {
                try p.parseAnd();
                try p.emit(.lor, 0);
            } else break;
        }
    }

    fn parseAnd(p: *Parser) ParseError!void {
        try p.parseBor();
        while (true) {
            p.skipWs();
            if (p.eat("&&")) {
                try p.parseBor();
                try p.emit(.land, 0);
            } else if (p.eatIdent("and")) {
                try p.parseBor();
                try p.emit(.land, 0);
            } else break;
        }
    }

    fn parseBor(p: *Parser) ParseError!void {
        try p.parseBxor();
        while (true) {
            p.skipWs();
            if (p.peek() == '|' and p.pos + 1 < p.src.len and p.src[p.pos + 1] == '|') break;
            if (p.peek() != '|') break;
            p.pos += 1;
            try p.parseBxor();
            try p.emit(.bor, 0);
        }
    }

    fn parseBxor(p: *Parser) ParseError!void {
        try p.parseBand();
        while (true) {
            p.skipWs();
            if (p.peek() != '^') break;
            p.pos += 1;
            try p.parseBand();
            try p.emit(.bxor, 0);
        }
    }

    fn parseBand(p: *Parser) ParseError!void {
        try p.parseEq();
        while (true) {
            p.skipWs();
            if (p.peek() != '&') break;
            if (p.pos + 1 < p.src.len and p.src[p.pos + 1] == '&') break;
            p.pos += 1;
            try p.parseEq();
            try p.emit(.band, 0);
        }
    }

    fn parseEq(p: *Parser) ParseError!void {
        try p.parseCmp();
        while (true) {
            p.skipWs();
            var tag: ?Tag = null;
            if (p.eat("==")) {
                tag = .eq;
            } else if (p.eat("!=")) {
                tag = .ne;
            } else if (p.eat("<>")) {
                tag = .ne;
            } else if (p.peek() == '=' and (p.pos + 1 >= p.src.len or p.src[p.pos + 1] != '=')) {
                p.pos += 1;
                tag = .eq;
            } else break;
            try p.parseCmp();
            try p.emit(tag.?, 0);
        }
    }

    fn parseCmp(p: *Parser) ParseError!void {
        try p.parseShift();
        while (true) {
            p.skipWs();
            var tag: ?Tag = null;
            if (p.eat("<=")) {
                tag = .le;
            } else if (p.eat(">=")) {
                tag = .ge;
            } else if (p.peek() == '<') {
                p.pos += 1;
                tag = .lt;
            } else if (p.peek() == '>') {
                p.pos += 1;
                tag = .gt;
            } else break;
            try p.parseShift();
            try p.emit(tag.?, 0);
        }
    }

    fn parseShift(p: *Parser) ParseError!void {
        try p.parseAdd();
        while (true) {
            p.skipWs();
            var tag: ?Tag = null;
            if (p.eat("<<")) {
                tag = .shl;
            } else if (p.eat(">>")) {
                tag = .shr;
            } else break;
            try p.parseAdd();
            try p.emit(tag.?, 0);
        }
    }

    fn parseAdd(p: *Parser) ParseError!void {
        try p.parseMul();
        while (true) {
            p.skipWs();
            const c = p.peek();
            if (c != '+' and c != '-') break;
            p.pos += 1;
            try p.parseMul();
            try p.emit(if (c == '+') .add else .sub, 0);
        }
    }

    fn parseMul(p: *Parser) ParseError!void {
        try p.parseUnary();
        while (true) {
            p.skipWs();
            var tag: ?Tag = null;
            if (p.peek() == '*' and (p.pos + 1 >= p.src.len or p.src[p.pos + 1] != '*')) {
                p.pos += 1;
                tag = .mul;
            } else if (p.peek() == '/') {
                p.pos += 1;
                tag = .div;
            } else if (p.peek() == '%') {
                p.pos += 1;
                tag = .mod;
            } else break;
            try p.parseUnary();
            try p.emit(tag.?, 0);
        }
    }

    fn parsePow(p: *Parser) ParseError!void {
        try p.parsePrimary();
        p.skipWs();
        if (p.eat("**")) {
            p.skipWs();
            const c = p.peek();
            if (c == '!') {
                return ParseError.Syntax;
            } else if (c == 'n' or c == 'N') {
                const save = p.pos;
                if (p.eatIdent("not")) {
                    p.pos = save;
                    return ParseError.Syntax;
                }
            }
            try p.parseUnary();
            try p.emit(.pow, 0);
        }
    }

    fn parseUnary(p: *Parser) ParseError!void {
        p.skipWs();
        const c = p.peek();
        if (c == '-') {
            p.pos += 1;
            try p.parseUnary();
            try p.emit(.neg, 0);
            return;
        }
        if (c == '+') {
            p.pos += 1;
            try p.parseUnary();
            return;
        }
        if (c == '!') {
            if (p.pos + 1 < p.src.len and p.src[p.pos + 1] == '=') return ParseError.Syntax;
            p.pos += 1;
            try p.parseUnary();
            try p.emit(.lnot, 0);
            return;
        }
        if (p.eatIdent("not")) {
            try p.parseUnary();
            try p.emit(.lnot, 0);
            return;
        }
        try p.parsePow();
    }

    fn scanIdent(p: *Parser) ?[]const u8 {
        const s = p.pos;
        if (s >= p.src.len) return null;
        const c = p.src[s];
        if (!std.ascii.isAlphabetic(c) and c != '_') return null;
        var e = s + 1;
        while (e < p.src.len) {
            const d = p.src[e];
            if (!std.ascii.isAlphanumeric(d) and d != '_') break;
            e += 1;
        }
        p.pos = e;
        return p.src[s..e];
    }

    fn scanNumber(p: *Parser) ParseError!void {
        const s = p.pos;
        var is_float = false;
        if (p.pos + 1 < p.src.len and p.src[p.pos] == '0' and (p.src[p.pos + 1] == 'x' or p.src[p.pos + 1] == 'X')) {
            p.pos += 2;
            const ds = p.pos;
            while (p.pos < p.src.len and std.ascii.isHex(p.src[p.pos])) : (p.pos += 1) {}
            if (p.pos == ds) return ParseError.Syntax;
            const text = p.src[s..p.pos];
            const v = std.fmt.parseInt(i64, text, 0) catch return ParseError.Syntax;
            try p.emit(.push_i, v);
            return;
        }
        var digits = false;
        while (p.pos < p.src.len and std.ascii.isDigit(p.src[p.pos])) {
            p.pos += 1;
            digits = true;
        }
        if (p.peek() == '.') {
            const after = p.pos + 1;
            if (after < p.src.len and std.ascii.isDigit(p.src[after])) {
                is_float = true;
                p.pos += 1;
                while (p.pos < p.src.len and std.ascii.isDigit(p.src[p.pos])) : (p.pos += 1) {}
            } else if (digits) {
                is_float = true;
                p.pos += 1;
            } else {
                return ParseError.Syntax;
            }
        } else if (!digits) {
            return ParseError.Syntax;
        }
        if (p.peek() == 'e' or p.peek() == 'E') {
            var q = p.pos + 1;
            if (q < p.src.len and (p.src[q] == '+' or p.src[q] == '-')) q += 1;
            if (q < p.src.len and std.ascii.isDigit(p.src[q])) {
                is_float = true;
                q += 1;
                while (q < p.src.len and std.ascii.isDigit(p.src[q])) : (q += 1) {}
                p.pos = q;
            }
        }
        const text = p.src[s..p.pos];
        for (text) |c| {
            if (c == '_') return ParseError.Syntax;
        }
        if (!is_float) {
            if (std.fmt.parseInt(i64, text, 10)) |v| {
                try p.emit(.push_i, v);
                return;
            } else |_| {}
        }
        const f = std.fmt.parseFloat(f64, text) catch return ParseError.Syntax;
        try p.emit(.push_f, @bitCast(f));
    }

    fn parsePrimary(p: *Parser) ParseError!void {
        p.depth += 1;
        if (p.depth > max_depth) return ParseError.DepthLimit;
        defer p.depth -= 1;
        p.skipWs();
        const c = p.peek();
        if (c == '(') {
            p.pos += 1;
            try p.parseTernary();
            p.skipWs();
            if (p.peek() != ')') return ParseError.Syntax;
            p.pos += 1;
            return;
        }
        if (std.ascii.isDigit(c) or (c == '.' and p.pos + 1 < p.src.len and std.ascii.isDigit(p.src[p.pos + 1]))) {
            try p.scanNumber();
            return;
        }
        if (std.ascii.isAlphabetic(c) or c == '_') {
            const save = p.pos;
            const name = p.scanIdent() orelse return ParseError.Syntax;
            if (std.ascii.eqlIgnoreCase(name, "PI")) {
                try p.emit(.push_f, @bitCast(@as(f64, math.pi)));
                return;
            }
            if (std.ascii.eqlIgnoreCase(name, "E")) {
                try p.emit(.push_f, @bitCast(@as(f64, math.e)));
                return;
            }
            if (std.ascii.eqlIgnoreCase(name, "true")) {
                try p.emit(.push_b, 1);
                return;
            }
            if (std.ascii.eqlIgnoreCase(name, "false")) {
                try p.emit(.push_b, 0);
                return;
            }
            p.skipWs();
            if (p.peek() != '(') return ParseError.UnknownName;
            const b = findBuiltin(name) orelse return ParseError.UnknownName;
            p.pos += 1;
            var nargs: u8 = 0;
            p.skipWs();
            if (p.peek() == ')') {
                p.pos += 1;
            } else {
                while (true) {
                    try p.parseTernary();
                    nargs += 1;
                    if (nargs > 2) return ParseError.BadArity;
                    p.skipWs();
                    if (p.peek() == ',') {
                        p.pos += 1;
                        continue;
                    }
                    if (p.peek() == ')') {
                        p.pos += 1;
                        break;
                    }
                    return ParseError.Syntax;
                }
            }
            if (nargs < b.min_args or nargs > b.max_args) return ParseError.BadArity;
            if (std.mem.eql(u8, b.name, "log") and nargs == 1) {
                try p.emit(.ln, 0);
            } else {
                try p.emit(b.tag, 0);
            }
            _ = save;
            return;
        }
        return ParseError.Syntax;
    }
};

var expr_mtx: sync.SpinLock = .{};
var expr_slots: [max_slots]?*Prog = std.mem.zeroes([max_slots]?*Prog);
var expr_free: [max_slots]u16 = undefined;
var expr_free_count: usize = 0;

fn exprAlloc(prog: *Prog) ?u16 {
    expr_mtx.lock();
    defer expr_mtx.unlock();
    var idx: ?u16 = null;
    if (expr_free_count > 0) {
        expr_free_count -= 1;
        idx = expr_free[expr_free_count];
    } else {
        var k: usize = 0;
        while (k < max_slots) : (k += 1) {
            if (expr_slots[k] == null) {
                idx = @intCast(k);
                break;
            }
        }
    }
    if (idx) |v| {
        expr_slots[v] = prog;
        return v;
    }
    return null;
}

fn exprGet(h: i64) ?*Prog {
    if (h <= 0 or h > max_slots) return null;
    expr_mtx.lock();
    defer expr_mtx.unlock();
    return expr_slots[@intCast(h - 1)];
}

pub fn exprCompile(src: []const u8) i64 {
    var buf: [max_ops]Op = undefined;
    var par = Parser{ .src = src, .pos = 0, .ops = &buf, .count = 0, .depth = 0 };
    par.parseTernary() catch return 0;
    par.skipWs();
    if (par.pos != src.len or src.len == 0) return 0;
    const trial = run(buf[0..par.count]) catch return 0;
    _ = trial;
    const ops = std.heap.c_allocator.dupe(Op, buf[0..par.count]) catch return 0;
    const prog = std.heap.c_allocator.create(Prog) catch {
        std.heap.c_allocator.free(ops);
        return 0;
    };
    prog.* = .{ .ops = ops };
    const slot = exprAlloc(prog) orelse {
        std.heap.c_allocator.free(ops);
        std.heap.c_allocator.destroy(prog);
        return 0;
    };
    return @as(i64, slot) + 1;
}

pub fn exprEval(h: i64) f64 {
    const prog = exprGet(h) orelse return math.nan(f64);
    return run(prog.ops) catch math.nan(f64);
}

pub fn exprFree(h: i64) void {
    if (h <= 0 or h > max_slots) return;
    expr_mtx.lock();
    defer expr_mtx.unlock();
    const idx: usize = @intCast(h - 1);
    if (expr_slots[idx]) |prog| {
        std.heap.c_allocator.free(prog.ops);
        std.heap.c_allocator.destroy(prog);
        expr_slots[idx] = null;
        if (expr_free_count < max_slots) {
            expr_free[expr_free_count] = @intCast(idx);
            expr_free_count += 1;
        }
    }
}
