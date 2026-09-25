// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using O5Kit.Core;
using O5Kit.Input;
using UnityEngine;

#if IL2CPP
using MelonLoader;
#endif

namespace O5Kit.Behaviour;

#if IL2CPP
[RegisterTypeInIl2Cpp]
#endif
/// <summary>Smooth wheel + right-drag scrolling for a content/viewport pair.</summary>
public class UIScrollController
#if IL2CPP
    (IntPtr ptr) : MonoBehaviour(ptr)
#else
    : MonoBehaviour
#endif
{
    /// <summary>Scrollable content.</summary>
    public RectTransform? content;
    /// <summary>Visible window the content scrolls inside.</summary>
    public RectTransform? viewport;

    /// <summary>Pixels scrolled per wheel notch.</summary>
    public float wheelStrength = 42f;

    /// <summary>Right-drag sensitivity multiplier.</summary>
    public float dragSensitivity = 1f;
    /// <summary>Scrollbar-style drag ratio (reserved for scrollbar mode).</summary>
    public float dragToScrollRatio = 1f;

    /// <summary>Smooth-scroll glide duration in seconds.</summary>
    public float scrollDuration = 0.2f;

    /// <summary>Easing for the smooth-scroll glide.</summary>
    public O5Ease scrollEase = O5Ease.OutCirc;

    /// <summary>When set and true, wheel events are left for a nested consumer.</summary>
    public static Func<bool>? ShouldConsumeParentScroll { get; set; }

    private bool _rightDragging;

    private float _targetY;
    private ITweenHandle? _scrollTween;

    private void Awake() {
        if (content != null) {
            _targetY = content.anchoredPosition.y;
        }
    }

    private void Update() {
        if (content == null || viewport == null) {
            return;
        }

        HandleWheel();
        HandleRightDrag();
    }

    private void HandleWheel() {
        if (ShouldConsumeParentScroll?.Invoke() == true) {
            return;
        }

        float wheel = O5Input.MouseScrollDelta.y;

        if (Math.Abs(wheel) <= 0.0001f) {
            return;
        }

        AddDelta(-wheel * wheelStrength);
        ApplyTween();
    }

    private void HandleRightDrag() {
        if (content == null || viewport == null) {
            return;
        }

        if (O5Input.GetMouseButtonDown(1)) {
            _rightDragging = true;
        }

        if (O5Input.GetMouseButtonUp(1)) {
            _rightDragging = false;
            ApplyTween();
        }

        if (!_rightDragging) {
            return;
        }

        float contentHeight = content.rect.height;
        float viewportHeight = viewport.rect.height;

        float maxOffset = Math.Max(0f, contentHeight - viewportHeight);

        if (maxOffset <= 0f) {
            return;
        }

        Vector2 mouse = O5Input.MousePosition;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            viewport,
            mouse,
            null,
            out Vector2 local
        );

        float normalized = 1f - Math.Clamp(
            (local.y + (viewportHeight * 0.5f)) / viewportHeight,
            0f, 1f
        );

        _targetY = normalized * maxOffset;

        ApplyTween();
    }

    private void AddDelta(float deltaPixels) {
        if (content == null || viewport == null) {
            return;
        }

        float contentHeight = content.rect.height;
        float viewportHeight = viewport.rect.height;
        float maxOffset = Math.Max(0f, contentHeight - viewportHeight);

        _targetY += deltaPixels;
        _targetY = Math.Clamp(_targetY, 0f, maxOffset);
    }

    private void ApplyTween() {
        if (content == null) {
            return;
        }

        _scrollTween?.Kill();

        var c = content;
        float target = _targetY;
        _scrollTween = O5Boot.Tween.TweenFloat(
            () => c.anchoredPosition.y,
            x => {
                if (c) {
                    c.anchoredPosition = new Vector2(c.anchoredPosition.x, x);
                }
            },
            target,
            scrollDuration,
            null,
            scrollEase);
    }

    /// <summary>Assigns the content/viewport pair after creation.</summary>
    /// <param name="content">Scrollable content.</param>
    /// <param name="viewport">Visible window.</param>
    public void SetContent(RectTransform content, RectTransform viewport) {
        this.content = content;
        this.viewport = viewport;

        if (content != null) {
            _targetY = content.anchoredPosition.y;
        }
    }
}
