using System.Collections.Generic;
using UnityEngine;

namespace ThirdLamp
{
    /// <summary>
    /// Named sounds, pooled positional one-shots, and the ambience beds (wind, crickets, distant dogs,
    /// the house settling). No music. House creaks become more frequent as perception rises.
    /// </summary>
    public class AudioEventManager : MonoBehaviour
    {
        const int PoolSize = 20;

        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        readonly List<AudioSource> pool = new List<AudioSource>();
        readonly List<float> reservedUntil = new List<float>();
        AudioSource ui, wind, crickets, drone;
        float nextDog, nextCreak;
        bool mind;

        public void Setup()
        {
            foreach (var kv in ProceduralAudio.BuildAll())
            {
                var over = Resources.Load<AudioClip>("ThirdLamp/Audio/" + kv.Key);
                clips[kv.Key] = over != null ? over : kv.Value;
            }
            for (int i = 0; i < PoolSize; i++)
            {
                var go = new GameObject("Sfx" + i);
                go.transform.SetParent(transform, false);
                var s = go.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 1f;
                s.rolloffMode = AudioRolloffMode.Logarithmic;
                s.minDistance = 1.2f;
                s.maxDistance = 40f;
                s.dopplerLevel = 0f;
                pool.Add(s);
                reservedUntil.Add(0f);
            }
            ui = Make2D("UI", null, 1f, false);
            wind = Make2D("Wind", Clip("wind"), 0.3f, true);
            crickets = Make2D("Crickets", Clip("crickets"), 0.2f, true);
            drone = Make2D("Drone", Clip("drone"), 0f, true);
            nextDog = Time.time + 12f;
            nextCreak = Time.time + 90f;
        }

        AudioSource Make2D(string n, AudioClip clip, float vol, bool loop)
        {
            var go = new GameObject(n);
            go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>();
            s.spatialBlend = 0f;
            s.clip = clip;
            s.loop = loop;
            s.volume = vol;
            s.playOnAwake = false;
            if (loop) s.Play();
            return s;
        }

        public AudioClip Clip(string id)
        {
            if (clips.TryGetValue(id, out var c)) return c;
            Debug.LogWarning($"[ThirdLamp] Missing sound '{id}'");
            return null;
        }

        public void PlayAt(string id, Vector3 position, float volume = 1f, float pitch = 1f, float delay = 0f)
        {
            var clip = Clip(id);
            if (clip == null) return;
            int idx = -1;
            for (int i = 0; i < pool.Count; i++)
                if (Time.time >= reservedUntil[i]) { idx = i; break; }
            if (idx < 0) return;
            var s = pool[idx];
            s.transform.position = position;
            s.clip = clip;
            s.volume = volume;
            s.pitch = pitch;
            if (delay > 0f) s.PlayDelayed(delay); else s.Play();
            reservedUntil[idx] = Time.time + delay + clip.length / Mathf.Max(0.1f, pitch) + 0.05f;
        }

        public void Play2D(string id, float volume = 1f, float pitch = 1f)
        {
            var clip = Clip(id);
            if (clip == null) return;
            ui.pitch = pitch;
            ui.PlayOneShot(clip, volume);
        }

        /// <summary>Creates a looping positional source (fridge hum, generator, CRT fan).</summary>
        public AudioSource Loop(string id, Transform parent, Vector3 localPos, float volume, float minDist, float maxDist, bool playNow = true)
        {
            var go = new GameObject("Loop_" + id);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var s = go.AddComponent<AudioSource>();
            s.clip = Clip(id);
            s.loop = true;
            s.volume = volume;
            s.spatialBlend = 1f;
            s.minDistance = minDist;
            s.maxDistance = maxDist;
            s.rolloffMode = AudioRolloffMode.Logarithmic;
            s.dopplerLevel = 0f;
            s.playOnAwake = false;
            if (playNow) s.Play();
            return s;
        }

        public void SetMind(bool value) => mind = value;

        void Update()
        {
            if (Game.Player == null) return;
            var zone = RoomZone.At(Game.Player.transform.position);
            bool outside = zone == null || !zone.indoor;
            float k = Time.deltaTime * 1.5f;
            wind.volume = Mathf.MoveTowards(wind.volume, mind ? 0f : outside ? 0.32f : 0.07f, k * 0.3f);
            crickets.volume = Mathf.MoveTowards(crickets.volume, mind ? 0f : outside ? 0.18f : 0.02f, k * 0.2f);
            drone.volume = Mathf.MoveTowards(drone.volume, mind ? 0.35f : 0f, k * 0.2f);
            AudioListener.volume = Game.Mode == InputMode.Paused ? 0f : 1f;

            if (mind || Game.Mode == InputMode.Ended) return;

            if (Time.time >= nextDog)
            {
                nextDog = Time.time + Random.Range(28f, 75f);
                ui.panStereo = Random.Range(-0.7f, 0.7f);
                Play2D("dog", outside ? 0.12f : 0.04f, Random.Range(0.9f, 1.1f));
            }

            if (Time.time >= nextCreak)
            {
                int stage = Game.Perception != null ? Game.Perception.Stage : 0;
                float[] min = { 90f, 55f, 35f, 22f, 14f };
                float[] max = { 160f, 95f, 60f, 40f, 25f };
                nextCreak = Time.time + Random.Range(min[stage], max[stage]);
                if (!outside && zone != null)
                {
                    var b = zone.bounds;
                    var p = new Vector3(Random.Range(b.min.x, b.max.x), 2.6f, Random.Range(b.min.z, b.max.z));
                    PlayAt("creak", p, 0.12f + 0.04f * stage, Random.Range(0.8f, 1.1f));
                }
            }
        }
    }
}
