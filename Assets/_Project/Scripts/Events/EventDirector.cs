using System.Collections.Generic;
using UnityEngine;

namespace ThirdLamp
{
    /// <summary>
    /// Evaluates narrative events a few times per second and fires their actions when all conditions hold.
    /// No randomness: every scripted beat is gated by progression, perception, location and object state.
    /// </summary>
    public class EventDirector : MonoBehaviour
    {
        public float tickInterval = 0.2f;

        readonly List<NarrativeEvent> events = new List<NarrativeEvent>();
        readonly Dictionary<NarrativeEvent, float> holdTimers = new Dictionary<NarrativeEvent, float>();
        readonly Dictionary<string, float> lastFired = new Dictionary<string, float>();
        readonly HashSet<string> fired = new HashSet<string>();
        float tickTimer;

        public IReadOnlyList<NarrativeEvent> Events => events;
        public bool HasFired(string id) => fired.Contains(id);

        public void Register(NarrativeEvent e)
        {
            if (e == null) return;
            if (events.Exists(x => x.id == e.id)) { Debug.LogWarning($"[ThirdLamp] Duplicate event '{e.id}'"); return; }
            events.Add(e);
        }

        void Update()
        {
            if (Game.Mode == InputMode.Paused || Game.Mode == InputMode.Ended) return;
            tickTimer += Time.deltaTime;
            if (tickTimer < tickInterval) return;
            float dt = tickTimer;
            tickTimer = 0f;

            for (int i = 0; i < events.Count; i++)
            {
                var e = events[i];
                if (e.once && fired.Contains(e.id)) continue;
                if (!e.once && lastFired.TryGetValue(e.id, out var t) && Time.time - t < e.cooldown) continue;

                if (!AllHold(e, dt))
                {
                    holdTimers.Remove(e);
                    continue;
                }

                holdTimers.TryGetValue(e, out var held);
                held += dt;
                holdTimers[e] = held;
                if (held >= e.delay) Fire(e);
            }
        }

        static bool AllHold(NarrativeEvent e, float dt)
        {
            bool ok = true;
            // Evaluate every condition (no short-circuit) so hold timers stay honest.
            foreach (var c in e.conditions)
                if (c != null && !c.Evaluate(dt)) ok = false;
            return ok;
        }

        public void Fire(NarrativeEvent e)
        {
            holdTimers.Remove(e);
            lastFired[e.id] = Time.time;
            if (e.once) fired.Add(e.id);
            foreach (var c in e.conditions) c?.ResetTimers();
            foreach (var a in e.actions)
            {
                try { a?.Execute(false); }
                catch (System.Exception ex) { Debug.LogException(ex); }
            }
            Game.State.Set("evt_" + e.id);
        }

        public List<string> FiredIds() => new List<string>(fired);

        /// <summary>Marks events as already fired and silently re-applies their persistent world changes.</summary>
        public void RestoreFired(List<string> ids)
        {
            foreach (var id in ids)
            {
                fired.Add(id);
                var e = events.Find(x => x.id == id);
                if (e == null) continue;
                foreach (var a in e.actions)
                    if (a != null && a.RestoresState) a.Execute(true);
            }
        }
    }
}
