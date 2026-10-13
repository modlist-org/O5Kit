const std = @import("std");
const expr = @import("expr.zig");

fn eval(s: []const u8) ?f64 {
    const h = expr.exprCompile(s);
    if (h == 0) return null;
    defer expr.exprFree(h);
    return expr.exprEval(h);
}

fn eq(s: []const u8, want: f64) !void {
    const got = eval(s) orelse {
        std.debug.print("FAIL(declined): {s}\n", .{s});
        return error.Declined;
    };
    try std.testing.expectEqual(want, got);
}

fn tol(s: []const u8, want: f64, rel: f64) !void {
    const got = eval(s) orelse {
        std.debug.print("FAIL(declined): {s}\n", .{s});
        return error.Declined;
    };
    const diff = @abs(got - want);
    const allow = @max(@abs(want) * rel, 1e-300);
    if (diff > allow) {
        std.debug.print("FAIL: {s} got {} want {}\n", .{ s, got, want });
        return error.Mismatch;
    }
}

fn fail(s: []const u8) !void {
    if (eval(s) != null) {
        std.debug.print("FAIL(accepted): {s}\n", .{s});
        return error.ShouldDecline;
    }
}

fn nan(s: []const u8) !void {
    const got = eval(s) orelse {
        std.debug.print("FAIL(declined): {s}\n", .{s});
        return error.Declined;
    };
    try std.testing.expect(std.math.isNan(got));
}

test "integers i64 wrap" {
    try eq("2+3*4", 14.0);
    try eq("10-4-3", 3.0);
    try eq("2*(3+4)", 14.0);
    try eq("-5+3", -2.0);
    try eq("2*-3", -6.0);
    try eq("---5", -5.0);
    try eq("(2+3)*(4-1)", 15.0);
    try eq("10%3+1", 2.0);
    try eq("2*3%2", 0.0);
    try eq("2147483647+1", 2147483648.0);
    try eq("1000000*1000000", 1000000000000.0);
    try eq("9223372036854775807+1", -9223372036854775808.0);
    try eq("9007199254775807+1", 9007199254775808.0);
    try eq("3000000000+0", 3000000000.0);
    try eq("0x10", 16.0);
}

test "div always f64" {
    try eq("1/2", 0.5);
    try eq("7/2", 3.5);
    try eq("10/4", 2.5);
    try eq("2*(3+4)/2", 7.0);
    try eq("1/0", std.math.inf(f64));
    try eq("PI/2", 1.5707963267948966);
}

test "mod" {
    try eq("10%3", 1.0);
    try eq("-5%2", -1.0);
    try eq("7%-2", 1.0);
    try eq("-7%2", -1.0);
    try eq("2%2", 0.0);
    try eq("5.5%2", 1.5);
    try eq("7.5%-2.0", 1.5);
    try nan("5.5%0.0");
    try nan("5%0");
}

test "bitwise i64" {
    try eq("2^3", 1.0);
    try eq("2*3^1", 7.0);
    try eq("2^3*2", 4.0);
    try eq("1+2^3", 0.0);
    try eq("2^3==1", 2.0);
    try eq("2^3!=1", 3.0);
    try eq("2^3<2", 2.0);
    try eq("5&3", 1.0);
    try eq("5|3", 7.0);
    try eq("1&1", 1.0);
    try eq("1|0", 1.0);
    try eq("7&7", 7.0);
    try eq("7|0", 7.0);
    try eq("15^15", 0.0);
    try eq("5|3&1", 5.0);
    try eq("2&3^1", 3.0);
    try eq("5^3|1", 7.0);
    try eq("1<<2&3", 0.0);
    try eq("2+3&1", 1.0);
    try eq("2+3|1", 5.0);
    try eq("5&3==1", 0.0);
    try eq("3.5^0", 4.0);
    try eq("2.5^0", 2.0);
    try eq("2^2.0", 0.0);
    try eq("0.5^2", 2.0);
    try eq("1<<3", 8.0);
    try eq("8>>2", 2.0);
    try eq("1<<64", 1.0);
    try eq("1>>64", 1.0);
    try eq("64>>1", 32.0);
    try eq("3<<2+1", 24.0);
    try eq("-1<<1", -2.0);
    try eq("1<<-1", -9223372036854775808.0);
    try eq("true&false", 0.0);
    try eq("true|false", 1.0);
}

test "power right assoc tighter than unary" {
    try eq("2**3**2", 512.0);
    try eq("-2**2", -4.0);
    try eq("2*3**2", 18.0);
    try eq("2**-1", 0.5);
    try eq("(-2)**2", 4.0);
    try eq("Pow(2,3)", 8.0);
    try eq("POW(2,3)", 8.0);
    try eq("Pow(2.0,3)", 8.0);
    try fail("Pow(2)");
}

