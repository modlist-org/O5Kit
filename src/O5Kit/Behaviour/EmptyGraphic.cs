// SPDX-License-Identifier: LGPL-3.0-or-later

namespace O5Kit.Behaviour;

#if IL2CPP
using MelonLoader;
#endif

/// <summary>Transparent raycast target for layout containers.</summary>
#if IL2CPP
[RegisterTypeInIl2Cpp]
#endif
public class EmptyGraphic : UnityEngine.UI.Graphic {
#if IL2CPP
    public EmptyGraphic(System.IntPtr ptr) : base(ptr) {
    }
#endif
#if IL2CPP
    public
#else
    protected
#endif
    override void OnPopulateMesh(UnityEngine.UI.VertexHelper vh) => vh.Clear();
}
