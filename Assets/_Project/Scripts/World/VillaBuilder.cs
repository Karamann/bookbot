using System.Collections.Generic;
using UnityEngine;

namespace ThirdLamp
{
    /// <summary>
    /// Builds the whole slice at runtime from primitives: villa, grounds, props, interactables,
    /// lights, zones and the player. Placeholder geometry; textures come from <see cref="Tex"/> slots.
    /// Layout (metres): house x∈[-8,8], z∈[0,14]. Living SW, kitchen SE, hallway z∈[6,8] running
    /// west→east to the basement door at x=6, study NW, bathroom and storage NE. Car to the south.
    /// </summary>
    public partial class VillaBuilder
    {
        public const float WallH = 2.8f;
        const float T = 0.2f;

        readonly Transform root;
        Transform cur;

        public Vector3 PlayerStart = new Vector3(-2.7f, 0f, -13.2f);
        public float PlayerStartYaw = 8f;

        public VillaBuilder(Transform root)
        {
            this.root = root;
            cur = root;
        }

        public void Build()
        {
            Environment();
            Grounds();
            Shell();
            Living();
            Kitchen();
            Hallway();
            Study();
            Bathroom();
            Storage();
            MindGeometry();
            Zones();
            Player();
            foreach (var s in Object.FindObjectsByType<LightSwitch>(FindObjectsSortMode.None)) s.RefreshVisual();
        }

        // =====================================================================
        // helpers
        // =====================================================================

        struct Op
        {
            public float center, width, bottom, top;
            public bool window;
        }

        static Op DoorOp(float center, float width = 0.9f, float top = 2.1f) => new Op { center = center, width = width, bottom = 0f, top = top };
        static Op WindowOp(float center, float width = 1.1f, float bottom = 0.9f, float top = 2.0f) => new Op { center = center, width = width, bottom = bottom, top = top, window = true };

        Transform Group(string name, Transform parent = null)
        {
            var g = new GameObject(name).transform;
            g.SetParent(parent != null ? parent : root, false);
            return g;
        }

        GameObject Prim(PrimitiveType type, string name, Vector3 pos, Vector3 scale, Material mat, Transform parent = null, bool collider = true, Quaternion? rot = null)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent != null ? parent : cur, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = rot ?? Quaternion.identity;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            if (!collider) Object.Destroy(go.GetComponent<Collider>());
            return go;
        }

        GameObject Box(string name, Vector3 center, Vector3 size, Material mat, Transform parent = null, bool collider = true, Quaternion? rot = null)
            => Prim(PrimitiveType.Cube, name, center, size, mat, parent, collider, rot);

        GameObject Cyl(string name, Vector3 center, float diameter, float height, Material mat, Transform parent = null, bool collider = true)
            => Prim(PrimitiveType.Cylinder, name, center, new Vector3(diameter, height / 2f, diameter), mat, parent, collider);

        /// <summary>Quad whose visible face points away from <paramref name="intoWall"/>.</summary>
        GameObject Quad(string name, Vector3 center, Vector2 size, Vector3 intoWall, Material mat, Transform parent = null)
            => Prim(PrimitiveType.Quad, name, center, new Vector3(size.x, size.y, 1f), mat, parent, false, Quaternion.LookRotation(intoWall, Vector3.up));

        /// <summary>Material tiled by metres for a box of this size.</summary>
        static Material Surf(string tex, Vector3 size, float tileMeters = 1f, float smooth = 0.1f, Color? tint = null)
        {
            float a, b;
            float minDim = Mathf.Min(size.x, Mathf.Min(size.y, size.z));
            if (Mathf.Approximately(minDim, size.y)) { a = size.x; b = size.z; }
            else { a = Mathf.Max(size.x, size.z); b = size.y; }
            float tx = Mathf.Max(0.5f, Mathf.Round(a / tileMeters * 2f) / 2f);
            float ty = Mathf.Max(0.5f, Mathf.Round(b / tileMeters * 2f) / 2f);
            return Mats.Lit(tex, tint ?? Color.white, smooth, tx, ty);
        }

