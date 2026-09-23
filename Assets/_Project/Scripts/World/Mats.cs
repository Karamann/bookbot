using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ThirdLamp
{
    /// <summary>Creates materials for whichever pipeline is active (URP Lit/Unlit or built-in Standard).</summary>
    public static class Mats
    {
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();

        static bool Urp => GraphicsSettings.currentRenderPipeline != null;

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

        public static void ClearCache() => cache.Clear();
    }
}
