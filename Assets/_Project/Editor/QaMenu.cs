using System.IO;
using ThirdLamp.Qa;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ThirdLamp.EditorTools
{
    /// <summary>
    /// Tools › The Third Lamp › QA. Each run is one Play-mode session; runs queued together are chained
    /// automatically. Reports go to &lt;project&gt;/QA/latest_&lt;kind&gt;/ and screenshots to &lt;project&gt;/Screenshots/.
    /// If a run is interrupted (Play stopped by hand, an exception, a domain reload), the rest of the
    /// queue is dropped instead of restarting forever.
    /// </summary>
    [InitializeOnLoad]
    public static class QaMenu
    {
        const string ScenePath = "Assets/_Project/Scenes/Slice.unity";
        const string ActiveKey = "ThirdLamp.Qa.Active";
        const string Menu = "Tools/The Third Lamp/QA/";

        static QaMenu() => EditorApplication.playModeStateChanged += OnPlayModeChanged;

        [MenuItem(Menu + "Run All (lint, bot, monkey, screenshots)", priority = 0)]
        public static void RunAll() => Begin("lint", "bot", "monkey:1:5", "screenshots");

        [MenuItem(Menu + "Run Level Lint", priority = 20)]
        public static void RunLint() => Begin("lint");

        [MenuItem(Menu + "Run Bot Playthrough", priority = 21)]
        public static void RunBot() => Begin("bot");

        [MenuItem(Menu + "Run Monkey (5 min)", priority = 22)]
        public static void RunMonkey() => Begin("monkey:1:5");

        [MenuItem(Menu + "Run Monkey ×3 seeds (3 × 4 min)", priority = 23)]
        public static void RunMonkeys() => Begin("monkey:11:4", "monkey:22:4", "monkey:33:4");

        [MenuItem(Menu + "Capture Screenshots", priority = 40)]
        public static void CaptureScreenshots() => Begin("screenshots");

        [MenuItem(Menu + "Set Screenshot Baseline", priority = 41)]
        public static void SetBaseline()
        {
            var dir = ScreenshotTour.OutputDir;
            var shots = Directory.Exists(dir) ? Directory.GetFiles(dir, "*.png") : new string[0];
            if (shots.Length == 0) { Debug.LogWarning("[ThirdLamp] No screenshots yet. Run Capture Screenshots first."); return; }
            var baseDir = Path.Combine(dir, "baseline");
            if (Directory.Exists(baseDir)) Directory.Delete(baseDir, true);
            Directory.CreateDirectory(baseDir);
            foreach (var f in shots) if (!Path.GetFileName(f).StartsWith("_")) File.Copy(f, Path.Combine(baseDir, Path.GetFileName(f)));
            Debug.Log($"[ThirdLamp] Baseline set from {shots.Length} screenshots. Later captures write Screenshots/diff/.");
        }

        [MenuItem(Menu + "Open QA Folder", priority = 60)]
        public static void OpenFolder()
        {
            Directory.CreateDirectory(QaUtil.QaRoot);
            EditorUtility.RevealInFinder(QaUtil.QaRoot);
        }

        [MenuItem(Menu + "Cancel Queued Runs", priority = 61)]
        public static void Cancel()
        {
            QaQueue.Clear();
            SessionState.EraseBool(ActiveKey);
            if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
        }

        /// <summary>Command line: Unity -projectPath . -executeMethod ThirdLamp.EditorTools.QaMenu.RunAllBatch (no -quit, no -nographics).</summary>
        public static void RunAllBatch() => RunAll();

        static void Begin(params string[] runs)
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("[ThirdLamp] Stop Play mode before starting QA runs."); return; }
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            QaQueue.Clear();
            QaQueue.Enqueue(runs);
            SessionState.SetBool(ActiveKey, true);
            Debug.Log($"[ThirdLamp] QA queued: {string.Join(", ", runs)}. Leave the Game view focused and the mouse still.");
            StartNext();
        }

        static void StartNext()
        {
            if (!File.Exists(ScenePath)) { Debug.LogWarning("[ThirdLamp] Slice scene missing. Use Tools › The Third Lamp › Recreate Slice Scene."); QaQueue.Clear(); return; }
            EditorSceneManager.OpenScene(ScenePath);
            QaUtil.DeleteCheckpoint(); // every run starts from a fresh night
            EditorApplication.isPlaying = true;
        }

        static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change != PlayModeStateChange.EnteredEditMode || !SessionState.GetBool(ActiveKey, false)) return;
            if (!string.IsNullOrEmpty(QaQueue.Running))
            {
                Debug.LogWarning($"[ThirdLamp] QA run '{QaQueue.Running}' was interrupted. Remaining runs cancelled.");
                QaQueue.Clear();
            }
            if (!QaQueue.Empty)
            {
                EditorApplication.delayCall += StartNext;
                return;
            }
            SessionState.EraseBool(ActiveKey);
            Debug.Log("[ThirdLamp] QA finished. Reports: QA/latest_*/report.md · Screenshots/metrics.md");
            if (Application.isBatchMode) EditorApplication.Exit(0);
            else EditorUtility.RevealInFinder(QaUtil.QaRoot);
        }
    }
}
