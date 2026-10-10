// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using GTweens.Contexts;
using GTweens.Easings;
using GTweens.Extensions;
using GTweens.Tweens;
using UnityEngine;
using UnityEngine.UI;

#if IL2CPP
using MelonLoader;
#endif

namespace O5Kit.Core;

/// <summary>
/// Built-in tween backend on top of the embedded GTweens library (MIT, see
/// ThirdParty/GTweens). <see cref="AutoTweenRunner"/> picks LitMotion when
/// available (IL2CPP) and this runner otherwise, so plain Mono titles get a
/// real tween engine with no external dependency. Only GTweens' float core is
/// used; Unity Vector/Color tweens run through a 0→1 progress tween plus a
/// manual lerp, mirroring the old Simple runner's dead-target guards.
/// </summary>
public sealed class GTweenRunner : ITweenRunner {
    /// <summary>Shared instance.</summary>
    public static GTweenRunner Shared { get; } = new();

    private readonly GTweensContext _context = new();
    private GTweenPump? _pump;

    /// <inheritdoc/>
    public ITweenHandle TweenFloat(Func<float> getter, Action<float> setter, float to, float duration, Action? onComplete = null, O5Ease ease = O5Ease.OutSine)
        => Play(GTweenExtensions.Tween(() => getter(), v => setter(v), to, Math.Max(duration, 0.0001f)), ease, onComplete);

    /// <inheritdoc/>
    public ITweenHandle TweenColor(Func<Color> getter, Action<Color> setter, Color to, float duration, Action? onComplete = null, O5Ease ease = O5Ease.OutSine) {
        Color from = getter();
        return Progress(duration, ease, onComplete,
            t => setter(Color.LerpUnclamped(from, to, t)));
    }

    /// <inheritdoc/>
    public ITweenHandle TweenColor(Graphic graphic, Color to, float duration, Action? onComplete = null, O5Ease ease = O5Ease.OutSine) {
        Color from = graphic ? graphic.color : to;
        var target = graphic;
        return Progress(duration, ease, onComplete,
            t => {
                if (target) {
                    target.color = Color.Lerp(from, to, t);
                }
            });
    }

    /// <inheritdoc/>
    public ITweenHandle TweenAlpha(CanvasGroup canvasGroup, float to, float duration, Action? onComplete = null, O5Ease ease = O5Ease.OutSine) {
        float from = canvasGroup ? canvasGroup.alpha : to;
        var target = canvasGroup;
        return Progress(duration, ease, onComplete,
            t => {
                if (target) {
                    target.alpha = Mathf.LerpUnclamped(from, to, t);
                }
            });
    }

    /// <inheritdoc/>
    public ITweenHandle TweenAlpha(Graphic graphic, float to, float duration, Action? onComplete = null, O5Ease ease = O5Ease.OutSine) {
        float from = graphic ? graphic.color.a : to;
        var target = graphic;
        return Progress(duration, ease, onComplete,
            t => {
                if (target) {
                    var c = target.color;
                    c.a = Mathf.LerpUnclamped(from, to, t);
                    target.color = c;
                }
            });
    }

    /// <inheritdoc/>
    public ITweenHandle TweenAnchorPos(RectTransform rectTransform, Vector2 to, float duration, Action? onComplete = null, O5Ease ease = O5Ease.OutSine) {
        Vector2 from = rectTransform ? rectTransform.anchoredPosition : to;
        var target = rectTransform;
        return Progress(duration, ease, onComplete,
            t => {
                if (target) {
                    target.anchoredPosition = Vector2.LerpUnclamped(from, to, t);
                }
            });
    }

    /// <inheritdoc/>
    public ITweenHandle TweenSizeDelta(RectTransform rectTransform, Vector2 to, float duration, Action? onComplete = null, O5Ease ease = O5Ease.OutSine) {
        Vector2 from = rectTransform ? rectTransform.sizeDelta : to;
        var target = rectTransform;
        return Progress(duration, ease, onComplete,
            t => {
                if (target) {
                    target.sizeDelta = Vector2.LerpUnclamped(from, to, t);
                }
            });
    }

    /// <inheritdoc/>
    public ITweenHandle TweenScale(RectTransform rectTransform, Vector3 to, float duration, Action? onComplete = null, O5Ease ease = O5Ease.OutSine) {
        Vector3 from = rectTransform ? rectTransform.localScale : to;
        var target = rectTransform;
        return Progress(duration, ease, onComplete,
            t => {
                if (target) {
                    target.localScale = Vector3.LerpUnclamped(from, to, t);
                }
            });
    }

