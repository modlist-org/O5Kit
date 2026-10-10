// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Reflection;
#if LITMOTION
using LitMotion;
#endif
using UnityEngine;
using UnityEngine.UI;

namespace O5Kit.Core;

/// <summary>Tween backend selected by <see cref="AutoTweenRunner"/>.</summary>
public enum TweenBackend {
    /// <summary>Built-in LitMotion path (needs Burst/Collections + netstandard2.1).</summary>
    LitMotion,
    /// <summary>Embedded GTweens library (no external dependency; the Mono default).</summary>
    GTween,
    /// <summary>Game-shipped DOTween bound via reflection (no compile-time reference).</summary>
    DOTween,
    /// <summary>Game-shipped PrimeTween bound via reflection (no compile-time reference).</summary>
    PrimeTween,
    /// <summary>Zero-dependency unscaled-time lerp runner.</summary>
    Simple,
    /// <summary>No animation backend. Values snap instantly, completions still fire.</summary>
    Instant,
}

/// <summary>Runtime probes. Never throws: every check returns false on any failure.</summary>
public static class TweenBackendProbe {
    /// <summary>True when a no-op LitMotion binding can be created on this runtime.</summary>
    public static bool TryLitMotion() {
#if LITMOTION
        try {
            if (!HasAssembly("Unity.Burst") || !HasAssembly("Unity.Collections")) {
                return false;
            }

            return TryLitMotionCore();
        } catch {
            return false;
        }
#else
        return false;
#endif
    }

#if LITMOTION
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static bool TryLitMotionCore() {
        try {
            var h = LMotion.Create(0f, 1f, 0.0001f).Bind(_ => { });
            try {
                h.TryCancel();
            } catch {
            }

            return true;
        } catch {
            return false;
        }
    }
#endif

    /// <summary>True when DG.Tweening.DOTween can be bound via reflection.</summary>
    public static bool TryDOTween() {
        try {
            return DOTweenRunner.Shared.IsAvailable;
        } catch {
            return false;
        }
    }

    /// <summary>True when PrimeTween.Tween can be bound via reflection.</summary>
    public static bool TryPrimeTween() {
        try {
            return PrimeTweenRunner.Shared.IsAvailable;
        } catch {
            return false;
        }
    }

    private static bool HasAssembly(string name) {
        try {
            Assembly[] loaded;
            try {
                loaded = AppDomain.CurrentDomain.GetAssemblies();
            } catch {
                return false;
            }

            for (int i = 0; i < loaded.Length; i++) {
                try {
                    if (loaded[i].GetName().Name == name) {
                        return true;
                    }
                } catch {
                }
            }

            try {
                Assembly.Load(name);
                return true;
            } catch {
                return false;
            }
        } catch {
            return false;
        }
    }
}

/// <summary>Give-up runner: snaps to the end value and fires completion immediately. Zero dependencies beyond UnityEngine + mscorlib.</summary>
public sealed class InstantTweenRunner : ITweenRunner {
    /// <summary>Shared instance.</summary>
    public static InstantTweenRunner Instance { get; } = new();

    /// <inheritdoc/>
    public ITweenHandle TweenFloat(Func<float> getter, Action<float> setter, float to, float duration, Action? onComplete = null, O5Ease ease = O5Ease.OutSine) {
        try {
            if (setter != null) {
                setter(to);
            }
        } catch {
        }

        try {
            if (onComplete != null) {
                onComplete();
            }
        } catch {
        }

        return DeadHandle.Instance;
    }

    /// <inheritdoc/>
    public ITweenHandle TweenColor(Func<Color> getter, Action<Color> setter, Color to, float duration, Action? onComplete = null, O5Ease ease = O5Ease.OutSine) {
        try {
            if (setter != null) {
                setter(to);
            }
        } catch {
        }

        try {
            if (onComplete != null) {
                onComplete();
            }
        } catch {
        }

        return DeadHandle.Instance;
    }

    /// <inheritdoc/>
    public ITweenHandle TweenColor(Graphic graphic, Color to, float duration, Action? onComplete = null, O5Ease ease = O5Ease.OutSine) {
        try {
            if (graphic) {
                graphic.color = to;
            }
        } catch {
        }

        try {
            if (onComplete != null) {
                onComplete();
            }
        } catch {
        }

        return DeadHandle.Instance;
    }

    /// <inheritdoc/>
    public ITweenHandle TweenAlpha(CanvasGroup canvasGroup, float to, float duration, Action? onComplete = null, O5Ease ease = O5Ease.OutSine) {
        try {
            if (canvasGroup) {
                canvasGroup.alpha = to;
            }
        } catch {
        }

        try {
            if (onComplete != null) {
                onComplete();
            }
        } catch {
        }

        return DeadHandle.Instance;
    }

    /// <inheritdoc/>
    public ITweenHandle TweenAlpha(Graphic graphic, float to, float duration, Action? onComplete = null, O5Ease ease = O5Ease.OutSine) {
        try {
            if (graphic) {
                var c = graphic.color;
                c.a = to;
                graphic.color = c;
            }
        } catch {
        }

        try {
            if (onComplete != null) {
                onComplete();
            }
        } catch {
        }

        return DeadHandle.Instance;
    }

    /// <inheritdoc/>
    public ITweenHandle TweenAnchorPos(RectTransform rectTransform, Vector2 to, float duration, Action? onComplete = null, O5Ease ease = O5Ease.OutSine) {
        try {
            if (rectTransform) {
                rectTransform.anchoredPosition = to;
            }
        } catch {
        }

        try {
            if (onComplete != null) {
                onComplete();
            }
        } catch {
        }

        return DeadHandle.Instance;
    }

    /// <inheritdoc/>
    public ITweenHandle TweenSizeDelta(RectTransform rectTransform, Vector2 to, float duration, Action? onComplete = null, O5Ease ease = O5Ease.OutSine) {
        try {
            if (rectTransform) {
                rectTransform.sizeDelta = to;
            }
        } catch {
        }

        try {
            if (onComplete != null) {
                onComplete();
            }
        } catch {
        }

        return DeadHandle.Instance;
    }

