// SPDX-License-Identifier: LGPL-3.0-or-later

using UnityEngine;
using UnityEngine.UI;
using O5Kit.Input;

namespace O5Kit.Core;

/// <summary>Floating tooltip that follows the cursor. Initialize once, tick per frame.</summary>
public static class O5Tooltip {
    private const float ShowDelay = 0.14f;
    private const float FadeDuration = 0.16f;

    private static GameObject? _obj;
    private static RectTransform? _rect;
    private static CanvasGroup? _canvas;
    private static TMPro.TextMeshProUGUI? _text;

    private static Vector2 _velocity;
    private static bool _visible;
    private static float _showTimer;
    private static ITweenHandle? _fade;

    /// <summary>Builds the hidden tooltip under <paramref name="parent"/>.</summary>
    /// <param name="parent">Canvas to parent under. Requires <see cref="O5Boot"/> sprites and fonts.</param>
    public static void Initialize(Transform parent) {
        Dispose();

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
        img.sprite = O5Boot.Sprites.RoundedControl;
        img.type = Image.Type.Sliced;

        _text = new GameObject("Text").AddComponent<TMPro.TextMeshProUGUI>();
        _text.transform.SetParent(_obj.transform, false);
        _text.font = O5Boot.Fonts.Regular;
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
    public static void Tick() {
        if (!O5Boot.Config.TooltipEnabled || !_visible || _obj == null || _rect == null) {
            return;
        }

        if (_showTimer > 0f) {
            _showTimer -= Time.unscaledDeltaTime;
            if (_showTimer <= 0f && _canvas != null) {
                _fade?.Kill();
                var c = _canvas;
                _fade = O5Boot.Tween.TweenFloat(() => c.alpha, v => c.alpha = v, 1f, FadeDuration);
            }

            return;
        }

        float scale = O5Boot.Config.UIScale;
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
    public static void Show(string tip) {
        if (!O5Boot.Config.TooltipEnabled || _obj == null || _rect == null || _text == null || _canvas == null) {
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
    public static void Hide() {
        if (!O5Boot.Config.TooltipEnabled || _obj == null || _canvas == null) {
            return;
        }

        _fade?.Kill();
        _showTimer = 0f;
        var c = _canvas;
        var o = _obj;
        _fade = O5Boot.Tween.TweenFloat(() => c.alpha, v => c.alpha = v, 0f, FadeDuration, () => {
            _visible = false;
            o.SetActive(false);
        });
    }

    /// <summary>Hides and destroys the tooltip.</summary>
    public static void Dispose() {
        _visible = false;
        _showTimer = 0f;
        _velocity = Vector2.zero;
        _fade?.Kill();
        _fade = null;

        if (_obj != null) {
            Object.Destroy(_obj);
        }

        _obj = null;
        _rect = null;
        _canvas = null;
        _text = null;
    }
}
