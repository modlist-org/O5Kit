// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using UnityEngine;
using UnityEngine.EventSystems;

#if IL2CPP
using MelonLoader;
#endif

namespace O5Kit.Behaviour;

/// <summary>EventTrigger wiring sugar with central IL2CPP handling.</summary>
public static class UnityUtils {
    /// <summary>Adds one trigger entry.</summary>
    /// <param name="trigger">Target trigger.</param>
    /// <param name="type">Event type.</param>
    /// <param name="cb">Callback receiving event data.</param>
    public static void AddEvent(EventTrigger trigger, EventTriggerType type, Action<BaseEventData> cb) {
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(
#if IL2CPP
            new Action<BaseEventData>(e => cb(e))
#else
            e => cb(e)
#endif
        );
        trigger.triggers.Add(entry);
    }

    /// <summary>Adds several trigger entries at once.</summary>
    /// <param name="trigger">Target trigger.</param>
    /// <param name="events">Type/callback pairs.</param>
    public static void AddEvents(EventTrigger trigger, params (EventTriggerType type, Action<BaseEventData> cb)[] events) {
        foreach (var (type, cb) in events) {
            AddEvent(trigger, type, cb);
        }
    }

    /// <summary>Adds one trigger entry. The callback ignores event data.</summary>
    /// <param name="trigger">Target trigger.</param>
    /// <param name="type">Event type.</param>
    /// <param name="cb">Callback.</param>
    public static void AddEvent(EventTrigger trigger, EventTriggerType type, Action cb)
        => AddEvent(trigger, type, _ => cb());

    /// <summary>Adds several trigger entries at once. Callbacks ignore event data.</summary>
    /// <param name="trigger">Target trigger.</param>
    /// <param name="events">Type/callback pairs.</param>
    public static void AddEvents(EventTrigger trigger, params (EventTriggerType type, Action cb)[] events) {
        foreach (var (type, cb) in events) {
            AddEvent(trigger, type, cb);
        }
    }

    /// <summary>Casts EventTrigger <see cref="BaseEventData"/> to <see cref="PointerEventData"/> safely on both Mono and IL2CPP.</summary>
    public static PointerEventData? AsPointerEventData(BaseEventData? e) {
#if IL2CPP
        return e?.TryCast<PointerEventData>();
#else
        return e as PointerEventData;
#endif
    }

    /// <summary>Casts a <see cref="Transform"/> to <see cref="RectTransform"/> safely on both Mono and IL2CPP.</summary>
    public static RectTransform? AsRectTransform(this Transform? transform) {
        if (transform == null) {
            return null;
        }
#if IL2CPP
        return transform.TryCast<RectTransform>();
#else
        return transform as RectTransform;
#endif
    }

#if IL2CPP
    private static Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<Vector3>? _cornerScratch;
#endif

    /// <summary>
    /// Fills <paramref name="fourCorners"/> with world-space corners safely on both Mono and IL2CPP.
    /// On IL2CPP, passing a managed <c>Vector3[]</c> directly to <see cref="RectTransform.GetWorldCorners"/>
    /// copies into a temporary <c>Il2CppStructArray&lt;Vector3&gt;</c> and discards the output.
    /// </summary>
    public static void GetWorldCornersSafe(this RectTransform rect, Vector3[] fourCorners) {
#if IL2CPP
        var buf = _cornerScratch ??= new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<Vector3>(4);
        rect.GetWorldCorners(buf);
        fourCorners[0] = buf[0];
        fourCorners[1] = buf[1];
        fourCorners[2] = buf[2];
        fourCorners[3] = buf[3];
#else
        rect.GetWorldCorners(fourCorners);
#endif
    }

    /// <summary>Returns false on IL2CPP when a Unity component type was stripped from the game build.</summary>
    public static bool IsComponentTypeAvailable<T>() where T : Component {
#if IL2CPP
        return Il2CppInterop.Runtime.Il2CppClassPointerStore<T>.NativeClassPtr != IntPtr.Zero;
#else
        return true;
#endif
    }

    /// <summary>Gets a component without crashing IL2CPP if <typeparamref name="T"/> was stripped.</summary>
    public static T? SafeGetComponent<T>(this GameObject? go) where T : Component {
        if (go == null || !IsComponentTypeAvailable<T>()) {
            return null;
        }
        return go.GetComponent<T>();
    }

    /// <summary>Casts a Unity <see cref="Component"/> safely on both Mono and IL2CPP.</summary>
    public static T? TryCastComponent<T>(this Component? comp) where T : Component {
        if (comp == null || !IsComponentTypeAvailable<T>()) {
            return null;
        }
#if IL2CPP
        return comp.TryCast<T>();
#else
        return comp as T;
#endif
    }

#if IL2CPP
    private static readonly System.Collections.Generic.Dictionary<Type, IntPtr> _nativeDefaultCtors = new();

