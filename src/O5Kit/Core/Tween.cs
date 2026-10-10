// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Collections.Generic;
#if LITMOTION
using LitMotion;
using LitMotion.Extensions;
#endif
using UnityEngine;
using UnityEngine.UI;

#if IL2CPP
using MelonLoader;
#endif

namespace O5Kit.Core;

/// <summary>Opaque handle for a running tween.</summary>
public interface ITweenHandle {
    /// <summary>Stops the tween. Pass true to snap to its end value (and fire completion).</summary>
    /// <param name="complete">Snap to the end value instead of freezing mid-way.</param>
    void Kill(bool complete = false);

    /// <summary>Whether the tween is still playing.</summary>
    bool IsAlive { get; }
}

/// <summary>Minimal tween surface O5Kit controls need (float / color / alpha).</summary>
public interface ITweenRunner {
    /// <summary>Tweens a float from its current value to <paramref name="to"/>.</summary>
    /// <param name="getter">Reads the current value.</param>
    /// <param name="setter">Applies the eased value.</param>
    /// <param name="to">Target value.</param>
    /// <param name="duration">Duration in seconds (unscaled).</param>
    /// <param name="onComplete">Fired once when the tween finishes naturally. Not fired by <see cref="ITweenHandle.Kill(bool)"/> unless it completes.</param>
    /// <param name="ease">Easing curve.</param>
    ITweenHandle TweenFloat(System.Func<float> getter, System.Action<float> setter, float to, float duration, System.Action? onComplete = null, O5Ease ease = O5Ease.OutSine);

    /// <summary>Tweens a color from its current value to <paramref name="to"/>.</summary>
    /// <param name="getter">Reads the current value.</param>
    /// <param name="setter">Applies the eased value.</param>
    /// <param name="to">Target value.</param>
    /// <param name="duration">Duration in seconds (unscaled).</param>
    /// <param name="onComplete">Fired once when the tween finishes naturally. Not fired by <see cref="ITweenHandle.Kill(bool)"/> unless it completes.</param>
    /// <param name="ease">Easing curve.</param>
    ITweenHandle TweenColor(System.Func<Color> getter, System.Action<Color> setter, Color to, float duration, System.Action? onComplete = null, O5Ease ease = O5Ease.OutSine);

    /// <summary>Tweens a <see cref="Graphic"/> color directly (no getter/setter closures).</summary>
    ITweenHandle TweenColor(Graphic graphic, Color to, float duration, System.Action? onComplete = null, O5Ease ease = O5Ease.OutSine);

    /// <summary>Tweens a <see cref="CanvasGroup"/> alpha directly (no getter/setter closures).</summary>
    ITweenHandle TweenAlpha(CanvasGroup canvasGroup, float to, float duration, System.Action? onComplete = null, O5Ease ease = O5Ease.OutSine);

    /// <summary>Tweens a <see cref="Graphic"/> alpha channel directly (no getter/setter closures).</summary>
    ITweenHandle TweenAlpha(Graphic graphic, float to, float duration, System.Action? onComplete = null, O5Ease ease = O5Ease.OutSine);

    /// <summary>Tweens a <see cref="RectTransform"/> anchored position directly (no getter/setter closures).</summary>
    ITweenHandle TweenAnchorPos(RectTransform rectTransform, Vector2 to, float duration, System.Action? onComplete = null, O5Ease ease = O5Ease.OutSine);

    /// <summary>Tweens a <see cref="RectTransform"/> size delta directly (no getter/setter closures).</summary>
    ITweenHandle TweenSizeDelta(RectTransform rectTransform, Vector2 to, float duration, System.Action? onComplete = null, O5Ease ease = O5Ease.OutSine);

    /// <summary>Tweens a <see cref="RectTransform"/> local scale.</summary>
    ITweenHandle TweenScale(RectTransform rectTransform, Vector3 to, float duration, System.Action? onComplete = null, O5Ease ease = O5Ease.OutSine);

    /// <summary>Tweens a <see cref="RectTransform"/> offset min.</summary>
    ITweenHandle TweenOffsetMin(RectTransform rectTransform, Vector2 to, float duration, System.Action? onComplete = null, O5Ease ease = O5Ease.OutSine);

    /// <summary>Wait step that completes after <paramref name="seconds"/> (unscaled). For sequences.</summary>
    ITweenHandle Delay(float seconds, System.Action? onComplete = null);
}

