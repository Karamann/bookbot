using System.Collections;
using UnityEngine;

namespace ThirdLamp
{
    /// <summary>
    /// Subtle film grain, vignette, fades, intro/ending cards. Pipeline-independent (IMGUI overlay),
    /// so it also works before URP post-processing is set up.
    /// </summary>
    public class ScreenFx : MonoBehaviour
    {
        Texture2D grain, vignette, scan;
        float fade = 1f;
        string cardTitle, cardSub;
        float cardAlpha;
        bool ended;

        public void Setup()
        {
            grain = new Texture2D(128, 128, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Point };
            var rng = new System.Random(7);
            var px = new Color32[128 * 128];
            for (int i = 0; i < px.Length; i++)
            {
                byte v = (byte)rng.Next(0, 256);
                px[i] = new Color32(v, v, v, (byte)rng.Next(60, 256));
            }
            grain.SetPixels32(px);
            grain.Apply();

            vignette = new Texture2D(128, 128, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 128; y++)
                for (int x = 0; x < 128; x++)
                {
                    float dx = (x - 63.5f) / 64f, dy = (y - 63.5f) / 64f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    vignette.SetPixel(x, y, new Color(0, 0, 0, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1.35f, d))));
                }
            vignette.Apply();

            scan = new Texture2D(1, 4, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Point };
            scan.SetPixels(new[] { new Color(0, 0, 0, 1), new Color(0, 0, 0, 0.2f), new Color(0, 0, 0, 0), new Color(0, 0, 0, 0.2f) });
            scan.Apply();
        }

        public void DrawGrain(Rect r, float alpha)
        {
            var old = GUI.color;
            GUI.color = new Color(1, 1, 1, alpha);
            var uv = new Rect(Random.value, Random.value, r.width / 256f, r.height / 256f);
            GUI.DrawTextureWithTexCoords(r, grain, uv);
            GUI.color = old;
        }

        public void DrawScanlines(Rect r, float alpha)
        {
            var old = GUI.color;
            GUI.color = new Color(1, 1, 1, alpha);
            GUI.DrawTextureWithTexCoords(r, scan, new Rect(0, 0, 1, r.height / 4f));
            GUI.color = old;
        }

        public void DrawVignette(Rect r, float alpha)
        {
            var old = GUI.color;
            GUI.color = new Color(1, 1, 1, alpha);
            GUI.DrawTexture(r, vignette, ScaleMode.StretchToFill);
            GUI.color = old;
        }

        public IEnumerator Intro()
        {
            Game.Mode = InputMode.Cinematic;
            fade = 1f;
            cardTitle = "Attica, Greece";
            cardSub = "October 2002";
            for (float t = 0; t < 1f; t += Time.deltaTime) { cardAlpha = t; yield return null; }
            yield return new WaitForSeconds(2.2f);
            for (float t = 1; t > 0f; t -= Time.deltaTime) { cardAlpha = t; yield return null; }
            cardAlpha = 0f;
            Game.Mode = InputMode.Play;
            for (float t = 1; t > 0f; t -= Time.deltaTime / 2.5f) { fade = t; yield return null; }
            fade = 0f;
            Game.State.Set("intro_done");
            Game.Hud.Tip("[WASD] walk   [Mouse] look   [E] interact   [Esc] pause & controls", 8f);
        }

        public void SkipIntro()
        {
            fade = 0f;
            cardAlpha = 0f;
        }

        public void EndSlice()
        {
            if (ended) return;
            ended = true;
            StartCoroutine(EndRoutine());
        }

        IEnumerator EndRoutine()
        {
            Game.Mode = InputMode.Ended;
            Game.State.clockRunning = false;
            for (float t = 0; t < 1f; t += Time.deltaTime / 4f) { fade = t; yield return null; }
            fade = 1f;
            yield return new WaitForSeconds(1.5f);
            cardTitle = "END OF PROTOTYPE";
            cardSub = "[F12] play again";
            for (float t = 0; t < 1f; t += Time.deltaTime / 2f) { cardAlpha = t; yield return null; }
            cardAlpha = 1f;
        }

        void OnGUI()
        {
            GUI.depth = -100;
            var full = new Rect(0, 0, Screen.width, Screen.height);
            bool mind = Game.Lighting != null && Game.Lighting.MindActive;
            DrawVignette(full, mind ? 0.95f : 0.6f);
            DrawGrain(full, mind ? 0.1f : 0.045f);
            if (fade > 0f) Hud.Fill(full, new Color(0, 0, 0, fade));
            if (cardAlpha > 0f)
            {
                float h = Screen.height;
                Hud.Label(new Rect(0, h * 0.44f, Screen.width, 40), cardTitle, Fonts.Serif(Mathf.RoundToInt(h * 0.032f)), TextAnchor.MiddleCenter, new Color(0.9f, 0.88f, 0.82f, cardAlpha));
                Hud.Label(new Rect(0, h * 0.5f, Screen.width, 30), cardSub, Fonts.Serif(Mathf.RoundToInt(h * 0.02f)), TextAnchor.MiddleCenter, new Color(0.7f, 0.68f, 0.62f, cardAlpha));
            }
        }
    }
}
