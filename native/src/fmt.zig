const std = @import("std");

pub const FormatKind = enum(u8) {
    fixed, // F
    number, // N
    decimal, // D
    percent, // P
    hex_upper, // X
    hex_lower, // x
    general, // G
    exponential, // E / e
    custom, // 0, #, ., ,, %
};

pub const FormatSpec = struct {
    kind: FormatKind,
    precision: u8,
    min_int_digits: u8,
    use_grouping: bool,
    min_frac_digits: u8,
    max_frac_digits: u8,
    has_percent: bool,
    exp_upper: bool,
};

fn isDigit(c: anytype) bool {
    return c >= '0' and c <= '9';
}

fn toUpper(c: anytype) u8 {
    if (c >= 'a' and c <= 'z') return @intCast(c - 32);
    return @intCast(c);
}

pub fn parseFormat(comptime Char: type, fmt: []const Char) FormatSpec {
    if (fmt.len == 0) {
        return .{
            .kind = .general,
            .precision = 0,
            .min_int_digits = 1,
            .use_grouping = false,
            .min_frac_digits = 0,
            .max_frac_digits = 0,
            .has_percent = false,
            .exp_upper = true,
        };
    }

    const first = fmt[0];
    const upper_first = toUpper(first);

    var is_standard = false;
    if (upper_first == 'F' or upper_first == 'N' or upper_first == 'D' or
        upper_first == 'P' or upper_first == 'X' or upper_first == 'G' or upper_first == 'E')
    {
        is_standard = true;
        for (fmt[1..]) |c| {
            if (!isDigit(c)) {
                is_standard = false;
                break;
            }
        }
    }

    if (is_standard) {
        var prec: ?u8 = null;
        if (fmt.len > 1) {
            var val: u32 = 0;
            for (fmt[1..]) |c| {
                val = val * 10 + (c - '0');
                if (val > 99) {
                    val = 99;
                    break;
                }
            }
            prec = @intCast(val);
        }

        switch (upper_first) {
            'F' => {
                const p = prec orelse 2;
                return .{
                    .kind = .fixed,
                    .precision = p,
                    .min_int_digits = 1,
                    .use_grouping = false,
                    .min_frac_digits = p,
                    .max_frac_digits = p,
                    .has_percent = false,
                    .exp_upper = true,
                };
            },
            'N' => {
                const p = prec orelse 2;
                return .{
                    .kind = .number,
                    .precision = p,
                    .min_int_digits = 1,
                    .use_grouping = true,
                    .min_frac_digits = p,
                    .max_frac_digits = p,
                    .has_percent = false,
                    .exp_upper = true,
                };
            },
            'D' => {
                const p = prec orelse 1;
                return .{
                    .kind = .decimal,
                    .precision = p,
                    .min_int_digits = p,
                    .use_grouping = false,
                    .min_frac_digits = 0,
                    .max_frac_digits = 0,
                    .has_percent = false,
                    .exp_upper = true,
                };
            },
            'P' => {
                const p = prec orelse 2;
                return .{
                    .kind = .percent,
                    .precision = p,
                    .min_int_digits = 1,
                    .use_grouping = false,
                    .min_frac_digits = p,
                    .max_frac_digits = p,
                    .has_percent = true,
                    .exp_upper = true,
                };
            },
            'X' => {
                const p = prec orelse 1;
                return .{
                    .kind = if (first == 'X') .hex_upper else .hex_lower,
                    .precision = p,
                    .min_int_digits = p,
                    .use_grouping = false,
                    .min_frac_digits = 0,
                    .max_frac_digits = 0,
                    .has_percent = false,
                    .exp_upper = true,
                };
            },
            'G' => {
                const p = prec orelse 0;
                return .{
                    .kind = .general,
                    .precision = p,
                    .min_int_digits = 1,
                    .use_grouping = false,
                    .min_frac_digits = 0,
                    .max_frac_digits = 0,
                    .has_percent = false,
                    .exp_upper = true,
                };
            },
            'E' => {
                const p = prec orelse 6;
                return .{
                    .kind = .exponential,
                    .precision = p,
                    .min_int_digits = 1,
                    .use_grouping = false,
                    .min_frac_digits = p,
                    .max_frac_digits = p,
                    .has_percent = false,
                    .exp_upper = (first == 'E'),
                };
            },
            else => unreachable,
        }
    }

    var min_int: u8 = 0;
    var min_frac: u8 = 0;
    var max_frac: u8 = 0;
    var grouping = false;
    var percent = false;
    var seen_dot = false;

    for (fmt) |c| {
        if (c == '%') {
            percent = true;
        } else if (c == ',') {
            grouping = true;
        } else if (c == '.') {
            seen_dot = true;
        } else if (c == '0') {
            if (!seen_dot) {
                if (min_int < 99) min_int += 1;
            } else {
                if (min_frac < 99) min_frac += 1;
                if (max_frac < 99) max_frac += 1;
            }
        } else if (c == '#') {
            if (seen_dot) {
                if (max_frac < 99) max_frac += 1;
            }
        }
    }

    return .{
        .kind = .custom,
        .precision = max_frac,
        .min_int_digits = min_int,
        .use_grouping = grouping,
        .min_frac_digits = min_frac,
        .max_frac_digits = max_frac,
        .has_percent = percent,
        .exp_upper = true,
    };
}

