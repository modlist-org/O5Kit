// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using O5Kit.Behaviour;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace O5Kit.Core;

/// <summary>Instance-based window frame with Overlayer's look. O5Kit manages structure and z-order only: visibility and open/close animation belong to the consumer. The close button raises <see cref="CloseRequested"/> instead of closing.</summary>
public sealed class O5Window : O5Object {
    /// <summary>Options this window was built with.</summary>
    public O5WindowOptions Options { get; }

    /// <summary>Content area. Factories fill this.</summary>
    public RectTransform Content { get; }

    /// <summary>Header icon image. Inactive when <see cref="O5WindowOptions.Icon"/> is null.</summary>
    public Image IconImage { get; }

    /// <summary>Header title text.</summary>
    public TMPro.TextMeshProUGUI TitleText { get; }

    /// <summary>Fired when the close button is pressed. Hide or destroy the window yourself.</summary>
    public event Action<O5Window>? CloseRequested;

    /// <summary>Fired when the panel receives pointer-down (focus).</summary>
    public event Action<O5Window>? Focused;

    private ITweenHandle? _closeTween;

    private O5Window(string id, RectTransform panel, RectTransform content, Image iconImage, TMPro.TextMeshProUGUI titleText, O5WindowOptions options)
        : base(id, panel) {
        Content = content;
        IconImage = iconImage;
        TitleText = titleText;
        Options = options;
    }

    /// <summary>Builds a window under <paramref name="parent"/>. Visibility is untouched; show it yourself.</summary>
    /// <param name="parent">Canvas or layout to parent under.</param>
    /// <param name="options">Appearance and behaviour. Null uses defaults.</param>
    public static O5Window Create(Transform parent, O5WindowOptions? options = null) {
        options ??= new O5WindowOptions();
        var theme = O5Boot.Theme;
        var sprites = O5Boot.Sprites;

        var panel = new GameObject("Panel").AddComponent<RectTransform>();
        panel.SetParent(parent, false);
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = options.Size;
        panel.anchoredPosition = options.Position ?? Vector2.zero;

        var bg = panel.gameObject.AddComponent<Image>();
        bg.color = options.PanelColor ?? theme.PanelBG;
        bg.type = Image.Type.Sliced;
        bg.sprite = sprites.RoundedPanel;

        if (options.ClipContent) {
            panel.gameObject.AddComponent<RectMask2D>();
        }

        float topH = options.TopBarHeight;

        var topBar = new GameObject("TopBar").AddComponent<RectTransform>();
        topBar.SetParent(panel, false);
        topBar.anchorMin = new Vector2(0f, 1f);
        topBar.anchorMax = new Vector2(1f, 1f);
        topBar.pivot = new Vector2(0.5f, 1f);
        topBar.offsetMin = new Vector2(0f, -topH);
        topBar.offsetMax = Vector2.zero;

        if (options.Draggable) {
            topBar.gameObject.AddComponent<DragHandler>();
        }

        var topImage = topBar.gameObject.AddComponent<Image>();
        topImage.color = options.TopBarColor ?? theme.TopBar;
        topImage.type = Image.Type.Sliced;
        topImage.sprite = sprites.TopBar;

        bool showIcon = options.Icon != null;
        float iconSize = options.IconSize;

        var iconObj = new GameObject("Icon");
        iconObj.transform.SetParent(topBar, false);
        var iconRect = iconObj.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = new Vector2(14f, 0f);
        iconRect.sizeDelta = new Vector2(iconSize, iconSize);
        var iconImage = iconObj.AddComponent<Image>();
        iconImage.sprite = options.Icon;
        iconImage.preserveAspect = true;
        iconObj.SetActive(showIcon);

        var titleText = new GameObject("Title").AddComponent<TMPro.TextMeshProUGUI>();
        titleText.transform.SetParent(topBar, false);
        titleText.text = options.Title;
        titleText.font = O5Boot.Fonts.Medium;
        titleText.fontSize = options.TitleFontSize ?? theme.FontSizeBody;
        titleText.color = options.TitleColor ?? Color.white;
        titleText.alignment = TMPro.TextAlignmentOptions.Left;
        titleText.verticalAlignment = TMPro.VerticalAlignmentOptions.Middle;
        var titleRect = titleText.rectTransform;
        titleRect.anchorMin = Vector2.zero;
        titleRect.anchorMax = Vector2.one;
        titleRect.offsetMin = new Vector2(showIcon ? 14f + iconSize + 10f : 16f, 0f);
        titleRect.offsetMax = new Vector2(-64f, 0f);

        var content = new GameObject("Content").AddComponent<RectTransform>();
        content.SetParent(panel, false);
        content.anchorMin = Vector2.zero;
        content.anchorMax = Vector2.one;
        var pad = options.Padding;
        content.offsetMin = new Vector2(pad.left, pad.bottom);
        content.offsetMax = new Vector2(-pad.right, -(topH + pad.top));

        var window = new O5Window(
            options.Id ?? options.Title,
            panel, content, iconImage, titleText, options);

        if (options.ShowCloseButton) {
            BuildCloseButton(topBar, window);
        }

        if (options.ShowOutline) {
            var outline = new GameObject("Outline").AddComponent<Image>();
            outline.transform.SetParent(panel, false);
            outline.transform.SetAsLastSibling();
            outline.color = Color.white;
            outline.sprite = sprites.RoundedOutline;
            outline.type = Image.Type.Sliced;
            outline.raycastTarget = false;
            var outlineRect = outline.rectTransform;
            outlineRect.anchorMin = Vector2.zero;
            outlineRect.anchorMax = Vector2.one;
            outlineRect.offsetMin = outlineRect.offsetMax = Vector2.zero;
        }

        if (options.Resizable) {
            ResizeHandle.CreateResizeHandles(panel, parent as RectTransform);
        }

        var focusTrigger = panel.gameObject.AddComponent<EventTrigger>();
        UnityUtils.AddEvent(focusTrigger, EventTriggerType.PointerDown, () => window.Focused?.Invoke(window));

        return window;
    }

