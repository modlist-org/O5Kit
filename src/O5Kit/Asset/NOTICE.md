# O5Kit bundled artwork — modlist-org shared art

The UI shapes and icons in `Image/` are modlist-org's own artwork.
The canonical copy lives here in O5Kit under O5Kit's license terms
(LGPL-3.0-or-later, same as the library); Overlayer consumes them from
O5Kit instead of duplicating the files. Loaded by
`O5Kit.Core.DefaultSpriteProvider` with Overlayer's slice table; no game
assets required.

# O5Kit bundled fonts — OFL-1.1

Font files in `Fonts/`:

- `SUIT-Regular.otf`, `SUIT-Medium.otf` (body font)
- `JetBrainsMonoNL-Regular.ttf`, `JetBrainsMonoNL-Medium.ttf` (monospace)

SUIT and JetBrains Mono are licensed under the SIL Open Font License 1.1
(OFL-1.1). Loaded by `O5Kit.Core.DefaultFontProvider`; no game assets required.