#if LITMOTION
/// <summary>LitMotion-backed runner (default). Driven by a hidden pump through a manual dispatcher, so it works regardless of mod load order.</summary>
public sealed class LitMotionRunner : ITweenRunner {
    /// <summary>Shared instance used as the <see cref="O5Context"/> default unless overridden.</summary>
    public static LitMotionRunner Instance { get; } = new();

    private static Ease MapEase(O5Ease ease) => ease switch {
        O5Ease.Linear => Ease.Linear,
        O5Ease.InSine => Ease.InSine,
        O5Ease.OutSine => Ease.OutSine,
        O5Ease.InOutSine => Ease.InOutSine,
        O5Ease.InQuad => Ease.InQuad,
        O5Ease.OutQuad => Ease.OutQuad,
        O5Ease.InOutQuad => Ease.InOutQuad,
        O5Ease.InCubic => Ease.InCubic,
        O5Ease.OutCubic => Ease.OutCubic,
        O5Ease.InOutCubic => Ease.InOutCubic,
        O5Ease.OutExpo => Ease.OutExpo,
        O5Ease.OutCirc => Ease.OutCirc,
        O5Ease.OutBack => Ease.OutBack,
        O5Ease.InQuart => Ease.InQuart,
        O5Ease.OutQuart => Ease.OutQuart,
        O5Ease.InOutQuart => Ease.InOutQuart,
        O5Ease.InQuint => Ease.InQuint,
        O5Ease.OutQuint => Ease.OutQuint,
        O5Ease.InOutQuint => Ease.InOutQuint,
        O5Ease.InExpo => Ease.InExpo,
        O5Ease.InOutExpo => Ease.InOutExpo,
        O5Ease.InCirc => Ease.InCirc,
        O5Ease.InOutCirc => Ease.InOutCirc,
        O5Ease.InBack => Ease.InBack,
        O5Ease.InOutBack => Ease.InOutBack,
        O5Ease.InElastic => Ease.InElastic,
        O5Ease.OutElastic => Ease.OutElastic,
        O5Ease.InOutElastic => Ease.InOutElastic,
        O5Ease.InBounce => Ease.InBounce,
        O5Ease.OutBounce => Ease.OutBounce,
        O5Ease.InOutBounce => Ease.InOutBounce,
        _ => Ease.OutSine,
    };

    private readonly ManualMotionDispatcher _dispatcher = new();
    private LitMotionPump? _pump;

    /// <inheritdoc/>
    public ITweenHandle TweenFloat(System.Func<float> getter, System.Action<float> setter, float to, float duration, System.Action? onComplete = null, O5Ease ease = O5Ease.OutSine) {
        EnsurePump();
        var builder = LMotion.Create(getter(), to, Math.Max((duration), 0.0001f))
            .WithEase(MapEase(ease))
            .WithScheduler(_dispatcher.Scheduler);
        if (onComplete != null) {
            builder = builder.WithOnComplete(onComplete);
        }

        return new Handle(builder.Bind(setter));
    }

    /// <inheritdoc/>
    public ITweenHandle TweenColor(System.Func<Color> getter, System.Action<Color> setter, Color to, float duration, System.Action? onComplete = null, O5Ease ease = O5Ease.OutSine) {
        EnsurePump();
        var builder = LMotion.Create(getter(), to, Math.Max((duration), 0.0001f))
            .WithEase(MapEase(ease))
            .WithScheduler(_dispatcher.Scheduler);
        if (onComplete != null) {
            builder = builder.WithOnComplete(onComplete);
        }

        return new Handle(builder.Bind(setter));
    }

    /// <inheritdoc/>
    public ITweenHandle TweenColor(Graphic graphic, Color to, float duration, System.Action? onComplete = null, O5Ease ease = O5Ease.OutSine) {
        EnsurePump();
        Color from = graphic ? graphic.color : to;
        var builder = LMotion.Create(from, to, Math.Max((duration), 0.0001f))
            .WithEase(MapEase(ease))
            .WithScheduler(_dispatcher.Scheduler)
            .WithCancelOnError();
        if (onComplete != null) {
            builder = builder.WithOnComplete(onComplete);
        }

        return new Handle(builder.BindToColor(graphic));
    }

