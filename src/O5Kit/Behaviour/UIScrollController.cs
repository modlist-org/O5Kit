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

    /// <summary>Owning kit context. Assign after AddComponent; required for smooth scrolling.</summary>
    public O5Context? Ctx { get; set; }

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

        ClampScrollPosition();
        HandleWheel();
        HandleRightDrag();
    }

    /// <summary>Keeps target and actual scroll offset inside the current valid range.
    /// Content/viewport size changes (items added/removed, window resized) otherwise
    /// leave a stale <see cref="_targetY"/> behind: next wheel input jumps, and a
    /// shrunken list keeps showing blank space at the bottom.</summary>
    private void ClampScrollPosition() {
        if (content == null || viewport == null) {
            return;
        }

        // The mouse-up edge can be missed (focus loss, hierarchy rebuild):
        // held-state is truth, so a stuck drag can't pin the scroll forever.
        if (_rightDragging && !O5Input.GetMouseButton(1)) {
            _rightDragging = false;
        }

        float maxOffset = Math.Max(0f, content.rect.height - viewport.rect.height);

        if (_targetY < 0f || _targetY > maxOffset) {
            _targetY = MathCompat.Clamp(_targetY, 0f, maxOffset);
            if (_scrollTween?.IsAlive == true) {
                // Retarget the in-flight glide instead of freezing mid-way:
                // killing it here is what made the scroll feel dead near the bottom.
                _scrollTween?.Kill();
                _scrollTween = null;
                if (Ctx != null) {
                    ApplyTween();
                }
            }
        }

        // While a glide is in flight its setter owns the position, so leave it alone.
        if (_scrollTween?.IsAlive == true) {
            return;
        }

        float y = content.anchoredPosition.y;
        float clampedY = MathCompat.Clamp(y, 0f, maxOffset);
        if (Math.Abs(y - clampedY) > 0.01f) {
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, clampedY);
            _targetY = clampedY;
            return;
        }

        // Idle and in range, but target drifted from the visible position
        // (external move, killed glide). Position wins: otherwise the next
        // wheel notch jumps from the stale target.
        if (!_rightDragging && Math.Abs(y - _targetY) > 0.5f) {
            _targetY = y;
        }
    }

    private void HandleWheel() {
        if (ShouldConsumeParentScroll?.Invoke() == true) {
            return;
        }

        if (!IsPointerOverViewport()) {
            return;
        }

        float wheel = O5Input.MouseScrollDelta.y;

        if (Math.Abs(wheel) <= 0.0001f) {
            return;
        }

        if (content == null || viewport == null) {
            return;
        }

        if (content.rect.height <= viewport.rect.height) {
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
            _rightDragging = IsPointerOverViewport();
        }

        // Held-state heals a missed mouse-up edge (same as in ClampScrollPosition).
        if (_rightDragging && !O5Input.GetMouseButton(1)) {
            _rightDragging = false;
        } else if (O5Input.GetMouseButtonUp(1) && _rightDragging) {
            _rightDragging = false;
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

        float normalized = 1f - MathCompat.Clamp(
            (local.y + (viewportHeight * 0.5f)) / viewportHeight,
            0f, 1f
        );

        _targetY = normalized * maxOffset;

        // Absolute mapping: drive the position directly. Restarting a 0.2s tween
        // every frame here only lags behind the cursor and churns handles.
        _scrollTween?.Kill();
        _scrollTween = null;
        content.anchoredPosition = new Vector2(content.anchoredPosition.x, _targetY);
    }

    private void AddDelta(float deltaPixels) {
        if (content == null || viewport == null) {
            return;
        }

        float contentHeight = content.rect.height;
        float viewportHeight = viewport.rect.height;
        float maxOffset = Math.Max(0f, contentHeight - viewportHeight);

        _targetY += deltaPixels;
        _targetY = MathCompat.Clamp(_targetY, 0f, maxOffset);
    }

    private void ApplyTween() {
        if (content == null) {
            return;
        }

        _scrollTween?.Kill();

        var c = content;
        float target = _targetY;
        O5Context ctx = Ctx ?? throw new InvalidOperationException(
            "O5Kit: UIScrollController.Ctx is not set. Assign your O5Context after AddComponent.");
        _scrollTween = ctx.Tween.TweenFloat(
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

    /// <summary>Maximum scroll offset for the current content/viewport sizes.</summary>
    public float MaxOffset {
        get {
            if (content == null || viewport == null) {
                return 0f;
            }

            return Math.Max(0f, content.rect.height - viewport.rect.height);
        }
    }

    /// <summary>Snaps scroll back to the top. Call after clearing/rebuilding rows
    /// so a stale bottom offset doesn't linger on a fresh list.</summary>
    public void ResetScroll() => ScrollTo(0f, true);

    /// <summary>Scrolls to an absolute offset (0 = top).</summary>
    /// <param name="offsetY">Desired offset in pixels.</param>
    /// <param name="instant">True snaps immediately; false glides with the scroll tween.</param>
    public void ScrollTo(float offsetY, bool instant = false) {
        if (content == null || viewport == null) {
            _targetY = 0f;
            return;
        }

        float maxOffset = Math.Max(0f, content.rect.height - viewport.rect.height);
        _targetY = MathCompat.Clamp(offsetY, 0f, maxOffset);
        _scrollTween?.Kill();
        _scrollTween = null;

        if (instant || Ctx == null) {
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, _targetY);
        } else {
            ApplyTween();
        }
    }

    /// <summary>True when the pointer is inside the viewport. Wheel and right-drag
    /// only engage then, so stacked windows don't scroll each other.</summary>
    private bool IsPointerOverViewport() {
        if (viewport == null) {
            return false;
        }

        return RectTransformUtility.RectangleContainsScreenPoint(
            viewport, O5Input.MousePosition, null);
    }
}
