using UnityEngine;

namespace ThirdLamp
{
    /// <summary>Tiny software canvas for procedural placeholder textures. Origin bottom-left.</summary>
    public class Pix
    {
        public readonly int w, h;
        public readonly Color[] c;

        public Pix(int width, int height, Color fill)
        {
            w = width;
            h = height;
            c = new Color[w * h];
            for (int i = 0; i < c.Length; i++) c[i] = fill;
        }

        public Pix Copy()
        {
            var p = new Pix(w, h, Color.black);
            System.Array.Copy(c, p.c, c.Length);
            return p;
        }

        public Color Get(int x, int y) => c[Mathf.Clamp(y, 0, h - 1) * w + Mathf.Clamp(x, 0, w - 1)];

        public void Set(int x, int y, Color col, float a = 1f)
        {
            if (x < 0 || y < 0 || x >= w || y >= h) return;
            int i = y * w + x;
            c[i] = a >= 1f ? col : Color.Lerp(c[i], col, a * col.a);
        }

        public void Rect(float x0, float y0, float x1, float y1, Color col, float a = 1f)
        {
            for (int y = Mathf.FloorToInt(y0); y < Mathf.CeilToInt(y1); y++)
                for (int x = Mathf.FloorToInt(x0); x < Mathf.CeilToInt(x1); x++)
                    Set(x, y, col, a);
        }

        public void Ellipse(float cx, float cy, float rx, float ry, Color col, float a = 1f)
        {
            for (int y = Mathf.FloorToInt(cy - ry); y <= Mathf.CeilToInt(cy + ry); y++)
                for (int x = Mathf.FloorToInt(cx - rx); x <= Mathf.CeilToInt(cx + rx); x++)
                {
                    float dx = (x + 0.5f - cx) / rx, dy = (y + 0.5f - cy) / ry;
                    if (dx * dx + dy * dy <= 1f) Set(x, y, col, a);
                }
        }

        public void Ring(float cx, float cy, float r, float thick, Color col, float a = 1f)
        {
            for (int y = Mathf.FloorToInt(cy - r - thick); y <= Mathf.CeilToInt(cy + r + thick); y++)
                for (int x = Mathf.FloorToInt(cx - r - thick); x <= Mathf.CeilToInt(cx + r + thick); x++)
                {
                    float d = Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy));
                    if (Mathf.Abs(d - r) <= thick / 2f) Set(x, y, col, a);
                }
        }

        public void Line(float x0, float y0, float x1, float y1, float thick, Color col, float a = 1f)
        {
            float len = Mathf.Max(Mathf.Abs(x1 - x0), Mathf.Abs(y1 - y0));
            int steps = Mathf.Max(1, Mathf.CeilToInt(len * 2f));
            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                float x = Mathf.Lerp(x0, x1, t), y = Mathf.Lerp(y0, y1, t);
                if (thick <= 1.2f) Set(Mathf.RoundToInt(x), Mathf.RoundToInt(y), col, a);
                else Ellipse(x, y, thick / 2f, thick / 2f, col, a);
            }
        }

        public void VGradient(Color bottom, Color top, float y0 = 0, float y1 = -1)
        {
            if (y1 < 0) y1 = h;
            for (int y = Mathf.FloorToInt(y0); y < Mathf.CeilToInt(y1) && y < h; y++)
            {
                var col = Color.Lerp(bottom, top, Mathf.InverseLerp(y0, y1, y));
                for (int x = 0; x < w; x++) Set(x, y, col);
            }
        }

        public void Noise(float amount, float scale, int seed, bool mono = true)
        {
            var r = new System.Random(seed);
            float ox = r.Next(0, 1000), oy = r.Next(0, 1000);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float n = (Mathf.PerlinNoise(ox + x / scale, oy + y / scale) - 0.5f) * 2f;
                    float fine = (float)(r.NextDouble() - 0.5) * 0.5f;
                    float v = (n + fine) * amount;
                    int i = y * w + x;
                    c[i] = new Color(c[i].r + v, c[i].g + v, c[i].b + v * (mono ? 1f : 0.7f), c[i].a);
                }
        }

        /// <summary>The recurring mark: circle over a vertical line over a triangle.</summary>
        public void Symbol(float cx, float cy, float size, float thick, Color col, float a = 1f)
        {
            Ring(cx, cy + size * 0.62f, size * 0.2f, thick, col, a);
            Line(cx, cy + size * 0.4f, cx, cy - 0.02f * size, thick, col, a);
            float ty = cy - 0.05f * size, by = cy - size * 0.55f, hw = size * 0.3f;
            Line(cx, ty, cx - hw, by, thick, col, a);
            Line(cx, ty, cx + hw, by, thick, col, a);
            Line(cx - hw, by, cx + hw, by, thick, col, a);
        }

        public Texture2D ToTexture(string name, bool pointFilter = false, bool clamp = false)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, true)
            {
                name = name,
                filterMode = pointFilter ? FilterMode.Point : FilterMode.Bilinear,
                wrapMode = clamp ? TextureWrapMode.Clamp : TextureWrapMode.Repeat,
                anisoLevel = 4,
            };
            t.SetPixels(c);
            t.Apply(true);
            return t;
        }
    }
}
