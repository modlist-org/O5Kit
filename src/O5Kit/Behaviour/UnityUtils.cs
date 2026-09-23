// SPDX-License-Identifier: LGPL-3.0-or-later

using System;
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
            DelegateSupport.ConvertDelegate<UnityEngine.Events.UnityAction<BaseEventData>>(new Action<BaseEventData>(
#endif
                e => cb(e)
#if IL2CPP
            ))
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
}
