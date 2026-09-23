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

    private Vector2 _startMouse;
    private Vector2 _startSize;
    private Vector2 _startPos;

    /// <summary>Minimum panel width before UI scale.</summary>
    public const float MIN_WIDTH = 900f;
    /// <summary>Minimum panel height before UI scale.</summary>
    public const float MIN_HEIGHT = 500f;

    private void Awake() {
        var trigger = gameObject.AddComponent<EventTrigger>();

        UnityUtils.AddEvents(trigger,
            (EventTriggerType.PointerDown, OnPointerDownInternal),
            (EventTriggerType.Drag, OnDragInternal)
        );
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
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            PanelParent,
            O5Input.MousePosition,
            null,
            out Vector2 currentMouse
        );

        Vector2 delta = currentMouse - _startMouse;
        Vector2 newSize = _startSize;
        Vector2 newPos = _startPos;

        float minW = MIN_WIDTH / O5Boot.Config.UIScale;
        float minH = MIN_HEIGHT / O5Boot.Config.UIScale;

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
        ResizeHandleType.TopLeft,
        ResizeHandleType.Top,
        ResizeHandleType.TopRight,

        ResizeHandleType.Left,
        ResizeHandleType.Right,

        ResizeHandleType.BottomLeft,
        ResizeHandleType.Bottom,
        ResizeHandleType.BottomRight
    };

    private const float HANDLE_CORNER = 26f;
    private const float HANDLE_SIDE = 12f;

    /// <summary>Creates all eight edge/corner handles around <paramref name="panel"/>.</summary>
    /// <param name="panel">Panel to make resizable.</param>
    /// <param name="panelParent">Coordinate space for measuring drags.</param>
    public static void CreateResizeHandles(RectTransform panel, RectTransform panelParent) {
        foreach (ResizeHandleType type in HandleOrder) {
            GameObject handle = new($"Resize_{type}");
            handle.transform.SetParent(panel, false);

            RectTransform rect = handle.AddComponent<RectTransform>();

            bool isCorner =
                type is ResizeHandleType.TopLeft
                or ResizeHandleType.TopRight
                or ResizeHandleType.BottomLeft
                or ResizeHandleType.BottomRight;

            if (isCorner) {
                rect.sizeDelta = new Vector2(HANDLE_CORNER, HANDLE_CORNER);
            }

            switch (type) {
                case ResizeHandleType.Top:
                    rect.anchorMin = new Vector2(0f, 1f);
                    rect.anchorMax = new Vector2(1f, 1f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.offsetMin = new Vector2(HANDLE_SIDE, -HANDLE_SIDE);
                    rect.offsetMax = new Vector2(-HANDLE_SIDE, HANDLE_SIDE);
                    rect.anchoredPosition = Vector2.zero;
                    break;

                case ResizeHandleType.Bottom:
                    rect.anchorMin = new Vector2(0f, 0f);
                    rect.anchorMax = new Vector2(1f, 0f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.offsetMin = new Vector2(HANDLE_SIDE, -HANDLE_SIDE);
                    rect.offsetMax = new Vector2(-HANDLE_SIDE, HANDLE_SIDE);
                    rect.anchoredPosition = Vector2.zero;
                    break;

                case ResizeHandleType.Left:
                    rect.anchorMin = new Vector2(0f, 0f);
                    rect.anchorMax = new Vector2(0f, 1f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.offsetMin = new Vector2(-HANDLE_SIDE, HANDLE_SIDE);
                    rect.offsetMax = new Vector2(HANDLE_SIDE, -HANDLE_SIDE);
                    rect.anchoredPosition = Vector2.zero;
                    break;

                case ResizeHandleType.Right:
                    rect.anchorMin = new Vector2(1f, 0f);
                    rect.anchorMax = new Vector2(1f, 1f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.offsetMin = new Vector2(-HANDLE_SIDE, HANDLE_SIDE);
                    rect.offsetMax = new Vector2(HANDLE_SIDE, -HANDLE_SIDE);
                    rect.anchoredPosition = Vector2.zero;
                    break;

                case ResizeHandleType.TopLeft:
                    rect.anchorMin = new Vector2(0f, 1f);
                    rect.anchorMax = new Vector2(0f, 1f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = new Vector2(-HANDLE_CORNER * 0.5f, HANDLE_CORNER * 0.5f);
                    break;

                case ResizeHandleType.TopRight:
                    rect.anchorMin = new Vector2(1f, 1f);
                    rect.anchorMax = new Vector2(1f, 1f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = new Vector2(HANDLE_CORNER * 0.5f, HANDLE_CORNER * 0.5f);
                    break;

                case ResizeHandleType.BottomLeft:
                    rect.anchorMin = new Vector2(0f, 0f);
                    rect.anchorMax = new Vector2(0f, 0f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = new Vector2(-HANDLE_CORNER * 0.5f, -HANDLE_CORNER * 0.5f);
                    break;

                case ResizeHandleType.BottomRight:
                    rect.anchorMin = new Vector2(1f, 0f);
                    rect.anchorMax = new Vector2(1f, 0f);
                    rect.pivot = new Vector2(0.5f, 0.5f);
                    rect.anchoredPosition = new Vector2(HANDLE_CORNER * 0.5f, -HANDLE_CORNER * 0.5f);
                    break;
            }

            rect.anchoredPosition = Vector2.zero;

            Image image = handle.AddComponent<Image>();
            image.sprite = O5Boot.Sprites.Circle;
            image.color = Color.clear;

            ResizeHandle resize = handle.AddComponent<ResizeHandle>();

            resize.Type = type;
            resize.Panel = panel;
            resize.PanelParent = panelParent;
        }
    }
}
