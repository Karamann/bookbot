using UnityEngine;
#if TL_URP
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#endif

namespace ThirdLamp
{
    /// <summary>
    /// When URP is the active pipeline: dim tungsten grade, vignette, grain, a touch of lens fringing for
    /// Reason; desaturated, darker, grainier for Mind. <see cref="Pulse"/> gives a short tightening of the
    /// frame on scares; it works on both pipelines (ScreenFx reads <see cref="PulseAmount"/>).
    /// </summary>
    public static class UrpPostFx
    {
        static float pulse;

        /// <summary>0..1, decays over about a second and a half.</summary>
        public static float PulseAmount => pulse;

        /// <summary>Brief vignette squeeze and exposure dip, e.g. when something is seen that shouldn't be.</summary>
        public static void Pulse(float strength = 1f) => pulse = Mathf.Max(pulse, Mathf.Clamp01(strength));

#if TL_URP
        static ColorAdjustments color;
        static WhiteBalance white;
        static Vignette vignette;
        static FilmGrain grain;
        static ChromaticAberration chroma;
        static LensDistortion lens;
        static LampMode mode;
#endif

        public static void Setup(Camera main, LightingStateManager lighting)
        {
            pulse = 0f;
            var runner = new GameObject("PostFxPulse").AddComponent<PulseRunner>();
            runner.hideFlags = HideFlags.HideInHierarchy;
#if TL_URP
            if (GraphicsSettings.currentRenderPipeline == null || main == null) return;
            main.GetUniversalAdditionalCameraData().renderPostProcessing = true;

            var go = new GameObject("PostFx");
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.profile = profile;

            color = profile.Add<ColorAdjustments>(true);
            white = profile.Add<WhiteBalance>(true);
            vignette = profile.Add<Vignette>(true);
            grain = profile.Add<FilmGrain>(true);
            chroma = profile.Add<ChromaticAberration>(true);
            lens = profile.Add<LensDistortion>(true);
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(1.05f);
            bloom.intensity.Override(0.4f);
            bloom.scatter.Override(0.75f);
            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.Neutral);

            Apply(LampMode.Reason);
            lighting.ModeChanged += Apply;
#endif
        }

#if TL_URP
        static void Apply(LampMode next)
        {
            mode = next;
            bool mind = mode == LampMode.Mind;
            color.saturation.Override(mind ? -45f : -14f);
            color.contrast.Override(mind ? 18f : 9f);
            white.temperature.Override(mind ? 30f : 10f);
            white.tint.Override(mind ? 6f : 2f);
            vignette.smoothness.Override(0.45f);
            grain.type.Override(FilmGrainLookup.Medium3);
            grain.intensity.Override(mind ? 0.6f : 0.3f);
            grain.response.Override(0.8f);
            lens.intensity.Override(mind ? -0.22f : -0.08f);
            ApplyPulse();
        }

        static void ApplyPulse()
        {
            bool mind = mode == LampMode.Mind;
            float p = pulse;
            color.postExposure.Override((mind ? -0.25f : -0.12f) - p * 0.45f);
            vignette.intensity.Override((mind ? 0.5f : 0.32f) + p * 0.25f);
            chroma.intensity.Override((mind ? 0.35f : 0.12f) + p * 0.5f);
        }
#endif

        class PulseRunner : MonoBehaviour
        {
            void Update()
            {
                if (pulse <= 0f) return;
                pulse = Mathf.Max(0f, pulse - Time.deltaTime / 1.5f);
#if TL_URP
                if (color != null) ApplyPulse();
#endif
            }
        }
    }
}
