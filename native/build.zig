const std = @import("std");

pub fn build(b: *std.Build) void {
    const target = b.standardTargetOptions(.{});
    const optimize = b.standardOptimizeOption(.{ .preferred_optimize_mode = .ReleaseFast });
    const mod = b.createModule(.{
        .root_source_file = b.path("src/root.zig"),
        .target = target,
        .optimize = optimize,
        .link_libc = true,
    });
    const lib = b.addLibrary(.{
        .name = "o5native",
        .root_module = mod,
        .linkage = .dynamic,
    });
    b.installArtifact(lib);

    const test_step = b.step("test", "Run unit tests");

    const expr_test_mod = b.createModule(.{
        .root_source_file = b.path("src/expr_test.zig"),
        .target = target,
        .optimize = optimize,
        .link_libc = true,
    });
    const expr_tests = b.addTest(.{
        .name = "expr-test",
        .root_module = expr_test_mod,
    });
    const run_expr_tests = b.addRunArtifact(expr_tests);
    test_step.dependOn(&run_expr_tests.step);

    const ring_test_mod = b.createModule(.{
        .root_source_file = b.path("src/ring_test.zig"),
        .target = target,
        .optimize = optimize,
        .link_libc = true,
    });
    const ring_tests = b.addTest(.{
        .name = "ring-test",
        .root_module = ring_test_mod,
    });
    const run_ring_tests = b.addRunArtifact(ring_tests);
    test_step.dependOn(&run_ring_tests.step);

    const tag_test_mod = b.createModule(.{
        .root_source_file = b.path("src/tag_test.zig"),
        .target = target,
        .optimize = optimize,
        .link_libc = true,
    });
    const tag_tests = b.addTest(.{
        .name = "tag-test",
        .root_module = tag_test_mod,
    });
    const run_tag_tests = b.addRunArtifact(tag_tests);
    test_step.dependOn(&run_tag_tests.step);

    const fmt_test_mod = b.createModule(.{
        .root_source_file = b.path("src/fmt_test.zig"),
        .target = target,
        .optimize = optimize,
        .link_libc = true,
    });
    const fmt_tests = b.addTest(.{
        .name = "fmt-test",
        .root_module = fmt_test_mod,
    });
    const run_fmt_tests = b.addRunArtifact(fmt_tests);
    test_step.dependOn(&run_fmt_tests.step);
}