    /// <inheritdoc/>
    public ITweenHandle TweenScale(RectTransform rectTransform, Vector3 to, float duration, Action? onComplete = null, O5Ease ease = O5Ease.OutSine) {
        try {
            if (rectTransform) {
                rectTransform.localScale = to;
            }
        } catch {
        }

        try {
            if (onComplete != null) {
                onComplete();
            }
        } catch {
        }

        return DeadHandle.Instance;
    }

    /// <inheritdoc/>
    public ITweenHandle TweenOffsetMin(RectTransform rectTransform, Vector2 to, float duration, Action? onComplete = null, O5Ease ease = O5Ease.OutSine) {
        try {
            if (rectTransform) {
                rectTransform.offsetMin = to;
            }
        } catch {
        }

        try {
            if (onComplete != null) {
                onComplete();
            }
        } catch {
        }

        return DeadHandle.Instance;
    }

    /// <inheritdoc/>
    public ITweenHandle Delay(float seconds, Action? onComplete = null) {
        try {
            if (onComplete != null) {
                onComplete();
            }
        } catch {
        }

        return DeadHandle.Instance;
    }

    private sealed class DeadHandle : ITweenHandle {
        public static DeadHandle Instance { get; } = new();

        public bool IsAlive => false;

        public void Kill(bool complete = false) {
        }
    }
}

/// <summary>Game-shipped DOTween bound purely via reflection. No compile-time reference, so O5Kit builds and loads whether or not the game ships DOTween.</summary>
public sealed class DOTweenRunner : ITweenRunner {
    internal static DOTweenRunner Shared { get; } = new();

    /// <summary>Shared instance. Check <see cref="IsAvailable"/> before direct use; <see cref="AutoTweenRunner"/> does this for you.</summary>
    public static DOTweenRunner Instance => Shared;

    private bool _probed;
    private bool _available;
    private MethodInfo? _floatMethod;
    private MethodInfo? _colorMethod;
    private MethodInfo? _vector2Method;
    private MethodInfo? _vector3Method;
    private MethodInfo? _delayedCall;
    private MethodInfo? _setEase;
    private MethodInfo? _setUpdate;
    private MethodInfo? _onComplete;
    private Type? _easeType;
    private MethodInfo? _killInstance;
    private MethodInfo? _isActiveInstance;

    /// <summary>Whether DOTween was found and bound. Cached after the first check.</summary>
    public bool IsAvailable {
        get {
            Ensure();
            return _available;
        }
    }

    /// <inheritdoc/>
    public ITweenHandle TweenFloat(Func<float> getter, Action<float> setter, float to, float duration, Action? onComplete = null, O5Ease ease = O5Ease.OutSine) {
        Ensure();
        ThrowIfUnavailable();
        float from;
        try {
            from = getter != null ? getter() : to;
        } catch {
            from = to;
        }

        object tween = InvokeVirtual(_floatMethod, from, to, duration, setter);
        return Finish(tween, ease, onComplete);
    }

    /// <inheritdoc/>
    public ITweenHandle TweenColor(Func<Color> getter, Action<Color> setter, Color to, float duration, Action? onComplete = null, O5Ease ease = O5Ease.OutSine) {
        Ensure();
        ThrowIfUnavailable();
        Color from;
        try {
            from = getter != null ? getter() : to;
        } catch {
            from = to;
        }

        object tween = InvokeVirtual(_colorMethod, from, to, duration, setter);
        return Finish(tween, ease, onComplete);
    }

    /// <inheritdoc/>
    public ITweenHandle TweenColor(Graphic graphic, Color to, float duration, Action? onComplete = null, O5Ease ease = O5Ease.OutSine) {
        Ensure();
        ThrowIfUnavailable();
        Color from;
        try {
            from = graphic ? graphic.color : to;
        } catch {
            from = to;
        }

        Graphic target = graphic;
        Action<Color> setter = v => {
            try {
                if (target) {
                    target.color = v;
                }
            } catch {
            }
        };
        object tween = InvokeVirtual(_colorMethod, from, to, duration, setter);
        return Finish(tween, ease, onComplete);
    }

    /// <inheritdoc/>
    public ITweenHandle TweenAlpha(CanvasGroup canvasGroup, float to, float duration, Action? onComplete = null, O5Ease ease = O5Ease.OutSine) {
        Ensure();
        ThrowIfUnavailable();
        float from;
        try {
            from = canvasGroup ? canvasGroup.alpha : to;
        } catch {
            from = to;
        }

        CanvasGroup target = canvasGroup;
        Action<float> setter = v => {
            try {
                if (target) {
                    target.alpha = v;
                }
            } catch {
            }
        };
        object tween = InvokeVirtual(_floatMethod, from, to, duration, setter);
        return Finish(tween, ease, onComplete);
    }

    /// <inheritdoc/>
    public ITweenHandle TweenAlpha(Graphic graphic, float to, float duration, Action? onComplete = null, O5Ease ease = O5Ease.OutSine) {
        Ensure();
        ThrowIfUnavailable();
        float from;
        try {
            from = graphic ? graphic.color.a : to;
        } catch {
            from = to;
        }

        Graphic target = graphic;
        Action<float> setter = v => {
            try {
                if (target) {
                    var c = target.color;
                    c.a = v;
                    target.color = c;
                }
            } catch {
            }
        };
        object tween = InvokeVirtual(_floatMethod, from, to, duration, setter);
        return Finish(tween, ease, onComplete);
    }

    /// <inheritdoc/>
    public ITweenHandle TweenAnchorPos(RectTransform rectTransform, Vector2 to, float duration, Action? onComplete = null, O5Ease ease = O5Ease.OutSine) {
        Ensure();
        ThrowIfUnavailable();
        Vector2 from;
        try {
            from = rectTransform ? rectTransform.anchoredPosition : to;
        } catch {
            from = to;
        }

        RectTransform target = rectTransform;
        Action<Vector2> setter = v => {
            try {
                if (target) {
                    target.anchoredPosition = v;
                }
            } catch {
            }
        };
        object tween = InvokeVirtual(_vector2Method, from, to, duration, setter);
        return Finish(tween, ease, onComplete);
    }