    /// <inheritdoc/>
    public ITweenHandle TweenAlpha(CanvasGroup canvasGroup, float to, float duration, System.Action? onComplete = null, O5Ease ease = O5Ease.OutSine) {
        EnsurePump();
        float from = canvasGroup ? canvasGroup.alpha : to;
        var builder = LMotion.Create(from, to, Math.Max((duration), 0.0001f))
            .WithEase(MapEase(ease))
            .WithScheduler(_dispatcher.Scheduler)
            .WithCancelOnError();
        if (onComplete != null) {
            builder = builder.WithOnComplete(onComplete);
        }

        return new Handle(builder.BindToAlpha(canvasGroup));
    }

    /// <inheritdoc/>
    public ITweenHandle TweenAlpha(Graphic graphic, float to, float duration, System.Action? onComplete = null, O5Ease ease = O5Ease.OutSine) {
        EnsurePump();
        float from = graphic ? graphic.color.a : to;
        var builder = LMotion.Create(from, to, Math.Max((duration), 0.0001f))
            .WithEase(MapEase(ease))
            .WithScheduler(_dispatcher.Scheduler)
            .WithCancelOnError();
        if (onComplete != null) {
            builder = builder.WithOnComplete(onComplete);
        }

        return new Handle(builder.BindToColorA(graphic));
    }

    /// <inheritdoc/>
    public ITweenHandle TweenAnchorPos(RectTransform rectTransform, Vector2 to, float duration, System.Action? onComplete = null, O5Ease ease = O5Ease.OutSine) {
        EnsurePump();
        Vector2 from = rectTransform ? rectTransform.anchoredPosition : to;
        var builder = LMotion.Create(from, to, Math.Max((duration), 0.0001f))
            .WithEase(MapEase(ease))
            .WithScheduler(_dispatcher.Scheduler)
            .WithCancelOnError();
        if (onComplete != null) {
            builder = builder.WithOnComplete(onComplete);
        }

        return new Handle(builder.BindToAnchoredPosition(rectTransform));
    }

    /// <inheritdoc/>
    public ITweenHandle TweenSizeDelta(RectTransform rectTransform, Vector2 to, float duration, System.Action? onComplete = null, O5Ease ease = O5Ease.OutSine) {
        EnsurePump();
        Vector2 from = rectTransform ? rectTransform.sizeDelta : to;
        var builder = LMotion.Create(from, to, Math.Max((duration), 0.0001f))
            .WithEase(MapEase(ease))
            .WithScheduler(_dispatcher.Scheduler)
            .WithCancelOnError();
        if (onComplete != null) {
            builder = builder.WithOnComplete(onComplete);
        }

        return new Handle(builder.BindToSizeDelta(rectTransform));
    }

    /// <inheritdoc/>
    public ITweenHandle TweenScale(RectTransform rectTransform, Vector3 to, float duration, System.Action? onComplete = null, O5Ease ease = O5Ease.OutSine) {
        EnsurePump();
        Vector3 from = rectTransform ? rectTransform.localScale : to;
        var builder = LMotion.Create(from, to, Math.Max((duration), 0.0001f))
            .WithEase(MapEase(ease))
            .WithScheduler(_dispatcher.Scheduler);
        if (onComplete != null) {
            builder = builder.WithOnComplete(onComplete);
        }

        return new Handle(builder.Bind(v => {
            if (rectTransform) {
                rectTransform.localScale = v;
            }
        }));
    }

    /// <inheritdoc/>
    public ITweenHandle TweenOffsetMin(RectTransform rectTransform, Vector2 to, float duration, System.Action? onComplete = null, O5Ease ease = O5Ease.OutSine) {
        EnsurePump();
        Vector2 from = rectTransform ? rectTransform.offsetMin : to;
        var builder = LMotion.Create(from, to, Math.Max((duration), 0.0001f))
            .WithEase(MapEase(ease))
            .WithScheduler(_dispatcher.Scheduler);
        if (onComplete != null) {
            builder = builder.WithOnComplete(onComplete);
        }

        return new Handle(builder.Bind(v => {
            if (rectTransform) {
                rectTransform.offsetMin = v;
            }
        }));
    }

    /// <inheritdoc/>
    public ITweenHandle Delay(float seconds, System.Action? onComplete = null) {
        EnsurePump();
        var builder = LMotion.Create(0f, 1f, Math.Max((seconds), 0.0001f))
            .WithScheduler(_dispatcher.Scheduler);
        if (onComplete != null) {
            builder = builder.WithOnComplete(onComplete);
        }

        return new Handle(builder.Bind(_ => { }));
    }

