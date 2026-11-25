// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using O5Kit.Core;
using O5Kit.Input;
using UnityEngine;
using UnityEngine.UI;

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
    /// <summary>When true, right-drag only scrolls after moving past <see cref="rightDragThreshold"/>,
    /// using drag deltas instead of jumping to the pointer position like a scrollbar.</summary>
    public bool rightDragRelativeOnly;
    /// <summary>Vertical pointer movement in screen pixels required to start relative right-drag scrolling.</summary>
    public float rightDragThreshold = 6f;

    /// <summary>Smooth-scroll glide duration in seconds.</summary>
    public float scrollDuration = 0.2f;

    /// <summary>Easing for the smooth-scroll glide.</summary>
    public O5Ease scrollEase = O5Ease.OutCirc;

    /// <summary>When set and true, wheel events are left for a nested consumer.</summary>
    public static Func<bool>? ShouldConsumeParentScroll { get; set; }

    /// <summary>Fired when right-drag scrolling moves a scroll view's content vertically, passing controller and delta pixels.</summary>
    public static event Action<UIScrollController, float>? OnRightDragScrolled;

    /// <summary>True while or on the frame a relative right-drag consumed the pointer press.</summary>
    public bool RightDragConsumed => (_rightDragging && _rightDragMoved) || _rightDragConsumedFrame == Time.frameCount;

    /// <summary>True while a held drag (right-click, or left-click in the right padding) is scrolling this view.</summary>
    public bool DragScrolling => _rightDragging && _rightDragMoved;

    /// <summary>When true, a left press in the right padding strip (between the content's
    /// inner right edge and the scroll view's right edge) acts exactly like a right press.</summary>
    public bool leftClickRightPaddingDrag = true;

    private bool _rightDragging;
    /// <summary>Mouse button that started the current drag (1 = right, 0 = left in right padding).</summary>
    private int _dragButton = 1;

    /// <summary>Mouse button driving the current (or last) drag scroll: 1 = right-click,
    /// 0 = left-click in the right padding strip. Check it in <see cref="OnRightDragScrolled"/>
    /// to tell the two apart.</summary>
    public int DragButton => _dragButton;

    /// <summary>True while a drag is showing its Shift candidate (Shift currently held):
    /// touch-style relative scrolling at the scroll sensitivity.</summary>
    public bool ShiftRelativeDrag => _rightDragging && _shiftRelative;
    private bool _shiftRelative;
    private float _shiftRelY;
    private float _normalRelY;

    /// <summary>Shift-drag fling: per-second velocity decay rate (touch-like friction).</summary>
    public float flingFriction = 2.2f;
    /// <summary>Release speed (content units/s) below which a Shift drag just stops, like touch.</summary>
    public float flingMinVelocity = 250f;
    /// <summary>A coasting fling stops once it slows below this speed (content units/s).</summary>
    public float flingStopVelocity = 15f;
    /// <summary>Pointer held still this long (s) before release means no fling.</summary>
    public float flingStillTime = 0.06f;
    /// <summary>Upper bound for fling speed (content units/s).</summary>
    public float flingMaxVelocity = 9000f;

    /// <summary>True while a released Shift drag is still coasting.</summary>
    public bool Flinging => Math.Abs(_flingVelocity) > 0f;

    private float _dragVelocity;
    private float _lastMoveTime;
    private float _flingVelocity;
    private bool _rightDragMoved;
    private float _rightDragStartMouseY;
    private float _rightDragLastLocalY;
    private int _rightDragConsumedFrame = -1;

    private float _targetY;
    private float _lastDragY;
    private ITweenHandle? _scrollTween;

    private bool UseScrollEasing => Ctx != null && Ctx.Config.ScrollEasing && Ctx.Config.AnimationSpeed > 0f;
    private bool AnimationsEnabled => Ctx == null || Ctx.Config.AnimationSpeed > 0f;

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
        HandleFling();
    }

    /// <summary>Stops any coasting fling.</summary>
    public void StopFling() => _flingVelocity = 0f;

    private void HandleFling() {
        if (_flingVelocity == 0f || content == null || viewport == null) {
            return;
        }

        float dt = Time.unscaledDeltaTime;
        float maxOffset = MaxOffset;
        float next = _targetY + _flingVelocity * dt;
        _flingVelocity *= (float)Math.Exp(-flingFriction * dt);

        if (next <= 0f || next >= maxOffset) {
            next = MathCompat.Clamp(next, 0f, maxOffset);
            _flingVelocity = 0f;
        } else if (Math.Abs(_flingVelocity) < flingStopVelocity) {
            _flingVelocity = 0f;
        }

        _targetY = next;
        SnapToTarget();
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
        if (_rightDragging && !O5Input.GetMouseButton(_dragButton)) {
            EndRightDrag();
        }

        float maxOffset = Math.Max(0f, content.rect.height - viewport.rect.height);

        if (_targetY < 0f || _targetY > maxOffset) {
            _targetY = MathCompat.Clamp(_targetY, 0f, maxOffset);
            if (_scrollTween?.IsAlive == true) {
                if (UseScrollEasing) {
                    // Retarget the in-flight glide instead of freezing mid-way:
                    // killing it here is what made the scroll feel dead near the bottom.
                    _scrollTween?.Kill();
                    _scrollTween = null;
                    ApplyTween();
                } else {
                    SnapToTarget();
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
        if (!IsPointerOverViewport()) {
            return;
        }

        float wheel = O5Input.MouseScrollDelta.y;

        if (Math.Abs(wheel) <= 0.0001f) {
            return;
        }

        if (ShouldConsumeParentScroll?.Invoke() == true) {
            return;
        }

        if (content == null || viewport == null) {
            return;
        }

        if (content.rect.height <= viewport.rect.height) {
            return;
        }

        StopFling();
        AddDelta(-wheel * wheelStrength);
        if (UseScrollEasing) {
            ApplyTween();
        } else {
            SnapToTarget();
        }
    }

    private void EndRightDrag() {
        // Shift showing at release: coast on like a touch fling, unless the pointer was
        // held still or moving too slowly, in which case it stops right there.
        if (_rightDragging && _rightDragMoved && _shiftRelative && content != null && AnimationsEnabled) {
            float v = Time.unscaledTime - _lastMoveTime > flingStillTime ? 0f : _dragVelocity;
            if (Math.Abs(v) >= flingMinVelocity) {
                _flingVelocity = MathCompat.Clamp(v, -flingMaxVelocity, flingMaxVelocity);
                _scrollTween?.Kill();
                _scrollTween = null;
                _targetY = content.anchoredPosition.y;
                _rightDragConsumedFrame = Time.frameCount;
                _rightDragging = false;
                _rightDragMoved = false;
                return;
            }
        }

        bool shouldTween = !rightDragRelativeOnly || _rightDragMoved;
        if (_rightDragMoved) {
            _rightDragConsumedFrame = Time.frameCount;
        }
        _rightDragging = false;
        _rightDragMoved = false;
        if (shouldTween && UseScrollEasing) {
            ApplyTween();
        }
    }

    private static bool ShiftHeld() =>
        O5Input.GetKey(KeyCode.LeftShift) || O5Input.GetKey(KeyCode.RightShift);

    /// <summary>Drag scrolling. Both modes are tracked side by side for the whole drag:
    /// the normal one (scrollbar-style jump, or plain relative for <see cref="rightDragRelativeOnly"/>)
    /// and the Shift one (touch-style relative at scroll sensitivity). The live Shift state picks
    /// which one is shown, so toggling Shift mid-drag compares them or undoes a wrong pick; the
    /// state at mouse release is what stays.</summary>
    private void HandleRightDrag() {
        if (content == null || viewport == null) {
            return;
        }

        bool rightDown = O5Input.GetMouseButtonDown(1);
        bool leftPaddingDown = !rightDown && !_rightDragging && leftClickRightPaddingDrag
            && O5Input.GetMouseButtonDown(0) && IsPointerOverRightPadding();
        if (rightDown || leftPaddingDown) {
            _rightDragging = rightDown ? (IsPointerOverViewport() || IsPointerOverRightPadding()) : true;
            _dragButton = rightDown ? 1 : 0;
            if (_rightDragging) {
                float startY = content.anchoredPosition.y;
                _lastDragY = startY;
                _normalRelY = startY;
                _shiftRelY = startY;
                _rightDragMoved = false;
                _rightDragStartMouseY = O5Input.MousePosition.y;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    viewport,
                    O5Input.MousePosition,
                    null,
                    out Vector2 startLocal
                );
                _rightDragLastLocalY = startLocal.y;
                _shiftRelative = ShiftHeld();
                _dragVelocity = 0f;
                _lastMoveTime = Time.unscaledTime;
                StopFling();
            }
        }

        // Held-state heals a missed mouse-up edge (same as in ClampScrollPosition).
        if (_rightDragging && !O5Input.GetMouseButton(_dragButton)) {
            EndRightDrag();
        } else if (O5Input.GetMouseButtonUp(_dragButton) && _rightDragging) {
            EndRightDrag();
        }

        if (!_rightDragging) {
            return;
        }

        float viewportHeight = viewport.rect.height;
        float maxOffset = Math.Max(0f, content.rect.height - viewportHeight);

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

        bool shift = ShiftHeld();
        bool modeChanged = shift != _shiftRelative;
        _shiftRelative = shift;

        if (!_rightDragMoved) {
            // The threshold keeps a plain click free for context menus.
            if (rightDragRelativeOnly && Math.Abs(mouse.y - _rightDragStartMouseY) < rightDragThreshold) {
                return;
            }
            _rightDragMoved = true;
        }

        float deltaLocalY = local.y - _rightDragLastLocalY;
        _rightDragLastLocalY = local.y;

        // Shift candidate: touch-style at the scroll sensitivity.
        float scrollSensitivity = (float)(Ctx?.Config.ScrollSensitivity ?? 1d);
        float prevShiftY = _shiftRelY;
        _shiftRelY = MathCompat.Clamp(_shiftRelY + deltaLocalY * scrollSensitivity, 0f, maxOffset);

        // Release velocity for the fling: smoothed over ~50ms of recent movement.
        float frameDt = Math.Max(Time.unscaledDeltaTime, 0.0001f);
        float instant = (_shiftRelY - prevShiftY) / frameDt;
        float smooth = 1f - (float)Math.Exp(-frameDt / 0.05f);
        _dragVelocity += (instant - _dragVelocity) * smooth;
        if (Math.Abs(deltaLocalY) > 0.0001f) {
            _lastMoveTime = Time.unscaledTime;
        }

        // Normal candidate.
        float normalY;
        if (rightDragRelativeOnly) {
            _normalRelY = MathCompat.Clamp(_normalRelY + deltaLocalY * dragSensitivity, 0f, maxOffset);
            normalY = _normalRelY;
        } else {
            float normalized = 1f - MathCompat.Clamp(
                (local.y + (viewportHeight * 0.5f)) / viewportHeight,
                0f, 1f
            );
            normalY = normalized * maxOffset;
        }

        _rightDragConsumedFrame = Time.frameCount;
        float next = shift ? _shiftRelY : normalY;
        if (!modeChanged && Math.Abs(next - _targetY) <= 0.0001f) {
            return;
        }
        _targetY = next;

        float moved = Math.Abs(_targetY - _lastDragY);
        if (moved > 0.5f) {
            _lastDragY = _targetY;
            OnRightDragScrolled?.Invoke(this, moved);
        }

        if (UseScrollEasing) {
            ApplyTween();
        } else {
            SnapToTarget();
        }
    }

    private void SnapToTarget() {
        _scrollTween?.Kill();
        _scrollTween = null;
        if (content != null) {
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, _targetY);
        }
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
        StopFling();
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
        StopFling();
        if (content == null || viewport == null) {
            _targetY = 0f;
            return;
        }

        float maxOffset = Math.Max(0f, content.rect.height - viewport.rect.height);
        _targetY = MathCompat.Clamp(offsetY, 0f, maxOffset);
        _scrollTween?.Kill();
        _scrollTween = null;

        if (instant || !UseScrollEasing) {
            SnapToTarget();
        } else {
            ApplyTween();
        }
    }

    /// <summary>True when the pointer is inside the scroll area (viewport plus the
    /// root's padding around it). Wheel and right-drag only engage then, so
    /// stacked windows don't scroll each other.</summary>
    private bool IsPointerOverViewport() {
        if (viewport == null) {
            return false;
        }

        RectTransform area = GetComponent<RectTransform>();
        return (area != null && RectTransformUtility.RectangleContainsScreenPoint(
            area, O5Input.MousePosition, null))
            || RectTransformUtility.RectangleContainsScreenPoint(
                viewport, O5Input.MousePosition, null);
    }

    private static readonly Vector3[] Corners = new Vector3[4];

    /// <summary>True when the pointer is in the right padding strip: horizontally between the
    /// content's inner right edge (minus layout right padding) and this scroll view's right edge,
    /// vertically within the viewport. Empty when there is no right padding.</summary>
    private bool IsPointerOverRightPadding() {
        if (content == null || viewport == null || transform is not RectTransform root) {
            return false;
        }

        Vector2 mouse = O5Input.MousePosition;

        viewport.GetWorldCorners(Corners);
        float bottom = Corners[0].y;
        float top = Corners[1].y;
        if (mouse.y < bottom || mouse.y > top) {
            return false;
        }

        content.GetWorldCorners(Corners);
        float innerRight = Corners[2].x;
        var layout = content.GetComponent<VerticalLayoutGroup>();
        if (layout != null) {
            innerRight -= layout.padding.right * content.lossyScale.x;
        }

        root.GetWorldCorners(Corners);
        float outerRight = Corners[2].x;

        if (outerRight - innerRight < 1f) {
            return false;
        }

        return mouse.x >= innerRight && mouse.x <= outerRight;
    }
}
