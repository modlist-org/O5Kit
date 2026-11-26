// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using O5Kit.Behaviour;
using UnityEngine;
using UnityEngine.UI;

namespace O5Kit.Core;

/// <summary>
/// Keeps O5Kit and Overlayer UI rendering identical across Gamma and Linear Unity games.
/// On Gamma games every tracking/sync call is a no-op; on Linear games it enables
/// <c>Canvas.vertexColorAlwaysGammaSpace</c> (Unity 2022+/Unity 6) and compensates
/// hardware linear alpha blending via <see cref="CanvasRenderer.SetColor"/> without
/// mutating <see cref="Graphic.color"/>.
/// </summary>
public static class O5ColorSpace {
    private static bool? _isLinear;

    /// <summary>True when the host Unity player runs in Linear color space.</summary>
    public static bool IsLinear {
        get {
            if (_isLinear.HasValue) {
                return _isLinear.Value;
            }

            try {
                _isLinear = QualitySettings.activeColorSpace == ColorSpace.Linear;
            } catch {
                _isLinear = false;
            }

            return _isLinear.Value;
        }
    }

    /// <summary>True when this Unity version supports <c>Canvas.vertexColorAlwaysGammaSpace</c>.</summary>
    public static bool HasGammaVertexColors => Runtime.HasGammaVertexColors;

    /// <summary>Forces vertex colors on <paramref name="canvas"/> to stay in gamma space when supported.</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void EnableGammaVertexColors(Canvas? canvas) {
        if (!IsLinear || canvas == null) {
            return;
        }

