using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdLamp
{
    [Serializable]
    public class CatalogueRecord
    {
        public string itemId;
        public string title;
        public string period;
        public string provenance;
        [TextArea] public string notes;
        [Tooltip("If this flag is set, show notesAlt instead of notes.")]
        public string altFlag;
        [TextArea] public string notesAlt;
    }

    /// <summary>Vardis's pre-filled collection database. Alex only confirms entries.</summary>
    public class CatalogueDatabase
    {
        readonly Dictionary<string, CatalogueRecord> records = new Dictionary<string, CatalogueRecord>();
        public readonly List<string> sessionItems = new List<string>();

        public void Add(CatalogueRecord r, bool inThisSession)
        {
            records[r.itemId] = r;
            if (inThisSession) sessionItems.Add(r.itemId);
        }

        public bool TryGet(string id, out CatalogueRecord r) => records.TryGetValue(id, out r);

        public bool SessionComplete
        {
            get
            {
                foreach (var id in sessionItems) if (!Game.State.Has("catalogued_" + id)) return false;
                return true;
            }
        }
    }

    /// <summary>
    /// The study's beige-box PC. Sitting at it moves the view to the monitor and draws a period
    /// database application on the CRT. Typing is read from IMGUI key events.
    /// </summary>
    public class CatalogueComputer : Interactable
    {
        enum Page { Boot, Login, Lookup, Record, Complete }

        public Transform viewPoint;
        public Renderer screenRenderer;
        public Material screenOn, screenOff;
        public string password = "phos";
        public AudioSource fanLoop;

        Page page = Page.Boot;
        bool active, booted;
        float bootTimer;
        string input = "";
        string message = "";
        string recordId;
        bool recordJustSaved;
        readonly List<string> recent = new List<string>();

        public override string Prompt => Game.Lighting.PowerOn ? "Sit at the computer" : "Computer (no power)";

        public void Setup()
        {
            Game.Lighting.PowerChanged += OnPower;
            OnPower(Game.Lighting.PowerOn);
        }

        void OnPower(bool on)
        {
            if (screenRenderer != null) screenRenderer.sharedMaterial = on ? screenOn : screenOff;
            if (!on)
            {
                booted = false;
                page = Page.Boot;
                if (active) StandUp();
            }
        }

        public override void Interact()
        {
            if (!Game.Lighting.PowerOn)
            {
                Game.Hud.Subtitle("Dead. No power.", 2f);
                return;
            }
            active = true;
            Game.Mode = InputMode.Computer;
            Game.Player.SetViewOverride(viewPoint);
            if (!booted)
            {
                page = Page.Boot;
                bootTimer = 0f;
                Game.Audio.PlayAt("beep", transform.position, 0.3f, 0.7f);
            }
            input = "";
            Game.State.Set("used_computer");
        }

        void StandUp()
        {
            active = false;
            Game.Player.ClearViewOverride();
            if (Game.Mode == InputMode.Computer) Game.Mode = InputMode.Play;
        }

        void Update()
        {
            if (!active) return;
            if (GameInput.Down(GameKey.Cancel)) { StandUp(); return; }
            if (page == Page.Boot)
            {
                bootTimer += Time.deltaTime;
                if (bootTimer > 3.2f)
                {
                    booted = true;
                    page = Game.State.Has("computer_logged_in") ? Page.Lookup : Page.Login;
                }
            }
        }

        void Submit()
        {
            Game.Audio.PlayAt("key", transform.position, 0.4f, 0.8f);
            switch (page)
            {
                case Page.Login:
                    if (input.Trim().ToLowerInvariant() == password)
                    {
                        Game.State.Set("computer_logged_in");
                        page = Page.Lookup;
                        message = "";
                    }
                    else message = "The password is incorrect. Please retype.";
                    input = "";
                    break;

                case Page.Lookup:
                    Lookup(input.Trim());
                    input = "";
                    break;

                case Page.Record:
                    if (!recordJustSaved && CanSave(recordId)) SaveRecord(recordId);
                    else page = Game.Catalogue.SessionComplete && !Game.State.Has("vardis_key_note") ? Page.Complete : Page.Lookup;
                    break;

                case Page.Complete:
                    Game.State.Set("vardis_key_note");
                    page = Page.Lookup;
                    break;
            }
        }

        void Lookup(string raw)
        {
            string id = raw.TrimStart('#', 'N', 'n', 'o', '.', ' ');
            if (id.Length > 0 && id.Length < 3 && int.TryParse(id, out var num)) id = num.ToString("000");
            if (id.Length == 0) return;
            if (!Game.Catalogue.TryGet(id, out _))
            {
                message = id == "000" || id.ToLowerInvariant() == "lamp" ? "ACCESS RESTRICTED." : $"No record {id} in collection.";
                return;
            }
            recordId = id;
            recordJustSaved = false;
            message = "";
            page = Page.Record;
        }

        bool CanSave(string id) => Game.State.Has("photographed_" + id) && !Game.State.Has("catalogued_" + id);

        void SaveRecord(string id)
        {
            Game.State.Set("catalogued_" + id);
            Game.State.Add(GameState.CataloguedCounter);
            recent.Add($"{id}  {Game.State.ClockText}");
            recordJustSaved = true;
            Game.Audio.PlayAt("beep", transform.position, 0.35f, 1.3f);
        }

        void OnGUI()
        {
            if (!active) return;
            GUI.depth = 3;
            HandleKeys();

            float sh = Screen.height, sw = Screen.width;
            float h = sh * 0.84f, w = h * 4f / 3f;
            if (w > sw * 0.94f) { w = sw * 0.94f; h = w * 0.75f; }
            var bezel = new Rect((sw - w) / 2f - 34, (sh - h) / 2f - 30, w + 68, h + 76);
            var scr = new Rect((sw - w) / 2f, (sh - h) / 2f, w, h);

            Hud.Fill(new Rect(0, 0, sw, sh), new Color(0, 0, 0, 0.85f));
            Hud.Fill(bezel, new Color(0.74f, 0.71f, 0.63f));
            Hud.Fill(new Rect(bezel.x + 6, bezel.y + 6, bezel.width - 12, 2), new Color(0.84f, 0.81f, 0.73f));
            Hud.Label(new Rect(bezel.x, bezel.yMax - 40, bezel.width, 30), "VISTRON 17\"", Fonts.UI(12), TextAnchor.MiddleCenter, new Color(0.45f, 0.43f, 0.38f));
            Hud.Fill(scr, Color.black);

            float u = h / 600f; // UI unit
            if (page == Page.Boot) DrawBoot(scr, u);
            else DrawDesktop(scr, u);

            Game.Fx.DrawScanlines(scr, 0.18f);
            Game.Fx.DrawVignette(scr, 0.55f);
            Hud.Label(new Rect(0, sh - 30, sw, 24), "[Type] number   [Enter] confirm   [Esc] stand up", Fonts.UI(13), TextAnchor.MiddleCenter, new Color(1, 1, 1, 0.4f));
        }

        void HandleKeys()
        {
            var e = Event.current;
            if (e.type != EventType.KeyDown || page == Page.Boot) return;
            if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter) { Submit(); e.Use(); return; }
            if (e.keyCode == KeyCode.Backspace)
            {
                if (input.Length > 0) input = input.Substring(0, input.Length - 1);
                Game.Audio.PlayAt("key", transform.position, 0.3f, 1.1f);
                e.Use();
                return;
            }
            char c = e.character;
            if (c != '\0' && !char.IsControl(c) && input.Length < 12 && (page == Page.Login || page == Page.Lookup))
            {
                input += c;
                Game.Audio.PlayAt("key", transform.position, 0.3f, UnityEngine.Random.Range(0.9f, 1.15f));
                e.Use();
            }
        }

        void DrawBoot(Rect scr, float u)
        {
            string[] lines =
            {
                "Modular BIOS v4.51PG",
                "Main Processor : 450MHz",
                "Memory Testing : 131072K OK",
                "",
                "Detecting IDE Primary Master ... ST38410A",
                "Detecting IDE Primary Slave  ... None",
                "",
                "Starting ARCHIVE 2.1 ...",
            };
            int shown = Mathf.Clamp(Mathf.FloorToInt(bootTimer * 3.2f), 0, lines.Length);
            var f = Fonts.Mono(Mathf.RoundToInt(18 * u));
            for (int i = 0; i < shown; i++)
                Hud.Label(new Rect(scr.x + 24 * u, scr.y + 24 * u + i * 24 * u, scr.width, 24 * u), lines[i], f, TextAnchor.UpperLeft, new Color(0.75f, 0.75f, 0.75f));
        }

        static readonly Color Desktop = new Color(0f, 0.43f, 0.45f);
        static readonly Color Face = new Color(0.76f, 0.76f, 0.76f);
        static readonly Color Title = new Color(0f, 0f, 0.5f);
        static readonly Color TextC = Color.black;

        void DrawDesktop(Rect scr, float u)
        {
            Hud.Fill(scr, Desktop);
            // taskbar
            var bar = new Rect(scr.x, scr.yMax - 30 * u, scr.width, 30 * u);
            Hud.Fill(bar, Face);
            Hud.Fill(new Rect(bar.x, bar.y, bar.width, 2 * u), Color.white);
            Hud.Fill(new Rect(bar.x + 4 * u, bar.y + 4 * u, 84 * u, 22 * u), new Color(0.84f, 0.84f, 0.84f));
            Hud.Label(new Rect(bar.x + 4 * u, bar.y + 4 * u, 84 * u, 22 * u), "Archive", Fonts.UIBold(Mathf.RoundToInt(14 * u)), TextAnchor.MiddleCenter, TextC);
            Hud.Label(new Rect(bar.xMax - 90 * u, bar.y + 4 * u, 80 * u, 22 * u), Game.State.ClockText, Fonts.UI(Mathf.RoundToInt(14 * u)), TextAnchor.MiddleCenter, TextC);

            var win = new Rect(scr.x + 60 * u, scr.y + 40 * u, scr.width - 120 * u, scr.height - 110 * u);
            Hud.Fill(win, Face);
            Hud.Fill(new Rect(win.x, win.y, win.width, 2 * u), Color.white);
            Hud.Fill(new Rect(win.x, win.y, 2 * u, win.height), Color.white);
            Hud.Fill(new Rect(win.xMax - 2 * u, win.y, 2 * u, win.height), new Color(0.3f, 0.3f, 0.3f));
            Hud.Fill(new Rect(win.x, win.yMax - 2 * u, win.width, 2 * u), new Color(0.3f, 0.3f, 0.3f));
            var tb = new Rect(win.x + 4 * u, win.y + 4 * u, win.width - 8 * u, 24 * u);
            Hud.Fill(tb, Title);
            string title = page == Page.Login ? "Log On to Archive" : "Vardis Collection — Catalogue";
            Hud.Label(new Rect(tb.x + 8 * u, tb.y, tb.width, tb.height), title, Fonts.UIBold(Mathf.RoundToInt(15 * u)), TextAnchor.MiddleLeft, Color.white);

            var body = new Rect(win.x + 24 * u, tb.yMax + 20 * u, win.width - 48 * u, win.height - tb.height - 40 * u);
            var f = Fonts.UI(Mathf.RoundToInt(16 * u));
            var fb = Fonts.UIBold(Mathf.RoundToInt(16 * u));
            var mono = Fonts.Mono(Mathf.RoundToInt(17 * u));
            float line = 26 * u;
            bool caret = Mathf.Repeat(Time.time, 1f) < 0.55f;

            switch (page)
            {
                case Page.Login:
                {
                    Hud.Label(new Rect(body.x, body.y, body.width, line), "Type a user name and password to log on.", f, TextAnchor.MiddleLeft, TextC);
                    Hud.Label(new Rect(body.x, body.y + line * 2, 140 * u, line), "User name:", f, TextAnchor.MiddleLeft, TextC);
                    Field(new Rect(body.x + 140 * u, body.y + line * 2, 260 * u, line), "vardis", mono, false);
                    Hud.Label(new Rect(body.x, body.y + line * 3.4f, 140 * u, line), "Password:", f, TextAnchor.MiddleLeft, TextC);
                    Field(new Rect(body.x + 140 * u, body.y + line * 3.4f, 260 * u, line), new string('*', input.Length) + (caret ? "_" : ""), mono, true);
                    Hud.Label(new Rect(body.x, body.y + line * 5.5f, body.width, line), message, f, TextAnchor.MiddleLeft, new Color(0.6f, 0, 0));
                    break;
                }
                case Page.Lookup:
                {
                    Hud.Label(new Rect(body.x, body.y, body.width, line), "Enter catalogue number:", fb, TextAnchor.MiddleLeft, TextC);
                    Field(new Rect(body.x, body.y + line * 1.2f, 200 * u, line), input + (caret ? "_" : ""), mono, true);
                    Hud.Label(new Rect(body.x + 220 * u, body.y + line * 1.2f, body.width, line), message, f, TextAnchor.MiddleLeft, new Color(0.6f, 0, 0));
                    Hud.Label(new Rect(body.x, body.y + line * 3f, body.width, line), "Recorded this session:", f, TextAnchor.MiddleLeft, TextC);
                    var list = new Rect(body.x, body.y + line * 4f, body.width, line * 6);
                    Hud.Fill(list, Color.white);
                    for (int i = 0; i < recent.Count; i++)
                        Hud.Label(new Rect(list.x + 8 * u, list.y + i * line, list.width, line), recent[i], mono, TextAnchor.MiddleLeft, TextC);
                    int remaining = 0;
                    foreach (var id in Game.Catalogue.sessionItems) if (!Game.State.Has("catalogued_" + id)) remaining++;
                    Hud.Label(new Rect(body.x, list.yMax + 8 * u, body.width, line), $"Items remaining in this session: {remaining}", f, TextAnchor.MiddleLeft, TextC);
                    break;
                }
                case Page.Record:
                {
                    Game.Catalogue.TryGet(recordId, out var r);
                    float yy = body.y;
                    Row(ref yy, body, u, "Number", r.itemId, f, fb);
                    Row(ref yy, body, u, "Object", r.title, f, fb);
                    Row(ref yy, body, u, "Period", r.period, f, fb);
                    Row(ref yy, body, u, "Provenance", r.provenance, f, fb);
                    string notes = !string.IsNullOrEmpty(r.altFlag) && Game.State.Has(r.altFlag) ? r.notesAlt : r.notes;
                    Hud.Label(new Rect(body.x, yy, 150 * u, line), "Notes", fb, TextAnchor.UpperLeft, TextC);
                    var nr = new Rect(body.x + 150 * u, yy, body.width - 150 * u, line * 4);
                    Hud.Fill(nr, Color.white);
                    Hud.Label(new Rect(nr.x + 6 * u, nr.y + 4 * u, nr.width - 12 * u, nr.height - 8 * u), notes, f, TextAnchor.UpperLeft, TextC, true);
                    yy += line * 4.4f;

                    string status;
                    Color sc = TextC;
                    if (recordJustSaved) { status = $"Record saved. Return the object to its place (No. {r.itemId}).  [Enter]"; sc = new Color(0, 0.4f, 0); }
                    else if (Game.State.Has("catalogued_" + r.itemId)) status = "Status: catalogued.  [Enter]";
                    else if (!Game.Catalogue.sessionItems.Contains(r.itemId)) status = "Status: not part of this session.  [Enter]";
                    else if (!Game.State.Has("photographed_" + r.itemId)) { status = "No photograph attached. Photograph the object first.  [Enter]"; sc = new Color(0.6f, 0, 0); }
                    else status = "Photograph received. Press [Enter] to save the record.";
                    Hud.Label(new Rect(body.x, yy, body.width, line), status, fb, TextAnchor.MiddleLeft, sc);
                    break;
                }
                case Page.Complete:
                {
                    var box = new Rect(body.x + body.width * 0.1f, body.y + line, body.width * 0.8f, line * 7);
                    Hud.Fill(box, Color.white);
                    Hud.Label(new Rect(box.x + 12 * u, box.y + 10 * u, box.width - 24 * u, box.height - 20 * u),
                        "All objects for this session have been recorded.\n\nA note is attached to the session file:\n\n\"Thank you. You were careful, which is all I asked. " +
                        "The key to my desk is kept with the Gathering. — E.V.\"\n\n[Enter]", f, TextAnchor.UpperLeft, TextC, true);
                    break;
                }
            }
        }

        static void Row(ref float y, Rect body, float u, string label, string value, GUIStyle f, GUIStyle fb)
        {
            float line = 26 * u;
            Hud.Label(new Rect(body.x, y, 150 * u, line), label, fb, TextAnchor.MiddleLeft, TextC);
            Hud.Label(new Rect(body.x + 150 * u, y, body.width - 150 * u, line), value, f, TextAnchor.MiddleLeft, TextC);
            y += line;
        }

        static void Field(Rect r, string text, GUIStyle font, bool active)
        {
            Hud.Fill(r, new Color(0.3f, 0.3f, 0.3f));
            Hud.Fill(new Rect(r.x + 2, r.y + 2, r.width - 2, r.height - 2), active ? Color.white : new Color(0.9f, 0.9f, 0.9f));
            Hud.Label(new Rect(r.x + 6, r.y, r.width - 8, r.height), text, font, TextAnchor.MiddleLeft, TextC);
        }
    }
}
