// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using O5Kit.Core;
using O5Kit.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

#if IL2CPP
using MelonLoader;
#endif

namespace O5Kit.Behaviour;

/// <summary>Edge/corner resize directions.</summary>
public enum ResizeHandleType {
    Top,
    Left,
    Right,
    Bottom,

    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight
}

#if IL2CPP
[RegisterTypeInIl2Cpp]
#endif
/// <summary>Panel edge/corner resize handle. Created via <see cref="CreateResizeHandles"/>.</summary>
public class ResizeHandle
#if IL2CPP
    (IntPtr ptr) : MonoBehaviour(ptr)
#else
    : MonoBehaviour
#endif
{
    /// <summary>Handle direction.</summary>
    public ResizeHandleType Type;
    /// <summary>Panel being resized.</summary>
    public RectTransform Panel = null!;
    /// <summary>Coordinate space the drag is measured in (usually the canvas rect).</summary>
    public RectTransform PanelParent = null!;

    /// <summary>Owning kit context. Assigned by <see cref="CreateResizeHandles"/>; required for drag math.</summary>
    public O5Context? Ctx { get; set; }

    private Vector2 _startMouse;
    private Vector2 _startSize;
    private Vector2 _startPos;
    private bool _dragging;

    /// <summary>Default minimum panel width before UI scale (Overlayer panel size).</summary>
    public static float DefaultMinWidth = 900f;
    /// <summary>Default minimum panel height before UI scale (Overlayer panel size).</summary>
    public static float DefaultMinHeight = 500f;

    /// <summary>Minimum panel width before UI scale. Non-positive falls back to <see cref="DefaultMinWidth"/>.</summary>
    public float minWidthBeforeScale = -1f;
    /// <summary>Minimum panel height before UI scale. Non-positive falls back to <see cref="DefaultMinHeight"/>.</summary>
    public float minHeightBeforeScale = -1f;

    private void Awake() {
        var trigger = gameObject.AddComponent<EventTrigger>();

        UnityUtils.AddEvents(trigger,
            (EventTriggerType.PointerDown, OnPointerDownInternal),
            (EventTriggerType.Drag, OnDragInternal),
            (EventTriggerType.PointerEnter, OnPointerEnterInternal),
            (EventTriggerType.PointerExit, OnPointerExitInternal),
            (EventTriggerType.PointerUp, OnPointerUpInternal),
            (EventTriggerType.Cancel, OnPointerUpInternal)
        );
    }

    private void OnPointerUpInternal() {
        _dragging = false;
        ShowIndicator(false);
    }

    private void OnPointerEnterInternal() {
        ShowIndicator(true);
    }

    private void OnPointerExitInternal() {
        if(!_dragging) {
            ShowIndicator(false);
        }
    }

    private void OnDisable() {
        _dragging = false;
        ShowIndicator(false);
    }

    private static ResizeHandle? _cursorOwner;
    private static readonly Vector2 CursorHotspot = new(16f, 16f);

    private void ShowIndicator(bool visible) {
        if(visible) {
            _cursorOwner = this;
            try {
                Cursor.SetCursor(O5ResizeCursor.GetCursorTexture(Type), CursorHotspot, CursorMode.Auto);
            } catch {
            }
        } else if(_cursorOwner == this) {
            _cursorOwner = null;
            try {
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            } catch {
            }
        }
    }

    private void OnPointerDownInternal() {
        _startSize = Panel.sizeDelta;
        _startPos = Panel.anchoredPosition;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            PanelParent,
            O5Input.MousePosition,
            null,
            out _startMouse
        );
    }

    /// <summary>Applies the in-progress resize. Wired to the drag event.</summary>
    public void OnDragInternal() {
        _dragging = true;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            PanelParent,
            O5Input.MousePosition,
            null,
            out Vector2 currentMouse
        );

        Vector2 delta = currentMouse - _startMouse;
        Vector2 newSize = _startSize;
        Vector2 newPos = _startPos;

        O5Context ctx = Ctx ?? throw new InvalidOperationException(
            "O5Kit: ResizeHandle.Ctx is not set. Create handles via ResizeHandle.CreateResizeHandles(ctx, ...).");
        float minW = (minWidthBeforeScale > 0f ? minWidthBeforeScale : DefaultMinWidth) / ctx.Config.UIScale;
        float minH = (minHeightBeforeScale > 0f ? minHeightBeforeScale : DefaultMinHeight) / ctx.Config.UIScale;

        Vector2 pivot = Panel.pivot;

        if (Type is ResizeHandleType.Right or ResizeHandleType.TopRight or ResizeHandleType.BottomRight) {
            newSize.x = Math.Max(minW, _startSize.x + delta.x);
        } else if (Type is ResizeHandleType.Left or ResizeHandleType.TopLeft or ResizeHandleType.BottomLeft) {
            newSize.x = Math.Max(minW, _startSize.x - delta.x);
        }

        if (Type is ResizeHandleType.Top or ResizeHandleType.TopLeft or ResizeHandleType.TopRight) {
            newSize.y = Math.Max(minH, _startSize.y + delta.y);
        } else if (Type is ResizeHandleType.Bottom or ResizeHandleType.BottomLeft or ResizeHandleType.BottomRight) {
            newSize.y = Math.Max(minH, _startSize.y - delta.y);
        }

        Vector2 sizeDiff = newSize - _startSize;

        if (Type is ResizeHandleType.Right or ResizeHandleType.TopRight or ResizeHandleType.BottomRight) {
            newPos.x = _startPos.x + (sizeDiff.x * (1f - pivot.x));
        } else if (Type is ResizeHandleType.Left or ResizeHandleType.TopLeft or ResizeHandleType.BottomLeft) {
            newPos.x = _startPos.x - (sizeDiff.x * pivot.x);
        }

        if (Type is ResizeHandleType.Top or ResizeHandleType.TopLeft or ResizeHandleType.TopRight) {
            newPos.y = _startPos.y + (sizeDiff.y * (1f - pivot.y));
        } else if (Type is ResizeHandleType.Bottom or ResizeHandleType.BottomLeft or ResizeHandleType.BottomRight) {
            newPos.y = _startPos.y - (sizeDiff.y * pivot.y);
        }

        Panel.sizeDelta = newSize;
        Panel.anchoredPosition = newPos;
    }

    private static readonly ResizeHandleType[] HandleOrder = {
        ResizeHandleType.Top,
        ResizeHandleType.Bottom,
        ResizeHandleType.Left,
        ResizeHandleType.Right,

        ResizeHandleType.TopLeft,
        ResizeHandleType.TopRight,
        ResizeHandleType.BottomLeft,
        ResizeHandleType.BottomRight
    };

    /// <summary>Outward thickness of edge handles. Handles sit fully outside the panel edge
    /// so clicks just inside the border reach the panel's own controls.</summary>
    private const float HANDLE_SIDE = 12f;
    /// <summary>Outward size of corner handles (diagonal square beyond each corner).</summary>
    private const float HANDLE_CORNER = 16f;

    /// <summary>Creates all eight edge/corner handles around <paramref name="panel"/>.
    /// Handles live in a sibling "ResizeFrame" that tracks the panel, so a clipping mask on the
    /// panel can't swallow their raycasts.</summary>
    /// <param name="ctx">Owning kit context.</param>
    /// <param name="panel">Panel to make resizable.</param>
    /// <param name="panelParent">Coordinate space for measuring drags.</param>
    /// <param name="minWidth">Minimum width before UI scale. Non-positive uses <see cref="DefaultMinWidth"/>.</param>
    /// <param name="minHeight">Minimum height before UI scale. Non-positive uses <see cref="DefaultMinHeight"/>.</param>
    /// <returns>The frame holding the handles (its children are the handles).</returns>
    public static RectTransform CreateResizeHandles(O5Context ctx, RectTransform panel, RectTransform panelParent, float minWidth = -1f, float minHeight = -1f) {
        RectTransform frame = new GameObject($"{panel.name}_ResizeFrame").AddComponent<RectTransform>();
        frame.SetParent(panel.parent, false);
        frame.SetSiblingIndex(panel.GetSiblingIndex() + 1);
        frame.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        ResizeFrameFollower follower = frame.gameObject.AddComponent<ResizeFrameFollower>();
        follower.Panel = panel;
        follower.Sync();

        foreach (ResizeHandleType type in HandleOrder) {
            GameObject handle = new($"Resize_{type}");
            handle.transform.SetParent(frame, false);

            RectTransform rect = handle.AddComponent<RectTransform>();
            rect.pivot = new Vector2(0.5f, 0.5f);

            switch (type) {
                case ResizeHandleType.Top:
                    rect.anchorMin = new Vector2(0f, 1f);
                    rect.anchorMax = new Vector2(1f, 1f);
                    rect.offsetMin = new Vector2(0f, 0f);
                    rect.offsetMax = new Vector2(0f, HANDLE_SIDE);
                    break;

                case ResizeHandleType.Bottom:
                    rect.anchorMin = new Vector2(0f, 0f);
                    rect.anchorMax = new Vector2(1f, 0f);
                    rect.offsetMin = new Vector2(0f, -HANDLE_SIDE);
                    rect.offsetMax = new Vector2(0f, 0f);
                    break;

                case ResizeHandleType.Left:
                    rect.anchorMin = new Vector2(0f, 0f);
                    rect.anchorMax = new Vector2(0f, 1f);
                    rect.offsetMin = new Vector2(-HANDLE_SIDE, 0f);
                    rect.offsetMax = new Vector2(0f, 0f);
                    break;

                case ResizeHandleType.Right:
                    rect.anchorMin = new Vector2(1f, 0f);
                    rect.anchorMax = new Vector2(1f, 1f);
                    rect.offsetMin = new Vector2(0f, 0f);
                    rect.offsetMax = new Vector2(HANDLE_SIDE, 0f);
                    break;

                case ResizeHandleType.TopLeft:
                    rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
                    rect.offsetMin = new Vector2(-HANDLE_CORNER, 0f);
                    rect.offsetMax = new Vector2(0f, HANDLE_CORNER);
                    break;

                case ResizeHandleType.TopRight:
                    rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
                    rect.offsetMin = new Vector2(0f, 0f);
                    rect.offsetMax = new Vector2(HANDLE_CORNER, HANDLE_CORNER);
                    break;

                case ResizeHandleType.BottomLeft:
                    rect.anchorMin = rect.anchorMax = new Vector2(0f, 0f);
                    rect.offsetMin = new Vector2(-HANDLE_CORNER, -HANDLE_CORNER);
                    rect.offsetMax = new Vector2(0f, 0f);
                    break;

                case ResizeHandleType.BottomRight:
                    rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
                    rect.offsetMin = new Vector2(0f, -HANDLE_CORNER);
                    rect.offsetMax = new Vector2(HANDLE_CORNER, 0f);
                    break;
            }

            Image image = handle.AddComponent<Image>();
            image.color = Color.clear;

            ResizeHandle resize = handle.AddComponent<ResizeHandle>();

            resize.Ctx = ctx;
            resize.Type = type;
            resize.Panel = panel;
            resize.PanelParent = panelParent;
            resize.minWidthBeforeScale = minWidth;
            resize.minHeightBeforeScale = minHeight;
        }

        follower.Sync();
        return frame;
    }
}

