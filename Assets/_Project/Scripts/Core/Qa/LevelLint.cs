#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace ThirdLamp.Qa
{
    /// <summary>
    /// Static checks on the built villa: hovering props, wall decals over openings, z-fighting faces,
    /// interpenetrating props, blocked doorways, interactables the player can't reach, and event / texture
    /// integrity. Writes findings plus a reachability map.
    /// </summary>
    public class LevelLint : QaRunner
    {
        protected override string Kind => "lint";
        protected override float MaxSeconds => 300f;

        Transform world;

        protected override IEnumerator Run()
        {
            QaUtil.SkipIntro();
            Game.Director.enabled = false;
            Game.State.clockRunning = false;
            // look at the house as the player first finds it, with the lamp's world hidden
            yield return null;
            Physics.SyncTransforms();
            world = Object.FindAnyObjectByType<SliceBootstrap>().transform.Find("World");

            Check("hovering props", Hovering);
            yield return null;
            Check("wall-mounted over openings", OverOpenings);
            yield return null;
            Check("z-fighting", ZFighting);
            yield return null;
            Check("interpenetration", Interpenetration);
            yield return null;
            Check("doorway clearance", Doorways);
            yield return null;
            Check("reachability", Reachability);
            yield return null;
            Check("event ids", EventIds);
            Check("texture slots", TextureSlots);
            yield return Shot("lint_start_view");
        }

        void Check(string name, System.Action body)
        {
            int before = report.findings.Count;
            float t0 = Time.realtimeSinceStartup;
            try
            {
                body();
                report.steps.Add(new StepResult { name = name, status = "PASS", seconds = Time.realtimeSinceStartup - t0, detail = $"{report.findings.Count - before} findings" });
                report.passed++;
            }
            catch (System.Exception e)
            {
                report.steps.Add(new StepResult { name = name, status = "FAIL", seconds = Time.realtimeSinceStartup - t0, detail = e.GetType().Name + ": " + e.Message });
                report.failed++;
            }
        }

        // ------------------------------------------------------------ helpers

        IEnumerable<Transform> PropGroups()
        {
            foreach (Transform room in world)
            {
                if (room.name == "House" || room.name == "Dressing" || room.name == "MindCorridor" || room.name == "Backdrop" ||
                    room.name == "Player" || room.name == "ReflectionBody" || room.name == "Zones") continue;
                foreach (Transform prop in room) yield return prop;
            }
        }

        static Bounds? RenderBounds(Transform t)
        {
            Bounds? b = null;
            foreach (var r in t.GetComponentsInChildren<Renderer>())
            {
                if (!r.enabled || !r.gameObject.activeInHierarchy || r is ParticleSystemRenderer) continue;
                if (b == null) b = r.bounds; else { var x = b.Value; x.Encapsulate(r.bounds); b = x; }
            }
            return b;
        }

        static bool IsOwn(Collider c, Transform group) => c.transform == group || c.transform.IsChildOf(group);

        // ------------------------------------------------------------ checks

        /// <summary>Props whose lowest point hangs 1.5–50 cm above whatever is below them.</summary>
        void Hovering()
        {
            foreach (var g in PropGroups())
            {
                var b = RenderBounds(g);
                if (b == null) continue;
                var bb = b.Value;
                if (bb.size.y < 0.005f) continue; // flat decals and rugs are checked elsewhere
                float best = float.MaxValue;
                foreach (var off in new[] { Vector3.zero, new Vector3(bb.extents.x * 0.6f, 0, 0), new Vector3(-bb.extents.x * 0.6f, 0, 0), new Vector3(0, 0, bb.extents.z * 0.6f), new Vector3(0, 0, -bb.extents.z * 0.6f) })
                {
                    var o = new Vector3(bb.center.x, bb.min.y + 0.005f, bb.center.z) + off;
                    foreach (var h in Physics.RaycastAll(o, Vector3.down, 0.6f, ~0, QueryTriggerInteraction.Ignore))
                        if (!IsOwn(h.collider, g)) best = Mathf.Min(best, h.distance - 0.005f);
                }
                if (best > 0.015f && best < 0.5f)
                    AddFinding(best > 0.05f ? "error" : "warning", "hovering", $"Lowest point is {best * 100f:0} cm above the surface below.", g, new Vector3(bb.center.x, bb.min.y, bb.center.z));
            }
        }

        /// <summary>Decals, pictures and notes on walls: probe from several points into the surface.</summary>
        void OverOpenings()
        {
            foreach (var r in world.GetComponentsInChildren<MeshRenderer>())
            {
                if (!r.enabled) continue;
                var n = r.name;
                bool decal = n.StartsWith("Decal_");
                bool picture = n == "Image" || n == "Photo" || n == "Canvas";
                if (!decal && !picture) continue;
                var t = r.transform;
                var into = t.forward; // quads face away from the surface they sit on
                if (Mathf.Abs(into.x) + Mathf.Abs(into.z) > 0.2f && Mathf.Abs(into.x) > 0.2f && Mathf.Abs(into.z) > 0.2f) continue; // corner cobwebs
                int miss = 0, total = 0;
                foreach (var uv in new[] { new Vector2(0, 0), new Vector2(-0.35f, -0.35f), new Vector2(0.35f, -0.35f), new Vector2(-0.35f, 0.35f), new Vector2(0.35f, 0.35f) })
                {
                    var p = t.TransformPoint(new Vector3(uv.x, uv.y, 0));
                    total++;
                    if (!Physics.Raycast(p - into * 0.02f, into, 0.12f, ~0, QueryTriggerInteraction.Ignore)) miss++;
                }
                if (miss >= 2)
                    AddFinding(miss >= 4 ? "error" : "warning", "over-opening", $"{miss}/{total} probe points have nothing behind them (hanging over a door, window or edge).", t, t.position);
            }
        }

        struct Face { public int axis; public float sign, coord; public Rect rect; public Material mat; public Transform t; }

        /// <summary>Axis-aligned cube and quad faces that sit in the same plane, face the same way, overlap, and use different materials.</summary>
        void ZFighting()
        {
            var buckets = new Dictionary<(int, int, int), List<Face>>();
            foreach (var mf in world.GetComponentsInChildren<MeshFilter>())
            {
                var r = mf.GetComponent<MeshRenderer>();
                if (r == null || !r.enabled || mf.sharedMesh == null) continue;
                var mesh = mf.sharedMesh.name;
                bool cube = mesh.StartsWith("Cube"), quad = mesh.StartsWith("Quad");
                if (!cube && !quad) continue;
                var t = mf.transform;
                var e = t.rotation.eulerAngles;
                if (!Axis(e.x) || !Axis(e.y) || !Axis(e.z)) continue;
                var bb = r.bounds;
                for (int axis = 0; axis < 3; axis++)
                {
                    for (int s = -1; s <= 1; s += 2)
                    {
                        if (quad)
                        {
                            var n = -t.forward; // visible side
                            if (Mathf.Abs(n[axis]) < 0.9f || Mathf.Sign(n[axis]) != s) continue;
                        }
                        else if (bb.size[axis] < 0.0005f) continue;
                        float coord = s > 0 ? bb.max[axis] : bb.min[axis];
                        if (quad) coord = bb.center[axis];
                        int a1 = (axis + 1) % 3, a2 = (axis + 2) % 3;
                        var rect = Rect.MinMaxRect(bb.min[a1], bb.min[a2], bb.max[a1], bb.max[a2]);
                        var f = new Face { axis = axis, sign = s, coord = coord, rect = rect, mat = r.sharedMaterial, t = t };
                        var key = (axis * 2 + (s > 0 ? 1 : 0), Mathf.RoundToInt(coord * 1000f), 0);
                        if (!buckets.TryGetValue(key, out var list)) buckets[key] = list = new List<Face>();
                        list.Add(f);
                    }
                }
            }
            var reported = new HashSet<(Transform, Transform)>();
            foreach (var kv in buckets)
            {
                var near = new List<Face>(kv.Value);
                if (buckets.TryGetValue((kv.Key.Item1, kv.Key.Item2 + 1, 0), out var up)) near.AddRange(up);
                var list = kv.Value;
                for (int i = 0; i < list.Count; i++)
                    for (int j = 0; j < near.Count; j++)
                    {
                        var a = list[i]; var b = near[j];
                        if (a.t == b.t || a.mat == b.mat || Mathf.Abs(a.coord - b.coord) > 0.0012f) continue;
                        if (!a.rect.Overlaps(b.rect)) continue;
                        var ov = Rect.MinMaxRect(Mathf.Max(a.rect.xMin, b.rect.xMin), Mathf.Max(a.rect.yMin, b.rect.yMin), Mathf.Min(a.rect.xMax, b.rect.xMax), Mathf.Min(a.rect.yMax, b.rect.yMax));
                        if (ov.width * ov.height < 0.0004f) continue;
                        if (!reported.Add((a.t, b.t)) || reported.Contains((b.t, a.t))) continue;
                        AddFinding("warning", "z-fighting", $"Coplanar with `{QaUtil.PathOf(b.t)}` over {ov.width * ov.height * 10000f:0} cm² (flicker).", a.t, a.t.position);
                    }
            }
        }

        static bool Axis(float deg) { float m = Mathf.Repeat(deg, 90f); return m < 0.5f || m > 89.5f; }

        /// <summary>Solid props pushing into each other or into walls by more than 3 cm.</summary>
        void Interpenetration()
        {
            var cols = new List<Collider>();
            foreach (var c in world.GetComponentsInChildren<Collider>())
                if (c.enabled && !c.isTrigger && c.gameObject.activeInHierarchy && c.GetComponentInParent<Openable>() == null) cols.Add(c);
            var seen = new HashSet<(Collider, Collider)>();
            foreach (var a in cols)
            {
                var ta = TopGroup(a.transform);
                foreach (var b in Physics.OverlapBox(a.bounds.center, a.bounds.extents, Quaternion.identity, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (b == a || !cols.Contains(b)) continue;
                    var tb = TopGroup(b.transform);
                    if (ta == tb) continue;
                    bool aHouse = ta.parent != null && ta.parent.name == "House" || ta.name.StartsWith("Wall_") || ta.name.StartsWith("Floor_");
                    bool bHouse = tb.parent != null && tb.parent.name == "House" || tb.name.StartsWith("Wall_") || tb.name.StartsWith("Floor_");
                    if (aHouse && bHouse) continue;
                    if (!seen.Add((a, b)) || seen.Contains((b, a))) continue;
                    if (Physics.ComputePenetration(a, a.transform.position, a.transform.rotation, b, b.transform.position, b.transform.rotation, out _, out var depth) && depth > 0.03f)
                        AddFinding(depth > 0.1f ? "error" : "warning", "interpenetration", $"{depth * 100f:0} cm into `{QaUtil.PathOf(b.transform)}`.", a.transform, a.bounds.center);
                }
            }
        }

        Transform TopGroup(Transform t)
        {
            while (t.parent != null && t.parent != world && t.parent.parent != world) t = t.parent;
            return t;
        }

        /// <summary>A player-sized capsule must fit through every doorway (door leaves themselves don't count).</summary>
        void Doorways()
        {
            foreach (var o in VillaBuilder.Openings)
            {
                var across = o.alongX ? Vector3.forward : Vector3.right;
                foreach (float s in new[] { -0.45f, 0f, 0.45f })
                {
                    var c = o.centre + across * s;
                    var hits = Physics.OverlapCapsule(c + Vector3.up * 0.35f, c + Vector3.up * 1.5f, 0.27f, ~(1 << Game.LayerIgnoreRaycast), QueryTriggerInteraction.Ignore);
                    foreach (var h in hits)
                    {
                        if (h.GetComponentInParent<Openable>() != null || h.GetComponentInParent<PlayerController>() != null) continue;
                        AddFinding("error", "doorway", $"Doorway in {o.wall} ({o.width:0.00} m) is blocked by `{QaUtil.PathOf(h.transform)}`.", h.transform, c);
                        goto next;
                    }
                }
                next:;
            }
        }

        // reachability grid
        const float RX0 = -21f, RX1 = 20f, RZ0 = -26f, RZ1 = 24f, RC = 0.3f;

        /// <summary>
        /// Flood-fill of where a player capsule can stand, from the start position. Every active
        /// interactable needs a reachable spot within 2.1 m with a clear line from eye height.
        /// </summary>
        void Reachability()
        {
            int w = Mathf.CeilToInt((RX1 - RX0) / RC), h = Mathf.CeilToInt((RZ1 - RZ0) / RC);
            var state = new sbyte[w, h]; // 0 unknown, 1 open, -1 blocked, 2 reached
            int mask = ~((1 << Game.LayerIgnoreRaycast) | (1 << Game.LayerMemory) | (1 << Game.LayerPlayerBody) | (1 << Game.LayerViewmodel) | (1 << Game.LayerMirror));
            float[,] floor = new float[w, h];
            for (int x = 0; x < w; x++)
                for (int z = 0; z < h; z++)
                {
                    var p = new Vector3(RX0 + (x + 0.5f) * RC, 2.5f, RZ0 + (z + 0.5f) * RC);
                    if (!Physics.Raycast(p, Vector3.down, out var down, 3.5f, mask, QueryTriggerInteraction.Ignore) || down.point.y > 0.35f) { state[x, z] = -1; continue; }
                    floor[x, z] = down.point.y;
                    var f = down.point;
                    bool blocked = false;
                    foreach (var c in Physics.OverlapCapsule(f + Vector3.up * 0.35f, f + Vector3.up * 1.55f, 0.26f, mask, QueryTriggerInteraction.Ignore))
                        if (c.GetComponentInParent<Openable>() == null && c.GetComponentInParent<PlayerController>() == null) { blocked = true; break; }
                    state[x, z] = (sbyte)(blocked ? -1 : 1);
                }

            var start = Game.Player.transform.position;
            int sx = Mathf.Clamp(Mathf.FloorToInt((start.x - RX0) / RC), 0, w - 1), sz = Mathf.Clamp(Mathf.FloorToInt((start.z - RZ0) / RC), 0, h - 1);
            var q = new Queue<Vector2Int>();
            if (state[sx, sz] == 1) { state[sx, sz] = 2; q.Enqueue(new Vector2Int(sx, sz)); }
            else AddFinding("error", "reachability", "The player start position is not standable.", Game.Player.transform, start);
            var dirs = new[] { new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1) };
            while (q.Count > 0)
            {
                var c = q.Dequeue();
                foreach (var d in dirs)
                {
                    var n = c + d;
                    if (n.x < 0 || n.y < 0 || n.x >= w || n.y >= h || state[n.x, n.y] != 1) continue;
                    if (Mathf.Abs(floor[n.x, n.y] - floor[c.x, c.y]) > 0.3f) continue;
                    state[n.x, n.y] = 2;
                    q.Enqueue(n);
                }
            }

            int reached = 0, open = 0;
            for (int x = 0; x < w; x++) for (int z = 0; z < h; z++) { if (state[x, z] == 2) reached++; if (state[x, z] >= 1) open++; }
            report.extras.Add($"reachability: {reached} of {open} standable cells ({RC} m) reachable from the start ({100f * reached / Mathf.Max(1, open):0}%)");

            foreach (var it in world.GetComponentsInChildren<Interactable>())
            {
                if (!it.isActiveAndEnabled) continue;
                var closedParent = it.transform.parent != null ? it.transform.parent.GetComponentInParent<Openable>() : null;
                var col = it.GetComponentInChildren<Collider>();
                if (col == null) { AddFinding("warning", "reachability", "Interactable has no collider, so it can never be focused.", it.transform, it.transform.position); continue; }
                var target = col.bounds.center;
                bool ok = false;
                int cx = Mathf.FloorToInt((target.x - RX0) / RC), cz = Mathf.FloorToInt((target.z - RZ0) / RC);
                int rr = Mathf.CeilToInt(2.1f / RC);
                for (int x = cx - rr; x <= cx + rr && !ok; x++)
                    for (int z = cz - rr; z <= cz + rr && !ok; z++)
                    {
                        if (x < 0 || z < 0 || x >= w || z >= h || state[x, z] != 2) continue;
                        var eye = new Vector3(RX0 + (x + 0.5f) * RC, floor[x, z] + 1.62f, RZ0 + (z + 0.5f) * RC);
                        var d = target - eye;
                        if (d.magnitude > 2.1f) continue;
                        if (Physics.Raycast(eye, d.normalized, out var hit, 2.2f, mask, QueryTriggerInteraction.Collide) && hit.collider.GetComponentInParent<Interactable>() == it) ok = true;
                    }
                if (!ok)
                {
                    bool expected = closedParent != null && closedParent != it;
                    AddFinding(expected ? "info" : "error", "reachability",
                        expected ? $"Not reachable until `{closedParent.name}` is opened (expected)." : $"{it.GetType().Name} can't be reached and looked at from anywhere the player can stand.",
                        it.transform, target);
                }
            }
            File.WriteAllBytes(Path.Combine(Dir, "reachability.png"), Map(state, w, h).EncodeToPNG());
            report.extras.Add("reachability.png: green = reachable, orange = standable but unreachable, grey = blocked, red = findings");
        }

        Texture2D Map(sbyte[,] state, int w, int h)
        {
            const int S = 3;
            var tex = new Texture2D(w * S, h * S, TextureFormat.RGB24, false);
            for (int x = 0; x < w; x++)
                for (int z = 0; z < h; z++)
                {
                    var c = state[x, z] == 2 ? new Color(0.2f, 0.45f, 0.25f) : state[x, z] == 1 ? new Color(0.9f, 0.55f, 0.1f) : new Color(0.25f, 0.25f, 0.27f);
                    for (int i = 0; i < S; i++) for (int j = 0; j < S; j++) tex.SetPixel(x * S + i, z * S + j, c);
                }
            foreach (var f in report.findings)
            {
                if (f.severity == "info") continue;
                int x = Mathf.FloorToInt((f.position.x - RX0) / RC) * S, z = Mathf.FloorToInt((f.position.z - RZ0) / RC) * S;
                for (int i = -2; i <= 2; i++) for (int j = -2; j <= 2; j++)
                    if (x + i >= 0 && z + j >= 0 && x + i < tex.width && z + j < tex.height) tex.SetPixel(x + i, z + j, Color.red);
            }
            tex.Apply();
            return tex;
        }

        /// <summary>Every world id an event condition or action refers to must be registered.</summary>
        void EventIds()
        {
            var registered = new HashSet<string>();
            foreach (var kv in Game.World.All) registered.Add(kv.Key);
            foreach (var e in Game.Director.Events)
            {
                foreach (var c in e.conditions) CheckIds(e.id, c, registered);
                foreach (var a in e.actions) CheckIds(e.id, a, registered);
            }
        }

        void CheckIds(string evt, object o, HashSet<string> registered)
        {
            if (o == null) return;
            foreach (var f in o.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (f.FieldType == typeof(string) && (f.Name == "targetId" || f.Name == "doorId" || f.Name == "atTargetId"))
                {
                    var id = (string)f.GetValue(o);
                    if (!string.IsNullOrEmpty(id) && !registered.Contains(id))
                        AddFinding("error", "event-id", $"Event `{evt}` uses world id `{id}`, which is not registered.", null, Vector3.zero);
                }
                else if (f.FieldType.IsArray && typeof(Condition).IsAssignableFrom(f.FieldType.GetElementType()))
                    foreach (var c in (Condition[])f.GetValue(o) ?? new Condition[0]) CheckIds(evt, c, registered);
            }
        }

        [System.Serializable] class Slot { public string name; }
        [System.Serializable] class Manifest { public Slot[] slots; }

        void TextureSlots()
        {
            var path = Path.Combine(Application.dataPath, "_Project/Art/asset_manifest.json");
            var m = JsonUtility.FromJson<Manifest>(File.ReadAllText(path));
            int ok = 0;
            foreach (var s in m.slots)
            {
                if (Tex.Exists(s.name)) ok++;
                else AddFinding("error", "texture", $"Manifest slot `{s.name}` has no texture in Resources.", null, Vector3.zero);
            }
            report.extras.Add($"texture slots: {ok}/{m.slots.Length} present");
        }
    }
}
#endif
