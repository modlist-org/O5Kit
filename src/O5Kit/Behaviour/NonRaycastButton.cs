// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using UnityEngine;
using UnityEngine.EventSystems;

#if IL2CPP
using MelonLoader;
#endif

namespace O5Kit.Behaviour;

#if IL2CPP
[RegisterTypeInIl2Cpp]
#endif
/// <summary>Clickable area without a visible raycast graphic.</summary>
public class NonRaycastButton
#if IL2CPP
    (IntPtr ptr) : MonoBehaviour(ptr)
#else
    : MonoBehaviour
#endif
{
    /// <summary>Fired on pointer click.</summary>
    public Action? onClick;

    private void Start() {
        var trigger = gameObject.AddComponent<EventTrigger>();
        UnityUtils.AddEvent(trigger, EventTriggerType.PointerClick, () => onClick?.Invoke());
    }
}
