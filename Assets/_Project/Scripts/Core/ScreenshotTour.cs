#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ThirdLamp
{
    /// <summary>
    /// Editor-only screenshot tour. Started from Tools › The Third Lamp › QA › Capture Screenshots (via the QA queue): enters
    /// Play mode, skips the intro, then teleports the player through fixed viewpoints and saves the Game
    /// view (HUD included) under several lighting states to &lt;project&gt;/Screenshots/.
    /// </summary>
    public class ScreenshotTour : MonoBehaviour
    {
        public static string OutputDir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Screenshots"));

        enum Light3 { AsFound, Lights, Torch, Lamp, Scare }

        struct Shot
        {
            public string name;
            public Vector3 feet;
            public float yaw, pitch;
            public Light3[] looks;
            public Shot(string name, Vector3 feet, float yaw, float pitch, params Light3[] looks)
            {
                this.name = name; this.feet = feet; this.yaw = yaw; this.pitch = pitch; this.looks = looks;
            }
        }

        static readonly string[] Groups = { "living", "porch", "kitchen", "hall", "study", "bath" };

        static readonly Shot[] Shots =
        {
            // outside, as the slice opens
            new Shot("01_arrival", new Vector3(-2.7f, 0, -13.2f), 8f, 0f, Light3.AsFound, Light3.Torch),
            new Shot("02_front_door", new Vector3(-4.5f, 0, -3.2f), 0f, -2f, Light3.AsFound, Light3.Torch),
            new Shot("03_generator_shed", new Vector3(11.2f, 0, -6.3f), 0f, -12f, Light3.AsFound, Light3.Torch),
            // inside
            new Shot("04_living_painting_wall", new Vector3(-4.5f, 0, 1.2f), -22f, 0f, Light3.Lights, Light3.Torch),
            new Shot("05_living_across", new Vector3(-7.4f, 0, 5.3f), 130f, -4f, Light3.Lights, Light3.Torch),
            new Shot("06_painting_closeup", new Vector3(-6.4f, 0, 4.45f), 0f, 0f, Light3.Lights, Light3.Torch),
            new Shot("07_kitchen", new Vector3(0.8f, 0, 0.8f), 50f, -4f, Light3.Lights, Light3.Torch),
            new Shot("08_hallway_east", new Vector3(-7.4f, 0, 7f), 90f, -3f, Light3.Lights, Light3.Torch),
            new Shot("09_hallway_west", new Vector3(5.4f, 0, 7f), -90f, -3f, Light3.Lights, Light3.Torch),
            new Shot("10_study_desk", new Vector3(-1.2f, 0, 10.5f), -24f, -8f, Light3.Lights, Light3.Torch),
            new Shot("11_bathroom_mirror", new Vector3(1.2f, 0, 10f), 90f, 0f, Light3.Lights, Light3.Torch),
            new Shot("12_storage", new Vector3(4.2f, 0, 8.7f), 35f, -6f, Light3.Lights, Light3.Torch),
            // later in the night: the perception events forced on, room lights on
            new Shot("13_scare_tv", new Vector3(-6.9f, 0, 4.6f), 131f, 6f, Light3.Scare),
            new Shot("14_scare_garden_window", new Vector3(-5.6f, 0, 1.3f), -52f, 0f, Light3.Scare),
            new Shot("15_scare_cupboard", new Vector3(5.2f, 0, 3.2f), -26f, -14f, Light3.Scare),
            new Shot("16_scare_footprints", new Vector3(-4.3f, 0, 7f), 90f, 24f, Light3.Scare, Light3.Torch),
            new Shot("17_scare_hall_photo", new Vector3(0.2f, 0, 7.3f), 180f, 0f, Light3.Scare),
            new Shot("18_scare_portrait", new Vector3(-2.6f, 0, 6.5f), 0f, 0f, Light3.Scare),
            new Shot("19_scare_kitchen_chalk", new Vector3(5.2f, 0, 2.2f), 100f, 0f, Light3.Scare),
            // the Third Lamp: lamp lit, lights and torch off. Kept last: leaving Mind inside the
            // corridor teleports the player back to the house.
            new Shot("20_lamp_living", new Vector3(-4.5f, 0, 1.2f), -22f, 0f, Light3.Lamp),
            new Shot("21_lamp_hallway", new Vector3(-2f, 0, 7f), 90f, 0f, Light3.Lamp),
            new Shot("22_lamp_corridor", new Vector3(13f, 0, 7f), 90f, 4f, Light3.Lamp),
            new Shot("23_lamp_great_mark", new Vector3(50f, 0, 7f), 90f, 3f, Light3.Lamp),
        };

        float started;

        void Update()
        {
            // a hidden Game view never renders, so WaitForEndOfFrame would wait forever
            if (started > 0f && Time.realtimeSinceStartup - started > 420f)
            {
                Debug.LogWarning("[ThirdLamp] Screenshot tour timed out. Is the Game view visible?");
                Qa.QaQueue.MarkFailed();
                started = -1f;
                Qa.QaQueue.Finish();
            }
        }

        IEnumerator Start()
        {
            started = Time.realtimeSinceStartup;
            DontDestroyOnLoad(gameObject);
            yield return null;
            yield return null;

            // no intro, no clock, no scripted events firing while we teleport around
            Qa.QaUtil.SkipIntro();
            Game.State.clockRunning = false;
            Game.Director.enabled = false;

            foreach (var o in FindObjectsByType<Openable>(FindObjectsSortMode.None))
                if (o.kind == Openable.Kind.Hinge && !o.locked && o.displayName != "cupboard")
                    o.SetOpen(true, true, true);

            var asFound = Game.Lighting.SwitchedOnGroups();
            var torch = Game.Player.GetComponent<Torch>();
            var lamp = FindAnyObjectByType<OilLamp>();

            Directory.CreateDirectory(OutputDir);
            foreach (var old in Directory.GetFiles(OutputDir, "*.png")) File.Delete(old);
            string baseDir = Path.Combine(OutputDir, "baseline"), diffDir = Path.Combine(OutputDir, "diff");
            if (Directory.Exists(diffDir)) Directory.Delete(diffDir, true);
            var metrics = new MetricsFile();
            var thumbs = new List<Color32[]>();
            int saved = 0;
            var written = new List<string>();

            bool scaresForced = false;
            foreach (var shot in Shots)
            {
                foreach (var look in shot.looks)
                {
                    if (look == Light3.Scare && !scaresForced) { ForceScares(); scaresForced = true; }
                    SetLights(look, asFound, torch, lamp);
                    Qa.QaUtil.Teleport(shot.feet, shot.yaw, shot.pitch);

                    // let lighting mode, fog and flicker settle
                    for (int i = 0; i < 8; i++) yield return null;
                    yield return new WaitForSeconds(0.3f);
                    yield return new WaitForEndOfFrame();

                    var tex = ScreenCapture.CaptureScreenshotAsTexture();
                    var file = Path.Combine(OutputDir, $"{shot.name}__{look.ToString().ToLowerInvariant()}.png");
                    File.WriteAllBytes(file, tex.EncodeToPNG());
                    var m = Measure(tex, look);
                    m.shot = Path.GetFileName(file);
                    var thumb = Thumb(tex);
                    Compare(thumb, Path.Combine(baseDir, m.shot), Path.Combine(diffDir, m.shot), m);
                    metrics.shots.Add(m);
                    thumbs.Add(thumb);
                    Destroy(tex);
                    written.Add(Path.GetFileName(file));
                    saved++;
                }
            }

            File.WriteAllText(Path.Combine(OutputDir, "metrics.json"), JsonUtility.ToJson(metrics, true));
            File.WriteAllText(Path.Combine(OutputDir, "metrics.md"), MetricsMarkdown(metrics));
            File.WriteAllBytes(Path.Combine(OutputDir, "_sheet.png"), Sheet(thumbs).EncodeToPNG());
            Debug.Log($"[ThirdLamp] Saved {saved} screenshots to {OutputDir}\n" + string.Join("\n", written));
            Qa.QaQueue.Finish();
        }

        // ------------------------------------------------------------------ metrics, sheet, diff

        [System.Serializable] public class ShotMetrics { public string shot, look, verdict, diffNote; public float meanLuminance, darkPercent, clippedPercent, diffPercent = -1f; }
        [System.Serializable] public class MetricsFile { public List<ShotMetrics> shots = new List<ShotMetrics>(); }

        /// <summary>Acceptable mean-luminance band per lighting state, before the image reads as unplayable.</summary>
        static Vector2 Band(Light3 look) => look switch
        {
            Light3.Lights => new Vector2(0.06f, 0.5f),
            Light3.Scare => new Vector2(0.06f, 0.5f),
            Light3.Torch => new Vector2(0.03f, 0.5f),
            Light3.Lamp => new Vector2(0.015f, 0.35f),
            _ => new Vector2(0.01f, 0.5f),
        };

        static ShotMetrics Measure(Texture2D tex, Light3 look)
        {
            var px = tex.GetPixels32();
            double sum = 0; int dark = 0, clip = 0, n = 0;
            for (int i = 0; i < px.Length; i += 7)
            {
                var c = px[i];
                float l = (0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b) / 255f;
                sum += l; n++;
                if (l < 0.02f) dark++;
                if (l > 0.97f) clip++;
            }
            var m = new ShotMetrics { look = look.ToString().ToLowerInvariant(), meanLuminance = (float)(sum / n), darkPercent = 100f * dark / n, clippedPercent = 100f * clip / n };
            var band = Band(look);
            m.verdict = m.meanLuminance < band.x ? "too dark" : m.meanLuminance > band.y ? "too bright" : m.darkPercent > 85f ? "mostly black" : "ok";
            return m;
        }

        /// <summary>
        /// Compares at thumbnail size, where film grain and bulb flicker average out, so only real
        /// changes (moved props, new textures, lighting) register.
        /// </summary>
        static void Compare(Color32[] now, string basePath, string diffPath, ShotMetrics m)
        {
            if (!File.Exists(basePath)) return;
            var old = new Texture2D(2, 2);
            if (!old.LoadImage(File.ReadAllBytes(basePath))) { Destroy(old); return; }
            var b = Thumb(old);
            Destroy(old);
            var d = new Color32[now.Length];
            int changed = 0;
            for (int i = 0; i < now.Length; i++)
            {
                int delta = Mathf.Max(Mathf.Abs(now[i].r - b[i].r), Mathf.Max(Mathf.Abs(now[i].g - b[i].g), Mathf.Abs(now[i].b - b[i].b)));
                byte g = (byte)((now[i].r + now[i].g + now[i].b) / 9);
                if (delta > 40) { changed++; d[i] = new Color32(255, 30, 30, 255); }
                else d[i] = new Color32(g, g, g, 255);
            }
            m.diffPercent = 100f * changed / now.Length;
            if (Mathf.Abs((float)Screen.width / Screen.height - (float)TW / TH) > 0.02f) m.diffNote = "Game view is not 16:9; set 1920×1080";
            var dt = new Texture2D(TW, TH, TextureFormat.RGBA32, false);
            dt.SetPixels32(d);
            dt.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(diffPath));
            File.WriteAllBytes(diffPath, dt.EncodeToPNG());
            Destroy(dt);
        }

        const int TW = 384, TH = 216, Cols = 4;

        static Color32[] Thumb(Texture2D tex)
        {
            var c = new Color32[TW * TH];
            for (int y = 0; y < TH; y++)
                for (int x = 0; x < TW; x++)
                    c[y * TW + x] = tex.GetPixelBilinear((x + 0.5f) / TW, (y + 0.5f) / TH);
            return c;
        }

        static Texture2D Sheet(List<Color32[]> thumbs)
        {
            int rows = Mathf.Max(1, (thumbs.Count + Cols - 1) / Cols);
            var sheet = new Texture2D(Cols * TW, rows * TH, TextureFormat.RGBA32, false);
            var bg = new Color32[sheet.width * sheet.height];
            for (int i = 0; i < bg.Length; i++) bg[i] = new Color32(25, 25, 25, 255);
            sheet.SetPixels32(bg);
            for (int i = 0; i < thumbs.Count; i++)
            {
                int col = i % Cols, row = rows - 1 - i / Cols; // first shot top-left
                sheet.SetPixels32(col * TW, row * TH, TW, TH, thumbs[i]);
            }
            sheet.Apply();
            return sheet;
        }

        static string MetricsMarkdown(MetricsFile f)
        {
            var sb = new System.Text.StringBuilder("# Screenshot metrics\n\n| Shot | Look | Mean lum. | Black % | Clipped % | Verdict | Changed vs baseline |\n|---|---|---|---|---|---|---|\n");
            foreach (var m in f.shots)
                sb.AppendLine($"| {m.shot} | {m.look} | {m.meanLuminance:0.000} | {m.darkPercent:0} | {m.clippedPercent:0.0} | {(m.verdict == "ok" ? "ok" : "⚠ " + m.verdict)} | {(m.diffPercent < 0 ? "–" : m.diffPercent.ToString("0.0") + "%")}{(string.IsNullOrEmpty(m.diffNote) ? "" : " (" + m.diffNote + ")")} |");
            return sb.ToString();
        }

        /// <summary>Puts every perception event into its "has happened" state, for review.</summary>
        static void ForceScares()
        {
            foreach (var r in Game.World.Find("tv")?.GetComponentsInChildren<IParamReceiver>(true) ?? new IParamReceiver[0]) r.SetParam("on", 1f);
            Game.World.Get<Openable>("kitchen_cabinet")?.SetOpen(true, true, true);
            Game.World.Find("wet_footprints")?.SetActive(true);
            Game.World.Find("chalk_reason_kitchen")?.SetActive(true);
            Game.World.Get<MaterialStates>("hall_photo")?.SetState("scratched");
            Game.World.Get<MaterialStates>("hall_portrait")?.SetState("scratched");
            var garden = Game.World.Find("garden_figure");
            if (garden != null)
            {
                var g = garden.GetComponent<Glimpse>();
                if (g != null) g.enabled = false; // it would vanish the moment the tour looks at it
                garden.SetActive(true);
            }
        }

        static void SetLights(Light3 look, List<string> asFound, Torch torch, OilLamp lamp)
        {
            Game.Lighting.SetPower(true);
            foreach (var g in Groups)
                Game.Lighting.SetGroup(g, look == Light3.Lights || look == Light3.Scare || (look == Light3.AsFound && asFound.Contains(g)));

            bool torchOn = look == Light3.Torch;
            if (torch != null && torch.On != torchOn) torch.Set(torchOn);

            if (lamp == null) return;
            bool lampOn = look == Light3.Lamp;
            if (lampOn && lamp.transform.parent != Game.Player.CameraPivot)
            {
                // carried low in the right hand, as when held
                foreach (var c in lamp.GetComponentsInChildren<Collider>(true)) c.enabled = false;
                lamp.transform.SetParent(Game.Player.CameraPivot, false);
                lamp.transform.localPosition = new Vector3(0.28f, -0.42f, 0.55f);
                lamp.transform.localRotation = Quaternion.identity;
            }
            if (lamp.Lit != lampOn) lamp.Toggle();
        }
    }
}
#endif