    /// <summary>Advances all motions by unscaled delta time. Called by the hidden pump.</summary>
    public void Update() => _dispatcher.Update(Time.unscaledDeltaTime);

    private void EnsurePump() {
        if (_pump != null) {
            return;
        }

        var go = new GameObject("O5LitMotionPlayer");
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.hideFlags = HideFlags.HideAndDontSave;
        _pump = go.AddComponent<LitMotionPump>();
        _pump.Owner = this;
    }

#if IL2CPP
    [RegisterTypeInIl2Cpp]
#endif
    private sealed class LitMotionPump
#if IL2CPP
        (IntPtr ptr) : MonoBehaviour(ptr)
#else
        : MonoBehaviour
#endif
    {
        /// <summary>Runner to pump. Assigned on creation.</summary>
        public LitMotionRunner? Owner;

        private void Update() => Owner?.Update();
    }

    private sealed class Handle : ITweenHandle {
        private MotionHandle _handle;

        public Handle(MotionHandle handle) => _handle = handle;

        public bool IsAlive => _handle.IsActive();

        public void Kill(bool complete = false) {
            if (complete) {
                _handle.TryComplete();
            } else {
                _handle.TryCancel();
            }
        }
    }
}
#endif

/// <summary>Zero-dependency unscaled-time runner. Fallback for LitMotion-less builds; good enough for hover/fade micro-animation.</summary>
public sealed class SimpleTweenRunner : ITweenRunner {
    /// <summary>Shared instance.</summary>
    public static SimpleTweenRunner Instance { get; } = new();

    private readonly List<Entry> _active = new();
    private SimplePump? _pump;

    /// <inheritdoc/>
    public ITweenHandle TweenFloat(System.Func<float> getter, System.Action<float> setter, float to, float duration, System.Action? onComplete = null, O5Ease ease = O5Ease.OutSine)
        => Add(t => setter(Mathf.LerpUnclamped(getter(), to, ApplyEase(ease, t))), duration, onComplete);

    /// <inheritdoc/>
    public ITweenHandle TweenColor(System.Func<Color> getter, System.Action<Color> setter, Color to, float duration, System.Action? onComplete = null, O5Ease ease = O5Ease.OutSine) {
        Color from = getter();
        return Add(t => setter(Color.LerpUnclamped(from, to, ApplyEase(ease, t))), duration, onComplete);
    }

    /// <inheritdoc/>
    public ITweenHandle TweenColor(Graphic graphic, Color to, float duration, System.Action? onComplete = null, O5Ease ease = O5Ease.OutSine) {
        Color from = graphic ? graphic.color : to;
        var target = graphic;
        return Add(t => {
            if (target) {
                target.color = Color.Lerp(from, to, ApplyEase(ease, t));
            }
        }, duration, onComplete);
    }

    /// <inheritdoc/>
    public ITweenHandle TweenAlpha(CanvasGroup canvasGroup, float to, float duration, System.Action? onComplete = null, O5Ease ease = O5Ease.OutSine) {
        float from = canvasGroup ? canvasGroup.alpha : to;
        var target = canvasGroup;
        return Add(t => {
            if (target) {
                target.alpha = Mathf.LerpUnclamped(from, to, ApplyEase(ease, t));
            }
        }, duration, onComplete);
    }

    /// <inheritdoc/>
    public ITweenHandle TweenAlpha(Graphic graphic, float to, float duration, System.Action? onComplete = null, O5Ease ease = O5Ease.OutSine) {
        float from = graphic ? graphic.color.a : to;
        var target = graphic;
        return Add(t => {
            if (target) {
                var c = target.color;
                c.a = Mathf.LerpUnclamped(from, to, ApplyEase(ease, t));
                target.color = c;
            }
        }, duration, onComplete);
    }

    /// <inheritdoc/>
    public ITweenHandle TweenAnchorPos(RectTransform rectTransform, Vector2 to, float duration, System.Action? onComplete = null, O5Ease ease = O5Ease.OutSine) {
        Vector2 from = rectTransform ? rectTransform.anchoredPosition : to;
        var target = rectTransform;
        return Add(t => {
            if (target) {
                target.anchoredPosition = Vector2.LerpUnclamped(from, to, ApplyEase(ease, t));
            }
        }, duration, onComplete);
    }

