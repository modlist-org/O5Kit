// SPDX-License-Identifier: LGPL-2.1-or-later

using UnityEngine;

namespace O5Kit.Input.OS;

/// <summary>OS-level cursor get/set (used for infinite-drag warping).</summary>
public abstract class OsApi {
    /// <summary>Moves the OS cursor to native coordinates.</summary>
    /// <param name="x">Native X (origin top-left).</param>
    /// <param name="y">Native Y (origin top-left).</param>
    public abstract void SetCursorPosition(int x, int y);

    /// <summary>Reads the OS cursor in native coordinates.</summary>
    public abstract Vector2Int GetCursorPosition();
}