test "comparisons logic ternary" {
    try eq("1>2", 0.0);
    try eq("1==1", 1.0);
    try eq("1!=1", 0.0);
    try eq("!(1>2)", 1.0);
    try eq("1>0 && 2>1", 1.0);
    try eq("1>2 || 2>1", 1.0);
    try eq("1<2==true", 1.0);
    try eq("3<2==false", 1.0);
    try eq("5%2==1", 1.0);
    try eq("1 and 0", 0.0);
    try eq("not true", 0.0);
    try eq("True OR False", 1.0);
    try eq("TRUE", 1.0);
    try eq("FALSE", 0.0);
    try eq("true", 1.0);
    try eq("1|0&&0", 0.0);
    try eq("2>1 ? 10 : 20", 10.0);
    try eq("1>2 ? 10 : 20", 20.0);
    try eq("true?1:2", 1.0);
    try eq("1?2:3", 2.0);
    try eq("2.5?1:2", 1.0);
    try eq("!5", 0.0);
    try eq("!0", 1.0);
    try eq("1&&2", 1.0);
    try eq("true>0", 1.0);
    try eq("1>true", 0.0);
    try eq("2==true", 0.0);
    try eq("0==false", 1.0);
}

test "floats" {
    try eq(".5+.5", 1.0);
    try eq("5.+1", 6.0);
    try eq("1e3", 1000.0);
    try eq("1E-3", 0.001);
    try eq("2*0.5", 1.0);
    try eq("2*-0.5", -1.0);
    try eq("0.1+0.2", 0.30000000000000004);
    try eq("1e-3*1000", 1.0);
    try eq("1/3", 0.3333333333333333);
    try eq("PI*2", 6.283185307179586);
    try eq("E*E", 7.3890560989306495);
    try eq("Sqrt(4)", 2.0);
    try eq("SQRT(4)", 2.0);
    try eq("Sin(0)", 0.0);
    try eq("Cos(0)", 1.0);
    try eq("Tan(0)", 0.0);
    try eq("Exp(0)", 1.0);
    try nan("0.0/0.0");
    try nan("Sqrt(-1)");
    try nan("Log(0)");
    try nan("Log(-1)");
    try eq("Log(10)", 2.302585092994046);
}

test "transcendentals" {
    try tol("Sin(1)", 0.8414709848078965, 1e-15);
    try tol("Cos(1)", 0.5403023058681398, 1e-15);
    try tol("Tan(1)", 1.5574077246549023, 1e-15);
    try tol("Asin(1)", 1.5707963267948966, 1e-15);
    try tol("Acos(0)", 1.5707963267948966, 1e-15);
    try tol("Atan(1)", 0.7853981633974483, 1e-15);
    try tol("Atan2(1,1)", 0.7853981633974483, 1e-15);
    try tol("Exp(1)", 2.718281828459045, 1e-15);
    try tol("Ln(10)", 2.302585092994046, 1e-15);
    try tol("Log10(100)", 2.0, 1e-15);
    try tol("Log(100,10)", 2.0, 1e-15);
    try tol("Sqrt(2)*Sqrt(2)", 2.0000000000000004, 1e-15);
    try tol("IEEERemainder(5,2)", 1.0, 1e-15);
}

test "floor ceil trunc abs sign min max" {
    try eq("Floor(2.9)", 2.0);
    try eq("Ceiling(2.1)", 3.0);
    try eq("Truncate(2.9)", 2.0);
    try eq("Floor(-2.1)", -3.0);
    try eq("Ceiling(-2.1)", -2.0);
    try eq("Truncate(-2.9)", -2.0);
    try eq("Abs(-5)", 5.0);
    try eq("Abs(-5.5)", 5.5);
    try eq("Abs(2.5)", 2.5);
    try eq("Abs(0)", 0.0);
    try eq("Abs(true)", 1.0);
    try eq("Sign(-5)", -1.0);
    try eq("Sign(0)", 0.0);
    try eq("Sign(2.5)", 1.0);
    try eq("Sign(0.0)", 0.0);
    try eq("Sign(true)", 1.0);
    try eq("Max(1,2)", 2.0);
    try eq("Max(2,1)", 2.0);
    try eq("Min(1,2)", 1.0);
    try eq("Min(2.5,3)", 2.5);
    try eq("Max(1.0,2)", 2.0);
    try eq("Max(2.5,2)", 2.5);
    try eq("Min(1.5,2)", 1.5);
    try eq("Max(2.5,(1^3))", 2.5);
    try eq("Min(2.5,(1^3))", 2.0);
    try eq("Min((1^3),2.5)", 2.0);
    try eq("Max((1^3),2.5)", 2.5);
    try eq("Max(1,true)", 1.0);
    try eq("Min(1,false)", 0.0);
    try nan("Max(PI,Sqrt(-2))");
    try nan("Min(Sqrt(-2),1)");
    try fail("Max(1,2,3)");
    try fail("Min(1,2,3)");
}

