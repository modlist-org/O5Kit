#!/usr/bin/env bash
set -euo pipefail
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TARGET="${1:-}"
if [[ -n "$TARGET" ]]; then
    zig build --release=fast -Dtarget="$TARGET" --prefix "$HERE/zig-out"
    TRIPLE="$TARGET"
else
    zig build --release=fast --prefix "$HERE/zig-out"
    TRIPLE="$(uname -s)"
fi

OS="$(echo "$TRIPLE" | cut -d- -f2 2>/dev/null || true)"
[[ -z "$OS" ]] && OS="$TRIPLE"
OS="$(echo "$OS" | tr '[:upper:]' '[:lower:]')"
case "$OS" in
    linux*) OS="linux"; EXT="so"; SRC="libo5native.so" ;;
    windows*|mingw*) OS="win"; EXT="dll"; SRC="o5native.dll" ;;
    darwin*|macos*) OS="mac"; EXT="dylib"; SRC="libo5native.dylib" ;;
    *) echo "unknown os in $TRIPLE" >&2; exit 1 ;;
esac

mv -f "$HERE/zig-out/lib/$SRC" "$HERE/zig-out/lib/O5KitNative.$OS.$EXT"
ls "$HERE/zig-out/lib"
