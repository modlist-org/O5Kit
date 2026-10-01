// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using O5Kit.Core;
using UnityEngine;

namespace O5Kit.Behaviour;

/// <summary>Runs an action every frame via <see cref="O5Object.TickAll"/> (port of Overlayer's UIWatcher).</summary>
public sealed class O5Watcher : O5Object {
    /// <summary>Per-frame callback.</summary>
    public Action? OnTick { get; set; }

    /// <summary>Creates a tick watcher. Pump with <see cref="O5Object.TickAll"/>.</summary>
    /// <param name="ctx">Owning kit context.</param>
    /// <param name="id">Stable identifier.</param>
    /// <param name="rect">Associated rect (unused for layout, kept for lookup/disposal).</param>
    /// <param name="onTick">Per-frame callback.</param>
    public O5Watcher(O5Context ctx, string id, RectTransform rect, Action? onTick = null) : base(ctx, id, rect) {
        OnTick = onTick;
        RegisterTick();
    }

    /// <inheritdoc/>
    public override void Tick() {
        if (!IsDisposed) {
            OnTick?.Invoke();
        }
    }

    /// <inheritdoc/>
    public override void Dispose() {
        if (IsDisposed) {
            return;
        }

        OnTick = null;
        base.Dispose();
    }
}