        GameObject TexBox(string name, Vector3 center, Vector3 size, string tex, float tile = 1f, float smooth = 0.1f, Transform parent = null, bool collider = true)
            => Box(name, center, size, Surf(tex, size, tile, smooth), parent, collider);

        /// <summary>Axis-aligned wall from (x0,z0) to (x1,z1) with door/window openings (offsets from the start).</summary>
        GameObject Wall(string name, float x0, float z0, float x1, float z1, string tex, params Op[] ops)
        {
            var g = Group(name, cur).gameObject;
            bool alongX = Mathf.Approximately(z0, z1);
            float a0 = alongX ? x0 : z0, a1 = alongX ? x1 : z1, fixedC = alongX ? z0 : x0;
            var sorted = new List<Op>(ops);
            sorted.Sort((p, q) => p.center.CompareTo(q.center));

            void Piece(float p0, float p1, float y0, float y1)
            {
                if (p1 - p0 < 0.01f || y1 - y0 < 0.01f) return;
                float c = (p0 + p1) / 2f, len = p1 - p0;
                var pos = alongX ? new Vector3(c, (y0 + y1) / 2f, fixedC) : new Vector3(fixedC, (y0 + y1) / 2f, c);
                var size = alongX ? new Vector3(len, y1 - y0, T) : new Vector3(T, y1 - y0, len);
                TexBox("Piece", pos, size, tex, 1.5f, 0.05f, g.transform);
            }

            float cursor = a0;
            foreach (var op in sorted)
            {
                float s = a0 + op.center - op.width / 2f, e = a0 + op.center + op.width / 2f;
                Piece(cursor, s, 0f, WallH);
                Piece(s, e, 0f, op.bottom);
                Piece(s, e, op.top, WallH);
                cursor = e;
                if (op.window) WindowFrame(g.transform, alongX, a0 + op.center, fixedC, op);
            }
            Piece(cursor, a1, 0f, WallH);
            return g;
        }

        void WindowFrame(Transform parent, bool alongX, float c, float fixedC, Op op)
        {
            var wood = Mats.Lit("wood_door", new Color(0.8f, 0.8f, 0.8f), 0.2f);
            float h = op.top - op.bottom, cy = (op.top + op.bottom) / 2f;
            Vector3 P(float along, float y) => alongX ? new Vector3(along, y, fixedC) : new Vector3(fixedC, y, along);
            Vector3 S(float along, float y, float depth) => alongX ? new Vector3(along, y, depth) : new Vector3(depth, y, along);
            Box("Sill", P(c, op.bottom - 0.02f), S(op.width + 0.1f, 0.05f, T + 0.08f), wood, parent);
            Box("Head", P(c, op.top), S(op.width, 0.05f, 0.08f), wood, parent, false);
            Box("JambA", P(c - op.width / 2f + 0.025f, cy), S(0.05f, h, 0.08f), wood, parent, false);
            Box("JambB", P(c + op.width / 2f - 0.025f, cy), S(0.05f, h, 0.08f), wood, parent, false);
            Box("Mullion", P(c, cy), S(0.04f, h, 0.06f), wood, parent, false);
            Box("Transom", P(c, op.bottom + h * 0.62f), S(op.width, 0.04f, 0.06f), wood, parent, false);
            // Invisible pane blocks walking through but not sight.
            var pane = Box("Pane", P(c, cy), S(op.width, h, 0.02f), wood, parent, true);
            Object.Destroy(pane.GetComponent<Renderer>());
            pane.layer = Game.LayerIgnoreRaycast;
        }

