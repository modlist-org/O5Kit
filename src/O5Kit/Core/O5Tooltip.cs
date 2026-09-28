// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using UnityEngine;
using UnityEngine.UI;
using O5Kit.Input;

namespace O5Kit.Core;

/// <summary>Floating tooltip that follows the cursor. Owned by an <see cref="O5Context"/>: initialize once under your canvas, tick per frame.</summary>
public sealed class O5Tooltip : IDisposable {
    private const float ShowDelay = 0.14f;
    private const float FadeDuration = 0.16f;

    private readonly O5Context _ctx;

    private GameObject? _obj;
    private RectTransform? _rect;
    private CanvasGroup? _canvas;
    private TMPro.TextMeshProUGUI? _text;

    private Vector2 _velocity;
    private bool _visible;
    private float _showTimer;
    private ITweenHandle? _fade;
    private bool _disposed;

    /// <summary>Creates an uninitialized tooltip bound to <paramref name="ctx"/>.</summary>
    /// <param name="ctx">Owning kit context.</param>
    public O5Tooltip(O5Context ctx) {
        _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
    }

    /// <summary>Builds the hidden tooltip under <paramref name="parent"/>.</summary>
    /// <param name="parent">Canvas to parent under.</param>
    public void Initialize(Transform parent) {
        Dispose();

        _disposed = false;
        _obj = new GameObject("Tooltip");
        _obj.transform.SetParent(parent, false);

        _rect = _obj.AddComponent<RectTransform>();
        _rect.anchorMin = _rect.anchorMax = new Vector2(0.5f, 0.5f);
        _rect.pivot = new Vector2(0f, 1f);

        _canvas = _obj.AddComponent<CanvasGroup>();
        _canvas.alpha = 0f;
        _canvas.blocksRaycasts = false;

        var bg = new GameObject("Bg").AddComponent<RectTransform>();
        bg.SetParent(_obj.transform, false);
        bg.anchorMin = Vector2.zero;
        bg.anchorMax = Vector2.one;
        bg.offsetMin = bg.offsetMax = Vector2.zero;

        var img = bg.gameObject.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.6f);
        img.sprite = _ctx.Sprites.RoundedControl;
        img.type = Image.Type.Sliced;

        _text = new GameObject("Text").AddComponent<TMPro.TextMeshProUGUI>();
        _text.transform.SetParent(_obj.transform, false);
        _text.font = _ctx.Fonts.Regular;
        _text.fontSize = 20f;
        _text.color = Color.white;
        _text.alignment = TMPro.TextAlignmentOptions.Left;
        var tr = _text.rectTransform;
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = new Vector2(16f, 16f);
        tr.offsetMax = new Vector2(-16f, -16f);

        _obj.SetActive(false);
    }

    /// <summary>Advances the show delay and cursor follow. Call once per frame.</summary>
    public void Tick() {
        if (!_ctx.Config.TooltipEnabled || !_visible || _obj == null || _rect == null) {
            return;
        }

        if (_showTimer > 0f) {
            _showTimer -= Time.unscaledDeltaTime;
            if (_showTimer <= 0f && _canvas != null) {
                _fade?.Kill();
                var c = _canvas;
                _fade = _ctx.Tween.TweenFloat(() => c.alpha, v => c.alpha = v, 1f, FadeDuration);
            }

            return;
        }

        float scale = _ctx.Config.UIScale;
        Vector2 target = O5Input.MousePosition + new Vector2(24f, -28f) * scale;
        Vector2 size = _rect.sizeDelta * scale;

        target.x = Mathf.Clamp(target.x, 0f, Screen.width - size.x);
        target.y = Mathf.Clamp(target.y, 0f, Screen.height - size.y);

        _rect.position = Vector2.SmoothDamp(
            _rect.position, target, ref _velocity, 0.02f,
            float.PositiveInfinity, Time.unscaledDeltaTime);
    }

    /// <summary>Shows a tooltip near the cursor after a short delay.</summary>
    /// <param name="tip">Text to display. Ignored when tooltips are disabled.</param>
    public void Show(string tip) {
        if (!_ctx.Config.TooltipEnabled || _obj == null || _rect == null || _text == null || _canvas == null) {
            return;
        }

        _fade?.Kill();

        _obj.SetActive(true);
        _obj.transform.SetAsLastSibling();
        _text.text = tip;

        Vector2 size = _text.GetPreferredValues(tip);
        _rect.sizeDelta = new Vector2(size.x + 32f, size.y + 32f);
        _rect.position = O5Input.MousePosition + new Vector2(20f, -20f);

        _canvas.alpha = 0f;
        _visible = true;
        _showTimer = ShowDelay;
    }

    /// <summary>Fades the tooltip out. Ignored when tooltips are disabled.</summary>
    public void Hide() {
        if (!_ctx.Config.TooltipEnabled || _obj == null || _canvas == null) {
            return;
        }

        _fade?.Kill();
        _showTimer = 0f;
        var c = _canvas;
        var o = _obj;
        _fade = _ctx.Tween.TweenFloat(() => c.alpha, v => c.alpha = v, 0f, FadeDuration, () => {
            _visible = false;
            o.SetActive(false);
        });
    }

    /// <summary>Hides and destroys the tooltip.</summary>
    public void Dispose() {
        if (_disposed) {
            return;
        }

        _disposed = true;
        _visible = false;
        _showTimer = 0f;
        _velocity = Vector2.zero;
        _fade?.Kill();
        _fade = null;

        if (_obj != null) {
            UnityEngine.Object.Destroy(_obj);
        }

        _obj = null;
        _rect = null;
        _canvas = null;
        _text = null;
    }
}
