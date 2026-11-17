// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace O5Kit.Native;

internal static class O5Native {
    /// <summary>Must match o5_abi_version_current in native/src/root.zig. The native ABI is frozen: additive only.</summary>
    public const int AbiVersion = 1;

    private static readonly IntPtr handle = TryLoad();

    public static bool IsAvailable => handle != IntPtr.Zero;

    internal static IntPtr Handle => handle;

    private static IntPtr TryLoad() {
        try {
            foreach (string path in CandidatePaths()) {
                try {
                    if (string.IsNullOrEmpty(path) || !File.Exists(path)) {
                        continue;
                    }
                    IntPtr h = Open(path);
                    if (h == IntPtr.Zero) {
                        continue;
                    }
                    // Version gate: managed copies of any age share one loaded
                    // library (first-win), so a mismatched native must be skipped
                    // instead of bound. A compatible copy may exist at another path.
                    if (CheckAbiVersion(h)) {
                        return h;
                    }
                } catch {
                }
            }
        } catch {
        }
        return IntPtr.Zero;
    }

    private static bool CheckAbiVersion(IntPtr h) {
        try {
            IntPtr sym = GetSymbol(h, "o5_abi_version");
            if (sym == IntPtr.Zero) {
                return false;
            }
            var fn = Marshal.GetDelegateForFunctionPointer(sym, typeof(O5AbiVersionFn)) as O5AbiVersionFn;
            if (fn == null) {
                return false;
            }
            return fn() == AbiVersion;
        } catch {
            return false;
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int O5AbiVersionFn();

    /// <summary>Resolves an exported symbol from a library opened via <see cref="Open"/>.</summary>
    internal static IntPtr GetSymbol(IntPtr h, string name) {
        if (h == IntPtr.Zero || string.IsNullOrEmpty(name)) {
            return IntPtr.Zero;
        }
        try {
            if (Path.DirectorySeparatorChar == '\\') {
                return GetProcAddress(h, name);
            }
            IntPtr p = DlsymLibDl2(h, name);
            if (p != IntPtr.Zero) {
                return p;
            }
            return DlsymLibDl(h, name);
        } catch {
            return IntPtr.Zero;
        }
    }

    private static IEnumerable<string> CandidatePaths() {
        var paths = new List<string>();
        void Add(string? path) {
            try {
                if (!string.IsNullOrEmpty(path) && !paths.Contains(path)) {
                    paths.Add(path);
                }
            } catch {
            }
        }

        try {
            Add(Environment.GetEnvironmentVariable("O5NATIVE_PATH"));
        } catch {
        }

        try {
            string? dir = Environment.GetEnvironmentVariable("O5NATIVE_DIR");
            if (!string.IsNullOrEmpty(dir)) {
                Add(Path.Combine(dir, FileName()));
            }
        } catch {
        }

        string? self = null;
        try {
            self = typeof(O5Native).Assembly.Location;
            if (string.IsNullOrEmpty(self)) {
                self = new Uri(typeof(O5Native).Assembly.CodeBase).LocalPath;
            }
        } catch {
        }
        if (!string.IsNullOrEmpty(self)) {
            string? selfDir = null;
            try {
                selfDir = Path.GetDirectoryName(self);
            } catch {
            }
            if (!string.IsNullOrEmpty(selfDir)) {
                Add(Path.Combine(selfDir, FileName()));
                try {
                    string? parent = Path.GetDirectoryName(selfDir);
                    if (!string.IsNullOrEmpty(parent)) {
                        Add(Path.Combine(parent, "UserLibs", FileName()));
                    }
                } catch {
                }
            }
        }

        try {
            string cwd = Directory.GetCurrentDirectory();
            if (!string.IsNullOrEmpty(cwd)) {
                Add(Path.Combine(cwd, "UserLibs", FileName()));
                Add(Path.Combine(cwd, "native", "zig-out", "lib", FileName()));
                Add(Path.Combine(cwd, "..", "native", "zig-out", "lib", FileName()));
                Add(Path.Combine(cwd, "..", "O5Kit", "native", "zig-out", "lib", FileName()));
                Add(Path.Combine(cwd, "..", "..", "O5Kit", "native", "zig-out", "lib", FileName()));
            }
        } catch {
        }

        try {
            string? baseDir = AppDomain.CurrentDomain.BaseDirectory;
            if (!string.IsNullOrEmpty(baseDir)) {
                Add(Path.Combine(baseDir, "UserLibs", FileName()));
                Add(Path.Combine(baseDir, "native", "zig-out", "lib", FileName()));
                Add(Path.Combine(baseDir, "..", "..", "..", "..", "..", "O5Kit", "native", "zig-out", "lib", FileName()));
                Add(Path.Combine(baseDir, "..", "..", "..", "O5Kit", "native", "zig-out", "lib", FileName()));
            }
        } catch {
        }

        return paths;
    }

    private static string FileName() {
        try {
            if (Path.DirectorySeparatorChar == '\\') {
                return "O5KitNative.win.dll";
            }
            if (File.Exists("/System/Library/CoreServices/SystemVersion.plist")) {
                return "O5KitNative.mac.dylib";
            }
        } catch {
        }
        return "O5KitNative.linux.so";
    }

    /// <summary>Opens a native library by full path and keeps the handle (never unloaded; process lifetime).</summary>
    internal static IntPtr Open(string fullPath) {
        try {
            if (Path.DirectorySeparatorChar == '\\') {
                return LoadLibraryW(fullPath);
            }
            const int RTLD_NOW = 2;
            const int RTLD_GLOBAL = 0x100;
            try {
                IntPtr h = DlopenLibDl2(fullPath, RTLD_NOW | RTLD_GLOBAL);
                if (h != IntPtr.Zero) {
                    return h;
                }
            } catch {
            }
            try {
                return DlopenLibDl(fullPath, RTLD_NOW | RTLD_GLOBAL);
            } catch {
            }
        } catch {
        }
        return IntPtr.Zero;
    }

    [DllImport("libdl.so.2", EntryPoint = "dlopen", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern IntPtr DlopenLibDl2(string fileName, int flags);

    [DllImport("libdl", EntryPoint = "dlopen", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern IntPtr DlopenLibDl(string fileName, int flags);

    [DllImport("libdl.so.2", EntryPoint = "dlsym", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern IntPtr DlsymLibDl2(IntPtr handle, string symbol);

    [DllImport("libdl", EntryPoint = "dlsym", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern IntPtr DlsymLibDl(IntPtr handle, string symbol);

    [DllImport("kernel32", EntryPoint = "LoadLibraryW", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr LoadLibraryW(string lpFileName);

    [DllImport("kernel32", EntryPoint = "GetProcAddress", SetLastError = true, CharSet = CharSet.Ansi)]
    private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
}
