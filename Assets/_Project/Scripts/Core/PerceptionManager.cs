using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdLamp
{
    /// <summary>
    /// The hidden "perception" variable. Never shown to the player (except in the F1 debug overlay).
    /// Each source id only counts once, so re-reading a document or re-inspecting a symbol is free.
    /// </summary>
    public class PerceptionManager : MonoBehaviour
    {
        public int Value { get; private set; }
        readonly HashSet<string> sources = new HashSet<string>();

        public event Action<int> Changed;

        /// <summary>0: 0–9, 1: 10–19, 2: 20–34, 3: 35–49, 4: 50+</summary>
        public int Stage => Value >= 50 ? 4 : Value >= 35 ? 3 : Value >= 20 ? 2 : Value >= 10 ? 1 : 0;

        public bool Add(int amount, string sourceId)
        {
            if (!string.IsNullOrEmpty(sourceId) && !sources.Add(sourceId)) return false;
            Value += amount;
            Changed?.Invoke(Value);
            if (Game.State != null) Game.State.NotifyChanged();
            return true;
        }

        public bool HasSource(string sourceId) => sources.Contains(sourceId);

        public List<string> AllSources() => new List<string>(sources);

        public void Restore(int value, List<string> savedSources)
        {
            Value = value;
            sources.Clear();
            foreach (var s in savedSources) sources.Add(s);
            Changed?.Invoke(Value);
        }
    }
}