    /// <inheritdoc/>
    public ITweenHandle TweenSizeDelta(RectTransform rectTransform, Vector2 to, float duration, Action? onComplete = null, O5Ease ease = O5Ease.OutSine) {
        Ensure();
        ThrowIfUnavailable();
        Vector2 from;
        try {
            from = rectTransform ? rectTransform.sizeDelta : to;
        } catch {
            from = to;
        }

        RectTransform target = rectTransform;
        Action<Vector2> setter = v => {
            try {
                if (target) {
                    target.sizeDelta = v;
                }
            } catch {
            }
        };
        object tween = InvokeVirtual(_vector2Method, from, to, duration, setter);
        return Finish(tween, ease, onComplete);
    }

    /// <inheritdoc/>
    public ITweenHandle TweenScale(RectTransform rectTransform, Vector3 to, float duration, Action? onComplete = null, O5Ease ease = O5Ease.OutSine) {
        Ensure();
        ThrowIfUnavailable();
        Vector3 from;
        try {
            from = rectTransform ? rectTransform.localScale : to;
        } catch {
            from = to;
        }

        RectTransform target = rectTransform;
        Action<Vector3> setter = v => {
            try {
                if (target) {
                    target.localScale = v;
                }
            } catch {
            }
        };
        object tween = InvokeVirtual(_vector3Method, from, to, duration, setter);
        return Finish(tween, ease, onComplete);
    }

    /// <inheritdoc/>
    public ITweenHandle TweenOffsetMin(RectTransform rectTransform, Vector2 to, float duration, Action? onComplete = null, O5Ease ease = O5Ease.OutSine) {
        Ensure();
        ThrowIfUnavailable();
        Vector2 from;
        try {
            from = rectTransform ? rectTransform.offsetMin : to;
        } catch {
            from = to;
        }

        RectTransform target = rectTransform;
        Action<Vector2> setter = v => {
            try {
                if (target) {
                    target.offsetMin = v;
                }
            } catch {
            }
        };
        object tween = InvokeVirtual(_vector2Method, from, to, duration, setter);
        return Finish(tween, ease, onComplete);
    }

    /// <inheritdoc/>
    public ITweenHandle Delay(float seconds, Action? onComplete = null) {
        Ensure();
        ThrowIfUnavailable();
        object tween = InvokeDelayedCall(Math.Max(seconds, 0.0001f), onComplete);
        return new Handle(this, tween);
    }

    private void ThrowIfUnavailable() {
        if (!_available) {
            throw new InvalidOperationException("O5Kit: DOTween is not available in this game.");
        }
    }

    private void Ensure() {
        if (_probed) {
            return;
        }

        _probed = true;
        try {
            Probe();
        } catch {
            _available = false;
        }
    }

    private void Probe() {
        Type? dovirtual = FindType("DG.Tweening.DOVirtual");
        Type? settingsExt = FindType("DG.Tweening.TweenSettingsExtensions");
        Type? ease = FindType("DG.Tweening.Ease");
        if (dovirtual == null || settingsExt == null || ease == null) {
            _available = false;
            return;
        }

        MethodInfo? floatM = FindVirtual(dovirtual, "Float", typeof(float));
        MethodInfo? colorM = FindVirtual(dovirtual, "Color", typeof(Color));
        MethodInfo? v2M = FindVirtual(dovirtual, "Vector2", typeof(Vector2));
        MethodInfo? v3M = FindVirtual(dovirtual, "Vector3", typeof(Vector3));
        MethodInfo? delayed = FindDelayedCall(dovirtual);
        MethodInfo? setEase = FindExtension(settingsExt, "SetEase", 2);
        MethodInfo? setUpdate = FindExtension(settingsExt, "SetUpdate", 2);
        MethodInfo? onComplete = FindExtension(settingsExt, "OnComplete", 2);
        if (floatM == null || colorM == null || v2M == null || v3M == null || delayed == null
            || setEase == null || setUpdate == null || onComplete == null) {
            _available = false;
            return;
        }

        _floatMethod = floatM;
        _colorMethod = colorM;
        _vector2Method = v2M;
        _vector3Method = v3M;
        _delayedCall = delayed;
        _setEase = setEase;
        _setUpdate = setUpdate;
        _onComplete = onComplete;
        _easeType = ease;
        _available = true;
    }

    private static Type? FindType(string fullName) {
        try {
            Assembly[] loaded;
            try {
                loaded = AppDomain.CurrentDomain.GetAssemblies();
            } catch {
                return null;
            }

            for (int i = 0; i < loaded.Length; i++) {
                try {
                    Type? t = loaded[i].GetType(fullName, false);
                    if (t != null) {
                        return t;
                    }
                } catch {
                }
            }

            string[] candidates = { "DOTween", "DOTweenPro" };
            for (int i = 0; i < candidates.Length; i++) {
                try {
                    Assembly a = Assembly.Load(candidates[i]);
                    try {
                        Type? t = a.GetType(fullName, false);
                        if (t != null) {
                            return t;
                        }
                    } catch {
                    }
                } catch {
                }
            }
        } catch {
        }

        return null;
    }

    private static MethodInfo? FindVirtual(Type dovirtual, string name, Type valueType) {
        try {
            MethodInfo[] methods = dovirtual.GetMethods(BindingFlags.Public | BindingFlags.Static);
            for (int i = 0; i < methods.Length; i++) {
                MethodInfo m = methods[i];
                if (m.Name != name) {
                    continue;
                }

                ParameterInfo[] ps = m.GetParameters();
                if (ps.Length != 4) {
                    continue;
                }

                if (ps[0].ParameterType != valueType || ps[1].ParameterType != valueType || ps[2].ParameterType != typeof(float)) {
                    continue;
                }

                if (!ps[3].ParameterType.IsSubclassOf(typeof(Delegate))) {
                    continue;
                }

                return m;
            }
        } catch {
        }

        return null;
    }