fn writeAscii(comptime Char: type, out: []Char, str: []const u8) usize {
    const len = @min(out.len, str.len);
    for (0..len) |i| {
        out[i] = @intCast(str[i]);
    }
    return len;
}

pub fn formatF64(comptime Char: type, val: f64, spec: FormatSpec, out: []Char) usize {
    if (out.len == 0) return 0;

    if (std.math.isNan(val)) {
        return writeAscii(Char, out, "NaN");
    }
    if (std.math.isInf(val)) {
        return if (val < 0) writeAscii(Char, out, "-Infinity") else writeAscii(Char, out, "Infinity");
    }

    switch (spec.kind) {
        .hex_upper, .hex_lower => {
            return formatHex(Char, val, spec, out);
        },
        .exponential => {
            return formatExp(Char, val, spec, out);
        },
        .general => {
            return formatGeneral(Char, val, spec, out);
        },
        .fixed, .number, .decimal, .percent, .custom => {
            return formatNumber(Char, val, spec, out);
        },
    }
}

fn formatHex(comptime Char: type, val: f64, spec: FormatSpec, out: []Char) usize {
    const is_neg = val < 0.0;
    const abs_v = if (is_neg) -val else val;
    const uval: u64 = if (abs_v >= 18446744073709551615.0)
        std.math.maxInt(u64)
    else
        @intFromFloat(abs_v);

    const hex_chars: *const [16]u8 = if (spec.kind == .hex_upper) "0123456789ABCDEF" else "0123456789abcdef";

    var buf: [128]u8 = undefined;
    var len: usize = 0;
    var temp = uval;

    if (temp == 0) {
        buf[0] = '0';
        len = 1;
    } else {
        while (temp > 0 and len < buf.len) : (temp /= 16) {
            buf[len] = hex_chars[@intCast(temp % 16)];
            len += 1;
        }
    }

    while (len < spec.min_int_digits and len < buf.len) {
        buf[len] = '0';
        len += 1;
    }

    var pos: usize = 0;
    if (is_neg and pos < out.len) {
        out[pos] = '-';
        pos += 1;
    }

    var i = len;
    while (i > 0) {
        i -= 1;
        if (pos < out.len) {
            out[pos] = @intCast(buf[i]);
            pos += 1;
        }
    }

    return pos;
}

