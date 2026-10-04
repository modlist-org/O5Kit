// SPDX-License-Identifier: LGPL-3.0-or-later

using System.Collections.Generic;
using O5Kit.Core;
using UnityEngine;

namespace O5Kit.Transition;

/// <summary>Slide-and-fade transition between registered pages.</summary>
public static class O5PageSwitcher {
    private static readonly List<ITweenHandle> _active = new();

    private static void KillActive() {
        foreach (var h in _active) {
            h.Kill();
        }

        _active.Clear();
    }

    /// <summary>Slides from one page to another with a crossfade.</summary>
    /// <param name="ctx">Owning kit context.</param>
    /// <param name="pages">Page registry (id to rect).</param>
    /// <param name="from">Outgoing page id.</param>
    /// <param name="to">Incoming page id.</param>
    /// <returns>False when ids match or a page is missing.</returns>
    public static bool SwitchPage(O5Context ctx, IReadOnlyDictionary<int, RectTransform> pages, int from, int to) {
        if (from == to) {
            return false;
        }

        if (!pages.TryGetValue(from, out RectTransform? fromPage)) {
            return false;
        }

        if (!pages.TryGetValue(to, out RectTransform? toPage)) {
            return false;
        }

        CanvasGroup? fromCg = fromPage.GetComponent<CanvasGroup>();
        CanvasGroup? toCg = toPage.GetComponent<CanvasGroup>();

        if (fromCg == null || toCg == null) {
            return false;
        }

        KillActive();

        foreach (var pair in pages) {
            if (pair.Key == to || pair.Value == null) {
                continue;
            }

            pair.Value.anchoredPosition = new Vector2(-1100f, pair.Value.anchoredPosition.y);
            var otherCg = pair.Value.GetComponent<CanvasGroup>();
            if (otherCg != null) {
                otherCg.alpha = 0f;
                otherCg.interactable = otherCg.blocksRaycasts = false;
            }
        }

        fromPage.anchoredPosition = Vector2.zero;
        toPage.anchoredPosition = new Vector2(1100f, 0f);

        fromCg.alpha = 1f;
        toCg.alpha = 0f;

        fromCg.interactable = fromCg.blocksRaycasts = false;
        toCg.interactable = toCg.blocksRaycasts = false;

        var runner = ctx.Tween;

        _active.Add(runner.TweenFloat(
            () => fromPage ? fromPage.anchoredPosition.x : -1100f,
            v => {
                if (fromPage) {
                    var p = fromPage.anchoredPosition;
                    p.x = v;
                    fromPage.anchoredPosition = p;
                }
            },
            -1100f, 0.45f, ease: O5Ease.OutExpo));

        _active.Add(runner.TweenFloat(
            () => fromCg ? fromCg.alpha : 0f,
            v => {
                if (fromCg) {
                    fromCg.alpha = v;
                }
            },
            0f, 0.3f));

        _active.Add(runner.TweenFloat(
            () => toPage ? toPage.anchoredPosition.x : 0f,
            v => {
                if (toPage) {
                    var p = toPage.anchoredPosition;
                    p.x = v;
                    toPage.anchoredPosition = p;
                }
            },
            0f, 0.45f,
            () => {
                if (fromCg) {
                    fromCg.interactable = fromCg.blocksRaycasts = false;
                }
            },
            O5Ease.OutExpo));

        _active.Add(runner.TweenFloat(
            () => toCg ? toCg.alpha : 1f,
            v => {
                if (toCg) {
                    toCg.alpha = v;
                }
            },
            1f, 0.3f));

        _active.Add(runner.TweenFloat(
            () => 0f, _ => { }, 1f, 0.1f,
            () => {
                if (toCg) {
                    toCg.interactable = toCg.blocksRaycasts = true;
                }
            }));

        return true;
    }
}
