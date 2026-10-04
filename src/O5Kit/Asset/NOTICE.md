# O5Kit bundled artwork — modlist-org shared art

The base UI shapes and control icons in `Image/` are modlist-org's own
artwork. Their canonical copy lives here in O5Kit under O5Kit's license
terms (LGPL-3.0-or-later, same as the library). O5Kit's default provider
loads these assets directly; Overlayer also consumes these shared base
assets from O5Kit. Mod-specific artwork (such as Overlayer's logo and
navigation icons) remains in the consuming mod's own assembly. No game
assets are required.

# O5Kit bundled fonts — OFL-1.1

Font files in `Font/`:

- `SUIT-Regular.otf`, `SUIT-Medium.otf` (body font)
- `JetBrainsMonoNL-Regular.ttf`, `JetBrainsMonoNL-Medium.ttf` (monospace)

SUIT and JetBrains Mono are licensed under the SIL Open Font License 1.1
(OFL-1.1). Loaded by `O5Kit.Core.DefaultFontProvider`; consuming mods may
bundle their own font defaults independently. No game assets are required.