    /// <inheritdoc/>
    public ITweenHandle TweenSizeDelta(RectTransform rectTransform, Vector2 to, float duration, System.Action? onComplete = null, O5Ease ease = O5Ease.OutSine) {
        Vector2 from = rectTransform ? rectTransform.sizeDelta : to;
        var target = rectTransform;
        return Add(t => {
            if (target) {
                target.sizeDelta = Vector2.LerpUnclamped(from, to, ApplyEase(ease, t));
            }
        }, duration, onComplete);
    }

    /// <inheritdoc/>
    public ITweenHandle TweenScale(RectTransform rectTransform, Vector3 to, float duration, System.Action? onComplete = null, O5Ease ease = O5Ease.OutSine) {
        Vector3 from = rectTransform ? rectTransform.localScale : to;
        var target = rectTransform;
        return Add(t => {
            if (target) {
                target.localScale = Vector3.LerpUnclamped(from, to, ApplyEase(ease, t));
            }
        }, duration, onComplete);
    }

    /// <inheritdoc/>
    public ITweenHandle TweenOffsetMin(RectTransform rectTransform, Vector2 to, float duration, System.Action? onComplete = null, O5Ease ease = O5Ease.OutSine) {
        Vector2 from = rectTransform ? rectTransform.offsetMin : to;
        var target = rectTransform;
        return Add(t => {
            if (target) {
                target.offsetMin = Vector2.LerpUnclamped(from, to, ApplyEase(ease, t));
            }
        }, duration, onComplete);
    }

    /// <inheritdoc/>
    public ITweenHandle Delay(float seconds, System.Action? onComplete = null)
        => Add(_ => { }, seconds, onComplete);