#if IL2CPP
[RegisterTypeInIl2Cpp]
#endif
/// <summary>Keeps a resize frame glued to its panel: same rect, active state and sibling slot
/// (right above the panel). Destroys itself with the panel.</summary>
public class ResizeFrameFollower
#if IL2CPP
    (IntPtr ptr) : MonoBehaviour(ptr)
#else
    : MonoBehaviour
#endif
{
    /// <summary>Panel to track.</summary>
    public RectTransform? Panel;

    private void LateUpdate() => Sync();

    /// <summary>Copies the panel's layout, visibility and sibling order onto this frame.</summary>
    public void Sync() {
        if (Panel == null) {
            Destroy(gameObject);
            return;
        }

        var self = (RectTransform)transform;
        if (self.parent != Panel.parent) {
            self.SetParent(Panel.parent, false);
        }

        self.anchorMin = Panel.anchorMin;
        self.anchorMax = Panel.anchorMax;
        self.pivot = Panel.pivot;
        self.sizeDelta = Panel.sizeDelta;
        self.anchoredPosition = Panel.anchoredPosition;
        self.localScale = Panel.localScale;
        self.localRotation = Panel.localRotation;

        CanvasGroup? group = Panel.GetComponent<CanvasGroup>();
        bool show = Panel.gameObject.activeSelf && (group == null || group.blocksRaycasts);
        for (int i = 0; i < self.childCount; i++) {
            GameObject child = self.GetChild(i).gameObject;
            if (child.activeSelf != show) {
                child.SetActive(show);
            }
        }

        int panelIndex = Panel.GetSiblingIndex();
        int index = self.GetSiblingIndex();
        if (index < panelIndex) {
            self.SetSiblingIndex(panelIndex);
        } else if (index > panelIndex + 1) {
            self.SetSiblingIndex(panelIndex + 1);
        }
    }
}