    /// <inheritdoc/>
    public ITweenHandle TweenOffsetMin(RectTransform rectTransform, Vector2 to, float duration, Action? onComplete = null, O5Ease ease = O5Ease.OutSine) {
        Vector2 from = rectTransform ? rectTransform.offsetMin : to;
        var target = rectTransform;
        return Progress(duration, ease, onComplete,
            t => {
                if (target) {
                    target.offsetMin = Vector2.LerpUnclamped(from, to, t);
                }
            });
    }

    /// <inheritdoc/>
    public ITweenHandle Delay(float seconds, Action? onComplete = null)
        => Progress(seconds, O5Ease.Linear, onComplete, _ => { });

    private ITweenHandle Progress(float duration, O5Ease ease, Action? onComplete, Action<float> fn) {
        float value = 0f;
        return Play(GTweenExtensions.Tween(() => value, v => {
            value = v;
            fn(v);
        }, 1f, Math.Max(duration, 0.0001f)), ease, onComplete);
    }

    private ITweenHandle Play(GTween tween, O5Ease ease, Action? onComplete) {
        tween.SetEasing(MapEase(ease));
        if (onComplete != null) {
            tween.OnComplete(onComplete);
        }
        _context.Play(tween);
        EnsurePump();
        return new Handle(tween);
    }

    /// <summary>Advances all active tweens by unscaled delta time. Called by the hidden pump.</summary>
    public void Update() {
        try {
            _context.Tick(Time.unscaledDeltaTime);
        } catch {
        }
    }

    private void EnsurePump() {
        try {
            if (_pump != null && !_pump.Equals(null)) {
                return;
            }
        } catch {
        }
        _pump = null;
        try {
            var go = new GameObject("O5GTweenPlayer");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            _pump = go.AddComponent<GTweenPump>();
            _pump.Owner = this;
        } catch {
            _pump = null;
        }
    }

    /// <summary>Maps every <see cref="O5Ease"/> to its GTweens twin (1:1 names).</summary>
    public static Easing MapEase(O5Ease ease) => ease switch {
        O5Ease.Linear => Easing.Linear,
        O5Ease.InSine => Easing.InSine,
        O5Ease.OutSine => Easing.OutSine,
        O5Ease.InOutSine => Easing.InOutSine,
        O5Ease.InQuad => Easing.InQuad,
        O5Ease.OutQuad => Easing.OutQuad,
        O5Ease.InOutQuad => Easing.InOutQuad,
        O5Ease.InCubic => Easing.InCubic,
        O5Ease.OutCubic => Easing.OutCubic,
        O5Ease.InOutCubic => Easing.InOutCubic,
        O5Ease.InQuart => Easing.InQuart,
        O5Ease.OutQuart => Easing.OutQuart,
        O5Ease.InOutQuart => Easing.InOutQuart,
        O5Ease.InQuint => Easing.InQuint,
        O5Ease.OutQuint => Easing.OutQuint,
        O5Ease.InOutQuint => Easing.InOutQuint,
        O5Ease.InExpo => Easing.InExpo,
        O5Ease.OutExpo => Easing.OutExpo,
        O5Ease.InOutExpo => Easing.InOutExpo,
        O5Ease.InCirc => Easing.InCirc,
        O5Ease.OutCirc => Easing.OutCirc,
        O5Ease.InOutCirc => Easing.InOutCirc,
        O5Ease.InBack => Easing.InBack,
        O5Ease.OutBack => Easing.OutBack,
        O5Ease.InOutBack => Easing.InOutBack,
        O5Ease.InElastic => Easing.InElastic,
        O5Ease.OutElastic => Easing.OutElastic,
        O5Ease.InOutElastic => Easing.InOutElastic,
        O5Ease.InBounce => Easing.InBounce,
        O5Ease.OutBounce => Easing.OutBounce,
        O5Ease.InOutBounce => Easing.InOutBounce,
        _ => Easing.Linear,
    };

    private sealed class Handle : ITweenHandle {
        private readonly GTween _tween;

        public Handle(GTween tween) => _tween = tween;

        public bool IsAlive {
            get {
                try {
                    return !_tween.IsCompletedOrKilled;
                } catch {
                    return false;
                }
            }
        }

        public void Kill(bool complete = false) {
            try {
                if (_tween.IsCompletedOrKilled) {
                    return;
                }
                if (complete) {
                    _tween.Complete();
                } else {
                    _tween.Kill();
                }
            } catch {
            }
        }
    }

#if IL2CPP
    [RegisterTypeInIl2Cpp]
#endif
    private sealed class GTweenPump
#if IL2CPP
        (IntPtr ptr) : MonoBehaviour(ptr)
#else
        : MonoBehaviour
#endif
    {
        /// <summary>Runner to pump. Assigned on creation.</summary>
        public GTweenRunner? Owner;

        private void Update() => Owner?.Update();
    }
}
