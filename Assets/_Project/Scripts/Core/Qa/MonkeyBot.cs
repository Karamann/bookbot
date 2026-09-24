#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace ThirdLamp.Qa
{
    /// <summary>
    /// Seeded random explorer. Walks the real CharacterController, pokes whatever it faces, flips
    /// lights, torch, lamp and power, and escapes any screen it lands in. Reports exceptions, stuck
    /// spots, escapes out of bounds, soft-locks, and a coverage heatmap over the level plan.
    /// </summary>
    public class MonkeyBot : QaRunner
    {
        public int seed = 1;
        public float minutes = 5f;

        protected override string Kind => "monkey";
        protected override float MaxSeconds => minutes * 60f + 120f;

        // map covers the grounds and the Mind corridor
        const float MinX = -22f, MaxX = 64f, MinZ = -28f, MaxZ = 26f, Cell = 0.5f;
        int W => Mathf.CeilToInt((MaxX - MinX) / Cell);
        int H => Mathf.CeilToInt((MaxZ - MinZ) / Cell);
        int[,] visits;

        System.Random rng;
        CharacterController cc;
        readonly HashSet<Vector2Int> stuckCells = new HashSet<Vector2Int>();
        int interactions, teleports, stuckCount, escapes, softLocks;
        readonly Dictionary<string, int> touched = new Dictionary<string, int>();

        static readonly Vector3[] Spawns =
        {
            new Vector3(-2.7f, 0, -13.2f), new Vector3(-4.5f, 0, 1.5f), new Vector3(4f, 0, 2f), new Vector3(-1f, 0, 7f),
            new Vector3(-4f, 0, 11f), new Vector3(1.7f, 0, 11f), new Vector3(11.2f, 0, -5f), new Vector3(-12f, 0, 8f),
        };

        float Rand(float a, float b) => a + (float)rng.NextDouble() * (b - a);

        protected override IEnumerator Run()
        {
            rng = new System.Random(seed);
            visits = new int[W, H];
            report.extras.Add($"seed {seed}, {minutes:0.#} minutes");
            QaUtil.SkipIntro();
            cc = Game.Player.GetComponent<CharacterController>();
            // give it the keys to the house so it can reach everything; the bot run tests the story gating
            foreach (var f in new[] { "has_front_key", CameraSystem.HasCameraFlag, Torch.HasTorchFlag }) Game.State.Set(f);
            yield return null;

            float end = Time.realtimeSinceStartup + minutes * 60f;
            float heading = Rand(0, 360), nextTurn = 0, nextPoke = 1, nextToggle = 8, nextTeleport = 40;
            Vector3 lastPos = Game.Player.transform.position;
            float stuckFor = 0, notPlayFor = 0;
            int shotsLeft = 6;

            while (Time.realtimeSinceStartup < end)
            {
                float now = Time.realtimeSinceStartup;
                var p = Game.Player.transform.position;
                Mark(p);

                // -------- screens: get out of whatever we are in
                if (Game.Mode == InputMode.Ended) { report.extras.Add($"Reached an ending at {now - (end - minutes * 60f):0} s."); break; }
                if (Game.Mode != InputMode.Play)
                {
                    notPlayFor += Time.deltaTime;
                    float limit = Game.Mode == InputMode.Call || Game.Mode == InputMode.Cinematic ? 60f : 20f;
                    if (notPlayFor > limit)
                    {
                        softLocks++;
                        AddFinding("error", "soft-lock", $"Stuck in {Game.Mode} mode for {limit:0} s.", Game.Player.transform, p);
                        ForcePlay();
                        notPlayFor = 0;
                    }
                    else if (notPlayFor > Rand(1f, 3f) && Game.Mode != InputMode.Call && Game.Mode != InputMode.Cinematic) ForcePlay();
                    yield return null;
                    continue;
                }
                notPlayFor = 0;

                // -------- walk
                if (now > nextTurn) { heading += Rand(-120, 120); nextTurn = now + Rand(0.6f, 2.5f); }
                QaUtil.SetView(heading, Rand(-5, 20));
                var dir = Quaternion.Euler(0, heading, 0) * Vector3.forward;
                cc.Move(dir * 1.7f * Time.deltaTime);

                // -------- stuck: trying to walk but not getting anywhere
                if ((p - lastPos).sqrMagnitude < 0.0004f) stuckFor += Time.deltaTime; else stuckFor = 0;
                lastPos = p;
                if (stuckFor > 0.7f) heading += Rand(90, 270);
                if (stuckFor > 3f)
                {
                    var cell = new Vector2Int(Mathf.FloorToInt(p.x / 1f), Mathf.FloorToInt(p.z / 1f));
                    if (stuckCells.Add(cell))
                        AddFinding("warning", "stuck", "Player could not move in any tried direction for 3 s.", Game.Player.transform, p);
                    stuckCount++;
                    Respawn();
                    stuckFor = 0;
                }

                // -------- out of bounds
                bool inMind = Game.Lighting.MindActive;
                if (p.y < -2f || p.x < -21.2f || p.z < -26.5f || p.z > 24.5f || (p.x > 20.5f && !inMind))
                {
                    escapes++;
                    AddFinding("error", "out-of-bounds", $"Player left the playable area (y {p.y:0.0}).", Game.Player.transform, p);
                    Respawn();
                }

                // -------- poke whatever is in front
                if (now > nextPoke)
                {
                    nextPoke = now + Rand(0.8f, 2.5f);
                    Poke();
                }

                // -------- flip things
                if (now > nextToggle)
                {
                    nextToggle = now + Rand(5f, 15f);
                    Toggle();
                    if (shotsLeft-- > 0) yield return Shot($"monkey_{seed}_{shotsLeft}");
                }

                if (now > nextTeleport)
                {
                    nextTeleport = now + Rand(25f, 60f);
                    Respawn();
                }
                yield return null;
            }
        }

        void Mark(Vector3 p)
        {
            int x = Mathf.FloorToInt((p.x - MinX) / Cell), z = Mathf.FloorToInt((p.z - MinZ) / Cell);
            if (x >= 0 && z >= 0 && x < W && z < H) visits[x, z]++;
        }

        void Respawn()
        {
            teleports++;
            var s = Spawns[rng.Next(Spawns.Length)];
            QaUtil.Teleport(s + new Vector3(Rand(-0.5f, 0.5f), 0, Rand(-0.5f, 0.5f)), Rand(0, 360));
        }

        static int InteractMask => ~((1 << Game.LayerIgnoreRaycast) | (1 << Game.LayerPlayerBody) | (1 << Game.LayerViewmodel) |
                                     (1 << Game.LayerMemory) | (1 << Game.LayerMirror));

        void Poke()
        {
            var camT = Game.MainCamera.transform;
            Interactable target = null;
            if (Physics.Raycast(camT.position, camT.forward, out var hit, 2.1f, InteractMask, QueryTriggerInteraction.Collide))
                target = hit.collider.GetComponentInParent<Interactable>();
            if (target == null)
            {
                // look for anything close and turn to it, as a curious player would
                var near = Physics.OverlapSphere(Game.Player.transform.position + Vector3.up, 2f, InteractMask, QueryTriggerInteraction.Collide);
                if (near.Length == 0) return;
                var c = near[rng.Next(near.Length)];
                target = c.GetComponentInParent<Interactable>();
                if (target == null) return;
                QaUtil.AimAt(c.bounds.center);
            }
            if (!target.isActiveAndEnabled || !target.CanInteract) return;
            try
            {
                if (target is InspectableObject item && Game.Interactor.Held != null) Game.Interactor.Drop();
                target.Interact();
                interactions++;
                var key = target.GetType().Name + " " + target.name;
                touched[key] = touched.TryGetValue(key, out var n) ? n + 1 : 1;
            }
            catch (System.Exception e)
            {
                AddFinding("error", "exception", $"{target.GetType().Name}.Interact threw {e.GetType().Name}: {e.Message}", target.transform, target.transform.position);
            }
            // occasionally drop what we are holding
            if (Game.Interactor.Held != null && rng.NextDouble() < 0.3) Game.Interactor.Drop();
        }

        void Toggle()
        {
            switch (rng.Next(5))
            {
                case 0: QaUtil.LightsAll(rng.NextDouble() < 0.5); break;
                case 1:
                    var t = Game.Player.GetComponent<Torch>();
                    if (t != null) t.Set(!t.On);
                    break;
                case 2:
                    if (Game.Interactor.Held is OilLamp lamp) lamp.Toggle();
                    break;
                case 3:
                    if (rng.NextDouble() < 0.3) Game.Lighting.SetPower(!Game.Lighting.PowerOn);
                    break;
                case 4:
                    Game.Perception.Add(rng.Next(1, 6), "monkey_" + rng.Next());
                    break;
            }
        }

        static void ForcePlay()
        {
            switch (Game.Mode)
            {
                case InputMode.Inspect: Game.Inspector.Close(); break;
                case InputMode.Computer:
                    foreach (var c in Object.FindObjectsByType<CatalogueComputer>(FindObjectsSortMode.None))
                        if (QaUtil.Get<bool>(c, "active")) QaUtil.Call(c, "StandUp");
                    break;
                case InputMode.Reading:
                    try { QaUtil.Set(Game.Hud, "doc", null); } catch (System.MissingFieldException) { }
                    break;
            }
            if (Game.Mode != InputMode.Play && Game.Mode != InputMode.Ended) Game.Mode = InputMode.Play;
            Time.timeScale = 1f;
        }

        protected override void BeforeWrite()
        {
            int visited = 0;
            for (int x = 0; x < W; x++) for (int z = 0; z < H; z++) if (visits[x, z] > 0) visited++;
            report.extras.Add($"{interactions} interactions, {teleports} respawns, {stuckCount} stuck, {escapes} escapes, {softLocks} soft-locks");
            report.extras.Add($"visited {visited} cells of {Cell} m ({visited * Cell * Cell:0} m²)");
            var top = new List<KeyValuePair<string, int>>(touched);
            top.Sort((a, b) => b.Value.CompareTo(a.Value));
            report.extras.Add("touched: " + string.Join(", ", top.ConvertAll(k => $"{k.Key}×{k.Value}")));
            File.WriteAllBytes(Path.Combine(Dir, "coverage.png"), Heatmap().EncodeToPNG());
            report.extras.Add("coverage.png: grey = solid at waist height, dark = open, yellow→red = time spent, magenta = stuck");
        }

        Texture2D Heatmap()
        {
            const int S = 4;
            var tex = new Texture2D(W * S, H * S, TextureFormat.RGB24, false);
            int max = 1;
            for (int x = 0; x < W; x++) for (int z = 0; z < H; z++) max = Mathf.Max(max, visits[x, z]);
            for (int x = 0; x < W; x++)
                for (int z = 0; z < H; z++)
                {
                    var c = new Vector3(MinX + (x + 0.5f) * Cell, 1f, MinZ + (z + 0.5f) * Cell);
                    Color col = Physics.CheckBox(c, new Vector3(Cell * 0.45f, 0.4f, Cell * 0.45f), Quaternion.identity, ~0, QueryTriggerInteraction.Ignore)
                        ? new Color(0.45f, 0.45f, 0.45f) : new Color(0.08f, 0.08f, 0.1f);
                    if (visits[x, z] > 0) col = Color.Lerp(new Color(1f, 0.9f, 0.2f), new Color(0.9f, 0.1f, 0.05f), Mathf.Log(visits[x, z]) / Mathf.Log(max + 1));
                    for (int i = 0; i < S; i++) for (int j = 0; j < S; j++) tex.SetPixel(x * S + i, z * S + j, col);
                }
            foreach (var f in report.findings)
            {
                int x = Mathf.FloorToInt((f.position.x - MinX) / Cell) * S, z = Mathf.FloorToInt((f.position.z - MinZ) / Cell) * S;
                for (int i = -3; i <= 3; i++) for (int j = -3; j <= 3; j++)
                    if (x + i >= 0 && z + j >= 0 && x + i < tex.width && z + j < tex.height) tex.SetPixel(x + i, z + j, Color.magenta);
            }
            tex.Apply();
            return tex;
        }
    }
}
#endif
