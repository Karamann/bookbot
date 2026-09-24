using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdLamp
{
    /// <summary>Base for event actions.</summary>
    [Serializable]
    public abstract class EventAction
    {
        /// <summary>
        /// True for actions that change persistent world state (visibility, materials, transforms).
        /// These are re-applied silently when a checkpoint is loaded. Sounds, messages and calls are not.
        /// </summary>
        public virtual bool RestoresState => false;
        public abstract void Execute(bool restoring);
    }

    /// <summary>Components that accept named numeric parameters from events (e.g. mirror lag).</summary>
    public interface IParamReceiver
    {
        void SetParam(string key, float value);
    }

    [Serializable]
    public class SetFlagAction : EventAction
    {
        public string flag;
        public SetFlagAction() { }
        public SetFlagAction(string flag) { this.flag = flag; }
        public override void Execute(bool restoring) => Game.State.Set(flag);
    }

    [Serializable]
    public class AddPerceptionAction : EventAction
    {
        public int amount;
        public string source;
        public AddPerceptionAction() { }
        public AddPerceptionAction(int amount, string source) { this.amount = amount; this.source = source; }
        public override void Execute(bool restoring) => Game.Perception.Add(amount, source);
    }

    [Serializable]
    public class SetActiveAction : EventAction
    {
        public string targetId;
        public bool active = true;
        public bool restoreOnLoad = true;
        public SetActiveAction() { }
        public SetActiveAction(string targetId, bool active, bool restoreOnLoad = true)
        { this.targetId = targetId; this.active = active; this.restoreOnLoad = restoreOnLoad; }
        public override bool RestoresState => restoreOnLoad;
        public override void Execute(bool restoring)
        {
            var go = Game.World.Find(targetId);
            if (go != null) go.SetActive(active);
        }
    }

    [Serializable]
    public class SetMaterialStateAction : EventAction
    {
        public string targetId;
        public string state;
        public SetMaterialStateAction() { }
        public SetMaterialStateAction(string targetId, string state) { this.targetId = targetId; this.state = state; }
        public override bool RestoresState => true;
        public override void Execute(bool restoring)
        {
            var ms = Game.World.Get<MaterialStates>(targetId);
            if (ms != null) ms.SetState(state);
        }
    }

    /// <summary>Moves an object by a small local offset. Used for "did that chair move?" moments.</summary>
    [Serializable]
    public class NudgeTransformAction : EventAction
    {
        public string targetId;
        public Vector3 positionOffset;
        public Vector3 eulerOffset;
        public NudgeTransformAction() { }
        public NudgeTransformAction(string targetId, Vector3 positionOffset, Vector3 eulerOffset)
        { this.targetId = targetId; this.positionOffset = positionOffset; this.eulerOffset = eulerOffset; }
        public override bool RestoresState => true;
        public override void Execute(bool restoring)
        {
            var go = Game.World.Find(targetId);
            if (go == null) return;
            go.transform.localPosition += positionOffset;
            go.transform.localRotation *= Quaternion.Euler(eulerOffset);
        }
    }

    [Serializable]
    public class SetParamAction : EventAction
    {
        public string targetId;
        public string key;
        public float value;
        public SetParamAction() { }
        public SetParamAction(string targetId, string key, float value) { this.targetId = targetId; this.key = key; this.value = value; }
        public override bool RestoresState => true;
        public override void Execute(bool restoring)
        {
            var go = Game.World.Find(targetId);
            if (go == null) return;
            foreach (var r in go.GetComponentsInChildren<IParamReceiver>(true)) r.SetParam(key, value);
        }
    }

    [Serializable]
    public class SetDoorAction : EventAction
    {
        public string doorId;
        public bool open;
        public bool silent = true;
        public SetDoorAction() { }
        public SetDoorAction(string doorId, bool open, bool silent = true) { this.doorId = doorId; this.open = open; this.silent = silent; }
        public override bool RestoresState => true;
        public override void Execute(bool restoring)
        {
            var d = Game.World.Get<Openable>(doorId);
            if (d != null) d.SetOpen(open, restoring, silent || restoring);
        }
    }

    [Serializable]
    public class PlaySoundAction : EventAction
    {
        public string clip;
        public string atTargetId;
        public Vector3 position;
        public float volume = 1f;
        public float delay;
        public PlaySoundAction() { }
        public PlaySoundAction(string clip, Vector3 position, float volume = 1f, float delay = 0f)
        { this.clip = clip; this.position = position; this.volume = volume; this.delay = delay; }
        public static PlaySoundAction At(string clip, string targetId, float volume = 1f, float delay = 0f)
            => new PlaySoundAction { clip = clip, atTargetId = targetId, volume = volume, delay = delay };

        public override void Execute(bool restoring)
        {
            if (restoring) return;
            Vector3 p = position;
            if (!string.IsNullOrEmpty(atTargetId))
            {
                var go = Game.World.Find(atTargetId);
                if (go != null) p = go.transform.position;
            }
            Game.Audio.PlayAt(clip, p, volume, 1f, delay);
        }
    }

    /// <summary>Brief tightening of the frame (vignette, exposure, fringing). Not replayed on load.</summary>
    [Serializable]
    public class PulseAction : EventAction
    {
        public float strength = 1f;
        public PulseAction() { }
        public PulseAction(float strength) { this.strength = strength; }
        public override void Execute(bool restoring)
        {
            if (!restoring) UrpPostFx.Pulse(strength);
        }
    }

    [Serializable]
    public class SendSmsAction : EventAction
    {
        public string from;
        [TextArea] public string text;
        public string flagOnSend;
        public SendSmsAction() { }
        public SendSmsAction(string from, string text, string flagOnSend = null) { this.from = from; this.text = text; this.flagOnSend = flagOnSend; }
        public override void Execute(bool restoring)
        {
            if (restoring) return;
            Game.Phone.ReceiveSms(from, text);
            if (!string.IsNullOrEmpty(flagOnSend)) Game.State.Set(flagOnSend);
        }
    }

    [Serializable]
    public class StartCallAction : EventAction
    {
        public CallScript call;
        public StartCallAction() { }
        public StartCallAction(CallScript call) { this.call = call; }
        public override void Execute(bool restoring)
        {
            if (!restoring) Game.Phone.StartCall(call);
        }
    }

    [Serializable]
    public class PowerAction : EventAction
    {
        public bool on;
        public PowerAction() { }
        public PowerAction(bool on) { this.on = on; }
        public override bool RestoresState => false; // power state is saved directly
        public override void Execute(bool restoring) => Game.Lighting.SetPower(on);
    }

    [Serializable]
    public class SubtitleAction : EventAction
    {
        public string text;
        public float seconds = 4f;
        public SubtitleAction() { }
        public SubtitleAction(string text, float seconds = 4f) { this.text = text; this.seconds = seconds; }
        public override void Execute(bool restoring)
        {
            if (!restoring) Game.Hud.Subtitle(text, seconds);
        }
    }

    [Serializable]
    public class CheckpointAction : EventAction
    {
        public string label;
        public CheckpointAction() { }
        public CheckpointAction(string label) { this.label = label; }
        public override void Execute(bool restoring)
        {
            if (!restoring) Game.Save.Checkpoint(label);
        }
    }

    [Serializable]
    public class EndSliceAction : EventAction
    {
        public override void Execute(bool restoring)
        {
            if (!restoring) Game.Fx.EndSlice();
        }
    }

    /// <summary>A timed list of positional sounds, e.g. footsteps crossing the roof.</summary>
    [Serializable]
    public class SoundSequenceAction : EventAction
    {
        [Serializable]
        public struct Step
        {
            public string clip;
            public Vector3 position;
            public float time;
            public float volume;
        }

        public List<Step> steps = new List<Step>();

        public SoundSequenceAction Add(string clip, Vector3 position, float time, float volume)
        {
            steps.Add(new Step { clip = clip, position = position, time = time, volume = volume });
            return this;
        }

        public override void Execute(bool restoring)
        {
            if (restoring) return;
            foreach (var s in steps) Game.Audio.PlayAt(s.clip, s.position, s.volume, UnityEngine.Random.Range(0.93f, 1.05f), s.time);
        }
    }
}