        Openable Door(string id, Vector3 hinge, bool alongX, float width, float openSign, bool startOpen, string displayName = "door", Material mat = null)
        {
            var g = Group("Door_" + id, cur);
            g.localPosition = hinge;
            mat = mat != null ? mat : Mats.Lit("wood_door", Color.white, 0.25f);
            var slabPos = alongX ? new Vector3(width / 2f, 1.04f, 0f) : new Vector3(0f, 1.04f, width / 2f);
            var slabSize = alongX ? new Vector3(width - 0.02f, 2.08f, 0.05f) : new Vector3(0.05f, 2.08f, width - 0.02f);
            Box("Slab", slabPos, slabSize, mat, g);
            var brass = Mats.Color(new Color(0.7f, 0.55f, 0.25f), 0.7f);
            var hp = alongX ? new Vector3(width - 0.12f, 1.0f, 0f) : new Vector3(0f, 1.0f, width - 0.12f);
            Prim(PrimitiveType.Sphere, "Handle", hp, new Vector3(0.07f, 0.07f, 0.12f), brass, g, false,
                alongX ? Quaternion.identity : Quaternion.Euler(0, 90, 0));
            var o = g.gameObject.AddComponent<Openable>();
            o.displayName = displayName;
            o.openEuler = new Vector3(0, 95f * openSign, 0);
            o.Setup(g, startOpen);
            Game.World.Register(id, g.gameObject);
            return o;
        }

