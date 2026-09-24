using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ThirdLamp
{
    /// <summary>Creates materials for whichever pipeline is active (URP Lit/Unlit or built-in Standard).</summary>
    public static class Mats
    {
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

        public static bool Urp => GraphicsSettings.currentRenderPipeline != null;

        public static Shader LitShader
        {
            get
            {
                var s = Urp ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard");
                return s != null ? s : Shader.Find("Diffuse");
            }
        }

        public static Shader UnlitShader
        {
            get
            {
                var s = Urp ? Shader.Find("Universal Render Pipeline/Unlit") : Shader.Find("Unlit/Texture");
                return s != null ? s : Shader.Find("Unlit/Texture");
            }
        }

        public static Material Lit(string texture, Color tint, float smoothness = 0.15f, float tileX = 1f, float tileY = 1f)
        {
            string key = $"lit|{texture}|{tint}|{smoothness}|{tileX:0.00}|{tileY:0.00}";
            if (cache.TryGetValue(key, out var m)) return m;
            m = new Material(LitShader) { name = texture ?? "color" };
            if (texture != null)
            {
                m.mainTexture = Tex.Get(texture);
                m.mainTextureScale = new Vector2(tileX, tileY);
            }
            m.color = tint;
            m.SetFloat("_Smoothness", smoothness);
            m.SetFloat("_Glossiness", smoothness);
            m.SetFloat("_Metallic", 0f);
            cache[key] = m;
            return m;
        }

        public static Material Color(Color c, float smoothness = 0.15f) => Lit(null, c, smoothness);

        public static Material Unlit(string texture) => UnlitTex(Tex.Get(texture), "unlit|" + texture);

        public static Material UnlitColor(Color c)
        {
            string key = "unlitc|" + c;
            if (cache.TryGetValue(key, out var m)) return m;
            var t = new Texture2D(2, 2) { name = "flat" };
            t.SetPixels(new[] { c, c, c, c });
            t.Apply();
            m = UnlitTex(t, key);
            return m;
        }

        public static Material UnlitTex(Texture t, string key = null)
        {
            if (key != null && cache.TryGetValue(key, out var m)) return m;
            m = new Material(UnlitShader) { name = key ?? "unlit", mainTexture = t };
            if (key != null) cache[key] = m;
            return m;
        }

        /// <summary>Alpha-tested lit material: foliage, ghosts, cobwebs, curtains. Two-sided under URP.</summary>
        public static Material Cutout(string texture, Color tint, float cutoff = 0.5f, float smoothness = 0.05f)
        {
            string key = $"cut|{texture}|{tint}|{cutoff}|{smoothness}";
            if (cache.TryGetValue(key, out var m)) return m;
            if (Urp)
            {
                m = new Material(LitShader);
                m.SetFloat("_AlphaClip", 1f);
                m.SetFloat("_Cull", 0f);
                m.EnableKeyword("_ALPHATEST_ON");
                m.SetOverrideTag("RenderType", "TransparentCutout");
            }
            else
            {
                var s = Shader.Find("Legacy Shaders/Transparent/Cutout/Diffuse");
                m = new Material(s != null ? s : LitShader);
            }
            m.name = "cut_" + texture;
            m.mainTexture = Tex.Get(texture);
            m.color = tint;
            m.SetFloat("_Cutoff", cutoff);
            m.SetFloat("_Smoothness", smoothness);
            m.SetFloat("_Metallic", 0f);
            m.renderQueue = (int)RenderQueue.AlphaTest;
            cache[key] = m;
            return m;
        }

        /// <summary>Unlit, tinted texture for far backdrops (sky, hills) that should not depend on scene lights.</summary>
        public static Material UnlitTinted(string texture, Color tint, bool cutout = false, float tileX = 1f)
        {
            string key = $"unlitt|{texture}|{tint}|{cutout}|{tileX}";
            if (cache.TryGetValue(key, out var m)) return m;
            if (cutout && !Urp)
            {
                var s = Shader.Find("Unlit/Transparent Cutout");
                m = new Material(s != null ? s : UnlitShader);
            }
            else m = new Material(UnlitShader);
            if (cutout && Urp)
            {
                m.SetFloat("_AlphaClip", 1f);
                m.EnableKeyword("_ALPHATEST_ON");
                m.SetOverrideTag("RenderType", "TransparentCutout");
            }
            if (cutout)
            {
                m.SetFloat("_Cutoff", 0.4f);
                m.renderQueue = (int)RenderQueue.AlphaTest;
            }
            m.name = "unlitt_" + texture;
            m.mainTexture = Tex.Get(texture);
            m.mainTextureScale = new Vector2(tileX, 1f);
            m.color = tint;
            cache[key] = m;
            return m;
        }

        /// <summary>Alpha-blended lit material with no depth write: stains, damp, dirt and glass.</summary>
        public static Material Transparent(string texture, Color tint, float smoothness = 0.05f, int queueOffset = 0)
        {
            string key = $"tr|{texture}|{tint}|{smoothness}|{queueOffset}";
            if (cache.TryGetValue(key, out var m)) return m;
            if (Urp)
            {
                m = new Material(LitShader);
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", 0f);
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0f);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.SetOverrideTag("RenderType", "Transparent");
                // stains, grime and glass must not throw shadows
                m.SetShaderPassEnabled("ShadowCaster", false);
            }
            else
            {
                var s = Shader.Find("Legacy Shaders/Transparent/Diffuse");
                m = new Material(s != null ? s : LitShader);
            }
            m.name = "tr_" + (texture ?? "color");
            if (texture != null) m.mainTexture = Tex.Get(texture);
            m.color = tint;
            m.SetFloat("_Smoothness", smoothness);
            m.SetFloat("_Metallic", 0f);
            m.renderQueue = (int)RenderQueue.Transparent + queueOffset;
            cache[key] = m;
            return m;
        }

        /// <summary>Surface decal (stain, damp, footprint). Drawn on a quad a few millimetres off the surface.</summary>
        public static Material Decal(string texture, Color tint) => Transparent(texture, tint, 0.05f, -50);

        /// <summary>Tinted see-through glass.</summary>
        public static Material Glass(Color tint) => Transparent(null, tint, 0.92f);

        public static void ClearCache() => cache.Clear();
    }
}