    /// <summary>Invokes an IL2CPP base method non-virtually to avoid <c>base.VirtualMethod()</c> recursion in injected subclasses.</summary>
    public static unsafe void InvokeBaseMethod(Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase target, IntPtr methodPtr) {
        if (target == null || methodPtr == IntPtr.Zero) {
            return;
        }
        IntPtr exc = IntPtr.Zero;
        Il2CppInterop.Runtime.IL2CPP.il2cpp_runtime_invoke(
            methodPtr,
            Il2CppInterop.Runtime.IL2CPP.Il2CppObjectBaseToPtrNotNull(target),
            (void**)null,
            ref exc);
        Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
    }

    /// <summary>Invokes a 1-arg IL2CPP base method non-virtually to avoid <c>base.VirtualMethod()</c> recursion in injected subclasses.</summary>
    public static unsafe void InvokeBaseMethod(
        Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase target,
        IntPtr methodPtr,
        Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase? arg0) {
        if (target == null || methodPtr == IntPtr.Zero) {
            return;
        }
        void** args = stackalloc void*[1];
        args[0] = (void*)Il2CppInterop.Runtime.IL2CPP.Il2CppObjectBaseToPtr(arg0);
        IntPtr exc = IntPtr.Zero;
        Il2CppInterop.Runtime.IL2CPP.il2cpp_runtime_invoke(
            methodPtr,
            Il2CppInterop.Runtime.IL2CPP.Il2CppObjectBaseToPtrNotNull(target),
            args,
            ref exc);
        Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
    }

