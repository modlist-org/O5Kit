const std = @import("std");

pub const TagType = enum(u32) {
    simple = 0,
    colon = 1,
    func = 2,
    js_expr = 3,
};

pub const TagSpan = extern struct {
    raw_start: u32,
    raw_len: u32,
    name_start: u32,
    name_len: u32,
    args_start: u32,
    args_len: u32,
    tag_type: TagType,
};

fn isIdentifierChar(c: u16) bool {
    return (c >= 'a' and c <= 'z') or
        (c >= 'A' and c <= 'Z') or
        (c >= '0' and c <= '9') or
        c == '_';
}

fn equalsIgnoreCaseAscii(src: []const u16, target: []const u8) bool {
    if (src.len != target.len) return false;
    for (src, 0..) |c, i| {
        const lower = if (c >= 'A' and c <= 'Z') c + 32 else c;
        const target_lower = if (target[i] >= 'A' and target[i] <= 'Z') target[i] + 32 else target[i];
        if (lower != target_lower) return false;
    }
    return true;
}

fn skipJsString(src: []const u16, start: usize) usize {
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

fn findBalancedClose(src: []const u16, start: usize) ?usize {
    var depth: usize = 1;
    var i = start;
    while (i < src.len) {
        const c = src[i];
        if (c == '/' and i + 1 < src.len and src[i + 1] == '/') {
            while (i < src.len and src[i] != '\n') {
                i += 1;
            }
            continue;
        }
        if (c == '/' and i + 1 < src.len and src[i + 1] == '*') {
            i += 2;
            while (i + 1 < src.len and !(src[i] == '*' and src[i + 1] == '/')) {
                i += 1;
            }
            i = @min(src.len, i + 2);
            continue;
        }
        if (c == '\'' or c == '"' or c == '`') {
            i = skipJsString(src, i);
            continue;
        }
        if (c == '{') {
            depth += 1;
        } else if (c == '}') {
            depth -= 1;
            if (depth == 0) return i;
        }
        i += 1;
    }
    return null;
}

pub fn scan(src: []const u16, out: []TagSpan) usize {
    var count: usize = 0;
    var i: usize = 0;

    while (i < src.len) {
        if (src[i] != '{') {
            i += 1;
            continue;
        }

        const tag_start = i;
        const name_start = i + 1;
        var name_end = name_start;
        while (name_end < src.len and isIdentifierChar(src[name_end])) {
            name_end += 1;
        }

        if (name_end == name_start or name_end >= src.len) {
            i += 1;
            continue;
        }

        const name_len = name_end - name_start;
        const name_slice = src[name_start..name_end];
        const delimiter = src[name_end];

        // 1. JSExpr special case: {JSExpr:...} or {JSExpr(...)}
        if ((delimiter == ':' or delimiter == '(') and equalsIgnoreCaseAscii(name_slice, "JSExpr")) {
            if (findBalancedClose(src, name_end + 1)) |close| {
                if (count < out.len) {
                    out[count] = .{
                        .raw_start = @intCast(tag_start),
                        .raw_len = @intCast(close - tag_start + 1),
                        .name_start = @intCast(name_start),
                        .name_len = @intCast(name_len),
                        .args_start = @intCast(name_end + 1),
                        .args_len = @intCast(close - name_end - 1),
                        .tag_type = .js_expr,
                    };
                }
                count += 1;
                i = close + 1;
                continue;
            }
        }

        // 2. Simple Tag: {Name}
        if (delimiter == '}') {
            if (count < out.len) {
                out[count] = .{
                    .raw_start = @intCast(tag_start),
                    .raw_len = @intCast(name_end - tag_start + 1),
                    .name_start = @intCast(name_start),
                    .name_len = @intCast(name_len),
                    .args_start = 0,
                    .args_len = 0,
                    .tag_type = .simple,
                };
            }
            count += 1;
            i = name_end + 1;
            continue;
        }

        // 3. Colon Tag: {Name:args}
        if (delimiter == ':') {
            var close = name_end + 1;
            while (close < src.len and src[close] != '}') {
                close += 1;
            }
            if (close < src.len and src[close] == '}') {
                if (count < out.len) {
                    out[count] = .{
                        .raw_start = @intCast(tag_start),
                        .raw_len = @intCast(close - tag_start + 1),
                        .name_start = @intCast(name_start),
                        .name_len = @intCast(name_len),
                        .args_start = @intCast(name_end + 1),
                        .args_len = @intCast(close - name_end - 1),
                        .tag_type = .colon,
                    };
                }
                count += 1;
                i = close + 1;
                continue;
            }
        }

        // 4. Func Tag: {Name(args)}
        if (delimiter == '(') {
            var close = name_end + 1;
            while (close < src.len and src[close] != '}') {
                close += 1;
            }
            if (close < src.len and src[close] == '}' and close > name_end + 1 and src[close - 1] == ')') {
                if (count < out.len) {
                    out[count] = .{
                        .raw_start = @intCast(tag_start),
                        .raw_len = @intCast(close - tag_start + 1),
                        .name_start = @intCast(name_start),
                        .name_len = @intCast(name_len),
                        .args_start = @intCast(name_end + 1),
                        .args_len = @intCast(close - 1 - (name_end + 1)),
                        .tag_type = .func,
                    };
                }
                count += 1;
                i = close + 1;
                continue;
            }
        }

        i += 1;
    }

    return count;
}
