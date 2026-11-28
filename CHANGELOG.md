# Changelog

The assembly name follows `major.minor` (`O5Kit.1.0`, `O5Kit.1.1`, ...), so each minor release loads side by side with older ones and existing binaries keep working. A minor release may still break source compatibility: read the migration notes below before recompiling a mod against a newer minor.

## 1.1.0

### Breaking changes (source)

- **Icons are TMP glyphs instead of sprites.** Monochrome icons come from a build-time icon font (`O5KitIcons.ttf`) rather than PNGs.
  - `IFontProvider` gains `TMP_FontAsset? Icons { get; }`. Custom providers must implement it (return `null` to disable glyph icons).
  - `ISpriteProvider.Icon(string)` is removed; use `O5Icons.For(fonts, glyph)` / `O5Icons.FromContext(ctx, glyph)`.
  - `O5ButtonOptions.Icon` and `O5InputOptions.Icon` are `O5Icon?` instead of `Sprite`.
  - `O5Button.Icon`, `O5InputField.Icon`, `O5Toggle.CircleIcon`, `O5Dropdown<T>.TriangleIcon`, `O5MultiDropdown<T>.TriangleIcon` are `TextMeshProUGUI` (replace `Icon`/`IconImage`, `CircleImage`, `TriangleImage`).
- **Bundled shapes are 128 px.** `O5Asset.Circle256`, `CircleHalf256`, `CircleOutline256O64` are renamed to `Circle128`, `CircleHalf128`, `CircleOutline128O32` (the old names remain as `[Obsolete]` aliases). `ToggleCircle128`, `Triangle128`, `X128` are removed (now icon glyphs).
- **Constructor and method signatures:** `O5Context` and `DefaultSpriteProvider` take an optional `O5ShapeSprites`; control constructors take TMP icon elements; `ResizeHandle.CreateResizeHandles` returns the frame `RectTransform`; `O5PageSwitcher.SwitchPage` and `O5Dropdown<T>.SetExpanded` gain optional parameters. Callers using named or default arguments only need a recompile.
- **Slider clamping:** `O5Slider.Set`, `SetNormalized` and the initial value clamp by `ClampInput` (text-input range) instead of `ClampDrag`. Drags and wheel steps still clamp by `ClampDrag`.
- **Mono target is `netstandard2.0`** (Unity 2020.3+). Build against netstandard 2.0 reference assemblies; Unity 2021.2+ `Managed/` folders need retargeting (`$(GamePath)/CompatManaged` from `o5dev`, or `ManagedRefPath`).

### Behaviour changes

- Scroll views accept wheel and right-drag over their padding; left-press in the right padding drag-scrolls. Shift-drag scrolls touch-style with fling.
- Window resize handles sit fully outside the panel edge and use the hardware cursor.
- Controls light a change bar when their value changes from outside.
- `O5Theme` presets are recoloured; `O5Theme.OverlayerStatic` is added.

### Added

- `O5ColorSpace`: Linear color-space UI parity (wired automatically by `O5Canvas`).
- `O5ShapeSprites`: rounded 9-slice sprites rasterized at the live canvas scale.
- `O5Palette`: OKLCH conversion, sRGB gamut clipping, monochrome theme derivation. Color picker OKLCH mode.
- Slider wheel adjustment, per-slider `AccentColor`, error shake; dropdown wheel selection and animated index indicator; button press/release animation.
- `O5Config.TooltipOpacity`, `O5Config.ScrollEasing`, `UIScrollController.DragScrolling`, `O5Context.IsDragBlocking`.
- IL2CPP: TTF/OTF font loading, automatic `[RegisterTypeInIl2Cpp]` registration in `new O5Context(...)`, interop helpers in `UnityUtils`.

### Fixed

- Controls created without `Dispose()` are no longer kept alive by their context's theme event.
- Shape sprites rasterized for a previous canvas scale are freed once no `Image` uses them.
- Failed font loads are cached instead of retried on every access.
- Hidden sliders and dropdowns no longer react to the mouse wheel after a page switch.
- Slider drag no longer has a dead zone when the input range is narrower than the drag range.
- Debug logging removed from slider drag.
- Assorted IL2CPP GC/cast fixes, TMP 3.0 slider padding, `SetBlocked` on destroyed objects at shutdown, IME composition while an input is focused.
- Bundled font OFL-1.1 licence texts are now shipped (embedded beside the fonts).
