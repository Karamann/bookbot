using System.Collections.Generic;
using UnityEngine;

namespace ThirdLamp
{
    /// <summary>
    /// Minimal diegetic-leaning HUD: a dot reticle, an interaction prompt, subtitles, one-line tips,
    /// the document reader, pause, and the F1 debug overlay.
    /// </summary>
    public class Hud : MonoBehaviour
    {
        struct Line { public string text; public float until; }

        readonly List<Line> subtitles = new List<Line>();
        string tip;
        float tipUntil;
        NoteReader doc;
        int docOpenedFrame;
        bool debug;
        Vector2 docScroll;

        // ---------- drawing helpers ----------
        public static void Fill(Rect r, Color c)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = old;
        }

        public static void Label(Rect r, string text, GUIStyle style, TextAnchor anchor, Color color, bool wrap = false)
        {
            if (string.IsNullOrEmpty(text)) return;
            style.alignment = anchor;
            style.wordWrap = wrap;
            style.normal.textColor = color;
            GUI.Label(r, text, style);
        }

        public static void ShadowLabel(Rect r, string text, GUIStyle style, TextAnchor anchor, Color color, bool wrap = true)
        {
            var sr = r;
            sr.x += 1.5f;
            sr.y += 1.5f;
            Label(sr, text, style, anchor, new Color(0, 0, 0, color.a * 0.85f), wrap);
            Label(r, text, style, anchor, color, wrap);
        }

        // ---------- API ----------
        public void Subtitle(string text, float seconds = 4f)
        {
            if (string.IsNullOrEmpty(text)) return;
            subtitles.RemoveAll(l => l.text == text);
            subtitles.Add(new Line { text = text, until = Time.time + seconds });
            if (subtitles.Count > 3) subtitles.RemoveAt(0);
        }

        public void ClearSubtitle() => subtitles.Clear();

        public void Tip(string text, float seconds)
        {
            tip = text;
            tipUntil = Time.time + seconds;
        }

        public void ShowDocument(NoteReader note)
        {
            doc = note;
            docOpenedFrame = Time.frameCount;
            docScroll = Vector2.zero;
            Game.Mode = InputMode.Reading;
        }

        void Update()
        {
            subtitles.RemoveAll(l => Time.time > l.until);

            if ((Debug.isDebugBuild || Application.isEditor) && GameInput.Down(GameKey.Debug)) debug = !debug;

            if (doc != null && Game.Mode == InputMode.Reading && Time.frameCount != docOpenedFrame &&
                (GameInput.Down(GameKey.Interact) || GameInput.Down(GameKey.Cancel) || GameInput.Down(GameKey.Confirm)))
            {
                doc = null;
                Game.Mode = InputMode.Play;
                Game.Audio.PlayAt("paper", Game.MainCamera.transform.position, 0.3f, 1.2f);
                return;
            }
            if (doc != null) docScroll.y -= GameInput.Scroll * 40f;

            if (Game.PlayStable && GameInput.Down(GameKey.Cancel)) SetPaused(true);
            else if (Game.Mode == InputMode.Paused && !Game.ModeJustChanged && GameInput.Down(GameKey.Cancel)) SetPaused(false);

            if ((Game.Mode == InputMode.Play || Game.Mode == InputMode.Paused) && GameInput.Down(GameKey.LoadCheckpoint))
            {
                SetPaused(false);
                Game.Save.LoadCheckpoint();
            }
            if (Game.Mode == InputMode.Paused || Game.Mode == InputMode.Ended)
            {
                if (GameInput.Down(GameKey.Restart))
                {
                    SetPaused(false);
                    Game.Save.Restart();
                }
            }
        }

        void SetPaused(bool paused)
        {
            if (paused == (Game.Mode == InputMode.Paused)) return;
            Game.Mode = paused ? InputMode.Paused : InputMode.Play;
            Time.timeScale = paused ? 0f : 1f;
        }

        void OnGUI()
        {
            GUI.depth = 10;
            float w = Screen.width, h = Screen.height;

            if (Game.Mode == InputMode.Play && (Game.PhotoCamera == null || !Game.PhotoCamera.IsRaised))
            {
                var focus = Game.Interactor != null ? Game.Interactor.Focus : null;
                var c = new Vector2(w / 2f, h / 2f);
                if (focus != null)
                {
                    DrawRing(c, 7f, new Color(1, 1, 1, 0.8f));
                    ShadowLabel(new Rect(0, c.y + 22, w, 26), "[E]  " + focus.Prompt, Fonts.UI(17), TextAnchor.UpperCenter, new Color(0.95f, 0.92f, 0.85f, 0.95f), false);
                }
                else Fill(new Rect(c.x - 1.5f, c.y - 1.5f, 3, 3), new Color(1, 1, 1, 0.55f));
            }

            // subtitles
            float sy = h * 0.8f;
            for (int i = subtitles.Count - 1; i >= 0; i--)
            {
                ShadowLabel(new Rect(w * 0.15f, sy, w * 0.7f, 60), subtitles[i].text, Fonts.UI(20), TextAnchor.UpperCenter, new Color(0.96f, 0.94f, 0.88f));
                sy -= 34f;
            }

            if (tip != null && Time.time < tipUntil && Game.Mode != InputMode.Ended)
                ShadowLabel(new Rect(0, h - 40, w, 24), tip, Fonts.UI(14), TextAnchor.MiddleCenter, new Color(1, 1, 1, 0.55f), false);

            if (doc != null && Game.Mode == InputMode.Reading) DrawDocument(w, h);

            if (Game.Mode == InputMode.Paused)
            {
                Fill(new Rect(0, 0, w, h), new Color(0, 0, 0, 0.7f));
                ShadowLabel(new Rect(0, h * 0.38f, w, 40), "PAUSED", Fonts.UI(28), TextAnchor.MiddleCenter, Color.white, false);
                ShadowLabel(new Rect(0, h * 0.46f, w, 90),
                    "[Esc] resume      [F9] load last checkpoint      [F12] restart\n\n" +
                    "WASD move · Shift walk faster · E interact · F inspect held item · Q put down\n" +
                    "C / right mouse camera · Left mouse photo · R review · Tab phone · T torch · L lamp",
                    Fonts.UI(15), TextAnchor.UpperCenter, new Color(1, 1, 1, 0.8f));
            }

            if (debug) DrawDebug();
        }

