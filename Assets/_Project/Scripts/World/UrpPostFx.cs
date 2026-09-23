using UnityEngine;
#if TL_URP
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#endif

namespace ThirdLamp
{
    /// <summary>
    /// When URP is the active pipeline: warm tungsten grade, gentle vignette and grain for Reason;
    /// desaturated, darker, grainier for Mind. No-op on the built-in pipeline.
    /// </summary>
    public static class UrpPostFx
    {
#if TL_URP
        static ColorAdjustments color;
        static WhiteBalance white;
        static Vignette vignette;
        static FilmGrain grain;
#endif

        public static void Setup(Camera main, LightingStateManager lighting)
        {
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
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(1.1f);
            bloom.intensity.Override(0.35f);
            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.Neutral);

            Apply(LampMode.Reason);
            lighting.ModeChanged += Apply;
#endif
        }

#if TL_URP
        static void Apply(LampMode mode)
        {
            bool mind = mode == LampMode.Mind;
            color.saturation.Override(mind ? -45f : -8f);
            color.contrast.Override(mind ? 18f : 6f);
            color.postExposure.Override(mind ? -0.2f : 0f);
            white.temperature.Override(mind ? 30f : 14f);
            white.tint.Override(mind ? 6f : 2f);
            vignette.intensity.Override(mind ? 0.5f : 0.28f);
            vignette.smoothness.Override(0.45f);
            grain.type.Override(FilmGrainLookup.Medium3);
            grain.intensity.Override(mind ? 0.55f : 0.22f);
            grain.response.Override(0.8f);
        }
#endif
    }
}
