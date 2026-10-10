// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
using System.Collections.Generic;
using UnityEngine;

namespace O5Kit.Behaviour;

/// <summary>Procedural resize arrow textures and sprites (no asset files needed).</summary>
public static class O5ResizeCursor {
    private const int Size = 32;
    private static readonly Dictionary<int, Texture2D> TextureCache = new();
    private static readonly Dictionary<int, Sprite> SpriteCache = new();

    public static Sprite GetSprite(ResizeHandleType type) {
        int angle = type switch {
            ResizeHandleType.Left or ResizeHandleType.Right => 0,
            ResizeHandleType.Top or ResizeHandleType.Bottom => 90,
            ResizeHandleType.TopLeft or ResizeHandleType.BottomRight => 45,
            _ => 135,
        };
        if(!SpriteCache.TryGetValue(angle, out var sprite)) {
            sprite = Sprite.Create(GetTexture(angle),
                new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f));
            SpriteCache[angle] = sprite;
        }
        return sprite;
    }

    private static Texture2D GetTexture(int angle) {
        if(!TextureCache.TryGetValue(angle, out var tex)) {
            tex = Build(angle);
            TextureCache[angle] = tex;
        }
        return tex;
    }

    private static Texture2D Build(int angleDegrees) {
        var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false) {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        bool[] shape = new bool[Size * Size];
        float rad = angleDegrees * Mathf.PI / 180f;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        float half = Size / 2f;
        for(int y = 0; y < Size; y++) {
            for(int x = 0; x < Size; x++) {
                float dx = x + 0.5f - half;
                float dy = (y + 0.5f - half) * -1f;
                float u = dx * cos + dy * sin;
                float v = -dx * sin + dy * cos;
                shape[y * Size + x] = InArrow(u, v);
            }
        }
        var pixels = new Color32[Size * Size];
        for(int y = 0; y < Size; y++) {
            for(int x = 0; x < Size; x++) {
                if(shape[y * Size + x]) {
                    pixels[y * Size + x] = new Color32(255, 255, 255, 255);
                    continue;
                }
                bool near = false;
                for(int oy = -1; oy <= 1 && !near; oy++) {
                    for(int ox = -1; ox <= 1 && !near; ox++) {
                        if(ox == 0 && oy == 0) {
                            continue;
                        }
                        int nx = x + ox;
                        int ny = y + oy;
                        if(nx >= 0 && nx < Size && ny >= 0 && ny < Size && shape[ny * Size + nx]) {
                            near = true;
                        }
                    }
                }
                pixels[y * Size + x] = near ? new Color32(0, 0, 0, 255) : new Color32(0, 0, 0, 0);
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply(false, false);
        return tex;
    }

    private static bool InArrow(float u, float v) {
        float au = Math.Abs(u);
        float av = Math.Abs(v);
        if(au <= 8f && av <= 2f) {
            return true;
        }
        if(au > 8f && au <= 13f && av <= (13f - au) * 0.9f) {
            return true;
        }
        return false;
    }
}
