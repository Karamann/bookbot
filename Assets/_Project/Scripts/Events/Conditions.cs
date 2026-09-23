using System;
using UnityEngine;

namespace ThirdLamp
{
    /// <summary>Base for event conditions. Serialized polymorphically via [SerializeReference].</summary>
    [Serializable]
    public abstract class Condition
    {
        /// <param name="dt">Seconds since the previous evaluation, for conditions that must hold over time.</param>
        public abstract bool Evaluate(float dt);
        public virtual void ResetTimers() { }
    }

    [Serializable]
    public class FlagCondition : Condition
    {
        public string flag;
        public bool expected = true;
        public FlagCondition() { }
        public FlagCondition(string flag, bool expected = true) { this.flag = flag; this.expected = expected; }
        public override bool Evaluate(float dt) => Game.State.Has(flag) == expected;
    }

    [Serializable]
    public class CounterCondition : Condition
    {
        public string counter;
        public int atLeast;
        public CounterCondition() { }
        public CounterCondition(string counter, int atLeast) { this.counter = counter; this.atLeast = atLeast; }
        public override bool Evaluate(float dt) => Game.State.Get(counter) >= atLeast;
    }

    [Serializable]
    public class PerceptionCondition : Condition
    {
        public int atLeast;
        public int below = int.MaxValue;
        public PerceptionCondition() { }
        public PerceptionCondition(int atLeast, int below = int.MaxValue) { this.atLeast = atLeast; this.below = below; }
        public override bool Evaluate(float dt) { int v = Game.Perception.Value; return v >= atLeast && v < below; }
    }

    [Serializable]
    public class ClockCondition : Condition
    {
        public int hour, minute;
        public ClockCondition() { }
        public ClockCondition(int hour, int minute) { this.hour = hour; this.minute = minute; }
        public override bool Evaluate(float dt) => Game.State.ClockAtLeast(hour, minute);
    }

    [Serializable]
    public class TimeSinceFlagCondition : Condition
    {
        public string flag;
        public float seconds;
        public TimeSinceFlagCondition() { }
        public TimeSinceFlagCondition(string flag, float seconds) { this.flag = flag; this.seconds = seconds; }
        public override bool Evaluate(float dt) => Game.State.TimeSince(flag) >= seconds;
    }

    [Serializable]
    public class ZoneCondition : Condition
    {
        public string zone;
        public bool inside = true;
        public ZoneCondition() { }
        public ZoneCondition(string zone, bool inside = true) { this.zone = zone; this.inside = inside; }
        public override bool Evaluate(float dt)
        {
            if (Game.Player == null) return false;
            bool isIn = RoomZone.IdAt(Game.Player.transform.position) == zone;
            return isIn == inside;
        }
    }

    [Serializable]
    public class IndoorsCondition : Condition
    {
        public bool indoors = true;
        public IndoorsCondition() { }
        public IndoorsCondition(bool indoors) { this.indoors = indoors; }
        public override bool Evaluate(float dt)
        {
            if (Game.Player == null) return false;
            var z = RoomZone.At(Game.Player.transform.position);
            return (z != null && z.indoor) == indoors;
        }
    }

    /// <summary>
    /// looking = true: the player has kept the target near the centre of view for holdSeconds.
    /// looking = false: the target is completely out of sight (eyes and camera viewfinder).
    /// </summary>
    [Serializable]
    public class LookingAtCondition : Condition
    {
        public string targetId;
        public bool looking = true;
        public float maxDistance = 30f;
        public float holdSeconds;
        float timer;

        public LookingAtCondition() { }
        public LookingAtCondition(string targetId, bool looking, float maxDistance = 30f, float holdSeconds = 0f)
        { this.targetId = targetId; this.looking = looking; this.maxDistance = maxDistance; this.holdSeconds = holdSeconds; }

        public override bool Evaluate(float dt)
        {
            var go = Game.World.Find(targetId);
            if (go == null) return false;
            if (!looking) return !Perceive.PlayerCanSee(go, maxDistance);
            if (Perceive.PlayerFocusedOn(go, maxDistance)) timer += dt; else timer = 0f;
            return timer >= holdSeconds;
        }

        public override void ResetTimers() => timer = 0f;
    }

    [Serializable]
    public class CameraRaisedCondition : Condition
    {
        public bool raised = true;
        public CameraRaisedCondition() { }
        public CameraRaisedCondition(bool raised) { this.raised = raised; }
        public override bool Evaluate(float dt) => Game.PhotoCamera != null && Game.PhotoCamera.IsRaised == raised;
    }

    [Serializable]
    public class LightModeCondition : Condition
    {
        public LampMode mode;
        public LightModeCondition() { }
        public LightModeCondition(LampMode mode) { this.mode = mode; }
        public override bool Evaluate(float dt) => Game.Lighting.Mode == mode;
    }

    [Serializable]
    public class PowerCondition : Condition
    {
        public bool on = true;
        public PowerCondition() { }
        public PowerCondition(bool on) { this.on = on; }
        public override bool Evaluate(float dt) => Game.Lighting.PowerOn == on;
    }

    [Serializable]
    public class DoorOpenCondition : Condition
    {
        public string doorId;
        public bool open = true;
        public DoorOpenCondition() { }
        public DoorOpenCondition(string doorId, bool open) { this.doorId = doorId; this.open = open; }
        public override bool Evaluate(float dt)
        {
            var d = Game.World.Get<Openable>(doorId);
            return d != null && d.IsOpen == open;
        }
    }

    [Serializable]
    public class PlayModeCondition : Condition
    {
        public override bool Evaluate(float dt) => Game.Mode == InputMode.Play;
    }

    [Serializable]
    public class AnyCondition : Condition
    {
        [SerializeReference] public Condition[] options;
        public AnyCondition() { }
        public AnyCondition(params Condition[] options) { this.options = options; }
        public override bool Evaluate(float dt)
        {
            bool any = false;
            // Evaluate all so timers inside children keep ticking.
            foreach (var c in options) any |= c.Evaluate(dt);
            return any;
        }
        public override void ResetTimers() { foreach (var c in options) c.ResetTimers(); }
    }
}