    /// <summary>Builds a window with a title and size. Shorthand for <see cref="Create(Transform, O5WindowOptions)"/>.</summary>
    /// <param name="parent">Canvas or layout to parent under.</param>
    /// <param name="title">Top bar title.</param>
    /// <param name="size">Initial panel size.</param>
    public static O5Window Create(Transform parent, string title, Vector2 size)
        => Create(parent, new O5WindowOptions { Title = title, Size = size });

    private static void BuildCloseButton(RectTransform topBar, O5Window window) {
        var close = new GameObject("Close").AddComponent<RectTransform>();
        close.SetParent(topBar, false);
        close.anchorMin = new Vector2(1f, 0.5f);
        close.anchorMax = new Vector2(1f, 0.5f);
        close.pivot = new Vector2(1f, 0.5f);
        close.anchoredPosition = new Vector2(-16f, 0f);
        close.sizeDelta = new Vector2(38f, 38f);

        var btn = close.gameObject.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
#if IL2CPP
        btn.onClick.AddListener(new Action(() => window.CloseRequested?.Invoke(window)));
#else
        btn.onClick.AddListener(() => window.CloseRequested?.Invoke(window));
#endif

        var bgObj = new GameObject("Bg");
        bgObj.transform.SetParent(close, false);
        var bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        var bgImage = bgObj.AddComponent<Image>();
        bgImage.sprite = O5Boot.Sprites.Circle;
        bgImage.color = new Color(0.886f, 0.404f, 0.427f, 0f);

        var xObj = new GameObject("X");
        xObj.transform.SetParent(close, false);
        var xRect = xObj.AddComponent<RectTransform>();
        xRect.anchorMin = Vector2.zero;
        xRect.anchorMax = Vector2.one;
        xRect.offsetMin = new Vector2(4f, 4f);
        xRect.offsetMax = new Vector2(-4f, -4f);
        var xImage = xObj.AddComponent<Image>();
        xImage.sprite = O5Boot.Sprites.Icon("x") ?? O5Boot.Sprites.Circle;
        xImage.preserveAspect = true;

        var trigger = close.gameObject.AddComponent<EventTrigger>();
        var bg = bgImage;
        UnityUtils.AddEvents(trigger,
            (EventTriggerType.PointerEnter, () => {
                window._closeTween?.Kill();
                window._closeTween = O5Boot.Tween.TweenFloat(
                    () => bg.color.a,
                    v => {
                        if (bg) {
                            var c = bg.color;
                            c.a = v;
                            bg.color = c;
                        }
                    },
                    1f, 0.12f);
            }),
            (EventTriggerType.PointerExit, () => {
                window._closeTween?.Kill();
                window._closeTween = O5Boot.Tween.TweenFloat(
                    () => bg.color.a,
                    v => {
                        if (bg) {
                            var c = bg.color;
                            c.a = v;
                            bg.color = c;
                        }
                    },
                    0f, 0.12f);
            })
        );
    }

    /// <summary>Moves the window above its siblings.</summary>
    public void BringToFront() {
        if (IsDisposed) {
            return;
        }

        Rect.transform.SetAsLastSibling();
    }

    /// <summary>Replaces the header title.</summary>
    /// <param name="title">New title.</param>
    public void SetTitle(string title) {
        if (TitleText) {
            TitleText.text = title;
        }
    }

    /// <summary>Replaces the header icon. Null hides the icon slot.</summary>
    /// <param name="icon">New icon, or null to hide.</param>
    public void SetIcon(Sprite? icon) {
        if (IconImage) {
            IconImage.sprite = icon;
            IconImage.gameObject.SetActive(icon != null);
        }
    }

    /// <inheritdoc/>
    public override void Dispose() {
        if (IsDisposed) {
            return;
        }

        _closeTween?.Kill();
        _closeTween = null;
        base.Dispose();
        UnityEngine.Object.Destroy(Rect.gameObject);
    }
}
