const std = @import("std");

pub const TagSpan = extern struct {
    raw_start: u32,
    raw_len: u32,
    expr_start: u32,
    expr_len: u32,
    fmt_start: u32,
    fmt_len: u32,
};

fn skipString(src: []const u16, start: usize) usize {
    const quote = src[start];
    var i = start + 1;
    while (i < src.len) {
        if (src[i] == '\\') {
            i += 2;
            continue;
        }
        if (src[i] == quote) {
            return i + 1;
        }
        if (quote != '`' and src[i] == '\n') {
            return i;
        }
        i += 1;
    }
    return src.len;
}

pub fn scan(src: []const u16, out: []TagSpan) usize {
    var count: usize = 0;
    var i: usize = 0;
    const len = src.len;

    while (i < len) {
        if (src[i] != '{') {
            i += 1;
            continue;
        }

        // Escape: '{{' -> skip
        if (i + 1 < len and src[i + 1] == '{') {
            i += 2;
            continue;
        }

        const tag_start = i;
        var depth: usize = 1;
        var paren_depth: usize = 0;
        var bracket_depth: usize = 0;
        var colon_pos: ?usize = null;
        var idx = i + 1;
        var matched = false;

        while (idx < len) {
            const c = src[idx];

            // Line comment: '//'
            if (c == '/' and idx + 1 < len and src[idx + 1] == '/') {
                idx += 2;
                while (idx < len and src[idx] != '\n') {
                    idx += 1;
                }
                continue;
            }

            // Block comment: '/*'
            if (c == '/' and idx + 1 < len and src[idx + 1] == '*') {
                idx += 2;
                while (idx + 1 < len and !(src[idx] == '*' and src[idx + 1] == '/')) {
                    idx += 1;
                }
                idx = @min(len, idx + 2);
                continue;
            }

            // String literals
            if (c == '\'' or c == '"' or c == '`') {
                idx = skipString(src, idx);
                continue;
            }

            if (c == '(') {
                paren_depth += 1;
            } else if (c == ')') {
                if (paren_depth > 0) paren_depth -= 1;
            } else if (c == '[') {
                bracket_depth += 1;
            } else if (c == ']') {
                if (bracket_depth > 0) bracket_depth -= 1;
            } else if (c == '{') {
                depth += 1;
            } else if (c == '}') {
                depth -= 1;
                if (depth == 0) {
                    matched = true;
                    break;
                }
            } else if (c == ':') {
                // Top-level colon is format delimiter
                if (depth == 1 and paren_depth == 0 and bracket_depth == 0 and colon_pos == null) {
                    colon_pos = idx;
                }
            }

            idx += 1;
        }

        if (matched) {
            const tag_end = idx;
            const raw_len = tag_end - tag_start + 1;

            const expr_start: usize = tag_start + 1;
            var expr_len: usize = 0;
            var fmt_start: usize = 0;
            var fmt_len: usize = 0;

            if (colon_pos) |c_pos| {
                expr_len = c_pos - expr_start;
                fmt_start = c_pos + 1;
                fmt_len = tag_end - fmt_start;
            } else {
                expr_len = tag_end - expr_start;
            }

            if (count < out.len) {
                out[count] = .{
                    .raw_start = @intCast(tag_start),
                    .raw_len = @intCast(raw_len),
                    .expr_start = @intCast(expr_start),
                    .expr_len = @intCast(expr_len),
                    .fmt_start = @intCast(fmt_start),
                    .fmt_len = @intCast(fmt_len),
                };
            }
            count += 1;
            i = tag_end + 1;
        } else {
            i += 1;
        }
    }

    return count;
}
