using System.Collections.Generic;
using UnityEngine;

namespace ThirdLamp
{
    /// <summary>
    /// A scripted story beat: when every condition holds, run the actions.
    /// Events can be authored as assets (Create > Third Lamp > Narrative Event) or in code (SliceContent).
    /// </summary>
    [CreateAssetMenu(menuName = "Third Lamp/Narrative Event", fileName = "NarrativeEvent")]
    public class NarrativeEvent : ScriptableObject
    {
        public string id;
        [Tooltip("Fire at most once per playthrough.")]
        public bool once = true;
        [Tooltip("For repeatable events: minimum seconds between firings.")]
        public float cooldown = 30f;
        [Tooltip("Conditions must hold continuously for this many seconds before the event fires.")]
        public float delay;
        [TextArea] public string designNote;

        [SerializeReference] public List<Condition> conditions = new List<Condition>();
        [SerializeReference] public List<EventAction> actions = new List<EventAction>();

        public static NarrativeEvent Create(string id)
        {
            var e = CreateInstance<NarrativeEvent>();
            e.id = id;
            e.name = id;
            return e;
        }

        public NarrativeEvent When(params Condition[] c) { conditions.AddRange(c); return this; }
        public NarrativeEvent Do(params EventAction[] a) { actions.AddRange(a); return this; }
        public NarrativeEvent After(float seconds) { delay = seconds; return this; }
        public NarrativeEvent Repeatable(float cooldownSeconds) { once = false; cooldown = cooldownSeconds; return this; }
        public NarrativeEvent Note(string text) { designNote = text; return this; }
    }
}
