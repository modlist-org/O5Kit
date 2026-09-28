// SPDX-License-Identifier: LGPL-3.0-or-later

using UnityEngine;

namespace O5Kit.Core;

/// <summary>9-slice shape of one sprite role: sampled border plus pixels-per-unit.</summary>
/// <param name="Ppu">Pixels per unit. Larger values render thinner slice borders.</param>
/// <param name="Border">Border in pixels (x=left, y=bottom, z=right, w=top).</param>
public sealed record O5Slice(float Ppu, Vector4 Border);

/// <summary>Sprite slicing style for <see cref="DefaultSpriteProvider"/>. Defaults match Overlayer's slicer so standalone O5Kit renders the same look. Override per role (or the whole record) for your own art.</summary>
public sealed record O5SpriteStyle {
    /// <summary>Large rounded panel background.</summary>
    public O5Slice Panel { get; init; } = new(1024f, new Vector4(128f, 128f, 128f, 128f));

    /// <summary>Small rounded control background (rows, list items).</summary>
    public O5Slice Control { get; init; } = new(2048f, new Vector4(128f, 128f, 128f, 128f));

    /// <summary>Rounded-top bar background. Bottom border stays 0 for top-bar art.</summary>
    public O5Slice TopBar { get; init; } = new(1024f, new Vector4(128f, 0f, 128f, 128f));

    /// <summary>Rounded outline used for hover rings and window borders.</summary>
    public O5Slice Outline { get; init; } = new(2048f, new Vector4(128f, 128f, 128f, 128f));

    /// <summary>Pixels per unit for plain icons and fills. Matches Overlayer's simple sprites.</summary>
    public float SimplePpu { get; init; } = 100f;

    /// <summary>Default style (Overlayer values).</summary>
    public static O5SpriteStyle Default { get; } = new();
}