    private static float ApplyEase(O5Ease ease, float t) => ease switch {
        O5Ease.Linear => t,
        O5Ease.InSine => 1f - Mathf.Cos(t * Mathf.PI * 0.5f),
        O5Ease.OutSine => Mathf.Sin(t * Mathf.PI * 0.5f),
        O5Ease.InOutSine => -(Mathf.Cos(Mathf.PI * t) - 1f) * 0.5f,
        O5Ease.InQuad => t * t,
        O5Ease.OutQuad => 1f - (1f - t) * (1f - t),
        O5Ease.InOutQuad => t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f,
        O5Ease.InCubic => t * t * t,
        O5Ease.OutCubic => 1f - Mathf.Pow(1f - t, 3f),
        O5Ease.InOutCubic => t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f,
        O5Ease.OutExpo => t >= 1f ? 1f : 1f - Mathf.Pow(2f, -10f * t),
        O5Ease.OutCirc => Mathf.Sqrt(1f - Mathf.Pow(t - 1f, 2f)),
        O5Ease.OutBack => 1f + 2.70158f * Mathf.Pow(t - 1f, 3f) + 1.70158f * Mathf.Pow(t - 1f, 2f),
        O5Ease.InQuart => t * t * t * t,
        O5Ease.OutQuart => 1f - Mathf.Pow(1f - t, 4f),
        O5Ease.InOutQuart => t < 0.5f ? 8f * t * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 4f) / 2f,
        O5Ease.InQuint => t * t * t * t * t,
        O5Ease.OutQuint => 1f - Mathf.Pow(1f - t, 5f),
        O5Ease.InOutQuint => t < 0.5f ? 16f * t * t * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 5f) / 2f,
        O5Ease.InExpo => t <= 0f ? 0f : Mathf.Pow(2f, 10f * t - 10f),
        O5Ease.InOutExpo => t <= 0f ? 0f : t >= 1f ? 1f : t < 0.5f ? Mathf.Pow(2f, 20f * t - 10f) / 2f : (2f - Mathf.Pow(2f, -20f * t + 10f)) / 2f,
        O5Ease.InCirc => 1f - Mathf.Sqrt(1f - t * t),
        O5Ease.InOutCirc => t < 0.5f ? (1f - Mathf.Sqrt(1f - 4f * t * t)) / 2f : (Mathf.Sqrt(1f - Mathf.Pow(-2f * t + 2f, 2f)) + 1f) / 2f,
        O5Ease.InBack => 2.70158f * t * t * t - 1.70158f * t * t,
        O5Ease.InOutBack => t < 0.5f ? (4f * t * t * ((3.59491f * 2f * t) - 2.59491f)) / 2f : (Mathf.Pow(2f * t - 2f, 2f) * ((3.59491f * (2f * t - 2f)) + 2.59491f) + 2f) / 2f,
        O5Ease.InElastic => t <= 0f ? 0f : t >= 1f ? 1f : -Mathf.Pow(2f, 10f * t - 10f) * Mathf.Sin((10f * t - 10.75f) * 2.0944f),
        O5Ease.OutElastic => t <= 0f ? 0f : t >= 1f ? 1f : Mathf.Pow(2f, -10f * t) * Mathf.Sin((10f * t - 0.75f) * 2.0944f) + 1f,
        O5Ease.InOutElastic => t <= 0f ? 0f : t >= 1f ? 1f : t < 0.5f ? -(Mathf.Pow(2f, 20f * t - 10f) * Mathf.Sin((20f * t - 11.125f) * 1.39626f)) / 2f : Mathf.Pow(2f, -20f * t + 10f) * Mathf.Sin((20f * t - 11.125f) * 1.39626f) / 2f,
        O5Ease.InBounce => 1f - BounceOut(1f - t),
        O5Ease.OutBounce => BounceOut(t),
        O5Ease.InOutBounce => t < 0.5f ? (1f - BounceOut(1f - 2f * t)) / 2f : (1f + BounceOut(2f * t - 1f)) / 2f,
        _ => t,
    };

    private static float BounceOut(float t) {
        const float n1 = 7.5625f;
        const float d1 = 2.75f;
        if(t < 1f / d1) {
            return n1 * t * t;
        }

        if(t < 2f / d1) {
            t -= 1.5f / d1;
            return n1 * t * t + 0.75f;
        }

        if(t < 2.5f / d1) {
            t -= 2.25f / d1;
            return n1 * t * t + 0.9375f;
        }

        t -= 2.625f / d1;
        return n1 * t * t + 0.984375f;
    }

    private ITweenHandle Add(System.Action<float> fn, float duration, System.Action? onComplete) {
        var e = new Entry { Fn = fn, Duration = Math.Max((duration), 0.0001f), OnComplete = onComplete };
        _active.Add(e);
        EnsurePump();
        return e;
    }

    private void EnsurePump() {
        if (_pump != null) {
            return;
        }

        var go = new GameObject("O5TweenPlayer");
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.hideFlags = HideFlags.HideAndDontSave;
        _pump = go.AddComponent<SimplePump>();
        _pump.Owner = this;
    }

    /// <summary>Advances all active tweens by unscaled delta time. Called by the hidden pump.</summary>
    public void Update() {
        float dt = Time.unscaledDeltaTime;
        for (int i = _active.Count - 1; i >= 0; i--) {
            var e = _active[i];
            if (!e.IsAlive) {
                _active.RemoveAt(i);
                continue;
            }

            e.Elapsed += dt;
            float t = Mathf.Clamp01(e.Elapsed / e.Duration);
            try {
                e.Fn(t);
            } catch {
            }

            if (t >= 1f) {
                e.IsAlive = false;
                _active.RemoveAt(i);
                try {
                    e.OnComplete?.Invoke();
                } catch {
                }
            }
        }
    }

    private sealed class Entry : ITweenHandle {
        public System.Action<float> Fn = _ => { };
        public System.Action? OnComplete;
        public float Duration;
        public float Elapsed;
        public bool IsAlive { get; set; } = true;

        public void Kill(bool complete = false) {
            if (complete) {
                try {
                    Fn(1f);
                    OnComplete?.Invoke();
                } catch {
                }
            }

            IsAlive = false;
        }
    }

#if IL2CPP
    [RegisterTypeInIl2Cpp]
#endif
    private sealed class SimplePump
#if IL2CPP
        (IntPtr ptr) : MonoBehaviour(ptr)
#else
        : MonoBehaviour
#endif
    {
        /// <summary>Runner to pump. Assigned on creation.</summary>
        public SimpleTweenRunner? Owner;

        private void Update() => Owner?.Update();
    }
}

/// <summary>Per-context decorator scaling every duration by a config speed multiplier. Higher speed shortens durations; zero or below collapses them to near-instant (completions still fire).</summary>
public sealed class ScaledTweenRunner : ITweenRunner {
    private readonly ITweenRunner _inner;
    private readonly O5Config _config;