        static void DrawRing(Vector2 c, float r, Color col)
        {
            for (int i = 0; i < 20; i++)
            {
                float a = i / 20f * Mathf.PI * 2f;
                Fill(new Rect(c.x + Mathf.Cos(a) * r - 1, c.y + Mathf.Sin(a) * r - 1, 2, 2), col);
            }
        }

        void DrawDocument(float w, float h)
        {
            Fill(new Rect(0, 0, w, h), new Color(0, 0, 0, 0.75f));
            float pw = Mathf.Min(w * 0.46f, h * 0.72f), ph = h * 0.86f;
            var paper = new Rect((w - pw) / 2f, (h - ph) / 2f, pw, ph);

            Color paperC, ink;
            GUIStyle body;
            float scale = h / 1080f;
            switch (doc.style)
            {
                case DocStyle.Typed: paperC = new Color(0.93f, 0.92f, 0.88f); ink = new Color(0.12f, 0.12f, 0.14f); body = Fonts.Mono(Mathf.RoundToInt(19 * scale)); break;
                case DocStyle.Letter: paperC = new Color(0.86f, 0.8f, 0.66f); ink = new Color(0.18f, 0.12f, 0.08f); body = Fonts.Hand(Mathf.RoundToInt(23 * scale)); break;
                case DocStyle.Newspaper: paperC = new Color(0.82f, 0.8f, 0.74f); ink = new Color(0.1f, 0.1f, 0.1f); body = Fonts.Serif(Mathf.RoundToInt(20 * scale)); break;
                case DocStyle.Postcard: paperC = new Color(0.95f, 0.93f, 0.86f); ink = new Color(0.1f, 0.15f, 0.35f); body = Fonts.Hand(Mathf.RoundToInt(23 * scale)); break;
                default: paperC = new Color(0.95f, 0.93f, 0.84f); ink = new Color(0.08f, 0.1f, 0.3f); body = Fonts.Hand(Mathf.RoundToInt(24 * scale)); break;
            }
            Fill(new Rect(paper.x + 6, paper.y + 8, paper.width, paper.height), new Color(0, 0, 0, 0.4f));
            Fill(paper, paperC);
            if (doc.style == DocStyle.Handwritten)
                for (float y = paper.y + 90 * scale; y < paper.yMax - 20; y += 34 * scale)
                    Fill(new Rect(paper.x + 20, y, paper.width - 40, 1), new Color(0.5f, 0.6f, 0.8f, 0.25f));
            Game.Fx.DrawGrain(paper, 0.06f);

            var inner = new Rect(paper.x + 40 * scale, paper.y + 40 * scale, paper.width - 80 * scale, paper.height - 80 * scale);
            GUI.BeginGroup(inner);
            var content = new Rect(0, Mathf.Min(0, docScroll.y), inner.width, inner.height * 3);
            Label(content, doc.body, body, TextAnchor.UpperLeft, ink, true);
            GUI.EndGroup();
            ShadowLabel(new Rect(0, h - 34, w, 24), "[E] put it down    [Scroll] read on", Fonts.UI(13), TextAnchor.MiddleCenter, new Color(1, 1, 1, 0.5f), false);
        }

        void DrawDebug()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"clock {Game.State.ClockText}   mode {Game.Mode}   lamp {Game.Lighting.Mode}   power {Game.Lighting.PowerOn}");
            sb.AppendLine($"perception {Game.Perception.Value} (stage {Game.Perception.Stage})   zone {RoomZone.IdAt(Game.Player.transform.position)}");
            foreach (var c in Game.State.AllCounters()) sb.Append($"{c.key}={c.value}  ");
            sb.AppendLine();
            var flags = Game.State.AllFlags();
            flags.Sort();
            sb.AppendLine(string.Join("  ", flags));
            Fill(new Rect(8, 8, Screen.width * 0.6f, 180), new Color(0, 0, 0, 0.7f));
            Label(new Rect(16, 14, Screen.width * 0.6f - 16, 170), sb.ToString(), Fonts.Mono(12), TextAnchor.UpperLeft, new Color(0.6f, 1f, 0.6f), true);
        }
    }
}
