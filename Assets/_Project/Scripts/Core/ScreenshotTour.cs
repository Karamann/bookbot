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
    /// Editor-only screenshot tour. Started from Tools › The Third Lamp › Capture Screenshots: enters
    /// Play mode, skips the intro, then teleports the player through fixed viewpoints and saves the Game
    /// view (HUD included) under several lighting states to &lt;project&gt;/Screenshots/.
    /// </summary>
    public class ScreenshotTour : MonoBehaviour
    {
        public const string PendingKey = "ThirdLamp.ScreenshotTour.Pending";
        public static string OutputDir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Screenshots"));

        enum Light3 { AsFound, Lights, Torch, Lamp }

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
            // the Third Lamp: lamp lit, lights and torch off. Kept last: leaving Mind inside the
            // corridor teleports the player back to the house.
            new Shot("13_lamp_living", new Vector3(-4.5f, 0, 1.2f), -22f, 0f, Light3.Lamp),
            new Shot("14_lamp_hallway", new Vector3(-2f, 0, 7f), 90f, 0f, Light3.Lamp),
            new Shot("15_lamp_corridor", new Vector3(13f, 0, 7f), 90f, 4f, Light3.Lamp),
            new Shot("16_lamp_great_mark", new Vector3(50f, 0, 7f), 90f, 3f, Light3.Lamp),
        };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void MaybeStart()
        {
            if (!SessionState.GetBool(PendingKey, false)) return;
            SessionState.SetBool(PendingKey, false);
            if (FindAnyObjectByType<SliceBootstrap>() == null)
            {
                Debug.LogWarning("[ThirdLamp] Screenshot tour needs the Slice scene.");
                return;
            }
            new GameObject("ScreenshotTour").AddComponent<ScreenshotTour>();
        }

        IEnumerator Start()
        {
            yield return null;
            yield return null;

            // no intro, no clock, no scripted events firing while we teleport around
            Game.Fx.StopAllCoroutines();
            Game.Fx.SkipIntro();
            Game.Mode = InputMode.Play;
            Game.State.clockRunning = false;
            Game.Director.enabled = false;

            foreach (var o in FindObjectsByType<Openable>(FindObjectsSortMode.None))
                if (o.kind == Openable.Kind.Hinge && !o.locked && !o.name.Contains("basement"))
                    o.SetOpen(true, true, true);

            var asFound = Game.Lighting.SwitchedOnGroups();
            var torch = Game.Player.GetComponent<Torch>();
            var lamp = FindAnyObjectByType<OilLamp>();
            var pitchField = typeof(PlayerController).GetField("pitch", BindingFlags.NonPublic | BindingFlags.Instance);

            Directory.CreateDirectory(OutputDir);
            int saved = 0;
            var written = new List<string>();

            foreach (var shot in Shots)
            {
                foreach (var look in shot.looks)
                {
                    SetLights(look, asFound, torch, lamp);
                    Game.Player.Teleport(shot.feet, shot.yaw);
                    pitchField?.SetValue(Game.Player, shot.pitch);
                    Game.Player.CameraPivot.localRotation = Quaternion.Euler(shot.pitch, 0, 0);

                    // let lighting mode, fog and flicker settle
                    for (int i = 0; i < 8; i++) yield return null;
                    yield return new WaitForSeconds(0.3f);
                    yield return new WaitForEndOfFrame();

                    var tex = ScreenCapture.CaptureScreenshotAsTexture();
                    var file = Path.Combine(OutputDir, $"{shot.name}__{look.ToString().ToLowerInvariant()}.png");
                    File.WriteAllBytes(file, tex.EncodeToPNG());
                    Destroy(tex);
                    written.Add(Path.GetFileName(file));
                    saved++;
                }
            }

            Debug.Log($"[ThirdLamp] Saved {saved} screenshots to {OutputDir}\n" + string.Join("\n", written));
            EditorUtility.RevealInFinder(OutputDir);
            EditorApplication.isPlaying = false;
        }

        static void SetLights(Light3 look, List<string> asFound, Torch torch, OilLamp lamp)
        {
            Game.Lighting.SetPower(true);
            foreach (var g in Groups)
                Game.Lighting.SetGroup(g, look == Light3.Lights || (look == Light3.AsFound && asFound.Contains(g)));

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
