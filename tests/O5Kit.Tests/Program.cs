// SPDX-License-Identifier: LGPL-3.0-or-later

using O5Kit.Tests;

// Native probing happens on first O5Kit.Native access. Point it at the repo's
// freshly built library so the native path is exercised for real. With
// --force-fallback the managed fallback path is exercised instead.
static class Program {
    static int Main(string[] args) {
        bool forceFallback = args.Contains("--force-fallback");
        if (forceFallback) {
            Environment.SetEnvironmentVariable("O5NATIVE_DIR", "/nonexistent-o5kit-test");
            EvalTests.ExpectFallback = true;
        } else {
            string root = RepoRoot();
            string lib = Path.Combine(root, "native", "zig-out", "lib");
            if (Directory.Exists(lib)) {
                Environment.SetEnvironmentVariable("O5NATIVE_DIR", lib);
            }
        }

        EvalTests.Detect();
        EvalTests.Run();
        ScaleTests.Run();
        CompatTests.Run();
        OptionsTests.Run();
        ThemeTests.Run();
        PaletteTests.Run();
        ConfigTests.Run();
        return T.Report();
    }

    static string RepoRoot() {
        // tests/O5Kit.Tests/bin/<cfg>/<tfm> -> up 5 = repo root
        string dir = AppContext.BaseDirectory;
        for (int i = 0; i < 5; i++) {
            dir = Path.GetDirectoryName(dir) ?? dir;
        }
        return dir;
    }
}
