#nullable enable
using O5Kit.Core;
using UnityEngine;

namespace O5Kit.Factory;

/// <summary>
/// Creates and updates monochrome <see cref="TMPro.TextMeshProUGUI"/> icon elements.
/// </summary>
public static class O5FactoryIcons {
    public static TMPro.TextMeshProUGUI CreateIcon(
        Transform parent,
        string name,
        O5Icon icon,
        Color color,
        float? fixedFontSize = null
    ) => CreateIcon(parent, name, icon.Font, icon.Glyph, color, fixedFontSize);

    public static TMPro.TextMeshProUGUI CreateIcon(
        Transform parent,
        string name,
        TMPro.TMP_FontAsset? font,
        char glyph,
        Color color,
        float? fixedFontSize = null
    ) {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        var tmp = go.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.raycastTarget = false;
        tmp.richText = false;
        tmp.fontStyle = TMPro.FontStyles.Normal;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.margin = Vector4.zero;
        tmp.extraPadding = false;
        tmp.characterSpacing = 0f;
        tmp.wordSpacing = 0f;
        tmp.lineSpacing = 0f;
        tmp.paragraphSpacing = 0f;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TMPro.TextOverflowModes.Overflow;
        if (fixedFontSize.HasValue) {
            tmp.enableAutoSizing = false;
            tmp.fontSize = fixedFontSize.Value;
        } else {
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 4f;
            tmp.fontSizeMax = 512f;
        }
        tmp.color = color;
        ApplyGlyph(tmp, font, glyph);
        return tmp;
    }

    public static void SetIcon(TMPro.TextMeshProUGUI? tmp, O5Icon icon)
        => ApplyGlyph(tmp, icon.Font, icon.Glyph);

    public static void ApplyGlyph(TMPro.TextMeshProUGUI? tmp, TMPro.TMP_FontAsset? font, char glyph) {
        if (tmp == null) {
            return;
        }
        if (font == null || glyph == '\0') {
            tmp.text = string.Empty;
            tmp.enabled = false;
            return;
        }
        if (tmp.font != font) {
            tmp.font = font;
        }
        string target = glyph.ToString();
        if (tmp.text != target) {
            tmp.text = target;
        }
        if (!tmp.enabled) {
            tmp.enabled = true;
        }
    }
}