    private static unsafe T? CreateNativeIl2CppObject<T>(Func<IntPtr, T> wrap) where T : Il2CppInterop.Runtime.InteropTypes.Il2CppObjectBase {
        IntPtr klass = Il2CppInterop.Runtime.Il2CppClassPointerStore<T>.NativeClassPtr;
        if (klass == IntPtr.Zero) {
            return null;
        }
        if (!_nativeDefaultCtors.TryGetValue(typeof(T), out IntPtr ctor)) {
            var field = typeof(T).GetField(
                "NativeMethodInfoPtr__ctor_Public_Void_0",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            ctor = field != null ? (IntPtr)field.GetValue(null)! : IntPtr.Zero;
            _nativeDefaultCtors[typeof(T)] = ctor;
        }
        if (ctor == IntPtr.Zero) {
            return null;
        }
        IntPtr objPtr = Il2CppInterop.Runtime.IL2CPP.il2cpp_object_new(klass);
        if (objPtr == IntPtr.Zero) {
            return null;
        }
        IntPtr exc = IntPtr.Zero;
        Il2CppInterop.Runtime.IL2CPP.il2cpp_runtime_invoke(ctor, objPtr, (void**)null, ref exc);
        Il2CppInterop.Runtime.Il2CppException.RaiseExceptionIfNecessary(exc);
        return wrap(objPtr);
    }
    private static readonly System.Collections.Generic.Dictionary<IntPtr, (UnityEngine.UI.MaskableGraphic Graphic, Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<Vector3>? Corners, UnityEngine.UI.MaskableGraphic.CullStateChangedEvent? CullEvent)> _pinnedMaskableGraphics = new();

    private static T EnsureUnityEvent<T>(T? existing, Func<IntPtr, T> wrap) where T : UnityEngine.Events.UnityEventBase {
        if (existing != null
            && existing.m_Calls != null
            && existing.m_Calls.m_RuntimeCalls != null
            && existing.m_Calls.m_PersistentCalls != null
            && existing.m_PersistentCalls != null) {
            return existing;
        }
        return CreateNativeIl2CppObject(wrap) ?? existing!;
    }

    private static void PrunePinnedMaskableGraphics() {
        if (_pinnedMaskableGraphics.Count < 32) {
            return;
        }
        System.Collections.Generic.List<IntPtr>? dead = null;
        foreach (var kv in _pinnedMaskableGraphics) {
            if (kv.Value.Graphic == null) {
                (dead ??= new()).Add(kv.Key);
            }
        }
        if (dead != null) {
            foreach (var key in dead) {
                _pinnedMaskableGraphics.Remove(key);
            }
        }
    }
#endif

    /// <summary>
    /// Initializes native <see cref="UnityEngine.UI.MaskableGraphic"/> defaults on IL2CPP-injected subclasses.
    /// <c>ClassInjector</c> skips <c>MaskableGraphic..ctor()</c> and leaves <c>has_references=0</c>/<c>gc_desc=0</c>
    /// on Unity 6, so we initialize and GC-pin <c>m_Corners</c> and <c>m_OnCullStateChanged</c> for the graphic's lifetime.
    /// Safe no-op on Mono.
    /// </summary>
    public static void InitInjectedMaskableGraphic(UnityEngine.UI.MaskableGraphic? graphic) {
#if IL2CPP
        if (graphic == null) {
            return;
        }
        graphic.m_ShouldRecalculateStencil = true;
        graphic.m_ShouldRecalculate = true;
        graphic.m_Maskable = true;
        var corners = graphic.m_Corners ?? new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<Vector3>(4);
        graphic.m_Corners = corners;
        var cullEv = EnsureUnityEvent(
            graphic.m_OnCullStateChanged,
            p => new UnityEngine.UI.MaskableGraphic.CullStateChangedEvent(p));
        graphic.m_OnCullStateChanged = cullEv;
        PrunePinnedMaskableGraphics();
        _pinnedMaskableGraphics[graphic.Pointer] = (graphic, corners, cullEv);
        if (graphic.isActiveAndEnabled) {
            graphic.RecalculateClipping();
            graphic.RecalculateMasking();
            graphic.SetMaterialDirty();
        }
#endif
    }

    /// <summary>
    /// Initializes native <see cref="TMPro.TMP_InputField"/> and <see cref="UnityEngine.UI.Selectable"/> defaults on
    /// IL2CPP-injected subclasses (where <c>ClassInjector</c> skips <c>TMP_InputField..ctor()</c>, leaving
    /// <c>onValueChanged</c>, <c>onEndEdit</c>, <c>m_ProcessingEvent</c>, etc. null or hollow). Safe no-op on Mono.
    /// </summary>
    public static void InitInjectedTMPInputField(TMPro.TMP_InputField? field) {
#if IL2CPP
        if (field == null || (field.m_OnValueChanged != null && field.m_OnValueChanged.m_Calls != null && field.m_CanvasGroupCache != null)) {
            return;
        }
        field.m_Colors = UnityEngine.UI.ColorBlock.defaultColorBlock;
        field.m_Transition = UnityEngine.UI.Selectable.Transition.ColorTint;
        field.m_Navigation = UnityEngine.UI.Navigation.defaultNavigation;
        field.m_Interactable = true;
        field.m_GroupsAllowInteraction = true;
        field.m_CurrentIndex = -1;
        field.m_CanvasGroupCache ??= new Il2CppSystem.Collections.Generic.List<UnityEngine.CanvasGroup>();
        field.m_AnimationTriggers ??= CreateNativeIl2CppObject(p => new UnityEngine.UI.AnimationTriggers(p));

        field.m_OnEndEdit = EnsureUnityEvent(field.m_OnEndEdit, p => new TMPro.TMP_InputField.SubmitEvent(p));
        field.m_OnSubmit = EnsureUnityEvent(field.m_OnSubmit, p => new TMPro.TMP_InputField.SubmitEvent(p));
        field.m_OnSelect = EnsureUnityEvent(field.m_OnSelect, p => new TMPro.TMP_InputField.SelectionEvent(p));
        field.m_OnDeselect = EnsureUnityEvent(field.m_OnDeselect, p => new TMPro.TMP_InputField.SelectionEvent(p));
        field.m_OnTextSelection = EnsureUnityEvent(field.m_OnTextSelection, p => new TMPro.TMP_InputField.TextSelectionEvent(p));
        field.m_OnEndTextSelection = EnsureUnityEvent(field.m_OnEndTextSelection, p => new TMPro.TMP_InputField.TextSelectionEvent(p));
        field.m_OnValueChanged = EnsureUnityEvent(field.m_OnValueChanged, p => new TMPro.TMP_InputField.OnChangeEvent(p));
        field.m_OnTouchScreenKeyboardStatusChanged = EnsureUnityEvent(field.m_OnTouchScreenKeyboardStatusChanged, p => new TMPro.TMP_InputField.TouchScreenKeyboardEvent(p));
        field.m_ProcessingEvent ??= CreateNativeIl2CppObject(p => new UnityEngine.Event(p));

        field.m_Text ??= string.Empty;
        field.m_OriginalText ??= string.Empty;
        field.m_RegexValue ??= string.Empty;
        field.m_AsteriskChar = '*';
        field.m_GlobalPointSize = 14f;
        field.m_ScrollSensitivity = 1f;
        field.m_CaretBlinkRate = 0.85f;
        field.m_CaretWidth = 1;
        field.m_RichText = true;
        field.m_OnFocusSelectAll = true;
        field.m_ResetOnDeActivation = true;
        field.m_RestoreOriginalTextOnEscape = true;
        field.m_DoubleClickDelay = 0.5f;
        field.m_CaretColor = new Color(50f / 255f, 50f / 255f, 50f / 255f, 1f);
        field.m_SelectionColor = new Color(168f / 255f, 206f / 255f, 1f, 192f / 255f);
#endif
    }
}
