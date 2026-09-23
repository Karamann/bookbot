using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdLamp
{
    [Serializable]
    public class CallLine
    {
        public string text;
        public float seconds = 3f;
        public CallLine() { }
        public CallLine(string text, float seconds) { this.text = text; this.seconds = seconds; }
    }

    [Serializable]
    public class CallOption
    {
        public string label;
        public string flag;
        public List<CallLine> response = new List<CallLine>();
    }

    [Serializable]
    public class CallScript
    {
        public string id;
        public string caller = "Unknown";
        public List<CallLine> intro = new List<CallLine>();
        public string question;
        public List<CallOption> options = new List<CallOption>();
    }

    [Serializable]
    public class Sms
    {
        public string from;
        public string text;
        public string time;
        public bool read;
    }

    /// <summary>
    /// Early-2000s mobile phone: monochrome LCD, SMS inbox, and scripted calls with numbered choices.
    /// The backlight doubles as a feeble light source.
    /// </summary>
    public class PhoneSystem : MonoBehaviour
    {
        enum Screen { Idle, Inbox, Message, Ringing, InCall }

        public Light backlight;
        public bool IsOut { get; private set; }
        public List<Sms> Inbox { get; } = new List<Sms>();
        public int Unread { get { int n = 0; foreach (var m in Inbox) if (!m.read) n++; return n; } }

        Screen screen = Screen.Idle;
        int selected;
        Sms viewing;
        float slide, ringTimer, ringElapsed, callTimer, callStart;
        CallScript call;
        List<CallLine> queue = new List<CallLine>();
        int queueIndex;
        bool awaitingChoice, firstSmsTip;
        int missedCalls;
        float retryAt = -1f;
        string lineText = "";

        void Update()
        {
            slide = Mathf.MoveTowards(slide, IsOut ? 1f : 0f, Time.deltaTime * 5f);
            if (backlight != null) backlight.enabled = slide > 0.5f;

            UpdateRinging();
            UpdateCall();

            if (Game.PlayStable && GameInput.Down(GameKey.Phone)) SetOut(!IsOut);
            if (!IsOut || (Game.Mode != InputMode.Play && Game.Mode != InputMode.Call)) return;

            switch (screen)
            {
                case Screen.Idle:
                    if (GameInput.Down(GameKey.Confirm) || GameInput.Down(GameKey.NavRight)) { screen = Screen.Inbox; selected = 0; Beep(); }
                    break;
                case Screen.Inbox:
                    if (GameInput.Down(GameKey.NavUp)) { selected = Mathf.Max(0, selected - 1); Beep(); }
                    if (GameInput.Down(GameKey.NavDown)) { selected = Mathf.Min(Inbox.Count - 1, selected + 1); Beep(); }
                    if ((GameInput.Down(GameKey.Confirm) || GameInput.Down(GameKey.NavRight)) && Inbox.Count > 0)
                    {
                        viewing = Inbox[Inbox.Count - 1 - selected];
                        viewing.read = true;
                        screen = Screen.Message;
                        Beep();
                    }
                    if (GameInput.Down(GameKey.NavLeft)) { screen = Screen.Idle; Beep(); }
                    break;
                case Screen.Message:
                    if (GameInput.Down(GameKey.NavLeft) || GameInput.Down(GameKey.Confirm)) { screen = Screen.Inbox; Beep(); }
                    break;
                case Screen.Ringing:
                    if (GameInput.Down(GameKey.Confirm)) Answer();
                    break;
                case Screen.InCall:
                    if (awaitingChoice)
                    {
                        if (GameInput.Down(GameKey.Choice1)) Choose(0);
                        else if (GameInput.Down(GameKey.Choice2)) Choose(1);
                    }
                    break;
            }
        }

        void Beep() => Game.Audio.Play2D("beep", 0.12f);

        public void SetOut(bool value)
        {
            IsOut = value;
            Game.Audio.Play2D("beep", 0.15f, value ? 1.1f : 0.9f);
            if (value && screen != Screen.Ringing && screen != Screen.InCall) screen = Unread > 0 ? Screen.Inbox : Screen.Idle;
            if (value && Game.State != null) Game.State.Set("phone_opened");
        }

        public void ReceiveSms(string from, string text)
        {
            Inbox.Add(new Sms { from = from, text = text, time = Game.State.ClockText });
            Game.Audio.Play2D("vibrate", 0.45f);
            Game.State.Add("sms_received");
            if (!firstSmsTip)
            {
                firstSmsTip = true;
                Game.Hud.Tip("Your phone vibrates.   [Tab] phone   [Arrows/Enter] navigate", 6f);
            }
            else if (!IsOut) Game.Hud.Subtitle("(Phone vibrates.)", 2f);
        }

        // ---- calls ----
        public void StartCall(CallScript script)
        {
            call = script;
            screen = Screen.Ringing;
            ringElapsed = 0f;
            ringTimer = 0f;
            retryAt = -1f;
        }

        void UpdateRinging()
        {
            if (call == null) return;
            if (screen != Screen.Ringing)
            {
                if (retryAt > 0f && Time.time >= retryAt && screen != Screen.InCall && Game.Mode == InputMode.Play)
                {
                    retryAt = -1f;
                    screen = Screen.Ringing;
                    ringElapsed = 0f;
                }
                return;
            }
            ringElapsed += Time.deltaTime;
            ringTimer -= Time.deltaTime;
            if (ringTimer <= 0f)
            {
                ringTimer = 2.4f;
                Game.Audio.Play2D("ring", 0.35f);
                Game.Audio.Play2D("vibrate", 0.4f);
                if (!IsOut) Game.Hud.Subtitle("(Your phone is ringing.  [Tab], then [Enter] to answer)", 2.3f);
            }
            if (ringElapsed > 24f)
            {
                missedCalls++;
                screen = Screen.Idle;
                retryAt = Time.time + 45f;
            }
        }

        void Answer()
        {
            screen = Screen.InCall;
            Game.Mode = InputMode.Call;
            callStart = Time.time;
            queue = new List<CallLine>(call.intro);
            queueIndex = 0;
            callTimer = 0f;
            awaitingChoice = false;
            lineText = "";
            Game.Audio.Play2D("beep", 0.2f, 0.8f);
            Game.State.Set(call.id + "_answered");
        }

        void UpdateCall()
        {
            if (screen != Screen.InCall || call == null || awaitingChoice) return;
            callTimer -= Time.deltaTime;
            if (callTimer > 0f) return;
            if (queueIndex < queue.Count)
            {
                var line = queue[queueIndex++];
                lineText = line.text;
                callTimer = line.seconds;
                if (!string.IsNullOrEmpty(line.text)) Game.Hud.Subtitle(line.text, line.seconds);
                return;
            }
            if (!string.IsNullOrEmpty(call.question) && !Game.State.Has(call.id + "_chosen"))
            {
                awaitingChoice = true;
                lineText = call.question;
                Game.Hud.Subtitle(call.question, 999f);
                return;
            }
            HangUp();
        }

        void Choose(int index)
        {
            if (index >= call.options.Count) return;
            var opt = call.options[index];
            Game.Hud.ClearSubtitle();
            Game.Hud.Subtitle($"\"{opt.label}\"", 1.6f);
            if (!string.IsNullOrEmpty(opt.flag)) Game.State.Set(opt.flag);
            Game.State.Set(call.id + "_chosen");
            awaitingChoice = false;
            queue = new List<CallLine> { new CallLine("", 1.6f) };
            queue.AddRange(opt.response);
            queueIndex = 0;
            callTimer = 0f;
        }

        void HangUp()
        {
            Game.Audio.Play2D("hangup", 0.3f);
            Game.State.Set(call.id + "_done");
            call = null;
            screen = Screen.Idle;
            lineText = "";
            Game.Mode = InputMode.Play;
        }

        // ---- LCD ----
        static readonly Color Lcd = new Color(0.58f, 0.68f, 0.5f);
        static readonly Color Ink = new Color(0.1f, 0.14f, 0.09f);

        void OnGUI()
        {
            if (slide <= 0.001f) return;
            GUI.depth = 4;
            float sh = UnityEngine.Screen.height, sw = UnityEngine.Screen.width;
            float scale = Mathf.Clamp(sh / 1080f, 0.6f, 2f);
            float pw = 210 * scale, ph = 420 * scale;
            float x = sw - pw - 60 * scale;
            float y = Mathf.Lerp(sh + 10, sh - ph * 0.72f, Mathf.SmoothStep(0, 1, slide));

            var bodyR = new Rect(x, y, pw, ph);
            Hud.Fill(bodyR, new Color(0.16f, 0.2f, 0.3f));
            Hud.Fill(new Rect(x + 8 * scale, y + 8 * scale, pw - 16 * scale, ph * 0.5f), new Color(0.1f, 0.12f, 0.17f));
            Hud.Label(new Rect(x, y + 12 * scale, pw, 16 * scale), "MOBILE", Fonts.UI(Mathf.RoundToInt(10 * scale)), TextAnchor.MiddleCenter, new Color(0.7f, 0.72f, 0.78f, 0.6f));
            var lcd = new Rect(x + 22 * scale, y + 34 * scale, pw - 44 * scale, (pw - 44 * scale) * 0.72f);
            Hud.Fill(lcd, Lcd);

            // keypad hints
            for (int r = 0; r < 4; r++)
                for (int c = 0; c < 3; c++)
                    Hud.Fill(new Rect(x + 30 * scale + c * 52 * scale, lcd.yMax + 60 * scale + r * 26 * scale, 36 * scale, 14 * scale), new Color(0.3f, 0.34f, 0.44f));

            int fs = Mathf.RoundToInt(13 * scale);
            var f = Fonts.Mono(fs);
            var big = Fonts.Mono(Mathf.RoundToInt(22 * scale));
            var pad = 6 * scale;
            var inner = new Rect(lcd.x + pad, lcd.y + pad, lcd.width - pad * 2, lcd.height - pad * 2);

            // status bar
            for (int i = 0; i < 4; i++) Hud.Fill(new Rect(inner.x, inner.y + (3 - i) * 4 * scale, (3 + i * 2) * scale, 2.4f * scale), Ink);
            for (int i = 0; i < 3; i++) Hud.Fill(new Rect(inner.xMax - 12 * scale, inner.y + i * 4 * scale, 12 * scale, 2.4f * scale), Ink);

            switch (screen)
            {
                case Screen.Idle:
                    Hud.Label(new Rect(inner.x, inner.y + 18 * scale, inner.width, 18 * scale), "HELLAS NET", f, TextAnchor.MiddleCenter, Ink);
                    Hud.Label(new Rect(inner.x, inner.y + 36 * scale, inner.width, 30 * scale), Game.State.ClockText, big, TextAnchor.MiddleCenter, Ink);
                    string sub = missedCalls > 0 ? $"{missedCalls} missed call" : Unread > 0 ? $"{Unread} message(s)" : "Menu";
                    Hud.Label(new Rect(inner.x, inner.yMax - 18 * scale, inner.width, 18 * scale), sub, f, TextAnchor.MiddleCenter, Ink);
                    break;
                case Screen.Inbox:
                    Hud.Label(new Rect(inner.x + 20 * scale, inner.y, inner.width, 16 * scale), "Inbox", f, TextAnchor.UpperLeft, Ink);
                    if (Inbox.Count == 0) Hud.Label(new Rect(inner.x, inner.y + 30 * scale, inner.width, 18 * scale), "(empty)", f, TextAnchor.MiddleCenter, Ink);
                    int start = Mathf.Clamp(selected - 2, 0, Mathf.Max(0, Inbox.Count - 4));
                    for (int i = start; i < Mathf.Min(Inbox.Count, start + 4); i++)
                    {
                        var m = Inbox[Inbox.Count - 1 - i];
                        var row = new Rect(inner.x, inner.y + 18 * scale + (i - start) * 17 * scale, inner.width, 16 * scale);
                        bool sel = i == selected;
                        if (sel) Hud.Fill(row, Ink);
                        Hud.Label(row, (m.read ? "  " : "* ") + m.from, f, TextAnchor.MiddleLeft, sel ? Lcd : Ink);
                    }
                    break;
                case Screen.Message:
                    Hud.Label(new Rect(inner.x, inner.y + 14 * scale, inner.width, 16 * scale), $"{viewing.from}  {viewing.time}", f, TextAnchor.UpperLeft, Ink);
                    Hud.Label(new Rect(inner.x, inner.y + 32 * scale, inner.width, inner.height - 32 * scale), viewing.text, f, TextAnchor.UpperLeft, Ink, true);
                    break;
                case Screen.Ringing:
                    bool blink = Mathf.Repeat(Time.time, 0.8f) < 0.5f;
                    Hud.Label(new Rect(inner.x, inner.y + 16 * scale, inner.width, 18 * scale), "Call from:", f, TextAnchor.MiddleCenter, Ink);
                    Hud.Label(new Rect(inner.x, inner.y + 34 * scale, inner.width, 18 * scale), blink ? call.caller : "", f, TextAnchor.MiddleCenter, Ink);
                    Hud.Label(new Rect(inner.x, inner.yMax - 18 * scale, inner.width, 18 * scale), "Answer [Enter]", f, TextAnchor.MiddleCenter, Ink);
                    break;
                case Screen.InCall:
                    int secs = Mathf.FloorToInt(Time.time - callStart);
                    Hud.Label(new Rect(inner.x, inner.y + 14 * scale, inner.width, 16 * scale), call != null ? call.caller : "", f, TextAnchor.MiddleCenter, Ink);
                    Hud.Label(new Rect(inner.x, inner.y + 30 * scale, inner.width, 16 * scale), $"{secs / 60:00}:{secs % 60:00}", f, TextAnchor.MiddleCenter, Ink);
                    if (awaitingChoice && call != null)
                    {
                        for (int i = 0; i < call.options.Count && i < 2; i++)
                            Hud.Label(new Rect(inner.x, inner.y + 50 * scale + i * 16 * scale, inner.width, 16 * scale), $"{i + 1}  {call.options[i].label}", f, TextAnchor.MiddleLeft, Ink);
                    }
                    break;
            }
        }

        // ---- save support ----
        public void Restore(List<Sms> saved)
        {
            Inbox.Clear();
            Inbox.AddRange(saved);
            firstSmsTip = saved.Count > 0;
        }
    }
}
