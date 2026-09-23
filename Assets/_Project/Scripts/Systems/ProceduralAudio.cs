using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdLamp
{
    /// <summary>
    /// Synthesised placeholder sounds so the slice is never silent for the wrong reasons.
    /// Any clip can be replaced by dropping a file at Resources/ThirdLamp/Audio/&lt;name&gt;.
    /// </summary>
    public static class ProceduralAudio
    {
        const int SR = 22050;
        static System.Random rng = new System.Random(2002);

        static float N() => (float)(rng.NextDouble() * 2.0 - 1.0);
        static float A(float fc) => 1f - Mathf.Exp(-2f * Mathf.PI * fc / SR);

        public static Dictionary<string, AudioClip> BuildAll()
        {
            rng = new System.Random(2002);
            var d = new Dictionary<string, AudioClip>();
            void Add(string name, float[] data) => d[name] = Make(name, data);

            Add("wind", Loop(Wind(9f), 1f));
            Add("crickets", Crickets(6f));
            Add("hum", Hum(2f));
            Add("generator", Engine(2f, 25f, 0.55f));
            Add("engine", Engine(2f, 40f, 0.4f));
            Add("fan", Loop(Filtered(3f, 1500f, 200f, 0.12f), 0.5f));
            Add("lamp_hiss", Loop(Filtered(3f, 9000f, 2500f, 0.05f), 0.5f));
            Add("drone", Drone(4f));
            Add("engine_tick", Ticks(6f));
            Add("ring", Ring());
            Add("vibrate", Vibrate());
            Add("step_wood", Step(0.2f, 900f, 110f, 0.9f));
            Add("step_tile", Step(0.14f, 3200f, 180f, 0.6f));
            Add("step_gravel", Gravel());
            Add("step_stone", Echo(Step(0.18f, 2200f, 90f, 0.8f), 0.35f, 3));
            Add("switch", Click(0.05f, 4000f, 1.0f));
            Add("key", Click(0.04f, 3000f, 0.6f));
            Add("door_open", Creak(0.9f, 180f, 260f, 0.5f));
            Add("creak", Creak(1.0f, 85f, 120f, 0.45f));
            Add("door_close", Thud(0.45f, 70f));
            Add("door_locked", Rattle(0.3f));
            Add("unlock", Rattle(0.35f));
            Add("drawer", Slide(0.6f));
            Add("pickup", Filtered(0.12f, 1200f, 100f, 0.5f, true));
            Add("place", Thud(0.16f, 180f));
            Add("paper", Paper(0.45f));
            Add("coffee", Coffee(7.5f));
            Add("shutter", Shutter());
            Add("beep", Tone(0.09f, 2200f, 0.35f));
            Add("cord_pull", CordPull());
            Add("sputter", Pulses(new[] { 0f, 0.12f, 0.2f, 0.34f, 0.5f }, 0.9f, 0.7f));
            Add("generator_start", Pulses(new[] { 0f, 0.16f, 0.29f, 0.39f, 0.47f, 0.54f, 0.6f, 0.65f, 0.7f, 0.74f, 0.78f, 0.82f, 0.86f, 0.9f, 0.94f, 0.98f, 1.02f, 1.06f, 1.1f }, 1.2f, 0.8f));
            Add("generator_die", Pulses(new[] { 0f, 0.05f, 0.1f, 0.16f, 0.23f, 0.32f, 0.44f, 0.6f, 0.82f, 1.1f }, 1.5f, 0.8f, true));
            Add("knock", Knock());
            Add("footstep_heavy", Echo(Step(0.3f, 500f, 60f, 1f), 0.25f, 2));
            Add("dog", Dog());
            Add("match", Match());
            Add("lamp_out", Filtered(0.3f, 600f, 40f, 0.5f, true));
            Add("hangup", Hangup());
            return d;
        }

        static AudioClip Make(string name, float[] data)
        {
            var clip = AudioClip.Create(name, data.Length, 1, SR, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float[] Buf(float seconds) => new float[Mathf.Max(1, Mathf.RoundToInt(seconds * SR))];

        /// <summary>Crossfades the tail into the head so the clip loops without a click.</summary>
        static float[] Loop(float[] src, float fadeSeconds)
        {
            int f = Mathf.RoundToInt(fadeSeconds * SR);
            var o = new float[src.Length - f];
            Array.Copy(src, o, o.Length);
            for (int i = 0; i < f; i++)
            {
                float k = (float)i / f;
                o[i] = src[i] * k + src[o.Length + i] * (1f - k);
            }
            return o;
        }

        static float[] Echo(float[] src, float decay, int taps)
        {
            int delay = Mathf.RoundToInt(0.07f * SR);
            var o = new float[src.Length + delay * taps];
            for (int t = 0; t <= taps; t++)
            {
                float g = Mathf.Pow(decay, t);
                for (int i = 0; i < src.Length; i++) o[i + t * delay] += src[i] * g;
            }
            return o;
        }

        static float[] Wind(float s)
        {
            var b = Buf(s);
            float lp = 0, lp2 = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = (float)i / SR;
                float gust = 0.55f + 0.3f * Mathf.Sin(t * 0.7f) + 0.15f * Mathf.Sin(t * 1.9f + 1f);
                lp += A(350f + gust * 300f) * (N() - lp);
                lp2 += A(90f) * (lp - lp2);
                b[i] = (lp * 0.8f + lp2 * 1.5f) * gust * 0.5f;
            }
            return b;
        }

        static float[] Crickets(float s)
        {
            var b = Buf(s);
            float[] rates = { 0.9f, 1.3f, 0.75f };
            float[] freqs = { 4400f, 4700f, 4100f };
            float[] amps = { 0.08f, 0.05f, 0.035f };
            for (int c = 0; c < 3; c++)
            {
                for (float start = c * 0.21f; start < s - 0.2f; start += rates[c])
                {
                    for (int p = 0; p < 3; p++)
                    {
                        int s0 = Mathf.RoundToInt((start + p * 0.045f) * SR), len = Mathf.RoundToInt(0.022f * SR);
                        for (int i = 0; i < len && s0 + i < b.Length; i++)
                        {
                            float env = Mathf.Sin(Mathf.PI * i / len);
                            b[s0 + i] += Mathf.Sin(2 * Mathf.PI * freqs[c] * i / SR) * env * amps[c];
                        }
                    }
                }
            }
            return b;
        }

        static float[] Hum(float s)
        {
            var b = Buf(s);
            float lp = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = (float)i / SR;
                lp += A(300f) * (N() - lp);
                b[i] = 0.18f * Mathf.Sin(2 * Mathf.PI * 50 * t) + 0.1f * Mathf.Sin(2 * Mathf.PI * 100 * t) +
                       0.05f * Mathf.Sin(2 * Mathf.PI * 150 * t) + lp * 0.05f;
            }
            return b;
        }

        static float[] Engine(float s, float f, float amp)
        {
            var b = Buf(s);
            float lp = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = (float)i / SR;
                float saw = 2f * (t * f - Mathf.Floor(t * f + 0.5f));
                float chug = 0.6f + 0.4f * Mathf.Sin(2 * Mathf.PI * f * 0.5f * t);
                lp += A(700f) * (N() - lp);
                b[i] = (saw * 0.5f + lp * 0.6f) * chug * amp;
            }
            return b;
        }

        static float[] Filtered(float s, float lowpass, float highpass, float amp, bool envelope = false)
        {
            var b = Buf(s);
            float lp = 0, hp = 0;
            for (int i = 0; i < b.Length; i++)
            {
                lp += A(lowpass) * (N() - lp);
                hp += A(highpass) * (lp - hp);
                float env = envelope ? Mathf.Sin(Mathf.PI * i / b.Length) : 1f;
                b[i] = (lp - hp) * amp * env * 2f;
            }
            return b;
        }

        static float[] Drone(float s)
        {
            var b = Buf(s);
            for (int i = 0; i < b.Length; i++)
            {
                float t = (float)i / SR;
                b[i] = 0.22f * Mathf.Sin(2 * Mathf.PI * 36f * t) + 0.14f * Mathf.Sin(2 * Mathf.PI * 36.25f * t) +
                       0.06f * Mathf.Sin(2 * Mathf.PI * 54f * t);
            }
            return b;
        }

        static float[] Ticks(float s)
        {
            var b = Buf(s);
            float[] at = { 0.4f, 1.9f, 2.6f, 4.1f, 5.3f };
            foreach (var a in at)
            {
                var c = Click(0.02f, 5000f, 0.25f);
                int s0 = Mathf.RoundToInt(a * SR);
                for (int i = 0; i < c.Length && s0 + i < b.Length; i++) b[s0 + i] += c[i];
            }
            return b;
        }

        static float[] Ring()
        {
            var b = Buf(1.3f);
            for (int g = 0; g < 2; g++)
                for (int k = 0; k < 6; k++)
                {
                    float f = k % 2 == 0 ? 1320f : 1050f;
                    int s0 = Mathf.RoundToInt((g * 0.6f + k * 0.08f) * SR), len = Mathf.RoundToInt(0.075f * SR);
                    for (int i = 0; i < len && s0 + i < b.Length; i++)
                        b[s0 + i] = Mathf.Sign(Mathf.Sin(2 * Mathf.PI * f * i / SR)) * 0.25f;
                }
            return b;
        }

        static float[] Vibrate()
        {
            var b = Buf(0.9f);
            for (int i = 0; i < b.Length; i++)
            {
                float t = (float)i / SR;
                bool on = t < 0.35f || (t > 0.5f && t < 0.85f);
                if (!on) continue;
                b[i] = (Mathf.Sin(2 * Mathf.PI * 170f * t) * 0.5f + N() * 0.15f) * 0.5f * (0.8f + 0.2f * Mathf.Sin(2 * Mathf.PI * 30 * t));
            }
            return b;
        }

        static float[] Step(float s, float lowpass, float thump, float amp)
        {
            var b = Buf(s);
            float lp = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = (float)i / SR;
                float env = Mathf.Exp(-t * 28f);
                lp += A(lowpass) * (N() - lp);
                b[i] = (lp * 1.6f + Mathf.Sin(2 * Mathf.PI * thump * t) * 0.6f) * env * amp;
            }
            return b;
        }

        static float[] Gravel()
        {
            var b = Buf(0.28f);
            float lp = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = (float)i / SR;
                float env = Mathf.Exp(-t * 12f) * (rng.NextDouble() < 0.08 ? 1f : 0.35f);
                lp += A(2600f) * (N() - lp);
                b[i] = lp * env * 1.2f;
            }
            return b;
        }

        static float[] Click(float s, float lowpass, float amp)
        {
            var b = Buf(s);
            float lp = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = (float)i / SR;
                lp += A(lowpass) * (N() - lp);
                b[i] = (lp + Mathf.Sin(2 * Mathf.PI * 1800 * t) * 0.3f) * Mathf.Exp(-t * 180f) * amp;
            }
            return b;
        }

        static float[] Creak(float s, float f0, float f1, float amp)
        {
            var b = Buf(s);
            float phase = 0, lp = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float k = (float)i / b.Length;
                float f = Mathf.Lerp(f0, f1, k) * (1f + 0.08f * Mathf.Sin(k * 40f)) * (1f + N() * 0.04f);
                phase += f / SR;
                float saw = 2f * (phase - Mathf.Floor(phase + 0.5f));
                float stick = Mathf.PerlinNoise(k * 30f, 0.5f) > 0.45f ? 1f : 0.2f;
                lp += A(1400f) * (saw - lp);
                b[i] = lp * Mathf.Sin(Mathf.PI * k) * stick * amp;
            }
            return b;
        }

        static float[] Thud(float s, float f)
        {
            var b = Buf(s);
            float lp = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = (float)i / SR;
                lp += A(1200f) * (N() - lp);
                b[i] = (Mathf.Sin(2 * Mathf.PI * f * t) * 0.8f * Mathf.Exp(-t * 14f)) + lp * Mathf.Exp(-t * 60f) * 0.8f;
            }
            return b;
        }

        static float[] Rattle(float s)
        {
            var b = Buf(s);
            float[] at = { 0f, 0.06f, 0.15f, 0.19f };
            foreach (var a in at)
            {
                var c = Click(0.05f, 6000f, 0.7f);
                int s0 = Mathf.RoundToInt(a * SR);
                for (int i = 0; i < c.Length && s0 + i < b.Length; i++) b[s0 + i] += c[i];
            }
            return b;
        }

        static float[] Slide(float s)
        {
            var b = Buf(s);
            float lp = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float k = (float)i / b.Length;
                lp += A(1100f) * (N() - lp);
                b[i] = lp * Mathf.Sin(Mathf.PI * k) * (0.7f + 0.3f * Mathf.PerlinNoise(k * 60f, 0.2f)) * 1.1f;
            }
            return b;
        }

        static float[] Paper(float s)
        {
            var b = Buf(s);
            float lp = 0, hp = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float k = (float)i / b.Length;
                lp += A(7000f) * (N() - lp);
                hp += A(1500f) * (lp - hp);
                float crinkle = Mathf.PerlinNoise(k * 90f, 0.1f) > 0.55f ? 1f : 0.25f;
                b[i] = (lp - hp) * Mathf.Sin(Mathf.PI * k) * crinkle * 0.9f;
            }
            return b;
        }

        static float[] Coffee(float s)
        {
            var b = Buf(s);
            float lp = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = (float)i / SR;
                lp += A(500f) * (N() - lp);
                float burst = Mathf.PerlinNoise(t * 7f, 0.3f);
                float env = Mathf.Clamp01(t / 0.8f) * Mathf.Clamp01((s - t) / 1f);
                b[i] = lp * (burst > 0.55f ? 1.4f : 0.3f) * env;
            }
            for (int k = 0; k < 40; k++)
            {
                int s0 = rng.Next(SR, b.Length - SR / 10);
                float f = 300f + (float)rng.NextDouble() * 400f;
                int len = SR / 25;
                for (int i = 0; i < len; i++)
                    b[s0 + i] += Mathf.Sin(2 * Mathf.PI * f * (1f + i / (float)len) * i / SR) * Mathf.Sin(Mathf.PI * i / len) * 0.15f;
            }
            return b;
        }

        static float[] Shutter()
        {
            var b = Buf(0.18f);
            var c1 = Click(0.04f, 7000f, 0.9f);
            var c2 = Click(0.04f, 5000f, 0.7f);
            int s2 = Mathf.RoundToInt(0.09f * SR);
            for (int i = 0; i < c1.Length; i++) b[i] += c1[i];
            for (int i = 0; i < c2.Length && s2 + i < b.Length; i++) b[s2 + i] += c2[i];
            return b;
        }

        static float[] Tone(float s, float f, float amp)
        {
            var b = Buf(s);
            for (int i = 0; i < b.Length; i++)
            {
                float k = (float)i / b.Length;
                b[i] = Mathf.Sign(Mathf.Sin(2 * Mathf.PI * f * i / SR)) * amp * 0.5f * Mathf.Min(1f, (1f - k) * 8f);
            }
            return b;
        }

        static float[] CordPull()
        {
            var b = Buf(0.5f);
            float lp = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float k = (float)i / b.Length;
                lp += A(Mathf.Lerp(400f, 2500f, k)) * (N() - lp);
                b[i] = lp * Mathf.Sin(Mathf.PI * k) * 1.2f;
            }
            return b;
        }

        static float[] Pulses(float[] times, float s, float amp, bool fade = false)
        {
            var b = Buf(s + 0.2f);
            for (int p = 0; p < times.Length; p++)
            {
                float g = fade ? 1f - (float)p / times.Length : 1f;
                var t = Thud(0.12f, 55f);
                int s0 = Mathf.RoundToInt(times[p] * SR);
                for (int i = 0; i < t.Length && s0 + i < b.Length; i++) b[s0 + i] += t[i] * amp * g;
            }
            return b;
        }

        static float[] Knock()
        {
            var b = Buf(0.5f);
            for (int i = 0; i < b.Length; i++)
            {
                float t = (float)i / SR;
                b[i] = (Mathf.Sin(2 * Mathf.PI * 90 * t) * 0.6f + Mathf.Sin(2 * Mathf.PI * 240 * t) * 0.3f) * Mathf.Exp(-t * 18f) +
                       Mathf.Sin(2 * Mathf.PI * 930 * t) * 0.12f * Mathf.Exp(-t * 7f);
            }
            return b;
        }

        static float[] Dog()
        {
            var b = Buf(0.95f);
            foreach (var start in new[] { 0f, 0.38f })
            {
                int s0 = Mathf.RoundToInt(start * SR), len = Mathf.RoundToInt(0.28f * SR);
                float lp = 0, hp = 0, phase = 0;
                for (int i = 0; i < len; i++)
                {
                    float k = (float)i / len;
                    float env = Mathf.Min(1f, k * 30f) * Mathf.Exp(-k * 5f);
                    phase += Mathf.Lerp(330f, 210f, k) / SR;
                    lp += A(1400f) * (N() - lp);
                    hp += A(350f) * (lp - hp);
                    b[s0 + i] += (Mathf.Sin(2 * Mathf.PI * phase) * 0.5f + (lp - hp) * 1.2f) * env * 0.6f;
                }
            }
            return b;
        }

        static float[] Match()
        {
            var b = Buf(0.9f);
            float lp = 0, hp = 0;
            for (int i = 0; i < b.Length; i++)
            {
                float t = (float)i / SR;
                lp += A(t < 0.12f ? 8000f : 900f) * (N() - lp);
                hp += A(t < 0.12f ? 2000f : 60f) * (lp - hp);
                float env = t < 0.12f ? 1f : Mathf.Exp(-(t - 0.12f) * 5f) * 0.5f;
                b[i] = (lp - hp) * env * 1.3f;
            }
            return b;
        }

        static float[] Hangup()
        {
            var b = Buf(0.9f);
            for (int k = 0; k < 3; k++)
            {
                int s0 = Mathf.RoundToInt(k * 0.3f * SR), len = Mathf.RoundToInt(0.18f * SR);
                for (int i = 0; i < len && s0 + i < b.Length; i++) b[s0 + i] = Mathf.Sin(2 * Mathf.PI * 425f * i / SR) * 0.35f;
            }
            return b;
        }
    }
}
