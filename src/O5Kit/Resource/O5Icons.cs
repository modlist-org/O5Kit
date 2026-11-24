#nullable enable
using O5Kit.Core;

namespace O5Kit.Resource;

/// <summary>
/// Unicode Private Use Area codepoints compiled into the embedded <c>O5KitIcons.ttf</c> font.
/// </summary>
public static class O5Icons {
    public const char Circle = '\uE001';
    public const char ToggleCircle = '\uE002';
    public const char Triangle = '\uE003';
    public const char X = '\uE004';

    public static O5Icon For(IFontProvider? fonts, char glyph) => new(fonts?.Icons, glyph);
    public static O5Icon FromContext(O5Context ctx, char glyph) => new(ctx.Fonts.Icons, glyph);
}
