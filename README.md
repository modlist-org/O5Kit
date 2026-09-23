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

# 🛠️ Installation

## 📦 From source

> [!IMPORTANT]
> You must clone with submodules. Without them the build fails at `CheckLitMotion`.

1. Clone with submodules (or init them afterwards):
   ```bash
   git clone --recurse-submodules <this-repo-url>
   # or: git submodule update --init --depth 1
   ```
2. Copy `Directory.Build.example.props` to `Directory.Build.props` and fill in your game paths (`GamePath`/`GameData`, optionally `Il2CppPath`/`Il2CppData`).
3. Build the configuration that matches your game:
   ```bash
   dotnet build src/O5Kit/O5Kit.csproj -c Release_Mono
   dotnet build src/O5Kit/O5Kit.csproj -c Release_IL2CPP
   ```

# ⌨️ Usage
### Setup
* **Configure once:** Call `O5Boot.Configure(...)` at startup with your `O5Config`, `O5Theme`, `ISpriteProvider`, `IFontProvider`. The tween backend defaults to the built-in LitMotion runner.
  ```csharp
  O5Kit.Core.O5Boot.Configure(
      config: new O5Kit.Core.O5Config { UIScale = 1f },
      sprites: new MySpriteProvider(),
      fonts: new MyFontProvider());
  ```
* **Or zero-setup:** `O5Kit.Core.O5Boot.EnsureDefaults()` installs bundled artwork, fonts and the LitMotion backend. No adapters needed to try controls.
* **Theme:** swap presets or derive your own at runtime:
  ```csharp
  O5Kit.Core.O5Boot.SetTheme(O5Kit.Core.O5Theme.Dark with { PanelBG = new Color(0.1f, 0.1f, 0.15f, 1f) });
  ```
  Applies to subsequently created controls; rebuild UI to restyle. Presets: `Dark`, `Light`, `Overlayer`.
* **Fonts:** load custom font files (missing slots fall back gracefully):
  ```csharp
  O5Kit.Core.O5Boot.Configure(fonts: new O5Kit.Core.FileFontProvider(regularBytes, monoBytes));
  ```
* **Create controls:** Use `O5Kit.Factory.O5Factory` (e.g. `Button(parent, onClick, "Hello", "btn_hello")`) or instantiate controls (`O5Button`, ...) directly.
* **Windows:** Use `O5Kit.Core.O5WindowManager` for multi-window UI. O5Kit manages structure and z-order; visibility and open/close animation are yours (the close button raises `CloseRequested` instead of closing):
  ```csharp
  var windows = O5Kit.Core.O5WindowManager.Create(modRoot);
  var win = windows.Create(new O5Kit.Core.O5WindowOptions { Title = "Hello", Icon = null });
  win.CloseRequested += w => w.Rect.gameObject.SetActive(false);
  win.Rect.gameObject.SetActive(true);
  var row = O5Kit.Factory.O5Factory.Row(win.Content);
  ```
* **Tick every frame:** Call `O5Kit.Core.O5Object.TickAll()` and `O5Kit.Core.O5Tooltip.Tick()` from your update loop.
* **Hotkeys:** register named shortcuts (press + optional hold) and pump them:
  ```csharp
  O5Kit.Input.O5ShortcutManager.Register("toggle", new O5Kit.Input.O5KeyCombo(KeyCode.BackQuote, KeyCode.LeftAlt), 0.4f, onPressed: _ => Toggle(), onHeld: _ => Reset());
  ```
  Pump with `O5ShortcutManager.HandleUpdate()` each frame; gate with `IsSuspended`. Combos stay rebindable via `Get(id).Combo`.
> [!NOTE]
> O5Kit targets `netstandard2.1` (Mono) and `net6.0` (IL2CPP). `*_IL2CPP` configurations define the `IL2CPP` symbol automatically, which enables the MelonLoader/Il2CppInterop code paths (`RegisterTypeInIl2Cpp`, `DelegateSupport`, `TryCast` — same pattern as Overlayer's `ML && IL2CPP` blocks). IL2CPP builds reference the `MelonLoader/net6` assemblies via `GamePath`. IL2CPP support is implemented for the future; it has not been verified against an IL2CPP game yet.

# 🧩 [Overlayer](https://github.com/modlist-org/Overlayer)
O5Kit powers the settings UI of Overlayer v5. Overlayer consumes this library and keeps all game-specific logic (FxValue, tags, modules) on its own side.
> [!NOTE]
> O5Kit is a library and cannot run standalone. Reference it from your mod and provide the resource adapters.

# ⚖️ Licenses
### O5Kit
- O5Kit is licensed under [LGPL-3.0-or-later](LICENCE.md).
- SPDX-License-Identifier: `LGPL-3.0-or-later`
- Copyright (C) 2026 modlist-org contributors.

### Logo
- The O5Kit logo is a placeholder. `o5kit_logo.svg` at the repository root will be replaced with the final logo before the first release.

### Artwork
- O5Kit bundled UI shapes ([`src/O5Kit/Asset`](src/O5Kit/Asset)) are too simple for copyright; no rights claimed (see [`NOTICE.md`](src/O5Kit/Asset/NOTICE.md)).
- O5Kit bundled fonts (SUIT, JetBrains Mono) under [OFL-1.1](https://opensource.org/licenses/OFL-1.1).

### Third-Party Code
- **[LitMotion](https://github.com/annulusgames/LitMotion/tree/422eb124051c81a9bc3f422ebf190703c5053514)**: Submodule under [`lib/LitMotion`](https://github.com/annulusgames/LitMotion/tree/422eb124051c81a9bc3f422ebf190703c5053514), compiled from source (Runtime only)
  - License: [MIT](https://github.com/annulusgames/LitMotion/blob/422eb124051c81a9bc3f422ebf190703c5053514/LICENSE)
- **[UniverseLib](https://github.com/sinai-dev/UniverseLib/tree/f6a9ed9a4d58bfe13eaae570c53f59a00b575ca0)**: Parts of input handling logic in [`src/O5Kit/Input`](src/O5Kit/Input) are referenced and derived from UniverseLib.
  - License: [LGPL-2.1](https://github.com/sinai-dev/UniverseLib/blob/f6a9ed9a4d58bfe13eaae570c53f59a00b575ca0/LICENSE)
- Unity assemblies (UnityEngine, Unity.Burst/Collections/Mathematics, TextMeshPro) are referenced from the game install and are not redistributed.
- All other external dependencies are managed and imported via **NuGet**.
---

> [!NOTE]
> O5Kit is currently under active development!
