using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdLamp
{
    /// <summary>
    /// Story state: flags (with the time they were set), integer counters and the in-game clock.
    /// Everything the EventDirector's conditions read lives here or in PerceptionManager.
    /// </summary>
    public class GameState : MonoBehaviour
    {
        public const string CataloguedCounter = "catalogued_items";

        readonly Dictionary<string, float> flags = new Dictionary<string, float>();
        readonly Dictionary<string, int> counters = new Dictionary<string, int>();

        [Tooltip("Minutes since midnight. The slice starts at 21:40.")]
        public float clockMinutes = 21 * 60 + 40;
        [Tooltip("In-game seconds per real second.")]
        public float clockScale = 5f;
        public bool clockRunning;

        /// <summary>Story date shown on the camera and phone.</summary>
        public const string DateText = "12.10.02";

        public event Action Changed;

        public bool Has(string flag) => !string.IsNullOrEmpty(flag) && flags.ContainsKey(flag);

        public void Set(string flag)
        {
            if (string.IsNullOrEmpty(flag) || flags.ContainsKey(flag)) return;
            flags[flag] = Time.time;
            Changed?.Invoke();
        }

        public void Clear(string flag)
        {
            if (flags.Remove(flag)) Changed?.Invoke();
        }

        /// <summary>Real seconds since the flag was set, or -1 if it isn't set.</summary>
        public float TimeSince(string flag) => flags.TryGetValue(flag, out var t) ? Time.time - t : -1f;

        public int Get(string counter) => counters.TryGetValue(counter, out var v) ? v : 0;

        public void Add(string counter, int amount = 1)
        {
            counters[counter] = Get(counter) + amount;
            Changed?.Invoke();
        }

        public void NotifyChanged() => Changed?.Invoke();

        public int Hour => Mathf.FloorToInt(clockMinutes / 60f) % 24;
        public int Minute => Mathf.FloorToInt(clockMinutes) % 60;
        public string ClockText => $"{Hour:00}:{Minute:00}";

        public bool ClockAtLeast(int hour, int minute) => clockMinutes >= hour * 60 + minute;

        void Update()
        {
            if (clockRunning && Game.Mode != InputMode.Paused && Game.Mode != InputMode.Ended)
                clockMinutes += Time.deltaTime * clockScale / 60f;
        }

        // ---- save support ----
        public List<string> AllFlags() => new List<string>(flags.Keys);

        public List<CounterEntry> AllCounters()
        {
            var list = new List<CounterEntry>();
            foreach (var kv in counters) list.Add(new CounterEntry { key = kv.Key, value = kv.Value });
            return list;
        }

        public void Restore(List<string> savedFlags, List<CounterEntry> savedCounters, float clock)
        {
            flags.Clear();
            counters.Clear();
            // Restored flags count as "set a long time ago" so timed follow-ups don't re-trigger instantly
            // unless their own conditions still hold.
            foreach (var f in savedFlags) flags[f] = Time.time - 3600f;
            foreach (var c in savedCounters) counters[c.key] = c.value;
            clockMinutes = clock;
            Changed?.Invoke();
        }
    }

    [Serializable]
    public struct CounterEntry
    {
        public string key;
        public int value;
    }
}