fn formatExp(comptime Char: type, val: f64, spec: FormatSpec, out: []Char) usize {
    if (std.math.isNan(val)) {
        return writeAscii(Char, out, "NaN");
    }
    if (std.math.isInf(val)) {
        return if (val < 0) writeAscii(Char, out, "-Infinity") else writeAscii(Char, out, "Infinity");
    }

    var v = val;
    var pos: usize = 0;

    if (v < 0.0) {
        if (pos < out.len) {
            out[pos] = '-';
            pos += 1;
        }
        v = -v;
    }

    var exp: i32 = 0;
    var mantissa: f64 = v;

    if (v != 0.0 and !std.math.isNan(v) and !std.math.isInf(v)) {
        const log_v = std.math.log10(v);
        if (!std.math.isNan(log_v) and !std.math.isInf(log_v)) {
            exp = @intFromFloat(@floor(log_v));
            mantissa = v / std.math.pow(f64, 10.0, @floatFromInt(exp));

            const prec = @min(spec.precision, 15);
            const mult = std.math.pow(f64, 10.0, @floatFromInt(prec));
            if (@round(mantissa * mult) / mult >= 10.0) {
                mantissa /= 10.0;
                exp += 1;
            }
        }
    }

    const prec = @min(spec.precision, 15);
    const fixed_spec: FormatSpec = .{
        .kind = .fixed,
        .precision = prec,
        .min_int_digits = 1,
        .use_grouping = false,
        .min_frac_digits = prec,
        .max_frac_digits = prec,
        .has_percent = false,
        .exp_upper = true,
    };

    const written = formatNumber(Char, mantissa, fixed_spec, out[pos..]);
    pos += written;

    if (spec.precision > 15) {
        const extra = spec.precision - 15;
        for (0..extra) |_| {
            if (pos < out.len) {
                out[pos] = '0';
                pos += 1;
            }
        }
    }

    if (pos < out.len) {
        out[pos] = if (spec.exp_upper) 'E' else 'e';
        pos += 1;
    }

    if (exp >= 0) {
        if (pos < out.len) {
            out[pos] = '+';
            pos += 1;
        }
    } else {
        if (pos < out.len) {
            out[pos] = '-';
            pos += 1;
        }
        exp = -exp;
    }

    var u_exp: u32 = @intCast(exp);
    var exp_buf: [3]u8 = undefined;
    exp_buf[2] = @intCast('0' + (u_exp % 10));
    u_exp /= 10;
    exp_buf[1] = @intCast('0' + (u_exp % 10));
    u_exp /= 10;
    exp_buf[0] = @intCast('0' + (u_exp % 10));

    for (exp_buf) |c| {
        if (pos < out.len) {
            out[pos] = @intCast(c);
            pos += 1;
        }
    }

    return pos;
}

fn formatGeneral(comptime Char: type, val: f64, spec: FormatSpec, out: []Char) usize {
    _ = spec;
    if (val == 0.0) {
        if (out.len > 0) {
            out[0] = '0';
            return 1;
        }
        return 0;
    }

    const abs_v = if (val < 0.0) -val else val;
    if (abs_v >= 1e15 or (abs_v < 1e-4 and abs_v > 0.0)) {
        const exp_spec: FormatSpec = .{
            .kind = .exponential,
            .precision = 6,
            .min_int_digits = 1,
            .use_grouping = false,
            .min_frac_digits = 1,
            .max_frac_digits = 6,
            .has_percent = false,
            .exp_upper = true,
        };
        return formatExp(Char, val, exp_spec, out);
    }

    const num_spec: FormatSpec = .{
        .kind = .fixed,
        .precision = 10,
        .min_int_digits = 1,
        .use_grouping = false,
        .min_frac_digits = 0,
        .max_frac_digits = 10,
        .has_percent = false,
        .exp_upper = true,
    };
    return formatNumber(Char, val, num_spec, out);
}