test "round half to even on exact ties" {
    try eq("Round(2.5,0)", 2.0);
    try eq("Round(-2.5,0)", -2.0);
    try eq("Round(3.5,0)", 4.0);
    try eq("Round(2.675,2)", 2.68);
    try eq("Round(1000,false)", 1000.0);
    try eq("Round(2.5,true)", 2.5);
    try fail("Round(2.5)");
}

test "consts case" {
    try eq("PI", 3.141592653589793);
    try eq("E", 2.718281828459045);
    try eq("pi", 3.141592653589793);
    try eq("e", 2.718281828459045);
    try eq("Pi", 3.141592653589793);
}

test "mixed-type sanity" {
    try eq("(1^3)/2", 1.0);
    try eq("7/2.0", 3.5);
    try eq("7.0/2", 3.5);
    try eq("(1^3)+1", 3.0);
    try eq("(1^3)+2.5", 4.5);
    try eq("-(1^3)", -2.0);
    try eq("(1^3)>1", 1.0);
    try eq("(1^3)>-1", 1.0);
    try eq("(1^3)==-1", 0.0);
    try eq("(1^3)>2.5", 0.0);
    try eq("(1^3)==2", 1.0);
    try eq("(1^3)==true", 0.0);
    try eq("1.5==true", 0.0);
    try eq("true==2.5", 0.0);
    try eq("(1^3)?1:2", 1.0);
    try eq("(1^3)+(-5)", -3.0);
    try eq("(1^3)+true", 3.0);
    try eq("(1^3)>-0.5", 1.0);
    try eq("(1^3)==2.5", 0.0);
    try eq("true^true", 0.0);
    try eq("false^100", 100.0);
    try eq("true&1", 1.0);
    try eq("1&true", 1.0);
    try eq("true|true", 1.0);
    try eq("(1^3)%2", 0.0);
    try eq("10%(1^3)", 0.0);
    try eq("(7^0)%2.5", 2.0);
    try eq("10%2.5", 0.0);
    try eq("(1^3)/0", std.math.inf(f64));
    try eq("(1^3)/(0-2)", -1.0);
    try eq("(1^3)/2.5", 0.8);
    try eq("1<<3000000000", 1.0);
    try eq("(1^3)/0.0", std.math.inf(f64));
    try eq("(1^3)%(0-2)", 0.0);
    try eq("Pow(2,true)", 2.0);
    try eq("2**-PI", 0.11331473229676088);
    try eq("2**--1", 2.0);
    try eq("+-5", -5.0);
    try eq("+5", 5.0);
    try eq("2*+3", 6.0);
    try eq("+(3)", 3.0);
    try fail("2**not true");
}

test "declines" {
    try fail("");
    try fail("   ");
    try fail("Tanh(0)");
    try fail("Cosh(0)");
    try fail("Sinh(0)");
    try fail("2 in (1,2,3)");
    try fail("foo(1)");
    try fail("Sin()");
    try fail("Sin(1,2)");
    try fail("2+");
    try fail("*2");
    try fail("(2+3");
    try fail("2+3)");
    try fail("1,2");
    try fail("'a'");
    try fail("0x");
    try fail("1e");
    try fail("()");
}

test "variable t and Math prefix" {
    const h = expr.exprCompile("50 + 40 * Math.sin(t * 2)");
    try std.testing.expect(h != 0);
    defer expr.exprFree(h);
    const r0 = expr.exprEvalT(h, 0.0);
    try std.testing.expectEqual(50.0, r0);
    const r_pi4 = expr.exprEvalT(h, std.math.pi / 4.0);
    try std.testing.expectApproxEqAbs(@as(f64, 90.0), r_pi4, 1e-9);

    try eq("Math.PI", std.math.pi);
    try eq("Math.E", std.math.e);
    try eq("Math.cos(0)", 1.0);
    try eq("Math.abs(-42)", 42.0);
    try eq("Math.round(1.5, 0)", 2.0);
    try eq("Math.round(2.5, 0)", 2.0);
    try eq("Math.round(1.234, 2)", 1.23);
}

test "variables v0..v31" {
    const h = expr.exprCompile("v0 * 2 + v1 - v2 / 2");
    try std.testing.expect(h != 0);
    defer expr.exprFree(h);
    const vars = [_]f64{ 10.0, 5.0, 6.0 };
    const r = expr.exprEvalVars(h, 0.0, &vars);
    try std.testing.expectEqual(22.0, r);

    const h2 = expr.exprCompile("Math.sin(t) * v0 + 10");
    try std.testing.expect(h2 != 0);
    defer expr.exprFree(h2);
    const vars2 = [_]f64{50.0};
    const r2 = expr.exprEvalVars(h2, std.math.pi / 2.0, &vars2);
    try std.testing.expectApproxEqAbs(@as(f64, 60.0), r2, 1e-9);
}
