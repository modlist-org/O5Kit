// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace O5Kit.Native;

internal static unsafe class O5Expr {
    public static bool IsSupported => EnsureApi();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate long CompileFn(byte* src, UIntPtr len);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate double EvalTFn(long handle, double t);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate double EvalVarsFn(long handle, double t, double* vars, UIntPtr varCount);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void FreeFn(long handle);

    private static readonly object apiLock = new object();
    private static bool apiResolved;
    private static CompileFn? compileFn;
    private static EvalTFn? evalTFn;
    private static EvalVarsFn? evalVarsFn;
    private static FreeFn? freeFn;

    /// <summary>Resolves all entry points from the version-checked library handle. All-or-nothing.</summary>
    private static bool EnsureApi() {
        if (!O5Native.IsAvailable) {
            return false;
        }
        if (apiResolved) {
            return compileFn != null;
        }
        lock (apiLock) {
            if (!apiResolved) {
                IntPtr h = O5Native.Handle;
                var compile = Resolve<CompileFn>(h, "o5_expr_compile");
                var evalT = Resolve<EvalTFn>(h, "o5_expr_eval_t");
                var evalVars = Resolve<EvalVarsFn>(h, "o5_expr_eval_vars");
                var free = Resolve<FreeFn>(h, "o5_expr_free");
                if (compile != null && evalT != null && evalVars != null && free != null) {
                    compileFn = compile;
                    evalTFn = evalT;
                    evalVarsFn = evalVars;
                    freeFn = free;
                }
                apiResolved = true;
            }
            return compileFn != null;
        }
    }

    private static T? Resolve<T>(IntPtr h, string name) where T : class {
        try {
            IntPtr sym = O5Native.GetSymbol(h, name);
            if (sym == IntPtr.Zero) {
                return null;
            }
            return Marshal.GetDelegateForFunctionPointer(sym, typeof(T)) as T;
        } catch {
            return null;
        }
    }

    public static long Compile(string source) {
        if (string.IsNullOrEmpty(source) || !EnsureApi()) {
            return 0;
        }
        try {
            byte[] bytes = Encoding.UTF8.GetBytes(source);
            fixed (byte* p = bytes) {
                return compileFn!(p, (UIntPtr)bytes.Length);
            }
        } catch {
            return 0;
        }
    }

    public static bool TryEval(long handle, out double value) => TryEval(handle, 0.0, out value);

    public static bool TryEval(long handle, double t, out double value) {
        value = 0;
        if (handle == 0 || !EnsureApi()) {
            return false;
        }
        try {
            value = evalTFn!(handle, t);
            return true;
        } catch {
            value = 0;
            return false;
        }
    }

    public static bool TryEval(long handle, double t, ReadOnlySpan<double> vars, out double value) {
        value = 0;
        if (handle == 0 || !EnsureApi()) {
            return false;
        }
        try {
            fixed (double* p = vars) {
                value = evalVarsFn!(handle, t, p, (UIntPtr)vars.Length);
            }
            return true;
        } catch {
            value = 0;
            return false;
        }
    }

    public static bool TryEval(long handle, double t, double* vars, nuint varCount, out double value) {
        value = 0;
        if (handle == 0 || !EnsureApi()) {
            return false;
        }
        try {
            value = evalVarsFn!(handle, t, vars, (UIntPtr)varCount);
            return true;
        } catch {
            value = 0;
            return false;
        }
    }

    public static void Free(long handle) {
        if (handle == 0 || !EnsureApi()) {
            return;
        }
        try {
            freeFn!(handle);
        } catch {
        }
    }
}