    private static MethodInfo? FindDelayedCall(Type dovirtual) {
        try {
            MethodInfo[] methods = dovirtual.GetMethods(BindingFlags.Public | BindingFlags.Static);
            for (int i = 0; i < methods.Length; i++) {
                MethodInfo m = methods[i];
                if (m.Name != "DelayedCall") {
                    continue;
                }

                ParameterInfo[] ps = m.GetParameters();
                if (ps.Length < 2 || ps.Length > 3) {
                    continue;
                }

                if (ps[0].ParameterType != typeof(float)) {
                    continue;
                }

                if (!ps[1].ParameterType.IsSubclassOf(typeof(Delegate))) {
                    continue;
                }

                return m;
            }
        } catch {
        }

        return null;
    }

    private static MethodInfo? FindExtension(Type ext, string name, int paramCount) {
        try {
            MethodInfo[] methods = ext.GetMethods(BindingFlags.Public | BindingFlags.Static);
            for (int i = 0; i < methods.Length; i++) {
                MethodInfo m = methods[i];
                if (m.Name != name) {
                    continue;
                }

                if (m.GetParameters().Length != paramCount) {
                    continue;
                }

                return m;
            }
        } catch {
        }

        return null;
    }

    private object MapEase(O5Ease ease) {
        try {
            if (_easeType != null) {
                try {
                    return Enum.Parse(_easeType, ease.ToString(), true);
                } catch {
                }
            }
        } catch {
        }

        try {
            object first = Enum.GetValues(_easeType!);
            System.Array arr = (System.Array)first;
            if (arr.Length > 0) {
                object? v = arr.GetValue(0);
                if (v != null) {
                    return v;
                }
            }
        } catch {
        }

        return 0;
    }

    private static Delegate MakeCallback(Type delegateType, Delegate source) {
        try {
            if (source.Target != null) {
                return Delegate.CreateDelegate(delegateType, source.Target, source.Method);
            }

            return Delegate.CreateDelegate(delegateType, source.Method);
        } catch {
            return Delegate.CreateDelegate(delegateType, source.Method);
        }
    }

    private object InvokeVirtual(MethodInfo? method, object from, object to, float duration, Delegate setter) {
        if (method == null) {
            throw new InvalidOperationException("O5Kit: DOTween binding is missing.");
        }

        ParameterInfo[] ps = method.GetParameters();
        Delegate cb = MakeCallback(ps[3].ParameterType, setter);
        try {
            object? tween = method.Invoke(null, new object[] { from, to, Math.Max(duration, 0.0001f), cb });
            if (tween == null) {
                throw new InvalidOperationException("O5Kit: DOTween returned no tween.");
            }

            return tween;
        } catch (TargetInvocationException ex) {
            throw ex.InnerException ?? ex;
        }
    }

    private object InvokeDelayedCall(float seconds, Action? onComplete) {
        if (_delayedCall == null) {
            throw new InvalidOperationException("O5Kit: DOTween binding is missing.");
        }

        ParameterInfo[] ps = _delayedCall.GetParameters();
        Action noop = () => { };
        Delegate cb = MakeCallback(ps[1].ParameterType, onComplete ?? noop);
        object[] args;
        if (ps.Length == 2) {
            args = new object[] { Math.Max(seconds, 0.0001f), cb };
        } else {
            args = new object[ps.Length];
            args[0] = Math.Max(seconds, 0.0001f);
            args[1] = cb;
            for (int i = 2; i < ps.Length; i++) {
                try {
                    if (ps[i].ParameterType == typeof(bool)) {
                        args[i] = true;
                    } else if (ps[i].HasDefaultValue) {
                        args[i] = ps[i].DefaultValue!;
                    } else if (ps[i].ParameterType.IsValueType) {
                        args[i] = Activator.CreateInstance(ps[i].ParameterType)!;
                    } else {
                        args[i] = null!;
                    }
                } catch {
                    try {
                        args[i] = Activator.CreateInstance(ps[i].ParameterType)!;
                    } catch {
                        args[i] = null!;
                    }
                }
            }
        }

        try {
            object? tween = _delayedCall.Invoke(null, args);
            if (tween == null) {
                throw new InvalidOperationException("O5Kit: DOTween returned no tween.");
            }

            if (onComplete != null) {
                ApplyOnComplete(tween, onComplete);
            }

            ApplyUnscaled(tween);
            return tween;
        } catch (TargetInvocationException ex) {
            throw ex.InnerException ?? ex;
        }
    }

    private ITweenHandle Finish(object tween, O5Ease ease, Action? onComplete) {
        ApplyEase(tween, ease);
        ApplyUnscaled(tween);
        if (onComplete != null) {
            ApplyOnComplete(tween, onComplete);
        }

        return new Handle(this, tween);
    }

    private void ApplyEase(object tween, O5Ease ease) {
        if (_setEase == null) {
            return;
        }

        try {
            object easeValue = MapEase(ease);
            MethodInfo m = _setEase;
            if (m.IsGenericMethodDefinition) {
                m = m.MakeGenericMethod(tween.GetType());
            }

            m.Invoke(null, new object[] { tween, easeValue });
        } catch {
        }
    }

    private void ApplyUnscaled(object tween) {
        if (_setUpdate == null) {
            return;
        }

        try {
            MethodInfo m = _setUpdate;
            if (m.IsGenericMethodDefinition) {
                m = m.MakeGenericMethod(tween.GetType());
            }

            m.Invoke(null, new object[] { tween, true });
        } catch {
        }
    }

    private void ApplyOnComplete(object tween, Action onComplete) {
        if (_onComplete == null) {
            return;
        }

        try {
            ParameterInfo[] ps = _onComplete.GetParameters();
            Delegate cb = MakeCallback(ps[1].ParameterType, onComplete);
            MethodInfo m = _onComplete;
            if (m.IsGenericMethodDefinition) {
                m = m.MakeGenericMethod(tween.GetType());
            }

            m.Invoke(null, new object[] { tween, cb });
        } catch {
        }
    }