    /// <summary>Wraps <paramref name="inner"/>, scaling durations by <paramref name="config"/>.</summary>
    /// <param name="inner">Backend runner.</param>
    /// <param name="config">Runtime options owning <see cref="O5Config.AnimationSpeed"/>.</param>
    public ScaledTweenRunner(ITweenRunner inner, O5Config config) {
        _inner = inner;
        _config = config;
    }

    private float Scale(float duration) {
        float speed = _config.AnimationSpeed;
        if (speed <= 0f) {
            return 0.0001f;
        }

        return Math.Max(duration / speed, 0.0001f);
    }

    /// <inheritdoc/>
    public ITweenHandle TweenFloat(System.Func<float> getter, System.Action<float> setter, float to, float duration, System.Action? onComplete = null, O5Ease ease = O5Ease.OutSine)
        => _inner.TweenFloat(getter, setter, to, Scale(duration), onComplete, ease);

    /// <inheritdoc/>
    public ITweenHandle TweenColor(System.Func<Color> getter, System.Action<Color> setter, Color to, float duration, System.Action? onComplete = null, O5Ease ease = O5Ease.OutSine)
        => _inner.TweenColor(getter, setter, to, Scale(duration), onComplete, ease);

    /// <inheritdoc/>
    public ITweenHandle TweenColor(Graphic graphic, Color to, float duration, System.Action? onComplete = null, O5Ease ease = O5Ease.OutSine)
        => _inner.TweenColor(graphic, to, Scale(duration), onComplete, ease);

    /// <inheritdoc/>
    public ITweenHandle TweenAlpha(CanvasGroup canvasGroup, float to, float duration, System.Action? onComplete = null, O5Ease ease = O5Ease.OutSine)
        => _inner.TweenAlpha(canvasGroup, to, Scale(duration), onComplete, ease);

    /// <inheritdoc/>
    public ITweenHandle TweenAlpha(Graphic graphic, float to, float duration, System.Action? onComplete = null, O5Ease ease = O5Ease.OutSine)
        => _inner.TweenAlpha(graphic, to, Scale(duration), onComplete, ease);

    /// <inheritdoc/>
    public ITweenHandle TweenAnchorPos(RectTransform rectTransform, Vector2 to, float duration, System.Action? onComplete = null, O5Ease ease = O5Ease.OutSine)
        => _inner.TweenAnchorPos(rectTransform, to, Scale(duration), onComplete, ease);

    /// <inheritdoc/>
    public ITweenHandle TweenSizeDelta(RectTransform rectTransform, Vector2 to, float duration, System.Action? onComplete = null, O5Ease ease = O5Ease.OutSine)
        => _inner.TweenSizeDelta(rectTransform, to, Scale(duration), onComplete, ease);

    /// <inheritdoc/>
    public ITweenHandle TweenScale(RectTransform rectTransform, Vector3 to, float duration, System.Action? onComplete = null, O5Ease ease = O5Ease.OutSine)
        => _inner.TweenScale(rectTransform, to, Scale(duration), onComplete, ease);

    /// <inheritdoc/>
    public ITweenHandle TweenOffsetMin(RectTransform rectTransform, Vector2 to, float duration, System.Action? onComplete = null, O5Ease ease = O5Ease.OutSine)
        => _inner.TweenOffsetMin(rectTransform, to, Scale(duration), onComplete, ease);

    /// <inheritdoc/>
    public ITweenHandle Delay(float seconds, System.Action? onComplete = null)
        => _inner.Delay(Scale(seconds), onComplete);
}

/// <summary>Static sugar for the most common control fades.</summary>
public static partial class O5Tween {
    /// <summary>Fades a <see cref="CanvasGroup"/> to <paramref name="to"/>.</summary>
    /// <param name="runner">Tween backend (usually <c>ctx.Tween</c>).</param>
    /// <param name="g">Target group. A destroyed group is skipped safely.</param>
    /// <param name="to">Target alpha.</param>
    /// <param name="duration">Duration in seconds (unscaled).</param>
    public static ITweenHandle Alpha(ITweenRunner runner, CanvasGroup g, float to, float duration)
        => runner.TweenFloat(
            () => g != null ? g.alpha : to,
            v => {
                if (g != null) {
                    g.alpha = v;
                }
            },
            to, duration);
}
