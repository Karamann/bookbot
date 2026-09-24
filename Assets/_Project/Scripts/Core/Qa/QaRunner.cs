#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace ThirdLamp.Qa
{
    /// <summary>
    /// Base for QA runs: captures errors and [ThirdLamp] warnings, records steps and findings, writes
    /// QA/&lt;timestamp&gt;_&lt;kind&gt;/report.json + report.md (copied to QA/latest_&lt;kind&gt;/), then hands control
    /// back to the QA queue. A watchdog ends the run if it exceeds <see cref="MaxSeconds"/>.
    /// </summary>
    public abstract class QaRunner : MonoBehaviour
    {
        [Serializable] public class StepResult { public string name, status, detail, screenshot; public float seconds; }
        [Serializable] public class Finding { public string severity, category, message, path; public Vector3 position; }
        [Serializable] public class LogLine { public string type, message, where; public int count; }
        [Serializable] public class Report
        {
            public string kind, started, unityVersion, summary;
            public float seconds;
            public int passed, failed, errors, warnings;
            public List<StepResult> steps = new List<StepResult>();
            public List<Finding> findings = new List<Finding>();
            public List<LogLine> log = new List<LogLine>();
            public List<string> eventsFired = new List<string>();
            public List<string> eventsNotFired = new List<string>();
            public List<string> extras = new List<string>();
        }

        protected abstract string Kind { get; }
        protected virtual float MaxSeconds => 900f;
        protected Report report = new Report();
        protected string Dir { get; private set; }
        float startTime;
        bool finished;
        int shotIndex;

        protected virtual void Awake()
        {
            report.kind = Kind;
            report.started = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            report.unityVersion = Application.unityVersion;
            Dir = Path.Combine(QaUtil.QaRoot, DateTime.Now.ToString("yyyyMMdd_HHmmss") + "_" + Kind);
            Directory.CreateDirectory(Dir);
            Application.logMessageReceived += OnLog;
        }

        void OnDestroy() => Application.logMessageReceived -= OnLog;

        IEnumerator Start()
        {
            startTime = Time.realtimeSinceStartup;
            yield return null;
            yield return null;
            yield return Run();
            Finish();
        }

        protected abstract IEnumerator Run();

        void Update()
        {
            if (!finished && Time.realtimeSinceStartup - startTime > MaxSeconds)
            {
                AddFinding("error", "timeout", $"Run exceeded {MaxSeconds:0} s and was stopped.", null, Vector3.zero);
                Finish();
            }
        }

        void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Log) return;
            if (type == LogType.Warning && !message.StartsWith("[ThirdLamp]")) return;
            string t = type == LogType.Warning ? "warning" : type == LogType.Exception ? "exception" : "error";
            string where = "";
            if (!string.IsNullOrEmpty(stack))
            {
                var lines = stack.Split('\n');
                foreach (var l in lines) if (l.Contains("ThirdLamp") && !l.Contains(".Qa.")) { where = l.Trim(); break; }
            }
            foreach (var e in report.log)
                if (e.type == t && e.message == message) { e.count++; return; }
            report.log.Add(new LogLine { type = t, message = message, where = where, count = 1 });
        }

        // ------------------------------------------------------------- steps

        /// <summary>
        /// Runs <paramref name="act"/> (exceptions become a failure), then waits up to <paramref name="timeout"/>
        /// seconds for <paramref name="done"/>. Records PASS/FAIL, optionally with a screenshot.
        /// </summary>
        protected IEnumerator Step(string name, Action act, Func<bool> done, float timeout = 5f, bool screenshot = false)
        {
            var r = new StepResult { name = name };
            float t0 = Time.realtimeSinceStartup;
            string error = null;
            try { act?.Invoke(); }
            catch (Exception e) { error = e.GetType().Name + ": " + (e.InnerException?.Message ?? e.Message); }

            bool ok = false;
            if (error == null)
            {
                while (Time.realtimeSinceStartup - t0 < timeout)
                {
                    bool d;
                    try { d = done == null || done(); }
                    catch (Exception e) { error = "check threw " + e.GetType().Name + ": " + e.Message; break; }
                    if (d) { ok = true; break; }
                    yield return null;
                }
            }
            r.seconds = Time.realtimeSinceStartup - t0;
            r.status = ok ? "PASS" : "FAIL";
            r.detail = error ?? (ok ? "" : $"condition not met within {timeout:0.#} s");
            if (screenshot || !ok)
            {
                r.screenshot = $"{++shotIndex:00}_{Safe(name)}.png";
                yield return QaUtil.Capture(Path.Combine(Dir, r.screenshot));
            }
            report.steps.Add(r);
            if (ok) report.passed++; else report.failed++;
        }

        protected IEnumerator Shot(string name)
        {
            yield return QaUtil.Capture(Path.Combine(Dir, $"{++shotIndex:00}_{Safe(name)}.png"));
        }

        protected void AddFinding(string severity, string category, string message, Transform t, Vector3 pos)
        {
            report.findings.Add(new Finding { severity = severity, category = category, message = message, path = t != null ? QaUtil.PathOf(t) : "", position = pos });
        }

        protected static bool Flag(string f) => Game.State.Has(f);

        static string Safe(string s)
        {
            var sb = new StringBuilder();
            foreach (var c in s) sb.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : '_');
            return sb.ToString();
        }

        // ------------------------------------------------------------- report

        protected virtual void BeforeWrite() { }

        protected void Finish()
        {
            if (finished) return;
            finished = true;
            StopAllCoroutines();
            report.seconds = Time.realtimeSinceStartup - startTime;
            if (Game.Director != null)
                foreach (var e in Game.Director.Events)
                    (Game.Director.HasFired(e.id) ? report.eventsFired : report.eventsNotFired).Add(e.id);
            foreach (var l in report.log) { if (l.type == "warning") report.warnings += l.count; else report.errors += l.count; }
            try { BeforeWrite(); }
            catch (Exception e) { report.extras.Add("BeforeWrite failed: " + e.Message); }
            int bad = 0;
            foreach (var f in report.findings) if (f.severity == "error") bad++;
            report.summary = $"{Kind}: {report.passed} passed, {report.failed} failed, {report.findings.Count} findings ({bad} errors), " +
                             $"{report.errors} exceptions/errors, {report.warnings} warnings, {report.seconds:0} s";

            File.WriteAllText(Path.Combine(Dir, "report.json"), JsonUtility.ToJson(report, true));
            File.WriteAllText(Path.Combine(Dir, "report.md"), Markdown());
            QaUtil.CopyDirectory(Dir, Path.Combine(QaUtil.QaRoot, "latest_" + Kind));
            Debug.Log($"[ThirdLamp] QA {report.summary}\n{Dir}");
            QaQueue.Finish();
        }

        string Markdown()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# QA report: {Kind}");
            sb.AppendLine();
            sb.AppendLine($"{report.started} · Unity {report.unityVersion} · {report.seconds:0} s");
            sb.AppendLine();
            sb.AppendLine("**" + report.summary + "**");
            if (report.steps.Count > 0)
            {
                sb.AppendLine("\n## Steps\n\n| # | Step | Result | Time | Detail |\n|---|---|---|---|---|");
                for (int i = 0; i < report.steps.Count; i++)
                {
                    var s = report.steps[i];
                    string shot = string.IsNullOrEmpty(s.screenshot) ? "" : $" ([shot]({s.screenshot}))";
                    sb.AppendLine($"| {i + 1} | {s.name} | {(s.status == "PASS" ? "✅" : "❌")} | {s.seconds:0.0}s | {s.detail}{shot} |");
                }
            }
            if (report.findings.Count > 0)
            {
                sb.AppendLine("\n## Findings\n\n| Severity | Category | Where | Message |\n|---|---|---|---|");
                var ordered = new List<Finding>(report.findings);
                ordered.Sort((a, b) => Rank(a.severity).CompareTo(Rank(b.severity)));
                foreach (var f in ordered)
                    sb.AppendLine($"| {f.severity} | {f.category} | `{f.path}` ({f.position.x:0.00}, {f.position.y:0.00}, {f.position.z:0.00}) | {f.message} |");
            }
            if (report.log.Count > 0)
            {
                sb.AppendLine("\n## Errors and warnings\n\n| Type | Count | Message | Where |\n|---|---|---|---|");
                foreach (var l in report.log) sb.AppendLine($"| {l.type} | {l.count} | {l.message.Replace("\n", " ").Replace("|", "/")} | {l.where} |");
            }
            if (report.eventsNotFired.Count > 0)
                sb.AppendLine("\n## Events that never fired\n\n" + string.Join(", ", report.eventsNotFired));
            if (report.extras.Count > 0)
            {
                sb.AppendLine("\n## Notes\n");
                foreach (var e in report.extras) sb.AppendLine("- " + e);
            }
            return sb.ToString();
        }

        static int Rank(string s) => s == "error" ? 0 : s == "warning" ? 1 : 2;
    }
}
#endif
