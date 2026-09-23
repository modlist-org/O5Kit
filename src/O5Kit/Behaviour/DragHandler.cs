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
/// <summary>Drag handle. Attach to a top-bar-like child; drags the parent rect.</summary>
public class DragHandler
#if IL2CPP
    (IntPtr ptr) : MonoBehaviour(ptr)
#else
    : MonoBehaviour
#endif
{
    private RectTransform? _rect;
    private Vector2 _offset;

    private void Awake() {
        _rect = transform.parent?.GetComponent<RectTransform>();
        SetupEvents();
    }

    private void SetupEvents() {
        var trigger = gameObject.AddComponent<EventTrigger>();

        UnityUtils.AddEvents(trigger,
            (EventTriggerType.PointerDown, OnPointerDownInternal),
            (EventTriggerType.Drag, OnDragInternal)
        );
    }

    private void OnPointerDownInternal() {
        _rect ??= transform.parent?.GetComponent<RectTransform>();
        if (_rect == null) {
            return;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rect.parent as RectTransform,
            O5Input.MousePosition,
            null,
            out Vector2 localPoint
        );
        _offset = _rect.anchoredPosition - localPoint;
    }

    private void OnDragInternal() {
        _rect ??= transform.parent?.GetComponent<RectTransform>();
        if (_rect == null) {
            return;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rect.parent as RectTransform,
            O5Input.MousePosition,
            null,
            out Vector2 localPoint
        );
        _rect.anchoredPosition = localPoint + _offset;
    }
}
