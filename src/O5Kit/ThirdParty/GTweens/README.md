# GTweens (embedded third-party)

Source: [GTweens by Guillemsc](https://github.com/Guillemsc/GTweens/tree/95debddfe122e5058c9be146083e1cd3397d53a4) (MIT, see `LICENSE`).
Upstream mirror: submodule under [`lib/GTweens`](../../../../lib/GTweens).
Recovered from Overlayer history (`Overlayer/GTweens`, removed in `184738a`);
files under `Source/` are unmodified upstream copies in the `GTweens.*`
namespaces so future upstream syncs stay trivial.

O5Kit (LGPL-3.0-or-later) uses it as the built-in tween backend
(`O5Kit.Core.GTweenRunner`): `AutoTweenRunner` picks LitMotion when available
(IL2CPP) and GTweens otherwise, so plain Mono titles need no external tween
engine. Only the float-tween core is exercised at runtime; the
`System.Drawing` / `System.Numerics` overloads in `GTweenExtensions` are never
called from O5Kit (Unity Vector/Color tweens run through a 0→1 float tween +
manual lerp, like the old Simple runner did).

Embed deviations from upstream (O5Kit builds with `ImplicitUsings` off and
targets netstandard2.0):
- missing `using System;` / `using System.Collections.Generic;`
  (`System.Threading[.Tasks]` in one file) added per file,
- `MathExtensions.Repeat` uses a local `Clamp` (`Math.Clamp` needs
  netstandard2.1+).
