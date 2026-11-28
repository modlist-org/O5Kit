# <div align="center">O5Kit</div>

<div align="center">
    <img src="o5kit_logo.svg" height="256" alt="O5Kit Logo">
</div>

<div align="center">

### ⟡ Pretty controls for every mod. ⟡

</div>

<div align="center">

Pretty Unity uGUI + input toolkit for mods, extracted from Overlayer v5!

</div>

> [!TIP]
> Using an AI coding agent with O5Kit? Point it at [`llms.txt`](llms.txt) (consumer API + copy-paste mod template) and [`AGENTS.md`](AGENTS.md) (build/IL2CPP invariants).

# 🛠️ Installation

## 📦 From source

1. Clone O5Kit (with submodules if building optional LitMotion support):
   ```bash
   git clone --recurse-submodules <this-repo-url>
   ```
2. Copy [`Directory.Build.example.props`](Directory.Build.example.props) to `Directory.Build.props` and fill in your game or sibling [`O5Dev`](https://github.com/modlist-org/O5Dev) paths (`GamePath`/`GameData`, and `Il2CppPath`/`Il2CppData` for IL2CPP).
3. Build the configuration that matches your game (emits `O5Kit.{major}.{minor}.dll` and XML docs `O5Kit.{major}.{minor}.xml`, e.g. `O5Kit.1.1.dll`):
   ```bash
   dotnet build src/O5Kit/O5Kit.csproj -c Release_Mono
   dotnet build src/O5Kit/O5Kit.csproj -c Release_IL2CPP
   ```
4. Unit tests (no Unity player needed; uses `UnityEngine.CoreModule.dll` from the `GamePath` set in step 2 for type layout only):
   ```bash
   ./test.sh
   ```
5. Optional native math library (needs `zig` 0.17+): `./native/build.sh` (`./native/build.sh x86_64-windows` to cross). Output lands in `native/zig-out/lib` as `O5KitNative.<os>.<ext>`. The native C ABI (`o5_*`) is frozen — additive only, gated by `o5_abi_version` — because all mods share one loaded copy (first-win). Managed binding resolves symbols explicitly from the loaded handle and falls back to C# when unavailable.

# ⌨️ Usage
### Setup
* **Create an isolated `O5Context`:** Every consumer owns its own context (no shared statics across mods). Bundled sprites, scale-aware shape rasterizer, SUIT/JetBrains Mono fonts, and GTweens fallback work out of the box when optional parameters are omitted:
  ```csharp
  var ctx = new O5Kit.Core.O5Context(
      config: new O5Kit.Core.O5Config { UIScale = 1f },
      theme: O5Kit.Core.O5Theme.Overlayer // or O5Theme.Dark / O5Theme.Light
  );
  ```
* **Theme:** swap presets or derive single-hue monochrome variants with `O5Palette.FromMonochrome(...)`:
  ```csharp
  ctx.SetTheme(O5Kit.Core.O5Theme.Dark with { PanelBG = new Color(0.1f, 0.1f, 0.15f, 1f) });
  ```
  Live controls update via `ThemeTransitioned` / `ThemeChanged`.
* **Fonts:** load custom font files (missing slots fall back gracefully):
  ```csharp
  var ctx = new O5Kit.Core.O5Context(fonts: new O5Kit.Core.FileFontProvider(regularBytes, mediumBytes, monoBytes));
  ```
* **Create controls:** Use `O5Kit.Factory.O5Factory` with `*Options` or inline configure lambdas:
  ```csharp
  var btn = O5Kit.Factory.O5Factory.Button(ctx, row, "btn_hello", o => {
      o.Text = "Hello";
      o.OnClick = () => MelonLogger.Msg("Clicked");
  });
  ```
  Available controls: `Button`, `Toggle`, `Slider`, `Input`, `DropDown<T>`, `MultiDropDown<T>`, `Segmented<T>`, `ColorPicker`, `Card`, `ScrollView`, and `O5SpriteEditor`.
* **Windows:** Use `O5Kit.Core.O5WindowManager` (wraps `O5Canvas` + `O5ColorSpace` Linear/Gamma color parity + `O5ShapeSprites` scale tracking). Visibility and open/close animation are yours (`CloseRequested` fires when X is clicked):
  ```csharp
  var windows = O5Kit.Core.O5WindowManager.Create(ctx, modRoot);
  var win = windows.Create(new O5Kit.Core.O5WindowOptions { Title = "Hello", Resizable = true });
  win.CloseRequested += w => w.Rect.gameObject.SetActive(false);
  win.Rect.gameObject.SetActive(true);
  var (_, content, _) = O5Kit.Factory.O5Factory.ScrollView(ctx, win.Content, spacing: 8f);
  ```
* **Tick every frame:** Pump live controls, scale-aware sprites, tooltips, and shortcuts from `OnUpdate`:
  ```csharp
  O5Kit.Core.O5Object.TickAll();
  ctx.Shapes.Tick(windows.Canvas.Canvas.scaleFactor);
  ctx.Tooltip.Tick();
  O5Kit.Input.O5ShortcutManager.HandleUpdate();
  ```
* **Hotkeys:** register named shortcuts (press + optional hold):
  ```csharp
  O5Kit.Input.O5ShortcutManager.Register(
      "toggle",
      new O5Kit.Input.O5KeyCombo(KeyCode.BackQuote, KeyCode.LeftAlt),
      holdSeconds: 0.4f,
      onPressed: _ => Toggle(),
      onHeld: _ => Reset());
  ```
> [!NOTE]
> O5Kit targets `netstandard2.0` (Mono, Unity 2020.3+) and `net6.0` (IL2CPP). `*_IL2CPP` configurations define `IL2CPP` and alias `TMPro` to `Il2CppTMPro`. In `UserLibs/` on IL2CPP, MelonLoader does not scan `[RegisterTypeInIl2Cpp]`; `new O5Context(...)` registers all 7 `O5Kit.Behaviour` types automatically (see [`llms.txt`](llms.txt#il2cpp-startup-registration)).
>
> Mono reference assemblies must target netstandard 2.0. Unity 2021.2+ `Managed/` folders (including O5Dev's Unity 6 build) reference netstandard 2.1 and fail with `CS1705`; O5Kit picks `$(GamePath)/CompatManaged` (created by `o5dev mod mono`) automatically when present, or set `ManagedRefPath` to a netstandard 2.0 reference folder.

# 🔀 Multi-mod coexistence (versioned side-by-side)

The game (Mono) loads **a single assembly per simple name**. Unsigned DLLs ignore versions too, so two `O5Kit.dll` files with different contents mean only the first-loaded one wins and the other mod dies with `MissingMethodException`/`TypeLoadException`. Same on UMM / MelonLoader / BepInEx.

Rule: **put the version in the assembly name; mods on the same version share one copy.** Renaming by version, not by mod, so 100 mods still mean only as many runtime copies as versions. The name is `O5Kit.{major}.{minor}` — patch excluded, drop-in replacement.

```bash
dotnet build src/O5Kit/O5Kit.csproj -c Release_Mono
# output: O5Kit.1.1.dll + O5Kit.1.1.xml for <Version>1.1.x (name auto-derived, namespaces stay O5Kit)
```

- Mods referencing the same `O5Kit.{major}.{minor}` share the first-loaded copy. Type identity is shared (mod-to-mod type exchange works), statics are shared (any mod pumping `TickAll()` drives everything). Each mod ships the same file in its own folder (disk duplication only; single runtime load).
- Different versions (`O5Kit.1.0` vs `O5Kit.1.1`) load side by side. Old mods are untouched; only new mods use the new version.
- Version policy: **major/minor bumps move the assembly name along. Patch (1.0.0 -> 1.0.1) keeps the name, drop-in.** Any minor-or-above change gets a new name. Because each name is isolated at runtime, a minor release may contain source-breaking API changes; see [`CHANGELOG.md`](CHANGELOG.md) before recompiling against a new minor.
- Special cases (private fork, version pin): per-mod rename via `-p:O5KitAssemblyName=MyMod.O5Kit`.
- Mono API caution: netstandard references are not the game Mono. Only use APIs that actually exist in the game's `Managed` folder. (Example: `IReadOnlyList<T>.Contains` without `System.Linq` binds to netstandard's `MemoryExtensions` Span overload and blows up at runtime.)
- Native (`O5KitNative.*`) is process-wide first-win shared + frozen ABI + `o5_abi_version` gate, so its file names stay as-is (no rename needed).
- LGPL: ship `LICENCE.md` + an O5Kit source link alongside the DLL.

# 🧩 [Overlayer](https://github.com/modlist-org/Overlayer)
O5Kit powers the settings UI of Overlayer v5. Overlayer consumes this library and keeps all game-specific logic (FxValue, tags, modules) on its own side.
> [!NOTE]
> O5Kit is a library and cannot run standalone. Reference it from your mod and use `O5Context` + `O5WindowManager`.

# ⚖️ Licenses
### O5Kit
- O5Kit is licensed under [LGPL-3.0-or-later](LICENCE.md).
- SPDX-License-Identifier: `LGPL-3.0-or-later`
- Copyright (C) 2026 modlist-org contributors.

### Logo
- The O5Kit logo is a placeholder. `o5kit_logo.svg` at the repository root will be replaced with the final logo before the first release.

### Artwork
- O5Kit bundled UI shapes and icons ([`src/O5Kit/Asset/Image`](src/O5Kit/Asset/Image), [`src/O5Kit/Asset/Icons`](src/O5Kit/Asset/Icons)) were created directly by the Overlayer team. Because they are simple geometric shapes, they are free for anyone to use, modify, or redistribute for any purpose without restriction (attribution is not required, though crediting the Overlayer team is greatly appreciated; see [`NOTICE.md`](src/O5Kit/Asset/NOTICE.md)).
- O5Kit bundled fonts (SUIT, JetBrains Mono) under [OFL-1.1](https://opensource.org/licenses/OFL-1.1). Licence texts: [`OFL-SUIT.txt`](src/O5Kit/Asset/Font/OFL-SUIT.txt), [`OFL-JetBrainsMono.txt`](src/O5Kit/Asset/Font/OFL-JetBrainsMono.txt).

### Third-Party Code
- **[LitMotion](https://github.com/annulusgames/LitMotion/tree/422eb124051c81a9bc3f422ebf190703c5053514)**: Submodule under [`lib/LitMotion`](https://github.com/annulusgames/LitMotion/tree/422eb124051c81a9bc3f422ebf190703c5053514), compiled from source (Runtime only)
  - License: [MIT](https://github.com/annulusgames/LitMotion/blob/422eb124051c81a9bc3f422ebf190703c5053514/LICENSE)
- **[GTweens](https://github.com/Guillemsc/GTweens/tree/95debddfe122e5058c9be146083e1cd3397d53a4)**: Submodule under [`lib/GTweens`](https://github.com/Guillemsc/GTweens/tree/95debddfe122e5058c9be146083e1cd3397d53a4), used as the built-in tween backend (`O5Kit.Core.GTweenRunner`, picked by `AutoTweenRunner` when LitMotion is unavailable). Built from a patched snapshot embedded under [`src/O5Kit/ThirdParty/GTweens`](src/O5Kit/ThirdParty/GTweens) (see its `README.md` for the deviations)
  - License: [MIT](https://github.com/Guillemsc/GTweens/blob/95debddfe122e5058c9be146083e1cd3397d53a4/LICENSE)
- **[UniverseLib](https://github.com/sinai-dev/UniverseLib/tree/f6a9ed9a4d58bfe13eaae570c53f59a00b575ca0)**: Parts of input handling logic in [`src/O5Kit/Input`](src/O5Kit/Input) are referenced and derived from UniverseLib.
  - License: [LGPL-2.1](https://github.com/sinai-dev/UniverseLib/blob/f6a9ed9a4d58bfe13eaae570c53f59a00b575ca0/LICENSE)
- Unity assemblies (UnityEngine, Unity.Burst/Collections/Mathematics, TextMeshPro) are referenced from the game install and are not redistributed.
- All other external dependencies are managed and imported via **NuGet**.
---

> [!NOTE]
> O5Kit is currently under active development!