        Runtime.EnableGammaVertexColors(canvas);
    }

    /// <summary>
    /// Registers <paramref name="canvas"/> for Linear-space color consistency.
    /// Does nothing when the host game already uses Gamma color space.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Track(Canvas? canvas, float fallbackBgLuma = 0.08f) {
        if (!IsLinear || canvas == null) {
            return;
        }

        Runtime.Track(canvas, fallbackBgLuma);
    }

    /// <summary>Unregisters <paramref name="canvas"/> when destroyed or disposed.</summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Untrack(Canvas? canvas) {
        if (!IsLinear || canvas == null) {
            return;
        }

        Runtime.Untrack(canvas);
    }

    /// <summary>IEC 61966-2-1 sRGB-to-Linear transfer function.</summary>
    public static float SrgbToLinear(float c) {
        c = Clamp01(c);
        return c <= 0.04045f
            ? c / 12.92f
            : (float)Math.Pow((c + 0.055d) / 1.055d, 2.4d);
    }

    /// <summary>IEC 61966-2-1 Linear-to-sRGB transfer function.</summary>
    public static float LinearToSrgb(float c) {
        c = Clamp01(c);
        return c <= 0.0031308f
            ? 12.92f * c
            : (float)((1.055d * Math.Pow(c, 1.0d / 2.4d)) - 0.055d);
    }

    /// <summary>Rec.709 relative luminance computed from sRGB components.</summary>
    public static float RelativeLuminance(Color c)
        => (0.2126f * Clamp01(c.r)) + (0.7152f * Clamp01(c.g)) + (0.0722f * Clamp01(c.b));

    /// <summary>
    /// Folds semi-transparent sRGB ancestor layers (ordered inner-to-outer) onto <paramref name="baseLuma"/>.
    /// </summary>
    public static float CompositeBackgroundLuminance(float baseLuma, IReadOnlyList<(float Luma, float Alpha)>? layersInnerToOuter) {
        float luma = Clamp01(baseLuma);
        if (layersInnerToOuter == null) {
            return luma;
        }

        for (int i = layersInnerToOuter.Count - 1; i >= 0; i--) {
            float a = Clamp01(layersInnerToOuter[i].Alpha);
            float layerLuma = Clamp01(layersInnerToOuter[i].Luma);
            luma = (a * layerLuma) + ((1f - a) * luma);
        }

        return luma;
    }

    /// <summary>
    /// Computes the <see cref="CanvasRenderer"/> multiplier color so hardware Linear blending
    /// over a surface of sRGB luminance <paramref name="bgLuma"/> matches Gamma-space blending.
    /// Returns <see cref="Color.white"/> for fully transparent or fully opaque foreground colors.
    /// </summary>
    public static Color ComputeLinearBlendTint(Color fg, float bgLuma, bool allowRgbBoost) {
        if (fg.a <= 0.001f || fg.a >= 0.999f) {
            return Color.white;
        }

        bgLuma = Clamp01(bgLuma);
        float fgY = RelativeLuminance(fg);
        float targetSrgb = (fg.a * fgY) + ((1f - fg.a) * bgLuma);
        float targetLin = SrgbToLinear(targetSrgb);
        float bgLin = SrgbToLinear(bgLuma);
        float fgLin = SrgbToLinear(fgY);

        if (Math.Abs(fgLin - bgLin) < 1e-4f) {
            return Color.white;
        }

        float aLin = Clamp01((targetLin - bgLin) / (fgLin - bgLin));
        float rgbScale = 1f;

        // Avoid 8-bit Color32 alpha quantization for ultra-faint light tints over dark UI:
        // scale vertex RGB slightly down so effective linear alpha rises above quantization floor.
        if (allowRgbBoost && aLin < 0.04f && fgY > Math.Max(0.45f, bgLuma + 0.25f)) {
            float newFgY = Math.Max(0.42f, bgLuma + 0.25f);
            float newFgLin = SrgbToLinear(newFgY);
            if (newFgLin > bgLin + 1e-4f) {
                float aBoost = (targetLin - bgLin) / (newFgLin - bgLin);
                if (aBoost <= 0.95f) {
                    rgbScale = newFgY / fgY;
                    aLin = aBoost;
                }
            }
        }

        float alphaScale = Math.Min(4f, aLin / fg.a);
        return new Color(rgbScale, rgbScale, rgbScale, alphaScale);
    }

    private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);

    // Isolated in a nested type so pure-.NET test hosts can exercise the color math
    // and IsLinear guard without loading UnityEngine.UI / UnityEngine.UIModule assemblies.
    private static class Runtime {
        private readonly struct TrackedCanvas {
            public readonly Canvas Canvas;
            public readonly float FallbackBgLuma;

            public TrackedCanvas(Canvas canvas, float fallbackBgLuma) {
                Canvas = canvas;
                FallbackBgLuma = fallbackBgLuma;
            }
        }

        private readonly struct AncestorNode {
            public readonly Graphic Bg;
            public readonly CanvasRenderer? Cr;
            public readonly Mask? Mask;

            public AncestorNode(Graphic bg, CanvasRenderer? cr, Mask? mask) {
                Bg = bg;
                Cr = cr;
                Mask = mask;
            }
        }

        private sealed class GraphicEntry {
            public readonly CanvasRenderer? Cr;
            public readonly Mask? Mask;
            public readonly TMPro.TMP_Text? SubText;
            public readonly bool IsEmpty;
            public readonly bool IsRawImage;
            public bool HasTint;
            public Color LastFg;
            public float LastBgLuma;
            public Color LastTint;
            public int BgChainFrame = -1000;
            public List<AncestorNode>? Ancestors;

            public GraphicEntry(Graphic g) {
                IsEmpty = IsEmptyGraphic(g);
                if (IsEmpty) {
                    return;
                }

                Cr = g.canvasRenderer;
                Mask = g.GetComponent<Mask>();
                TMPro.TMP_SubMeshUI? sub = AsGraphic<TMPro.TMP_SubMeshUI>(g);
                if (sub != null) {
                    SubText = sub.textComponent;
                }
                IsRawImage = AsGraphic<RawImage>(g) != null;
            }
        }

        private static readonly PropertyInfo? VertexColorGammaProp =
            typeof(Canvas).GetProperty("vertexColorAlwaysGammaSpace");

        private static readonly List<TrackedCanvas> Tracked = new();
#if IL2CPP
        private static readonly Dictionary<IntPtr, GraphicEntry> Entries = new();
#else
        private static readonly Dictionary<int, GraphicEntry> Entries = new();
#endif
        private static readonly List<(float Luma, float Alpha)> AncestorLayers = new(16);
        private static bool _hooked;
        private static Canvas.WillRenderCanvases? _renderHook;
        private static int _lastPruneFrame;

        public static bool HasGammaVertexColors => VertexColorGammaProp != null && VertexColorGammaProp.CanWrite;

        public static void EnableGammaVertexColors(Canvas canvas) {
            if (!HasGammaVertexColors) {
                return;
            }

            try {
                VertexColorGammaProp!.SetValue(canvas, true, null);
            } catch {
            }
        }

        public static void Track(Canvas canvas, float fallbackBgLuma) {
            EnableGammaVertexColors(canvas);

            int id = canvas.GetInstanceID();
            float clampedBg = Clamp01(fallbackBgLuma);
            for (int i = 0; i < Tracked.Count; i++) {
                if (Tracked[i].Canvas != null && Tracked[i].Canvas.GetInstanceID() == id) {
                    Tracked[i] = new TrackedCanvas(canvas, clampedBg);
                    return;
                }
            }

            Tracked.Add(new TrackedCanvas(canvas, clampedBg));
            EnsureHook(true);
        }

        public static void Untrack(Canvas canvas) {
            int id = canvas.GetInstanceID();
            for (int i = Tracked.Count - 1; i >= 0; i--) {
                Canvas c = Tracked[i].Canvas;
                if (c == null || c.GetInstanceID() == id) {
                    Tracked.RemoveAt(i);
                }
            }

            if (Tracked.Count == 0) {
                EnsureHook(false);
                Entries.Clear();
            }
        }

        private static void EnsureHook(bool on) {
            if (on == _hooked) {
                return;
            }

#if IL2CPP
            _renderHook ??= Il2CppInterop.Runtime.DelegateSupport.ConvertDelegate<Canvas.WillRenderCanvases>(new Action(SyncAll));
#else
            _renderHook ??= SyncAll;
#endif
            if (on) {
                try {
                    _ = CanvasUpdateRegistry.instance;
                } catch {
                }
                Canvas.willRenderCanvases += _renderHook;
            } else {
                Canvas.willRenderCanvases -= _renderHook;
            }

            _hooked = on;
        }

        private static void SyncAll() {
            int frame = Time.frameCount;
            for (int i = Tracked.Count - 1; i >= 0; i--) {
                TrackedCanvas entry = Tracked[i];
                if (!entry.Canvas) {
                    Tracked.RemoveAt(i);
                    continue;
                }

                if (!entry.Canvas.isActiveAndEnabled) {
                    continue;
                }

                SyncCanvas(entry.Canvas, entry.FallbackBgLuma, frame);
            }

            if (Tracked.Count == 0) {
                EnsureHook(false);
                Entries.Clear();
            } else if (Entries.Count > 512 && frame - _lastPruneFrame >= 300) {
                _lastPruneFrame = frame;
                PruneDeadEntries();
            }
        }

        private static void PruneDeadEntries() {
#if IL2CPP
            List<IntPtr>? dead = null;
#else
            List<int>? dead = null;
#endif
            foreach (var kv in Entries) {
                if (!kv.Value.IsEmpty && kv.Value.Cr == null) {
                    dead ??= new();
                    dead.Add(kv.Key);
                }
            }
            if (dead != null) {
                for (int i = 0; i < dead.Count; i++) {
                    Entries.Remove(dead[i]);
                }
            }
        }

        private static void SyncCanvas(Canvas canvas, float fallbackBgLuma, int frame) {
            var graphics = GraphicRegistry.GetGraphicsForCanvas(canvas);
            if (graphics == null) {
                return;
            }

            Transform canvasRoot = canvas.transform;
#if IL2CPP
            var col = graphics.TryCast<Il2CppSystem.Collections.Generic.ICollection<Graphic>>();
            int count = col != null ? col.Count : 0;
            for (int i = 0; i < count; i++) {
                Graphic g = graphics[i];
                if (g != null && g.isActiveAndEnabled) {
                    SyncGraphic(g, canvasRoot, fallbackBgLuma, frame);
                }
            }
#else
            for (int i = 0; i < graphics.Count; i++) {
                Graphic g = graphics[i];
                if (g != null && g.isActiveAndEnabled) {
                    SyncGraphic(g, canvasRoot, fallbackBgLuma, frame);
                }
            }
#endif
        }

        private static void SyncGraphic(Graphic g, Transform canvasRoot, float fallbackBgLuma, int frame) {
#if IL2CPP
            IntPtr key = g.Pointer;
#else
            int key = g.GetInstanceID();
#endif
            if (!Entries.TryGetValue(key, out GraphicEntry? entry)) {
                entry = new GraphicEntry(g);
                Entries[key] = entry;
            }

            if (entry.IsEmpty || entry.Cr == null) {
                return;
            }

            CanvasRenderer cr = entry.Cr;
            if (entry.Mask != null && entry.Mask.enabled && !entry.Mask.showMaskGraphic) {
                if (entry.HasTint) {
                    cr.SetColor(Color.white);
                    entry.HasTint = false;
                }
                return;
            }

            Color effectiveColor = entry.SubText != null ? entry.SubText.color : g.color;
            float inheritedAlpha = Clamp01(cr.GetInheritedAlpha());
            float combinedAlpha = Clamp01(effectiveColor.a) * inheritedAlpha;
            if (combinedAlpha <= 0.001f || combinedAlpha >= 0.999f) {
                if (entry.HasTint) {
                    cr.SetColor(Color.white);
                    entry.HasTint = false;
                }
                return;
            }

            Color effectiveFg = new(effectiveColor.r, effectiveColor.g, effectiveColor.b, combinedAlpha);
            float bgLuma = FindBackgroundLuminance(entry, g, canvasRoot, fallbackBgLuma, effectiveFg, frame);
            if (entry.HasTint
                && ColorsExact(entry.LastFg, effectiveFg)
                && Math.Abs(entry.LastBgLuma - bgLuma) < 0.005f
                && ColorsExact(cr.GetColor(), entry.LastTint)) {
                return;
            }

            bool allowRgbBoost = HasGammaVertexColors && !entry.IsRawImage;
            Color tint = ComputeLinearBlendTint(effectiveFg, bgLuma, allowRgbBoost);
            cr.SetColor(tint);
            entry.HasTint = true;
            entry.LastFg = effectiveFg;
            entry.LastBgLuma = bgLuma;
            entry.LastTint = tint;
        }

        private static void RefreshAncestorChain(GraphicEntry entry, Graphic g, Transform canvasRoot, int frame) {
            entry.BgChainFrame = frame;
            if (entry.Ancestors == null) {
                entry.Ancestors = new List<AncestorNode>(4);
            } else {
                entry.Ancestors.Clear();
            }

            Transform? p = g.transform.parent;
            while (p != null) {
                Graphic? bg = p.GetComponent<Graphic>();
                if (bg != null && !IsSkippedBackgroundGraphic(bg)) {
                    entry.Ancestors.Add(new AncestorNode(bg, bg.canvasRenderer, p.GetComponent<Mask>()));
                }

                if (p == canvasRoot) {
                    break;
                }
                p = p.parent;
            }
        }

        private static float FindBackgroundLuminance(GraphicEntry entry, Graphic g, Transform canvasRoot, float fallbackBgLuma, Color fg, int frame) {
            if (entry.Ancestors == null || frame - entry.BgChainFrame >= 60) {
                RefreshAncestorChain(entry, g, canvasRoot, frame);
            }

            AncestorLayers.Clear();
            float baseLuma = 0f;
            bool foundOpaque = false;

            List<AncestorNode> ancestors = entry.Ancestors!;
            for (int i = 0; i < ancestors.Count; i++) {
                AncestorNode node = ancestors[i];
                Graphic bg = node.Bg;
                if (bg == null || !bg.enabled) {
                    continue;
                }
                if (node.Cr != null && node.Cr.cull) {
                    continue;
                }
                if (node.Mask != null && node.Mask.enabled && !node.Mask.showMaskGraphic) {
                    continue;
                }

                Color c = bg.color;
                float alpha = Clamp01(c.a);
                if (alpha > 0.001f) {
                    float luma = RelativeLuminance(c);
                    if (alpha >= 0.995f) {
                        baseLuma = luma;
                        foundOpaque = true;
                        break;
                    }
                    AncestorLayers.Add((luma, alpha));
                }
            }

            if (!foundOpaque) {
                baseLuma = RelativeLuminance(fg) < 0.25f ? 0.45f : fallbackBgLuma;
            }

            float result = CompositeBackgroundLuminance(baseLuma, AncestorLayers);
            AncestorLayers.Clear();
            return result;
        }

        private static bool IsEmptyGraphic(Graphic g) {
#if IL2CPP
            if (g is EmptyGraphic) {
                return true;
            }
            return Il2CppInterop.Runtime.Il2CppClassPointerStore<EmptyGraphic>.NativeClassPtr != IntPtr.Zero
                && g.TryCast<EmptyGraphic>() != null;
#else
            return g is EmptyGraphic;
#endif
        }

        private static T? AsGraphic<T>(Graphic g) where T : Graphic {
#if IL2CPP
            return g as T ?? g.TryCast<T>();
#else
            return g as T;
#endif
        }

        private static bool IsSkippedBackgroundGraphic(Graphic bg)
            => IsEmptyGraphic(bg)
            || AsGraphic<TMPro.TMP_Text>(bg) != null
            || AsGraphic<TMPro.TMP_SubMeshUI>(bg) != null
            || AsGraphic<Text>(bg) != null;

        private static bool ColorsExact(Color a, Color b)
            => Math.Abs(a.r - b.r) < 1e-4f
            && Math.Abs(a.g - b.g) < 1e-4f
            && Math.Abs(a.b - b.b) < 1e-4f
            && Math.Abs(a.a - b.a) < 1e-4f;
    }
}
