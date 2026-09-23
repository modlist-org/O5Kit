// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Collections.Generic;
using LitMotion;
using UnityEngine;

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
}

/// <summary>LitMotion-backed runner (default). Driven by a hidden pump through a manual dispatcher, so it works regardless of mod load order.</summary>
public sealed class LitMotionRunner : ITweenRunner {
    /// <summary>Shared instance used by <see cref="O5Boot"/> unless overridden.</summary>
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
        _ => Ease.OutSine,
    };

    private readonly ManualMotionDispatcher _dispatcher = new();
    private LitMotionPump? _pump;

    /// <inheritdoc/>
    public ITweenHandle TweenFloat(System.Func<float> getter, System.Action<float> setter, float to, float duration, System.Action? onComplete = null, O5Ease ease = O5Ease.OutSine) {
        EnsurePump();
        var builder = LMotion.Create(getter(), to, Math.Max(duration, 0.0001f))
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
        var builder = LMotion.Create(getter(), to, Math.Max(duration, 0.0001f))
            .WithEase(MapEase(ease))
            .WithScheduler(_dispatcher.Scheduler);
        if (onComplete != null) {
            builder = builder.WithOnComplete(onComplete);
        }

        return new Handle(builder.Bind(setter));
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

    private static float ApplyEase(O5Ease ease, float t) => ease switch {
        O5Ease.Linear => t,
        O5Ease.InSine => 1f - MathF.Cos(t * MathF.PI * 0.5f),
        O5Ease.OutSine => MathF.Sin(t * MathF.PI * 0.5f),
        O5Ease.InOutSine => -(MathF.Cos(MathF.PI * t) - 1f) * 0.5f,
        O5Ease.InQuad => t * t,
        O5Ease.OutQuad => 1f - (1f - t) * (1f - t),
        O5Ease.InOutQuad => t < 0.5f ? 2f * t * t : 1f - MathF.Pow(-2f * t + 2f, 2f) / 2f,
        O5Ease.InCubic => t * t * t,
        O5Ease.OutCubic => 1f - MathF.Pow(1f - t, 3f),
        O5Ease.InOutCubic => t < 0.5f ? 4f * t * t * t : 1f - MathF.Pow(-2f * t + 2f, 3f) / 2f,
        O5Ease.OutExpo => t >= 1f ? 1f : 1f - MathF.Pow(2f, -10f * t),
        O5Ease.OutCirc => MathF.Sqrt(1f - MathF.Pow(t - 1f, 2f)),
        O5Ease.OutBack => 1f + 2.70158f * MathF.Pow(t - 1f, 3f) + 1.70158f * MathF.Pow(t - 1f, 2f),
        _ => t,
    };

    private ITweenHandle Add(System.Action<float> fn, float duration, System.Action? onComplete) {
        var e = new Entry { Fn = fn, Duration = Math.Max(duration, 0.0001f), OnComplete = onComplete };
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

/// <summary>Static sugar for the most common control fades.</summary>
public static class O5Tween {
    /// <summary>Fades a <see cref="CanvasGroup"/> to <paramref name="to"/>.</summary>
    /// <param name="g">Target group. A destroyed group is skipped safely.</param>
    /// <param name="to">Target alpha.</param>
    /// <param name="duration">Duration in seconds (unscaled).</param>
    public static ITweenHandle Alpha(CanvasGroup g, float to, float duration)
        => O5Boot.Tween.TweenFloat(
            () => g != null ? g.alpha : to,
            v => {
                if (g != null) {
                    g.alpha = v;
                }
            },
            to, duration);
}