    internal void CacheInstanceMethods(Type tweenType) {
        if (_killInstance != null && _isActiveInstance != null) {
            return;
        }

        try {
            MethodInfo? kill = tweenType.GetMethod("Kill", BindingFlags.Public | BindingFlags.Instance, null, new Type[] { typeof(bool) }, null);
            if (kill == null) {
                kill = tweenType.GetMethod("Kill", BindingFlags.Public | BindingFlags.Instance);
            }

            MethodInfo? active = tweenType.GetMethod("IsActive", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
            if (kill != null) {
                _killInstance = kill;
            }

            if (active != null) {
                _isActiveInstance = active;
            }
        } catch {
        }
    }

    private sealed class Handle : ITweenHandle {
        private readonly DOTweenRunner _owner;
        private readonly object _tween;

        public Handle(DOTweenRunner owner, object tween) {
            _owner = owner;
            _tween = tween;
            try {
                _owner.CacheInstanceMethods(tween.GetType());
            } catch {
            }
        }

        public bool IsAlive {
            get {
                try {
                    if (_owner._isActiveInstance != null) {
                        object? r = _owner._isActiveInstance.Invoke(_tween, null);
                        if (r is bool b) {
                            return b;
                        }
                    }
                } catch {
                }

                return false;
            }
        }

        public void Kill(bool complete = false) {
            try {
                if (_owner._killInstance != null) {
                    ParameterInfo[] ps = _owner._killInstance.GetParameters();
                    if (ps.Length == 0) {
                        if (complete) {
                            try {
                                MethodInfo? done = _tween.GetType().GetMethod("Complete", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
                                if (done != null) {
                                    done.Invoke(_tween, null);
                                    return;
                                }
                            } catch {
                            }
                        }

                        _owner._killInstance.Invoke(_tween, null);
                    } else {
                        _owner._killInstance.Invoke(_tween, new object[] { complete });
                    }
                }
            } catch {
            }
        }
    }
}

/// <summary>Game-shipped PrimeTween bound purely via reflection. Best-effort: any API mismatch marks it unavailable and <see cref="AutoTweenRunner"/> moves on.</summary>
public sealed class PrimeTweenRunner : ITweenRunner {
    internal static PrimeTweenRunner Shared { get; } = new();

    /// <summary>Shared instance. Check <see cref="IsAvailable"/> before direct use; <see cref="AutoTweenRunner"/> does this for you.</summary>
    public static PrimeTweenRunner Instance => Shared;

    private bool _probed;
    private bool _available;
    private Type? _tweenType;
    private Type? _easeType;
    private MethodInfo? _delayMethod;
    private MethodInfo? _onCompleteMethod;
    private PropertyInfo? _isAliveProp;
    private MethodInfo? _stopMethod;
    private MethodInfo? _completeMethod;

    /// <summary>Whether PrimeTween was found and bound. Cached after the first check.</summary>
    public bool IsAvailable {
        get {
            Ensure();
            return _available;
        }
    }

    /// <inheritdoc/>
    public ITweenHandle TweenFloat(Func<float> getter, Action<float> setter, float to, float duration, Action? onComplete = null, O5Ease ease = O5Ease.OutSine) {
        Ensure();
        ThrowIfUnavailable();
        float from = SafeGet(getter, to);
        object tween = InvokeCustom(from, to, duration, setter, ease);
        return Finish(tween, onComplete);
    }

    /// <inheritdoc/>
    public ITweenHandle TweenColor(Func<Color> getter, Action<Color> setter, Color to, float duration, Action? onComplete = null, O5Ease ease = O5Ease.OutSine) {
        Ensure();
        ThrowIfUnavailable();
        Color from = SafeGet(getter, to);
        object tween = InvokeCustom(from, to, duration, setter, ease);
        return Finish(tween, onComplete);
    }

    /// <inheritdoc/>
    public ITweenHandle TweenColor(Graphic graphic, Color to, float duration, Action? onComplete = null, O5Ease ease = O5Ease.OutSine) {
        Ensure();
        ThrowIfUnavailable();
        Color from = SafeColor(graphic, to);
        Graphic target = graphic;
        Action<Color> setter = v => {
            try {
                if (target) {
                    target.color = v;
                }
            } catch {
            }
        };
        object tween = InvokeCustom(from, to, duration, setter, ease);
        return Finish(tween, onComplete);
    }

    /// <inheritdoc/>
    public ITweenHandle TweenAlpha(CanvasGroup canvasGroup, float to, float duration, Action? onComplete = null, O5Ease ease = O5Ease.OutSine) {
        Ensure();
        ThrowIfUnavailable();
        float from;
        try {
            from = canvasGroup ? canvasGroup.alpha : to;
        } catch {
            from = to;
        }

        CanvasGroup target = canvasGroup;
        Action<float> setter = v => {
            try {
                if (target) {
                    target.alpha = v;
                }
            } catch {
            }
        };
        object tween = InvokeCustom(from, to, duration, setter, ease);
        return Finish(tween, onComplete);
    }

    /// <inheritdoc/>
    public ITweenHandle TweenAlpha(Graphic graphic, float to, float duration, Action? onComplete = null, O5Ease ease = O5Ease.OutSine) {
        Ensure();
        ThrowIfUnavailable();
        float from;
        try {
            from = graphic ? graphic.color.a : to;
        } catch {
            from = to;
        }

        Graphic target = graphic;
        Action<float> setter = v => {
            try {
                if (target) {
                    var c = target.color;
                    c.a = v;
                    target.color = c;
                }
            } catch {
            }
        };
        object tween = InvokeCustom(from, to, duration, setter, ease);
        return Finish(tween, onComplete);
    }

    /// <inheritdoc/>
    public ITweenHandle TweenAnchorPos(RectTransform rectTransform, Vector2 to, float duration, Action? onComplete = null, O5Ease ease = O5Ease.OutSine) {
        Ensure();
        ThrowIfUnavailable();
        Vector2 from;
        try {
            from = rectTransform ? rectTransform.anchoredPosition : to;
        } catch {
            from = to;
        }

        RectTransform target = rectTransform;
        Action<Vector2> setter = v => {
            try {
                if (target) {
                    target.anchoredPosition = v;
                }
            } catch {
            }
        };
        object tween = InvokeCustom(from, to, duration, setter, ease);
        return Finish(tween, onComplete);
    }

    /// <inheritdoc/>
    public ITweenHandle TweenSizeDelta(RectTransform rectTransform, Vector2 to, float duration, Action? onComplete = null, O5Ease ease = O5Ease.OutSine) {
        Ensure();
        ThrowIfUnavailable();
        Vector2 from;
        try {
            from = rectTransform ? rectTransform.sizeDelta : to;
        } catch {
            from = to;
        }

        RectTransform target = rectTransform;
        Action<Vector2> setter = v => {
            try {
                if (target) {
                    target.sizeDelta = v;
                }
            } catch {
            }
        };
        object tween = InvokeCustom(from, to, duration, setter, ease);
        return Finish(tween, onComplete);
    }

    /// <inheritdoc/>
    public ITweenHandle TweenScale(RectTransform rectTransform, Vector3 to, float duration, Action? onComplete = null, O5Ease ease = O5Ease.OutSine) {
        Ensure();
        ThrowIfUnavailable();
        Vector3 from;
        try {
            from = rectTransform ? rectTransform.localScale : to;
        } catch {
            from = to;
        }

        RectTransform target = rectTransform;
        Action<Vector3> setter = v => {
            try {
                if (target) {
                    target.localScale = v;
                }
            } catch {
            }
        };
        object tween = InvokeCustom(from, to, duration, setter, ease);
        return Finish(tween, onComplete);
    }

    /// <inheritdoc/>
    public ITweenHandle TweenOffsetMin(RectTransform rectTransform, Vector2 to, float duration, Action? onComplete = null, O5Ease ease = O5Ease.OutSine) {
        Ensure();
        ThrowIfUnavailable();
        Vector2 from;
        try {
            from = rectTransform ? rectTransform.offsetMin : to;
        } catch {
            from = to;
        }

        RectTransform target = rectTransform;
        Action<Vector2> setter = v => {
            try {
                if (target) {
                    target.offsetMin = v;
                }
            } catch {
            }
        };
        object tween = InvokeCustom(from, to, duration, setter, ease);
        return Finish(tween, onComplete);
    }

    /// <inheritdoc/>
    public ITweenHandle Delay(float seconds, Action? onComplete = null) {
        Ensure();
        ThrowIfUnavailable();
        object tween = InvokeDelay(Math.Max(seconds, 0.0001f));
        return Finish(tween, onComplete);
    }

    private void ThrowIfUnavailable() {
        if (!_available) {
            throw new InvalidOperationException("O5Kit: PrimeTween is not available in this game.");
        }
    }

    private void Ensure() {
        if (_probed) {
            return;
        }

        _probed = true;
        try {
            Probe();
        } catch {
            _available = false;
        }
    }

    private void Probe() {
        Type? tween = FindType("PrimeTween.Tween");
        Type? ease = FindType("PrimeTween.Ease");
        if (tween == null || ease == null) {
            _available = false;
            return;
        }

        PropertyInfo? alive = tween.GetProperty("isAlive", BindingFlags.Public | BindingFlags.Instance);
        MethodInfo? stop = tween.GetMethod("Stop", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
        MethodInfo? complete = tween.GetMethod("Complete", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
        MethodInfo? onComplete = FindOnComplete(tween);
        MethodInfo? delay = FindDelay(tween);
        if (alive == null || alive.PropertyType != typeof(bool) || stop == null || complete == null || onComplete == null || delay == null) {
            _available = false;
            return;
        }

        // End-to-end check: build one real micro-tween, then stop it.
        try {
            Action<float> setter = _ => { };
            object test = InvokeCustomInternal(tween, ease, 0f, 1f, 0.0001f, setter, O5Ease.Linear);
            try {
                stop.Invoke(test, null);
            } catch {
            }
        } catch {
            _available = false;
            return;
        }

        _tweenType = tween;
        _easeType = ease;
        _isAliveProp = alive;
        _stopMethod = stop;
        _completeMethod = complete;
        _onCompleteMethod = onComplete;
        _delayMethod = delay;
        _available = true;
    }

    private static Type? FindType(string fullName) {
        try {
            Assembly[] loaded;
            try {
                loaded = AppDomain.CurrentDomain.GetAssemblies();
            } catch {
                return null;
            }

            for (int i = 0; i < loaded.Length; i++) {
                try {
                    Type? t = loaded[i].GetType(fullName, false);
                    if (t != null) {
                        return t;
                    }
                } catch {
                }
            }

            string[] candidates = { "PrimeTween" };
            for (int i = 0; i < candidates.Length; i++) {
                try {
                    Assembly a = Assembly.Load(candidates[i]);
                    try {
                        Type? t = a.GetType(fullName, false);
                        if (t != null) {
                            return t;
                        }
                    } catch {
                    }
                } catch {
                }
            }
        } catch {
        }

        return null;
    }

    private static MethodInfo? FindOnComplete(Type tween) {
        try {
            MethodInfo[] methods = tween.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            for (int i = 0; i < methods.Length; i++) {
                MethodInfo m = methods[i];
                if (m.Name != "OnComplete") {
                    continue;
                }

                ParameterInfo[] ps = m.GetParameters();
                if (ps.Length != 1) {
                    continue;
                }

                if (!ps[0].ParameterType.IsSubclassOf(typeof(Delegate)) && ps[0].ParameterType != typeof(Action)) {
                    continue;
                }

                return m;
            }
        } catch {
        }

        return null;
    }

    private static MethodInfo? FindDelay(Type tween) {
        try {
            MethodInfo[] methods = tween.GetMethods(BindingFlags.Public | BindingFlags.Static);
            for (int i = 0; i < methods.Length; i++) {
                if (methods[i].Name == "Delay") {
                    return methods[i];
                }
            }
        } catch {
        }

        return null;
    }

    private static T SafeGet<T>(Func<T> getter, T fallback) {
        try {
            if (getter != null) {
                return getter();
            }
        } catch {
        }

        return fallback;
    }

    private static Color SafeColor(Graphic graphic, Color fallback) {
        try {
            if (graphic) {
                return graphic.color;
            }
        } catch {
        }

        return fallback;
    }

    private object MapEase(O5Ease ease) {
        try {
            if (_easeType != null) {
                try {
                    return Enum.Parse(_easeType, ease.ToString(), true);
                } catch {
                }
            }
        } catch {
        }

        try {
            System.Array arr = Enum.GetValues(_easeType!);
            if (arr.Length > 0) {
                object? v = arr.GetValue(0);
                if (v != null) {
                    return v;
                }
            }
        } catch {
        }

        return 0;
    }

    private object InvokeCustom<T>(T from, T to, float duration, Action<T> setter, O5Ease ease) {
        if (_tweenType == null || _easeType == null) {
            throw new InvalidOperationException("O5Kit: PrimeTween binding is missing.");
        }

        return InvokeCustomInternal(_tweenType, _easeType, from!, to!, duration, setter, ease);
    }

    private static object InvokeCustomInternal(Type tweenType, Type easeType, object from, object to, float duration, Delegate setter, O5Ease ease) {
        object easeValue;
        try {
            easeValue = Enum.Parse(easeType, ease.ToString(), true);
        } catch {
            try {
                System.Array arr = Enum.GetValues(easeType);
                easeValue = arr.GetValue(0)!;
            } catch {
                throw new InvalidOperationException("O5Kit: PrimeTween ease is unusable.");
            }
        }

        MethodInfo[] methods;
        try {
            methods = tweenType.GetMethods(BindingFlags.Public | BindingFlags.Static);
        } catch (Exception ex) {
            throw new InvalidOperationException("O5Kit: PrimeTween.Custom is unreadable.", ex);
        }

        Exception? last = null;
        for (int i = 0; i < methods.Length; i++) {
            MethodInfo m = methods[i];
            if (m.Name != "Custom" || !m.IsGenericMethodDefinition || m.GetGenericArguments().Length != 1) {
                continue;
            }

            try {
                Type valueType = from.GetType();
                MethodInfo generic = m.MakeGenericMethod(valueType);
                if (TryBuildCustomArgs(generic, from, to, duration, setter, easeValue, out object[] args)) {
                    try {
                        object? tween = generic.Invoke(null, args);
                        if (tween != null) {
                            return tween;
                        }
                    } catch (TargetInvocationException ex) {
                        last = ex.InnerException ?? ex;
                    } catch (Exception ex) {
                        last = ex;
                    }
                }
            } catch (Exception ex) {
                last = ex;
            }
        }

        throw new InvalidOperationException("O5Kit: no usable PrimeTween.Custom overload.", last);
    }

    private static bool TryBuildCustomArgs(MethodInfo generic, object from, object to, float duration, Delegate setter, object easeValue, out object[] args) {
        args = System.Array.Empty<object>();
        try {
            ParameterInfo[] ps = generic.GetParameters();
            if (ps.Length < 4) {
                return false;
            }

            Type valueType = from.GetType();
            if (ps[0].ParameterType != valueType || ps[1].ParameterType != valueType || ps[2].ParameterType != typeof(float)) {
                return false;
            }

            if (!ps[3].ParameterType.IsSubclassOf(typeof(Delegate))) {
                return false;
            }

            Delegate cb;
            try {
                if (setter.Target != null) {
                    cb = Delegate.CreateDelegate(ps[3].ParameterType, setter.Target, setter.Method);
                } else {
                    cb = Delegate.CreateDelegate(ps[3].ParameterType, setter.Method);
                }
            } catch {
                return false;
            }

            object[] built = new object[ps.Length];
            built[0] = from;
            built[1] = to;
            built[2] = Math.Max(duration, 0.0001f);
            built[3] = cb;
            for (int i = 4; i < ps.Length; i++) {
                try {
                    string name = ps[i].Name ?? string.Empty;
                    if (ps[i].ParameterType.IsInstanceOfType(easeValue) || ps[i].ParameterType == easeValue.GetType()) {
                        built[i] = easeValue;
                    } else if (string.Equals(name, "useUnscaledTime", StringComparison.OrdinalIgnoreCase)
                        && ps[i].ParameterType == typeof(bool)) {
                        built[i] = true;
                    } else if (ps[i].HasDefaultValue) {
                        built[i] = ps[i].DefaultValue!;
                    } else if (ps[i].ParameterType.IsValueType) {
                        built[i] = Activator.CreateInstance(ps[i].ParameterType)!;
                    } else {
                        built[i] = null!;
                    }
                } catch {
                    return false;
                }
            }

            args = built;
            return true;
        } catch {
            return false;
        }
    }

    private object InvokeDelay(float seconds) {
        if (_tweenType == null || _delayMethod == null) {
            throw new InvalidOperationException("O5Kit: PrimeTween binding is missing.");
        }

        try {
            ParameterInfo[] ps = _delayMethod.GetParameters();
            object[] args = new object[ps.Length];
            bool filledDuration = false;
            for (int i = 0; i < ps.Length; i++) {
                try {
                    if (!filledDuration && ps[i].ParameterType == typeof(float)) {
                        args[i] = Math.Max(seconds, 0.0001f);
                        filledDuration = true;
                    } else if (ps[i].ParameterType == typeof(bool)) {
                        args[i] = true;
                    } else if (ps[i].HasDefaultValue) {
                        args[i] = ps[i].DefaultValue!;
                    } else if (ps[i].ParameterType.IsValueType) {
                        args[i] = Activator.CreateInstance(ps[i].ParameterType)!;
                    } else {
                        args[i] = null!;
                    }
                } catch {
                    return InvokeCustom(0f, 1f, seconds, _ => { }, O5Ease.Linear);
                }
            }

            if (!filledDuration) {
                return InvokeCustom(0f, 1f, seconds, _ => { }, O5Ease.Linear);
            }

            try {
                MethodInfo m = _delayMethod;
                if (m.IsGenericMethodDefinition) {
                    m = m.MakeGenericMethod(typeof(float));
                }

                object? tween = m.Invoke(null, args);
                if (tween != null) {
                    return tween;
                }
            } catch (TargetInvocationException ex) {
                throw ex.InnerException ?? ex;
            }

            return InvokeCustom(0f, 1f, seconds, _ => { }, O5Ease.Linear);
        } catch (Exception ex) {
            if (ex is InvalidOperationException) {
                throw;
            }

            return InvokeCustom(0f, 1f, seconds, _ => { }, O5Ease.Linear);
        }
    }

    private ITweenHandle Finish(object tween, Action? onComplete) {
        if (onComplete != null) {
            AttachOnComplete(tween, onComplete);
        }

        return new Handle(this, tween);
    }

    private void AttachOnComplete(object tween, Action onComplete) {
        if (_onCompleteMethod == null) {
            return;
        }

        try {
            ParameterInfo[] ps = _onCompleteMethod.GetParameters();
            Delegate cb;
            if (onComplete.Target != null) {
                cb = Delegate.CreateDelegate(ps[0].ParameterType, onComplete.Target, onComplete.Method);
            } else {
                cb = Delegate.CreateDelegate(ps[0].ParameterType, onComplete.Method);
            }

            _onCompleteMethod.Invoke(tween, new object[] { cb });
        } catch {
        }
    }

    private sealed class Handle : ITweenHandle {
        private readonly PrimeTweenRunner _owner;
        private object _tween;

        public Handle(PrimeTweenRunner owner, object tween) {
            _owner = owner;
            _tween = tween;
        }

        public bool IsAlive {
            get {
                try {
                    if (_owner._isAliveProp != null) {
                        object? r = _owner._isAliveProp.GetValue(_tween, null);
                        if (r is bool b) {
                            return b;
                        }
                    }
                } catch {
                }

                return false;
            }
        }

        public void Kill(bool complete = false) {
            try {
                if (complete) {
                    if (_owner._completeMethod != null) {
                        _owner._completeMethod.Invoke(_tween, null);
                        return;
                    }
                }

                if (_owner._stopMethod != null) {
                    _owner._stopMethod.Invoke(_tween, null);
                }
            } catch {
            }
        }
    }
}

/// <summary>Automatic backend: LitMotion first, then game-shipped DOTween, then PrimeTween, then instant snap. First use wins and is cached.</summary>
public sealed class AutoTweenRunner : ITweenRunner {
    /// <summary>Shared instance used as the <see cref="O5Context"/> default unless overridden.</summary>
    public static AutoTweenRunner Instance { get; } = new();

    private bool _pickedDone;
    private ITweenRunner? _picked;

    /// <summary>Backend chosen on first use. Unknown until the first tween call.</summary>
    public TweenBackend Backend { get; private set; } = TweenBackend.LitMotion;

    /// <summary>True once a backend has been chosen.</summary>
    public bool IsPicked => _pickedDone;

    /// <inheritdoc/>
    public ITweenHandle TweenFloat(Func<float> getter, Action<float> setter, float to, float duration, Action? onComplete = null, O5Ease ease = O5Ease.OutSine)
        => Pick().TweenFloat(getter, setter, to, duration, onComplete, ease);

    /// <inheritdoc/>
    public ITweenHandle TweenColor(Func<Color> getter, Action<Color> setter, Color to, float duration, Action? onComplete = null, O5Ease ease = O5Ease.OutSine)
        => Pick().TweenColor(getter, setter, to, duration, onComplete, ease);

    /// <inheritdoc/>
    public ITweenHandle TweenColor(Graphic graphic, Color to, float duration, Action? onComplete = null, O5Ease ease = O5Ease.OutSine)
        => Pick().TweenColor(graphic, to, duration, onComplete, ease);

    /// <inheritdoc/>
    public ITweenHandle TweenAlpha(CanvasGroup canvasGroup, float to, float duration, Action? onComplete = null, O5Ease ease = O5Ease.OutSine)
        => Pick().TweenAlpha(canvasGroup, to, duration, onComplete, ease);

    /// <inheritdoc/>
    public ITweenHandle TweenAlpha(Graphic graphic, float to, float duration, Action? onComplete = null, O5Ease ease = O5Ease.OutSine)
        => Pick().TweenAlpha(graphic, to, duration, onComplete, ease);

    /// <inheritdoc/>
    public ITweenHandle TweenAnchorPos(RectTransform rectTransform, Vector2 to, float duration, Action? onComplete = null, O5Ease ease = O5Ease.OutSine)
        => Pick().TweenAnchorPos(rectTransform, to, duration, onComplete, ease);

    /// <inheritdoc/>
    public ITweenHandle TweenSizeDelta(RectTransform rectTransform, Vector2 to, float duration, Action? onComplete = null, O5Ease ease = O5Ease.OutSine)
        => Pick().TweenSizeDelta(rectTransform, to, duration, onComplete, ease);

    /// <inheritdoc/>
    public ITweenHandle TweenScale(RectTransform rectTransform, Vector3 to, float duration, Action? onComplete = null, O5Ease ease = O5Ease.OutSine)
        => Pick().TweenScale(rectTransform, to, duration, onComplete, ease);

    /// <inheritdoc/>
    public ITweenHandle TweenOffsetMin(RectTransform rectTransform, Vector2 to, float duration, Action? onComplete = null, O5Ease ease = O5Ease.OutSine)
        => Pick().TweenOffsetMin(rectTransform, to, duration, onComplete, ease);

    /// <inheritdoc/>
    public ITweenHandle Delay(float seconds, Action? onComplete = null)
        => Pick().Delay(seconds, onComplete);

    private ITweenRunner Pick() {
        if (_pickedDone && _picked != null) {
            return _picked;
        }

        _pickedDone = true;
#if LITMOTION
        try {
            if (TweenBackendProbe.TryLitMotion()) {
                Backend = TweenBackend.LitMotion;
                _picked = GetLitMotionRunner();
                return _picked;
            }
        } catch {
        }
#endif

        try {
            Backend = TweenBackend.GTween;
            _picked = GTweenRunner.Shared;
            return _picked;
        } catch {
        }

        try {
            if (DOTweenRunner.Shared.IsAvailable) {
                Backend = TweenBackend.DOTween;
                _picked = DOTweenRunner.Shared;
                return _picked;
            }
        } catch {
        }

        try {
            if (PrimeTweenRunner.Shared.IsAvailable) {
                Backend = TweenBackend.PrimeTween;
                _picked = PrimeTweenRunner.Shared;
                return _picked;
            }
        } catch {
        }

        try {
            Backend = TweenBackend.Simple;
            _picked = SimpleTweenRunner.Instance;
            return _picked;
        } catch {
        }

        Backend = TweenBackend.Instant;
        _picked = InstantTweenRunner.Instance;
        return _picked;
    }

#if LITMOTION
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static ITweenRunner GetLitMotionRunner() => LitMotionRunner.Instance;
#endif
}
