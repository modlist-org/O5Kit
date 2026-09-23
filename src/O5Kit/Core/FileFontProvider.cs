// SPDX-License-Identifier: LGPL-3.0-or-later

using System;

namespace O5Kit.Core;

/// <summary>File-based fonts with graceful fallback: missing slots reuse regular, missing regular uses the runtime system font.</summary>
public sealed class FileFontProvider : IFontProvider, IDisposable {
    private readonly Resource.O5Resources _resources = Resource.O5Resources.Bundled();
    private readonly byte[]? _regular;
    private readonly byte[]? _medium;
    private readonly byte[]? _mono;
    private readonly DefaultFontProvider _fallback = new();
    private TMPro.TMP_FontAsset? _regularAsset;
    private TMPro.TMP_FontAsset? _mediumAsset;
    private TMPro.TMP_FontAsset? _monoAsset;
    private bool _disposed;

    /// <summary>Creates a provider from font file bytes.</summary>
    /// <param name="regular">Regular TTF/OTF bytes.</param>
    /// <param name="medium">Medium TTF/OTF bytes. Null reuses regular.</param>
    /// <param name="mono">Monospace TTF/OTF bytes. Null reuses regular.</param>
    public FileFontProvider(byte[]? regular, byte[]? medium = null, byte[]? mono = null) {
        _regular = regular;
        _medium = medium;
        _mono = mono;
    }

    /// <inheritdoc/>
    public TMPro.TMP_FontAsset Regular => _regularAsset ??= LoadSlot("O5Custom-Regular.otf", _regular) ?? _fallback.Regular;

    /// <inheritdoc/>
    public TMPro.TMP_FontAsset Medium => _mediumAsset ??= LoadSlot("O5Custom-Medium.otf", _medium) ?? _fallback.Medium;

    /// <inheritdoc/>
    public TMPro.TMP_FontAsset Monospace => _monoAsset ??= LoadSlot("O5Custom-Mono.otf", _mono) ?? _fallback.Monospace;

    private TMPro.TMP_FontAsset? LoadSlot(string name, byte[]? data)
        => data == null ? null : _resources.LoadFontAsset(data, name);

    /// <summary>Destroys loaded font assets.</summary>
    public void Dispose() {
        if (_disposed) {
            return;
        }

        _disposed = true;
        _regularAsset = _mediumAsset = _monoAsset = null;
        _resources.Dispose();
    }
}
