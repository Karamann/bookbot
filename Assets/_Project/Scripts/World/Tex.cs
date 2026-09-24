using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdLamp
{
    /// <summary>
    /// Texture library. Each texture is a named slot: if Resources/ThirdLamp/Textures/&lt;name&gt; exists
    /// (e.g. PixelLab output, see Art/asset_manifest.json) it is used; otherwise a procedural placeholder.
    /// </summary>
    public static class Tex
    {
        static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();

        public static Texture2D Get(string name)
        {
            if (cache.TryGetValue(name, out var t) && t != null) return t;
            t = Resources.Load<Texture2D>("ThirdLamp/Textures/" + name);
            if (t == null)
            {
                if (!Generators.TryGetValue(name, out var gen))
                {
                    Debug.LogWarning($"[ThirdLamp] No texture or generator for '{name}'");
                    gen = () => new Pix(4, 4, Color.magenta).ToTexture(name);
                }
                t = gen();
            }
            cache[name] = t;
            return t;
        }

        public static void ClearCache() => cache.Clear();

        /// <summary>True if the slot has a generated texture in Resources or a procedural generator.</summary>
        public static bool Exists(string name)
        {
            if (cache.TryGetValue(name, out var t) && t != null) return true;
            if (Generators.ContainsKey(name)) return true;
            t = Resources.Load<Texture2D>("ThirdLamp/Textures/" + name);
            if (t == null) return false;
            cache[name] = t;
            return true;
        }

        static readonly Dictionary<string, Func<Texture2D>> Generators = new Dictionary<string, Func<Texture2D>>
        {
            { "plaster", () => Plaster(new Color(0.84f, 0.82f, 0.76f), 11) },
            { "plaster_ext", () => Plaster(new Color(0.88f, 0.88f, 0.85f), 12, true) },
            { "ceiling", () => Plaster(new Color(0.9f, 0.89f, 0.85f), 13) },
            { "wallpaper", Wallpaper },
            { "wood_floor", () => Planks(new Color(0.5f, 0.34f, 0.2f), 21) },
            { "wood_dark", () => Planks(new Color(0.26f, 0.16f, 0.1f), 22, 4) },
            { "wood_door", () => Planks(new Color(0.42f, 0.27f, 0.15f), 23, 3) },
            { "tile_kitchen", () => Tiles(new Color(0.86f, 0.83f, 0.74f), new Color(0.55f, 0.52f, 0.47f)) },
            { "tile_bath", () => Tiles(new Color(0.7f, 0.8f, 0.84f), new Color(0.5f, 0.55f, 0.58f)) },
            { "stone", Stone },
            { "dirt", Dirt },
            { "gravel", Gravel },
            { "asphalt", () => { var p = new Pix(64, 64, new Color(0.13f, 0.13f, 0.14f)); p.Noise(0.05f, 3f, 5); return p.ToTexture("asphalt"); } },
            { "rug", Rug },
            { "fabric", () => { var p = new Pix(64, 64, new Color(0.42f, 0.33f, 0.24f)); p.Noise(0.04f, 2f, 6); return p.ToTexture("fabric"); } },
            { "books", Books },
            { "painting_eight", () => Painting(false) },
            { "painting_nine", () => Painting(true) },
            { "icon_027", Icon },
            { "disc_014", Disc },
            { "symbol_chalk", () => SymbolTex(256, false) },
            { "symbol_large", () => SymbolTex(512, true) },
            { "note_paper", NotePaper },
            { "newspaper", Newspaper },
            { "family_photo", FamilyPhoto },
            { "crt_on", CrtOn },
            { "crt_off", () => { var p = new Pix(32, 24, new Color(0.08f, 0.1f, 0.09f)); p.Noise(0.02f, 4f, 3); return p.ToTexture("crt_off"); } },
            { "tv_off", () => { var p = new Pix(32, 24, new Color(0.05f, 0.06f, 0.06f)); p.Noise(0.02f, 4f, 4); return p.ToTexture("tv_off"); } },
            { "bronze", () => { var p = new Pix(64, 64, new Color(0.45f, 0.36f, 0.2f)); p.Noise(0.08f, 6f, 7, false); return p.ToTexture("bronze"); } },
            { "night_sky_glow", () => { var p = new Pix(8, 64, Color.black); p.VGradient(new Color(0.22f, 0.12f, 0.05f), new Color(0.01f, 0.015f, 0.03f)); return p.ToTexture("night_sky_glow", false, true); } },
        };

        // ---------- generators ----------

        static Texture2D Plaster(Color baseC, int seed, bool weathered = false)
        {
            var p = new Pix(128, 128, baseC);
            p.Noise(0.035f, 24f, seed);
            p.Noise(0.02f, 3f, seed + 1);
            if (weathered)
            {
                var r = new System.Random(seed);
                for (int i = 0; i < 6; i++)
                {
                    float x = r.Next(0, 128);
                    for (int y = 0; y < 50; y++) p.Set((int)x + r.Next(-1, 2), y, new Color(0.5f, 0.48f, 0.42f), 0.08f);
                }
            }
            return p.ToTexture("plaster");
        }

        static Texture2D Wallpaper()
        {
            var p = new Pix(128, 128, new Color(0.56f, 0.55f, 0.42f));
            for (int x = 0; x < 128; x++)
                if (x % 32 < 3) for (int y = 0; y < 128; y++) p.Set(x, y, new Color(0.62f, 0.6f, 0.47f));
            for (int y = 8; y < 128; y += 32)
                for (int x = 16; x < 128; x += 32)
                {
                    p.Line(x, y - 6, x + 6, y, 1, new Color(0.47f, 0.42f, 0.32f));
                    p.Line(x + 6, y, x, y + 6, 1, new Color(0.47f, 0.42f, 0.32f));
                    p.Line(x, y + 6, x - 6, y, 1, new Color(0.47f, 0.42f, 0.32f));
                    p.Line(x - 6, y, x, y - 6, 1, new Color(0.47f, 0.42f, 0.32f));
                }
            p.Noise(0.04f, 30f, 31);
            return p.ToTexture("wallpaper");
        }

        static Texture2D Planks(Color baseC, int seed, int planks = 6)
        {
            var p = new Pix(256, 256, baseC);
            var r = new System.Random(seed);
            int ph = 256 / planks;
            for (int k = 0; k < planks; k++)
            {
                float tint = (float)(r.NextDouble() - 0.5) * 0.08f;
                float off = r.Next(0, 1000);
                for (int y = k * ph; y < (k + 1) * ph; y++)
                    for (int x = 0; x < 256; x++)
                    {
                        float grain = Mathf.PerlinNoise(off + x / 60f, y / 2.5f) - 0.5f;
                        var col = baseC + new Color(tint + grain * 0.12f, tint * 0.8f + grain * 0.08f, tint * 0.6f + grain * 0.05f);
                        if (y == k * ph) col *= 0.55f;
                        p.Set(x, y, col);
                    }
                int seam = r.Next(20, 236);
                for (int y = k * ph; y < (k + 1) * ph; y++) p.Set(seam, y, baseC * 0.6f);
            }
            p.Noise(0.02f, 2f, seed + 3);
            return p.ToTexture("planks");
        }

        static Texture2D Tiles(Color tile, Color grout)
        {
            var p = new Pix(128, 128, tile);
            p.Noise(0.03f, 20f, 41);
            for (int i = 0; i < 128; i++)
                for (int k = 0; k < 128; k += 32)
                {
                    p.Set(k, i, grout); p.Set(k + 1, i, grout);
                    p.Set(i, k, grout); p.Set(i, k + 1, grout);
                }
            return p.ToTexture("tiles");
        }

        static Texture2D Stone()
        {
            var p = new Pix(256, 256, new Color(0.3f, 0.29f, 0.27f));
            var r = new System.Random(51);
            for (int row = 0; row < 8; row++)
            {
                int x = -r.Next(0, 40);
                while (x < 256)
                {
                    int bw = r.Next(40, 90);
                    float t = (float)(r.NextDouble() - 0.5) * 0.08f;
                    p.Rect(x + 2, row * 32 + 2, x + bw - 2, row * 32 + 30, new Color(0.4f + t, 0.38f + t, 0.34f + t));
                    x += bw;
                }
            }
            p.Noise(0.06f, 10f, 52);
            p.Noise(0.04f, 2f, 53);
            return p.ToTexture("stone");
        }

        static Texture2D Dirt()
        {
            var p = new Pix(256, 256, new Color(0.25f, 0.22f, 0.17f));
            p.Noise(0.08f, 40f, 61, false);
            p.Noise(0.05f, 4f, 62);
            var r = new System.Random(63);
            for (int i = 0; i < 900; i++)
            {
                int x = r.Next(0, 256), y = r.Next(0, 256);
                p.Line(x, y, x + r.Next(-2, 3), y + r.Next(2, 6), 1, new Color(0.38f, 0.36f, 0.22f), 0.5f);
            }
            return p.ToTexture("dirt");
        }

        static Texture2D Gravel()
        {
            var p = new Pix(128, 128, new Color(0.36f, 0.34f, 0.31f));
            var r = new System.Random(71);
            for (int i = 0; i < 700; i++)
            {
                float v = 0.3f + (float)r.NextDouble() * 0.3f;
                p.Ellipse(r.Next(0, 128), r.Next(0, 128), 1.5f + (float)r.NextDouble() * 2f, 1.2f + (float)r.NextDouble() * 1.5f, new Color(v, v * 0.97f, v * 0.9f));
            }
            return p.ToTexture("gravel");
        }

        static Texture2D Rug()
        {
            var p = new Pix(128, 96, new Color(0.45f, 0.1f, 0.08f));
            var border = new Color(0.75f, 0.62f, 0.38f);
            p.Rect(4, 4, 124, 8, border); p.Rect(4, 88, 124, 92, border);
            p.Rect(4, 4, 8, 92, border); p.Rect(120, 4, 124, 92, border);
            // meander band
            for (int x = 12; x < 116; x += 8)
            {
                p.Rect(x, 12, x + 2, 18, border); p.Rect(x, 16, x + 6, 18, border);
                p.Rect(x, 78, x + 2, 84, border); p.Rect(x, 82, x + 6, 84, border);
            }
            p.Ellipse(64, 48, 18, 14, new Color(0.18f, 0.12f, 0.25f));
            p.Noise(0.05f, 3f, 81);
            return p.ToTexture("rug");
        }

        static Texture2D Books()
        {
            var p = new Pix(128, 128, new Color(0.1f, 0.07f, 0.05f));
            var r = new System.Random(91);
            Color[] cols = { new Color(0.4f, 0.1f, 0.08f), new Color(0.12f, 0.2f, 0.3f), new Color(0.2f, 0.25f, 0.12f), new Color(0.5f, 0.42f, 0.28f), new Color(0.3f, 0.15f, 0.25f), new Color(0.6f, 0.55f, 0.45f) };
            for (int shelf = 0; shelf < 4; shelf++)
            {
                int y0 = shelf * 32 + 3;
                int x = 1;
                while (x < 127)
                {
                    int bw = r.Next(3, 8), bh = r.Next(18, 28);
                    var col = cols[r.Next(cols.Length)] * (0.7f + (float)r.NextDouble() * 0.4f);
                    p.Rect(x, y0, Mathf.Min(127, x + bw), y0 + bh, col);
                    if (r.NextDouble() < 0.5) p.Rect(x, y0 + bh - 6, x + bw, y0 + bh - 5, new Color(0.8f, 0.7f, 0.4f));
                    x += bw + (r.NextDouble() < 0.1 ? 3 : 0);
                }
                p.Rect(0, shelf * 32, 128, shelf * 32 + 3, new Color(0.3f, 0.2f, 0.12f));
            }
            return p.ToTexture("books");
        }

        /// <summary>
        /// "Gathering at the Spring". Both variants come from the same seed and are pixel-identical
        /// except for the ninth figure at the tree line, facing out of the canvas.
        /// </summary>
        static Texture2D Painting(bool ninth)
        {
            var p = new Pix(320, 240, Color.black);
            var r = new System.Random(31);
            p.VGradient(new Color(0.52f, 0.45f, 0.3f), new Color(0.24f, 0.22f, 0.2f), 110, 240);
            p.VGradient(new Color(0.3f, 0.28f, 0.16f), new Color(0.4f, 0.36f, 0.22f), 0, 118);
            // hills
            for (int x = 0; x < 320; x++)
            {
                float top = 128 + Mathf.Sin(x * 0.02f) * 8 + Mathf.Sin(x * 0.051f + 1) * 5;
                for (int y = 108; y < top; y++) p.Set(x, y, new Color(0.22f, 0.24f, 0.16f));
            }
            // cypress line
            float[] trees = { 18, 34, 52, 250, 268, 300, 312 };
            foreach (var tx in trees)
                p.Ellipse(tx, 142 + r.Next(0, 8), 5 + r.Next(0, 3), 26 + r.Next(0, 10), new Color(0.1f, 0.12f, 0.08f));
            // spring
            p.Ellipse(160, 66, 54, 15, new Color(0.12f, 0.2f, 0.22f));
            p.Ellipse(150, 69, 30, 5, new Color(0.35f, 0.42f, 0.4f), 0.6f);
            // rock bearing the mark
            p.Ellipse(262, 50, 16, 10, new Color(0.42f, 0.4f, 0.36f));
            p.Symbol(262, 51, 13, 1f, new Color(0.18f, 0.15f, 0.12f), 0.9f);
            // eight figures around the water, seen from behind
            Color[] robes = { new Color(0.55f, 0.18f, 0.12f), new Color(0.75f, 0.7f, 0.58f), new Color(0.2f, 0.25f, 0.4f), new Color(0.6f, 0.45f, 0.2f), new Color(0.3f, 0.3f, 0.28f), new Color(0.7f, 0.66f, 0.55f), new Color(0.45f, 0.15f, 0.15f), new Color(0.35f, 0.38f, 0.25f) };
            float[,] pos = { { 76, 70, 1f }, { 98, 88, 0.85f }, { 124, 98, 0.75f }, { 150, 102, 0.7f }, { 172, 102, 0.7f }, { 198, 98, 0.75f }, { 222, 88, 0.85f }, { 244, 70, 1f } };
            for (int i = 0; i < 8; i++) Figure(p, pos[i, 0], pos[i, 1], pos[i, 2], robes[i], false);
            if (ninth) Figure(p, 286, 122, 0.42f, new Color(0.78f, 0.76f, 0.7f), true);
            // varnish, grain, craquelure
            for (int y = 0; y < 240; y++)
                for (int x = 0; x < 320; x++)
                {
                    float dx = (x - 160) / 160f, dy = (y - 120) / 120f;
                    float v = 1f - 0.35f * (dx * dx + dy * dy);
                    var col = p.Get(x, y);
                    p.Set(x, y, new Color(col.r * v * 1.02f, col.g * v * 0.98f, col.b * v * 0.85f));
                }
            p.Noise(0.03f, 1.5f, 32);
            var cr = new System.Random(33);
            for (int i = 0; i < 90; i++)
            {
                float x = cr.Next(0, 320), y = cr.Next(0, 240);
                p.Line(x, y, x + cr.Next(-12, 12), y + cr.Next(-12, 12), 1, new Color(0.08f, 0.06f, 0.04f), 0.25f);
            }
            return p.ToTexture(ninth ? "painting_nine" : "painting_eight");
        }

        static void Figure(Pix p, float x, float y, float s, Color robe, bool facingOut)
        {
            float bodyH = 34 * s, bodyW = 8 * s;
            p.Ellipse(x, y + bodyH * 0.45f, bodyW, bodyH * 0.55f, robe);
            p.Ellipse(x, y + bodyH * 0.1f, bodyW * 1.2f, bodyH * 0.12f, robe * 0.8f);
            float hy = y + bodyH + 3 * s;
            if (facingOut)
            {
                p.Ellipse(x, hy, 4.2f * s + 0.8f, 5f * s + 0.8f, new Color(0.86f, 0.8f, 0.7f));
                p.Set(Mathf.RoundToInt(x - 1), Mathf.RoundToInt(hy), new Color(0.05f, 0.04f, 0.03f));
                p.Set(Mathf.RoundToInt(x + 1), Mathf.RoundToInt(hy), new Color(0.05f, 0.04f, 0.03f));
            }
            else
            {
                p.Ellipse(x, hy, 4.2f * s, 5f * s, new Color(0.2f, 0.14f, 0.1f));
            }
        }

        static Texture2D Icon()
        {
            var p = new Pix(128, 170, new Color(0.72f, 0.56f, 0.22f));
            p.Noise(0.07f, 5f, 101, false);
            var red = new Color(0.4f, 0.09f, 0.07f);
            p.Rect(0, 0, 128, 9, red); p.Rect(0, 161, 128, 170, red);
            p.Rect(0, 0, 9, 170, red); p.Rect(119, 0, 128, 170, red);
            p.Ring(64, 118, 22, 3, new Color(0.9f, 0.78f, 0.4f));
            p.Ellipse(64, 118, 14, 17, new Color(0.62f, 0.45f, 0.3f));
            p.Rect(58, 114, 60, 116, new Color(0.15f, 0.08f, 0.05f)); p.Rect(68, 114, 70, 116, new Color(0.15f, 0.08f, 0.05f));
            p.Ellipse(64, 55, 34, 48, new Color(0.22f, 0.1f, 0.12f));
            p.Line(64, 90, 64, 20, 2, new Color(0.5f, 0.36f, 0.16f));
            // the mark, tiny, in the lower-left of the border
            p.Symbol(5, 22, 12, 1f, new Color(0.9f, 0.84f, 0.66f), 0.85f);
            // broken corner shows raw wood
            for (int y = 130; y < 170; y++)
                for (int x = 128 - (y - 130); x < 128; x++) p.Set(x, y, new Color(0.45f, 0.33f, 0.2f));
            p.Noise(0.04f, 1.5f, 102);
            return p.ToTexture("icon_027");
        }

        static Texture2D Disc()
        {
            var p = new Pix(128, 128, new Color(0.3f, 0.36f, 0.26f));
            for (int y = 0; y < 128; y++)
                for (int x = 0; x < 128; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(64, 64)) / 64f;
                    var col = Color.Lerp(new Color(0.5f, 0.4f, 0.22f), new Color(0.3f, 0.42f, 0.32f), Mathf.PerlinNoise(x / 12f, y / 12f) * d);
                    p.Set(x, y, col);
                }
            p.Ring(64, 64, 54, 2, new Color(0.2f, 0.18f, 0.1f));
            p.Ring(64, 64, 30, 1.5f, new Color(0.2f, 0.18f, 0.1f));
            for (int i = 0; i < 24; i++)
            {
                float a = i / 24f * Mathf.PI * 2;
                p.Line(64 + Mathf.Cos(a) * 32, 64 + Mathf.Sin(a) * 32, 64 + Mathf.Cos(a) * 52, 64 + Mathf.Sin(a) * 52, 1, new Color(0.22f, 0.2f, 0.12f), 0.7f);
            }
            p.Symbol(64, 98, 16, 1.2f, new Color(0.14f, 0.12f, 0.07f));
            p.Noise(0.05f, 2f, 111);
            return p.ToTexture("disc_014");
        }

        static Texture2D SymbolTex(int size, bool large)
        {
            var p = new Pix(size, size, new Color(0.34f, 0.32f, 0.29f));
            p.Noise(0.05f, size / 12f, 121);
            var chalk = large ? new Color(0.62f, 0.5f, 0.32f) : new Color(0.85f, 0.83f, 0.78f);
            p.Symbol(size / 2f, size / 2f, size * 0.7f, size / 40f, chalk, 0.85f);
            p.Noise(0.03f, 1.5f, 122);
            return p.ToTexture(large ? "symbol_large" : "symbol_chalk");
        }

        static Texture2D NotePaper()
        {
            var p = new Pix(128, 160, new Color(0.93f, 0.9f, 0.8f));
            var r = new System.Random(131);
            for (int y = 140; y > 12; y -= 9)
            {
                int x = 10, end = r.Next(70, 118);
                while (x < end)
                {
                    int wl = r.Next(6, 18);
                    for (int k = 0; k < wl; k++) p.Set(x + k, y + (int)(Mathf.Sin((x + k) * 0.9f) * 1.5f), new Color(0.12f, 0.15f, 0.4f));
                    x += wl + 4;
                }
            }
            p.Noise(0.02f, 20f, 132);
            return p.ToTexture("note_paper");
        }

        static Texture2D Newspaper()
        {
            var p = new Pix(128, 160, new Color(0.8f, 0.78f, 0.72f));
            p.Rect(8, 138, 120, 152, new Color(0.15f, 0.15f, 0.15f));
            var r = new System.Random(141);
            for (int col = 0; col < 3; col++)
                for (int y = 130; y > 8; y -= 4)
                    p.Rect(8 + col * 38, y, 8 + col * 38 + r.Next(24, 34), y + 1.5f, new Color(0.35f, 0.35f, 0.35f));
            p.Rect(46, 70, 118, 126, new Color(0.45f, 0.45f, 0.43f));
            return p.ToTexture("newspaper");
        }

        static Texture2D FamilyPhoto()
        {
            var p = new Pix(128, 96, new Color(0.6f, 0.62f, 0.66f));
            p.VGradient(new Color(0.75f, 0.68f, 0.52f), new Color(0.55f, 0.66f, 0.72f), 0, 96);
            p.Rect(0, 0, 128, 34, new Color(0.3f, 0.45f, 0.52f));
            float[] xs = { 30, 52, 76, 98 };
            for (int i = 0; i < 4; i++)
            {
                p.Ellipse(xs[i], 44, 8, 18, new Color(0.5f, 0.3f + i * 0.08f, 0.25f));
                p.Ellipse(xs[i], 68, 6, 7, new Color(0.8f, 0.64f, 0.5f));
            }
            for (int y = 0; y < 96; y++)
                for (int x = 0; x < 128; x++)
                {
                    var c = p.Get(x, y);
                    p.Set(x, y, Color.Lerp(c, new Color(0.8f, 0.7f, 0.5f), 0.3f));
                }
            p.Noise(0.03f, 2f, 151);
            return p.ToTexture("family_photo");
        }

        static Texture2D CrtOn()
        {
            var p = new Pix(128, 96, new Color(0f, 0.43f, 0.45f));
            p.Rect(16, 20, 112, 84, new Color(0.76f, 0.76f, 0.76f));
            p.Rect(18, 76, 110, 82, new Color(0, 0, 0.5f));
            p.Rect(24, 50, 70, 56, Color.white);
            p.Rect(0, 0, 128, 8, new Color(0.76f, 0.76f, 0.76f));
            for (int y = 0; y < 96; y += 2) p.Rect(0, y, 128, y + 1, Color.black, 0.15f);
            return p.ToTexture("crt_on", true);
        }
    }
}
