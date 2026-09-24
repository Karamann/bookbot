#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace ThirdLamp.Qa
{
    /// <summary>
    /// Queue of QA runs, one per Play-mode session: "lint", "bot", "monkey:&lt;seed&gt;:&lt;minutes&gt;",
    /// "screenshots". Editor/QaMenu starts Play mode while the queue is non-empty; each runner calls
    /// <see cref="Finish"/>, which pops its entry and stops Play mode.
    /// </summary>
    public static class QaQueue
    {
        const string Key = "ThirdLamp.Qa.Queue";
        public const string RunningKey = "ThirdLamp.Qa.Running";

        static List<string> Items
        {
            get
            {
                var s = SessionState.GetString(Key, "");
                return string.IsNullOrEmpty(s) ? new List<string>() : new List<string>(s.Split('|'));
            }
            set => SessionState.SetString(Key, string.Join("|", value));
        }

        public static bool Empty => Items.Count == 0;
        public static string Peek() { var l = Items; return l.Count > 0 ? l[0] : null; }
        public static void Enqueue(params string[] kinds) { var l = Items; l.AddRange(kinds); Items = l; }
        public static void Clear() { SessionState.EraseString(Key); SessionState.EraseString(RunningKey); }
        public static string Running => SessionState.GetString(RunningKey, "");
        public const string ActiveKey = "ThirdLamp.Qa.Active";
        const string FailedKey = "ThirdLamp.Qa.Failed";
        public static void MarkFailed() => SessionState.SetBool(FailedKey, true);
        public static bool AnyFailed => SessionState.GetBool(FailedKey, false);
        public static void ResetFailed() => SessionState.EraseBool(FailedKey);

        static void Pop()
        {
            var l = Items;
            if (l.Count > 0) l.RemoveAt(0);
            Items = l;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Dispatch()
        {
            var head = Peek();
            // only runs started from the QA menu; a leftover queue must not hijack a normal Play press
            if (head == null || !SessionState.GetBool(ActiveKey, false)) return;
            if (UnityEngine.Object.FindAnyObjectByType<SliceBootstrap>() == null)
            {
                Debug.LogWarning("[ThirdLamp] QA runs need the Slice scene. Clearing the QA queue.");
                MarkFailed();
                Clear();
                EditorApplication.isPlaying = false;
                return;
            }
            SessionState.SetString(RunningKey, head);
            var go = new GameObject("QA_" + head);
            var parts = head.Split(':');
            switch (parts[0])
            {
                case "lint": go.AddComponent<LevelLint>(); break;
                case "bot": go.AddComponent<CriticalPathBot>(); break;
                case "monkey":
                    var m = go.AddComponent<MonkeyBot>();
                    if (parts.Length > 1 && int.TryParse(parts[1], out var seed)) m.seed = seed;
                    if (parts.Length > 2 && float.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var min)) m.minutes = min;
                    break;
                case "screenshots": go.AddComponent<ScreenshotTour>(); break;
                default:
                    Debug.LogWarning("[ThirdLamp] Unknown QA run '" + head + "'");
                    Finish();
                    break;
            }
        }

        /// <summary>Called by a runner when it is done: pops the queue and leaves Play mode.</summary>
        public static void Finish()
        {
            SessionState.EraseString(RunningKey);
            Pop();
            EditorApplication.isPlaying = false;
        }
    }

    /// <summary>Helpers shared by the QA runners and the screenshot tour.</summary>
    public static class QaUtil
    {
        public const BindingFlags Any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        public static string QaRoot => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "QA"));
        public static string CheckpointPath => Path.Combine(Application.persistentDataPath, "thirdlamp_checkpoint.json");

        public static void SkipIntro()
        {
            Game.Fx.StopAllCoroutines();
            Game.Fx.SkipIntro();
            Game.Mode = InputMode.Play;
            Game.State.Set("intro_done");
        }

        static FieldInfo yawField, pitchField;

        /// <summary>Sets the player's view angles (pitch: positive looks down).</summary>
        public static void SetView(float yaw, float pitch)
        {
            var p = Game.Player;
            yawField ??= typeof(PlayerController).GetField("yaw", Any);
            pitchField ??= typeof(PlayerController).GetField("pitch", Any);
            yawField?.SetValue(p, yaw);
            pitchField?.SetValue(p, pitch);
            p.transform.rotation = Quaternion.Euler(0, yaw, 0);
            p.CameraPivot.localRotation = Quaternion.Euler(pitch, 0, 0);
        }

        public static void Teleport(Vector3 feet, float yaw, float pitch = 0f)
        {
            Game.Player.Teleport(feet, yaw);
            SetView(yaw, pitch);
        }

        public static Vector3 Eye => Game.Player.CameraPivot.position;

        /// <summary>Turns the view toward a world point from where the player stands.</summary>
        public static void AimAt(Vector3 target)
        {
            Vector3 d = target - Eye;
            float yaw = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            float pitch = -Mathf.Atan2(d.y, new Vector2(d.x, d.z).magnitude) * Mathf.Rad2Deg;
            SetView(yaw, pitch);
        }

        public static void LightsAll(bool on)
        {
            foreach (var g in new[] { "living", "porch", "kitchen", "hall", "study", "bath" }) Game.Lighting.SetGroup(g, on);
        }

        public static object Call(object target, string method, params object[] args)
        {
            var m = target.GetType().GetMethod(method, Any);
            if (m == null) throw new MissingMethodException(target.GetType().Name, method);
            return m.Invoke(target, args);
        }

        public static T Get<T>(object target, string field)
        {
            var f = target.GetType().GetField(field, Any);
            if (f == null) throw new MissingFieldException(target.GetType().Name, field);
            return (T)f.GetValue(target);
        }

        public static void Set(object target, string field, object value)
        {
            var f = target.GetType().GetField(field, Any);
            if (f == null) throw new MissingFieldException(target.GetType().Name, field);
            f.SetValue(target, value);
        }

        public static string FieldString(object target, string field) => target.GetType().GetField(field, Any)?.GetValue(target)?.ToString() ?? "";

        public static T FindPickup<T>(Func<T, bool> match) where T : UnityEngine.Object
        {
            foreach (var o in UnityEngine.Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (match(o)) return o;
            return null;
        }

        static string BackupPath => CheckpointPath + ".qa-backup";

        /// <summary>Moves the developer's checkpoint aside so runs start fresh; <see cref="RestoreCheckpoint"/> puts it back.</summary>
        public static void SetAsideCheckpoint()
        {
            if (!File.Exists(CheckpointPath)) return;
            if (!File.Exists(BackupPath)) File.Move(CheckpointPath, BackupPath);
            else File.Delete(CheckpointPath); // a QA run's own checkpoint from the previous run
        }

        public static void RestoreCheckpoint()
        {
            if (!File.Exists(BackupPath)) return;
            if (File.Exists(CheckpointPath)) File.Delete(CheckpointPath);
            File.Move(BackupPath, CheckpointPath);
        }

        public static string PathOf(Transform t)
        {
            var s = t.name;
            while (t.parent != null) { t = t.parent; s = t.name + "/" + s; }
            return s;
        }

        /// <summary>Captures the Game view (HUD included) to a PNG. Must be yielded from a coroutine.</summary>
        public static IEnumerator Capture(string file)
        {
            yield return new WaitForEndOfFrame();
            var tex = ScreenCapture.CaptureScreenshotAsTexture();
            Directory.CreateDirectory(Path.GetDirectoryName(file));
            File.WriteAllBytes(file, tex.EncodeToPNG());
            UnityEngine.Object.Destroy(tex);
        }

        public static void CopyDirectory(string from, string to)
        {
            if (Directory.Exists(to)) Directory.Delete(to, true);
            Directory.CreateDirectory(to);
            foreach (var f in Directory.GetFiles(from)) File.Copy(f, Path.Combine(to, Path.GetFileName(f)));
        }
    }
}
#endif