fn formatNumber(comptime Char: type, val: f64, spec: FormatSpec, out: []Char) usize {
    var v = val;
    if (spec.has_percent) {
        if (v >= 1e306 or v <= -1e306) {
            const exp_spec: FormatSpec = .{
                .kind = .exponential,
                .precision = @min(spec.precision, 10),
                .min_int_digits = 1,
                .use_grouping = false,
                .min_frac_digits = 1,
                .max_frac_digits = @min(spec.precision, 10),
                .has_percent = true,
                .exp_upper = true,
            };
            return formatExp(Char, v, exp_spec, out);
        }
        v *= 100.0;
    }

    const is_neg = v < 0.0;
    const abs_v = if (is_neg) -v else v;

    if (abs_v >= 1e15) {
        const exp_spec: FormatSpec = .{
            .kind = .exponential,
            .precision = @min(spec.precision, 10),
            .min_int_digits = 1,
            .use_grouping = false,
            .min_frac_digits = 1,
            .max_frac_digits = @min(spec.precision, 10),
            .has_percent = spec.has_percent,
            .exp_upper = true,
        };
        return formatExp(Char, v, exp_spec, out);
    }

    const max_frac: usize = @min(spec.max_frac_digits, 15);
    const min_frac: usize = @min(spec.min_frac_digits, 15);
    const min_int: usize = spec.min_int_digits;

    const mult = std.math.pow(f64, 10.0, @floatFromInt(max_frac));
    const rounded = @round(abs_v * mult) / mult;

    const int_part: u64 = @intFromFloat(rounded);
    const frac_diff = (rounded - @as(f64, @floatFromInt(int_part))) * mult;
    const frac_part: u64 = @intFromFloat(@round(frac_diff));

    var int_digits: [128]u8 = undefined;
    var int_len: usize = 0;
    var temp = int_part;

    while (temp > 0 and int_len < int_digits.len) : (temp /= 10) {
        int_digits[int_len] = @intCast('0' + (temp % 10));
        int_len += 1;
    }

    while (int_len < min_int and int_len < int_digits.len) {
        int_digits[int_len] = '0';
        int_len += 1;
    }

    var pos: usize = 0;

    if (is_neg and (int_len > 0 or frac_part > 0)) {
        if (pos < out.len) {
            out[pos] = '-';
            pos += 1;
        }
    }

    if (int_len > 0) {
        var i = int_len;
        while (i > 0) {
            i -= 1;
            if (spec.use_grouping and i < int_len - 1 and (i + 1) % 3 == 0) {
                if (pos < out.len) {
                    out[pos] = ',';
                    pos += 1;
                }
            }
            if (pos < out.len) {
                out[pos] = @intCast(int_digits[i]);
                pos += 1;
            }
        }
    }

    if (spec.max_frac_digits > 0) {
        var frac_digits: [128]u8 = undefined;
        var f_temp = frac_part;
        var f_i: usize = max_frac;
        while (f_i > 0) {
            f_i -= 1;
            frac_digits[f_i] = @intCast('0' + (f_temp % 10));
            f_temp /= 10;
        }

        var eff_frac_len: usize = max_frac;
        while (eff_frac_len > min_frac and frac_digits[eff_frac_len - 1] == '0') {
            eff_frac_len -= 1;
        }

        if (eff_frac_len > 0 or spec.min_frac_digits > 0) {
            if (pos < out.len) {
                out[pos] = '.';
                pos += 1;
            }
            for (0..eff_frac_len) |idx| {
                if (pos < out.len) {
                    out[pos] = @intCast(frac_digits[idx]);
                    pos += 1;
                }
            }

            if (spec.min_frac_digits > 15) {
                const extra = spec.min_frac_digits - 15;
                for (0..extra) |_| {
                    if (pos < out.len) {
                        out[pos] = '0';
                        pos += 1;
                    }
                }
            }
        }
    }

    if (spec.has_percent) {
        if (pos < out.len) {
            out[pos] = '%';
            pos += 1;
        }
    }

    return pos;
}

// c abi helpers
pub fn o5_fmt_f64(
    val: f64,
    fmt_ptr: [*]const u16,
    fmt_len: usize,
    out_ptr: [*]u16,
    max_out: usize,
) usize {
    const spec = parseFormat(u16, fmt_ptr[0..fmt_len]);
    return formatF64(u16, val, spec, out_ptr[0..max_out]);
}

pub fn o5_fmt_f64_utf8(
    val: f64,
    fmt_ptr: [*]const u8,
    fmt_len: usize,
    out_ptr: [*]u8,
    max_out: usize,
) usize {
    const spec = parseFormat(u8, fmt_ptr[0..fmt_len]);
    return formatF64(u8, val, spec, out_ptr[0..max_out]);
}
