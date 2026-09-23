using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdLamp
{
    /// <summary>First Lamp (Reason), Second Lamp (Memory: lenses, mirrors), Third Lamp (Mind: the oil lamp in darkness).</summary>
    public enum LampMode { Reason, Memory, Mind }

    public class LightGroup
    {
        public string id;
        public bool switchedOn;
        public readonly List<Light> lights = new List<Light>();
        public readonly List<float> intensities = new List<float>();
        public readonly List<Renderer> bulbs = new List<Renderer>();
    }

    /// <summary>Objects that exist only under the Third Lamp (or only outside it).</summary>
    public class LampVisibility : MonoBehaviour
    {
        public bool mindOnly = true;
        public int minPerception;

        public void Apply(bool mind)
        {
            bool show = mindOnly ? mind && Game.Perception.Value >= minPerception : !mind;
            if (gameObject.activeSelf != show) gameObject.SetActive(show);
        }
    }

    /// <summary>
    /// Owns electric light groups, generator power, and the current perception mode.
    /// The Third Lamp only "works" in darkness: lamp lit, torch off, and the player's room unlit.
    /// </summary>
    public class LightingStateManager : MonoBehaviour
    {
        public const string MindCorridorZone = "mind_corridor";

        public bool PowerOn { get; private set; } = true;
        public LampMode Mode { get; private set; } = LampMode.Reason;
        public bool MindActive => Mode == LampMode.Mind;

        public Material bulbOnMaterial, bulbOffMaterial;
        public Color reasonAmbient = new Color(0.035f, 0.04f, 0.06f);
        public Color mindAmbient = new Color(0.008f, 0.006f, 0.004f);
        public Color reasonFog = new Color(0.02f, 0.03f, 0.05f);
        public Color mindFog = new Color(0.02f, 0.012f, 0.006f);
        public float reasonFogDensity = 0.022f, mindFogDensity = 0.06f;
        public Bounds[] corridorBounds = new Bounds[0];
        public Vector3 corridorExitPosition;
        public float corridorExitYaw = -90f;

        public event Action<bool> PowerChanged;
        public event Action<LampMode> ModeChanged;

        readonly Dictionary<string, LightGroup> groups = new Dictionary<string, LightGroup>();
        readonly List<LampVisibility> lampObjects = new List<LampVisibility>();
        readonly List<AudioSource> poweredAudio = new List<AudioSource>();
        readonly List<Light> alwaysPowered = new List<Light>();
        OilLamp lamp;
        Torch torch;
        Coroutine flicker;

        public void RegisterLamp(OilLamp l) => lamp = l;
        public void RegisterTorch(Torch t) => torch = t;
        public void RegisterLampObject(LampVisibility v) { lampObjects.Add(v); v.Apply(false); }
        public void RegisterPoweredAudio(AudioSource s) => poweredAudio.Add(s);
        public void RegisterPoweredLight(Light l) => alwaysPowered.Add(l);

        public LightGroup Group(string id)
        {
            if (!groups.TryGetValue(id, out var g))
            {
                g = new LightGroup { id = id };
                groups[id] = g;
            }
            return g;
        }

        public void AddLight(string groupId, Light light, Renderer bulb = null)
        {
            var g = Group(groupId);
            g.lights.Add(light);
            g.intensities.Add(light.intensity);
            if (bulb != null) g.bulbs.Add(bulb);
            ApplyGroup(g);
        }

        public bool GroupSwitchedOn(string id) => groups.TryGetValue(id, out var g) && g.switchedOn;
        public bool IsGroupLit(string id) => PowerOn && GroupSwitchedOn(id);

        public void SetGroup(string id, bool on)
        {
            var g = Group(id);
            g.switchedOn = on;
            ApplyGroup(g);
            Game.State?.NotifyChanged();
        }

        public void ToggleGroup(string id) => SetGroup(id, !GroupSwitchedOn(id));

        void ApplyGroup(LightGroup g, float scale = 1f)
        {
            bool lit = PowerOn && g.switchedOn;
            for (int i = 0; i < g.lights.Count; i++)
            {
                g.lights[i].enabled = lit;
                g.lights[i].intensity = g.intensities[i] * scale;
            }
            foreach (var b in g.bulbs)
                if (b != null && bulbOnMaterial != null) b.sharedMaterial = lit ? bulbOnMaterial : bulbOffMaterial;
        }

        public void SetPower(bool on)
        {
            if (PowerOn == on) return;
            PowerOn = on;
            if (on) Game.State.Clear("power_off"); else Game.State.Set("power_off");
            foreach (var g in groups.Values) ApplyGroup(g);
            foreach (var l in alwaysPowered) if (l != null) l.enabled = on;
            foreach (var s in poweredAudio)
            {
                if (s == null) continue;
                if (on) s.Play(); else s.Stop();
            }
            if (on)
            {
                if (flicker != null) StopCoroutine(flicker);
                flicker = StartCoroutine(RestoreFlicker());
            }
            PowerChanged?.Invoke(on);
        }

        IEnumerator RestoreFlicker()
        {
            float[] pattern = { 0.3f, 0f, 0.8f, 0.1f, 1f };
            foreach (var p in pattern)
            {
                foreach (var g in groups.Values) ApplyGroup(g, p);
                yield return new WaitForSeconds(0.07f);
            }
            foreach (var g in groups.Values) ApplyGroup(g);
            flicker = null;
        }

        public bool IsDarkAt(Vector3 position)
        {
            var z = RoomZone.At(position);
            if (z == null || !z.indoor) return true;
            if (string.IsNullOrEmpty(z.lightGroup)) return true;
            return !IsGroupLit(z.lightGroup);
        }

        void Update()
        {
            if (Game.Player == null) return;
            Vector3 p = Game.Player.transform.position;
            bool mind = lamp != null && lamp.Lit && lamp.IsNear(p) && (torch == null || !torch.On) && IsDarkAt(p);
            bool memory = Game.PhotoCamera != null && Game.PhotoCamera.IsRaised;
            var next = mind ? LampMode.Mind : memory ? LampMode.Memory : LampMode.Reason;
            if (next == Mode) return;

            bool wasMind = Mode == LampMode.Mind;
            Mode = next;
            if (mind != wasMind) ApplyMind(mind);
            ModeChanged?.Invoke(Mode);
        }

        void ApplyMind(bool mind)
        {
            bool inCorridor = false;
            if (Game.Player != null)
                foreach (var b in corridorBounds) inCorridor |= b.Contains(Game.Player.transform.position);
            foreach (var v in lampObjects) if (v != null) v.Apply(mind);
            RenderSettings.ambientLight = mind ? mindAmbient : reasonAmbient;
            RenderSettings.fogColor = mind ? mindFog : reasonFog;
            RenderSettings.fogDensity = mind ? mindFogDensity : reasonFogDensity;
            Game.Audio.SetMind(mind);

            if (mind)
            {
                Game.State.Set("mind_seen");
                Game.Perception.Add(5, "lamp_use_" + lamp.IgnitionCount);
            }
            else
            {
                Game.State.Set("mind_left");
                if (inCorridor) Game.Player.Teleport(corridorExitPosition, corridorExitYaw);
            }
        }

        // ---- save support ----
        public List<string> SwitchedOnGroups()
        {
            var list = new List<string>();
            foreach (var g in groups.Values) if (g.switchedOn) list.Add(g.id);
            return list;
        }

        public void RestoreGroups(List<string> on)
        {
            foreach (var g in groups.Values)
            {
                g.switchedOn = on.Contains(g.id);
                ApplyGroup(g);
            }
        }
    }
}
