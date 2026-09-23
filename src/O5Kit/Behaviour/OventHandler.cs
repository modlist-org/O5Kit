// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using O5Kit.Input;
using UnityEngine;
using UnityEngine.EventSystems;

#if IL2CPP
using MelonLoader;
#endif

namespace O5Kit.Behaviour;

#if IL2CPP
[RegisterTypeInIl2Cpp]
#endif
/// <summary>Click/hover probe. Reports mouse presses while hovered, without consuming them.</summary>
public class OventHandler
#if IL2CPP
    (IntPtr ptr) : MonoBehaviour(ptr)
#else
    : MonoBehaviour
#endif
{
    private RectTransform? _rectTransform;
    /// <summary>Fired with the pressed button while hovered.</summary>
    public Action<PointerEventData.InputButton>? OnClick;
    /// <summary>Fired every frame while hovered.</summary>
    public Action? OnHoverUpdate;
    /// <summary>Fired when the component is disabled.</summary>
    public Action? OnDisabled;
    private bool _isHovered;

    private void Awake() => _rectTransform = GetComponent<RectTransform>();

    private void Start() {
        var trigger = GetComponent<EventTrigger>() ?? gameObject.AddComponent<EventTrigger>();

        UnityUtils.AddEvents(trigger,
            (EventTriggerType.PointerEnter, () => _isHovered = true),
            (EventTriggerType.PointerExit, () => _isHovered = false)
        );
    }

    private void OnDisable() {
        _isHovered = false;
        OnDisabled?.Invoke();
    }

    private void Update() {
        if (!_isHovered) {
            return;
        }

        OnHoverUpdate?.Invoke();

        for (int i = 0; i < 3; i++) {
            if (O5Input.GetMouseButtonDown(i)) {
                OnClick?.Invoke((PointerEventData.InputButton)i);
            }
        }
    }
}