        Light PointLight(string name, Vector3 pos, Color color, float intensity, float range, bool shadows, Transform parent = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent != null ? parent : cur, false);
            go.transform.localPosition = pos;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            l.shadows = shadows ? LightShadows.Soft : LightShadows.None;
            l.shadowStrength = 0.85f;
            return l;
        }

        /// <summary>Ceiling pendant: cord, shade, bulb (bulb material swaps with the light state).</summary>
        void Pendant(string group, Vector3 pos, Color color, float intensity, float range, bool shadows, bool shade = true)
        {
            var g = Group("Pendant_" + group, cur);
            g.localPosition = pos;
            Box("Cord", new Vector3(0, (WallH - pos.y) / 2f, 0), new Vector3(0.01f, WallH - pos.y, 0.01f), Mats.Color(Color.black), g, false);
            var bulb = Prim(PrimitiveType.Sphere, "Bulb", Vector3.zero, Vector3.one * 0.09f, Game.Lighting.bulbOffMaterial, g, false);
            if (shade) Cyl("Shade", new Vector3(0, 0.08f, 0), 0.36f, 0.14f, Mats.Color(new Color(0.75f, 0.68f, 0.52f)), g, false);
            var l = PointLight("Light", new Vector3(0, -0.05f, 0), color, intensity, range, shadows, g);
            Game.Lighting.AddLight(group, l, bulb.GetComponent<Renderer>());
        }

        void Switch(string group, Vector3 pos, Vector3 intoWall)
        {
            var g = Group("Switch_" + group, cur);
            g.localPosition = pos;
            g.localRotation = Quaternion.LookRotation(intoWall);
            Box("Plate", Vector3.zero, new Vector3(0.08f, 0.12f, 0.015f), Mats.Color(new Color(0.9f, 0.88f, 0.82f)), g);
            var toggle = Group("Toggle", g);
            toggle.localPosition = new Vector3(0, 0, -0.012f);
            Box("Rocker", Vector3.zero, new Vector3(0.03f, 0.05f, 0.012f), Mats.Color(new Color(0.8f, 0.78f, 0.7f)), toggle, false);
            var sw = g.gameObject.AddComponent<LightSwitch>();
            sw.group = group;
            sw.toggle = toggle;
        }

        TextMesh Label3D(string text, Vector3 pos, Vector3 intoWall, float height, Color color, Transform parent = null)
        {
            var go = new GameObject("Label_" + text);
            go.transform.SetParent(parent != null ? parent : cur, false);
            go.transform.localPosition = pos;
            go.transform.localRotation = Quaternion.LookRotation(intoWall, Vector3.up);
            var tm = go.AddComponent<TextMesh>();
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tm.font = font;
            tm.text = text;
            tm.fontSize = 64;
            tm.characterSize = 0.01f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            if (font != null) go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            go.AddComponent<FitTextHeight>().height = height;
            return tm;
        }

        LampVisibility MindOnly(GameObject go, int minPerception = 0)
        {
            var v = go.AddComponent<LampVisibility>();
            v.mindOnly = true;
            v.minPerception = minPerception;
            Game.Lighting.RegisterLampObject(v);
            return v;
        }

        LampVisibility ReasonOnly(GameObject go)
        {
            var v = go.AddComponent<LampVisibility>();
            v.mindOnly = false;
            Game.Lighting.RegisterLampObject(v);
            return v;
        }

        static void SetLayerRecursive(GameObject go, int layer)
        {
            foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = layer;
            // Things only lenses and mirrors see must not leave shadows for the eye.
            if (layer == Game.LayerMemory || layer == Game.LayerPlayerBody)
                foreach (var r in go.GetComponentsInChildren<Renderer>(true))
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        static void StripColliders(GameObject go)
        {
            foreach (var c in go.GetComponentsInChildren<Collider>(true)) Object.Destroy(c);
        }

        NoteReader Note(string name, Vector3 pos, Vector3 size, string tex, string verb, string title, string body, DocStyle style, int perception = 0, string flag = null, Quaternion? rot = null)
        {
            var go = Box(name, pos, size, Mats.Lit(tex, Color.white, 0.05f), null, true, rot);
            var n = go.AddComponent<NoteReader>();
            n.verb = verb;
            n.title = title;
            n.body = body;
            n.style = style;
            n.perception = perception;
            n.flagOnRead = flag;
            return n;
        }

        /// <summary>Tall, still figure. Only visible to the lens and mirrors (Memory layer).</summary>
        GameObject Figure(string id, Vector3 pos, float yaw, bool startActive)
        {
            var g = Group("Figure_" + id, cur);
            g.localPosition = pos;
            g.localRotation = Quaternion.Euler(0, yaw, 0);
            var coat = Mats.Color(new Color(0.07f, 0.065f, 0.06f), 0.05f);
            var skin = Mats.Color(new Color(0.62f, 0.58f, 0.52f), 0.1f);
            Prim(PrimitiveType.Capsule, "Body", new Vector3(0, 0.95f, 0), new Vector3(0.46f, 0.92f, 0.3f), coat, g, false);
            Prim(PrimitiveType.Sphere, "Head", new Vector3(0, 1.82f, 0.02f), new Vector3(0.2f, 0.25f, 0.22f), skin, g, false);
            Prim(PrimitiveType.Sphere, "Hair", new Vector3(0, 1.87f, -0.02f), new Vector3(0.21f, 0.2f, 0.21f), coat, g, false);
            Box("EyeL", new Vector3(-0.04f, 1.83f, 0.12f), new Vector3(0.025f, 0.012f, 0.01f), Mats.Color(Color.black), g, false);
            Box("EyeR", new Vector3(0.04f, 1.83f, 0.12f), new Vector3(0.025f, 0.012f, 0.01f), Mats.Color(Color.black), g, false);
            Prim(PrimitiveType.Capsule, "ArmL", new Vector3(-0.27f, 1.0f, 0), new Vector3(0.12f, 0.4f, 0.12f), coat, g, false);
            Prim(PrimitiveType.Capsule, "ArmR", new Vector3(0.27f, 1.0f, 0), new Vector3(0.12f, 0.4f, 0.12f), coat, g, false);
            SetLayerRecursive(g.gameObject, Game.LayerMemory);
            var a = g.gameObject.AddComponent<Apparition>();
            a.id = id;
            Game.World.Register(id, g.gameObject);
            g.gameObject.SetActive(startActive);
            return g.gameObject;
        }
    }

    /// <summary>Scales a TextMesh once so its rendered height matches a target in metres.</summary>
    public class FitTextHeight : MonoBehaviour
    {
        public float height = 0.05f;

        void LateUpdate()
        {
            var r = GetComponent<Renderer>();
            if (r == null) { enabled = false; return; }
            float current = r.bounds.size.y;
            if (current > 0.0001f)
            {
                transform.localScale *= height / current;
                enabled = false;
            }
        }
    }
}
