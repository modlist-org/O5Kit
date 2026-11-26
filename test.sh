#!/usr/bin/env bash
#
# Runs the O5Kit unit tests without a game install.
#   ./test.sh [args...]
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJ="$ROOT/tests/O5Kit.Tests/O5Kit.Tests.csproj"

dotnet run --project "$PROJ" -c Release -- "$@"
