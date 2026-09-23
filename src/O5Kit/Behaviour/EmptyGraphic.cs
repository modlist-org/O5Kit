// SPDX-License-Identifier: LGPL-3.0-or-later

namespace O5Kit.Behaviour;

/// <summary>Transparent raycast target for layout containers.</summary>
public class EmptyGraphic : UnityEngine.UI.Graphic {
    protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh) => vh.Clear();
}
