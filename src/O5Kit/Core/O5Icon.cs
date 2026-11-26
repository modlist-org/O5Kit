#nullable enable
using System;

namespace O5Kit.Core;

/// <summary>
/// A monochrome vector icon represented by a glyph inside a TextMeshPro font asset.
/// </summary>
public readonly struct O5Icon : IEquatable<O5Icon> {
    public static O5Icon None => default;

    public TMPro.TMP_FontAsset? Font { get; }
    public char Glyph { get; }

    public O5Icon(TMPro.TMP_FontAsset? font, char glyph) {
        Font = font;
        Glyph = glyph;
    }

    public bool IsValid => Font != null && Glyph != '\0';
    public string GlyphString => IsValid ? Glyph.ToString() : string.Empty;

    public bool Equals(O5Icon other) => ReferenceEquals(Font, other.Font) && Glyph == other.Glyph;
    public override bool Equals(object? obj) => obj is O5Icon other && Equals(other);
    public override int GetHashCode() => unchecked(((Font?.GetHashCode() ?? 0) * 397) ^ Glyph.GetHashCode());
    public static bool operator ==(O5Icon left, O5Icon right) => left.Equals(right);
    public static bool operator !=(O5Icon left, O5Icon right) => !left.Equals(right);
}
