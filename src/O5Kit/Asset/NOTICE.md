# O5Kit bundled artwork — Overlayer team (Free to use)

All UI shapes in `Image/` (`Circle128.png`, `CircleHalf128.png`, `CircleOutline128O32.png`, and the legacy unreferenced files) and monochrome icon vector SVGs in `Icons/` (`Circle.svg`, `ToggleCircle.svg`, `Triangle.svg`, `X.svg`, compiled into `O5KitIcons.ttf` at build time) were created directly by the Overlayer team. Because they are simple geometric shapes, they are free for anyone to use, modify, or redistribute for any purpose without restriction.

Attribution is not required, though crediting the Overlayer team is greatly appreciated!

No game assets are required. O5Kit's default provider loads these assets directly; Overlayer also consumes these shared base shapes from O5Kit. Mod-specific artwork (such as Overlayer's logo and navigation icons) remains in the consuming mod's own assembly.

# O5Kit bundled fonts — OFL-1.1

Font files in `Font/`:

- `SUIT-Regular.otf`, `SUIT-Medium.otf` (body font)
- `JetBrainsMonoNL-Regular.ttf`, `JetBrainsMonoNL-Medium.ttf` (monospace)

SUIT and JetBrains Mono are licensed under the SIL Open Font License 1.1
(OFL-1.1). The full licence texts are in `Font/OFL-SUIT.txt` and
`Font/OFL-JetBrainsMono.txt` and are embedded in the O5Kit DLL beside the fonts. Loaded by `O5Kit.Core.DefaultFontProvider`; consuming mods may
bundle their own font defaults independently. No game assets are required.
