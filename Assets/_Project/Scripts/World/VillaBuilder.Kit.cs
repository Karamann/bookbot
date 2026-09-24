using UnityEngine;

namespace ThirdLamp
{
    /// <summary>
    /// Dressing kit: skirting, door casings, window glass, curtains and shutters, decals, sprites and
    /// foliage. Everything that needs a generated texture checks <see cref="Tex.Exists"/> first and is
    /// skipped when the slot is missing, so the villa still builds with only the base textures.
    /// </summary>
    public partial class VillaBuilder
    {
        static readonly Vector3 HouseCentre = new Vector3(0f, 0f, 7f);
        Material SkirtingMat => Mats.Lit("wood_dark", new Color(0.75f, 0.7f, 0.65f), 0.25f);
        Material CasingMat => Mats.Lit("wood_door", new Color(0.85f, 0.82f, 0.78f), 0.2f);

        // ---------------------------------------------------------------- architecture

        /// <summary>Skirting along one full-height wall piece, on the room side(s).</summary>
        void SkirtingFor(Transform parent, bool alongX, float p0, float p1, float fixedC, bool interior)
        {
            float len = p1 - p0, c = (p0 + p1) / 2f;
            if (len < 0.05f) return;
            foreach (float side in new[] { -1f, 1f })
            {
                if (!interior && !FacesInside(alongX, fixedC, side)) continue;
                float off = fixedC + side * (T / 2f + 0.008f);
                var pos = alongX ? new Vector3(c, 0.05f, off) : new Vector3(off, 0.05f, c);
                var size = alongX ? new Vector3(len, 0.1f, 0.016f) : new Vector3(0.016f, 0.1f, len);
                Box("Skirting", pos, size, SkirtingMat, parent, false);
            }
        }

        /// <summary>For an exterior wall: does the face on this side look into the house?</summary>
        static bool FacesInside(bool alongX, float fixedC, float side)
        {
            float centre = alongX ? HouseCentre.z : HouseCentre.x;
            return Mathf.Sign(centre - fixedC) == Mathf.Sign(side);
        }

        /// <summary>Architrave on both faces of a doorway and a lining inside the opening.</summary>
        void DoorCasing(Transform parent, bool alongX, float c, float fixedC, Op op)
        {
            const float w = 0.07f, d = 0.018f;
            float half = op.width / 2f;
            Vector3 P(float along, float y, float depthOff) => alongX ? new Vector3(along, y, fixedC + depthOff) : new Vector3(fixedC + depthOff, y, along);
            Vector3 S(float along, float y, float depth) => alongX ? new Vector3(along, y, depth) : new Vector3(depth, y, along);
            foreach (float side in new[] { -1f, 1f })
            {
                float off = side * (T / 2f + d / 2f);
                Box("CasingA", P(c - half - w / 2f, op.top / 2f, off), S(w, op.top, d), CasingMat, parent, false);
                Box("CasingB", P(c + half + w / 2f, op.top / 2f, off), S(w, op.top, d), CasingMat, parent, false);
                Box("CasingHead", P(c, op.top + w / 2f, off), S(op.width + 2f * w, w, d), CasingMat, parent, false);
            }
            // lining the reveal
            Box("LiningA", P(c - half + 0.006f, op.top / 2f, 0), S(0.012f, op.top, T), CasingMat, parent, false);
            Box("LiningB", P(c + half - 0.006f, op.top / 2f, 0), S(0.012f, op.top, T), CasingMat, parent, false);
            Box("LiningHead", P(c, op.top - 0.006f, 0), S(op.width, 0.012f, T), CasingMat, parent, false);
            Box("Threshold", P(c, 0.006f, 0), S(op.width, 0.012f, T + 0.04f), SkirtingMat, parent, false);
        }

        /// <summary>Glass, grime, and on exterior walls a net curtain inside and open shutters outside.</summary>
        void WindowDressing(Transform parent, bool alongX, float c, float fixedC, Op op, bool exterior)
        {
            float h = op.top - op.bottom, cy = (op.top + op.bottom) / 2f;
            Vector3 P(float along, float y, float depthOff) => alongX ? new Vector3(along, y, fixedC + depthOff) : new Vector3(fixedC + depthOff, y, along);
            Vector3 Normal(float side) => alongX ? new Vector3(0, 0, side) : new Vector3(side, 0, 0);

            if (Tex.Exists("window_grime"))
                foreach (float side in new[] { -1f, 1f })
                    Quad("Grime", P(c, cy, side * 0.012f), new Vector2(op.width - 0.06f, h - 0.06f), -Normal(side), Mats.Decal("window_grime", new Color(1, 1, 1, 0.75f)), parent);

            if (!exterior) return;
            float inSide = FacesInside(alongX, fixedC, 1f) ? 1f : -1f;

            if (Tex.Exists("curtain_net") && op.width >= 0.9f)
            {
                var net = Mats.Transparent("curtain_net", new Color(0.95f, 0.93f, 0.88f, 0.85f), 0.02f, 10);
                float cw = op.width * 0.42f, ch = h + 0.25f;
                foreach (float s in new[] { -1f, 1f })
                    Quad("Curtain", P(c + s * (op.width / 2f - cw / 2f + 0.05f), cy + 0.08f, inSide * (T / 2f + 0.05f)),
                        new Vector2(cw, ch), -Normal(inSide), net, parent);
                Box("CurtainRod", P(c, op.top + 0.12f, inSide * (T / 2f + 0.05f)),
                    alongX ? new Vector3(op.width + 0.3f, 0.02f, 0.02f) : new Vector3(0.02f, 0.02f, op.width + 0.3f),
                    Mats.Color(new Color(0.35f, 0.3f, 0.22f), 0.6f), parent, false);
            }

            if (Tex.Exists("shutter_wood"))
            {
                var shutter = Mats.Lit("shutter_wood", Color.white, 0.1f, 1f, h / 0.5f);
                float sw = op.width / 2f;
                foreach (float s in new[] { -1f, 1f })
                {
                    // folded back flat against the outside wall, a little ajar
                    var pos = P(c + s * (op.width / 2f + sw / 2f + 0.02f), cy, -inSide * (T / 2f + 0.03f));
                    var size = alongX ? new Vector3(sw, h, 0.03f) : new Vector3(0.03f, h, sw);
                    var go = Box("Shutter", pos, size, shutter, parent, false);
                    go.transform.localRotation = Quaternion.Euler(0, s * inSide * (alongX ? 8f : -8f), 0);
                }
            }
        }

        // ---------------------------------------------------------------- decals and sprites

        /// <summary>Flat decal a few millimetres off a surface. <paramref name="intoSurface"/> points into the wall/floor.</summary>
        GameObject Decal(string tex, Vector3 pos, Vector2 size, Vector3 intoSurface, float spin = 0f, float alpha = 1f, Transform parent = null)
        {
            if (!Tex.Exists(tex)) return null;
            intoSurface = intoSurface.normalized;
            var up = Mathf.Abs(intoSurface.y) > 0.9f ? Vector3.forward : Vector3.up;
            var rot = Quaternion.LookRotation(intoSurface, up) * Quaternion.Euler(0, 0, spin);
            return Prim(PrimitiveType.Quad, "Decal_" + tex, pos - intoSurface * 0.004f, new Vector3(size.x, size.y, 1f),
                Mats.Decal(tex, new Color(1, 1, 1, alpha)), parent, false, rot);
        }

        /// <summary>Upright cut-out sprite standing at <paramref name="basePos"/>, sized from the texture's aspect.</summary>
        GameObject Sprite(string name, string tex, Vector3 basePos, float height, bool billboard, Material mat = null, Transform parent = null)
        {
            if (!Tex.Exists(tex)) return null;
            var t = Tex.Get(tex);
            float w = height * t.width / Mathf.Max(1, t.height);
            var g = Group(name, parent != null ? parent : cur);
            g.localPosition = basePos;
            var q = Prim(PrimitiveType.Quad, "Sprite", new Vector3(0, height / 2f, 0), new Vector3(w, height, 1f),
                mat != null ? mat : Mats.Cutout(tex, Color.white), g, false);
            q.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
            if (billboard) g.gameObject.AddComponent<Billboard>();
            return g.gameObject;
        }

        /// <summary>Two (built-in: four) crossed quads, for foliage that reads from every side.</summary>
        GameObject Cross(string name, string tex, Vector3 basePos, float height, float yaw, Transform parent = null, Color? tint = null)
        {
            if (!Tex.Exists(tex)) return null;
            var t = Tex.Get(tex);
            float w = height * t.width / Mathf.Max(1, t.height);
            var g = Group(name, parent != null ? parent : cur);
            g.localPosition = basePos;
            g.localRotation = Quaternion.Euler(0, yaw, 0);
            var mat = Mats.Cutout(tex, tint ?? Color.white, 0.45f);
            int n = Mats.Urp ? 2 : 4;
            for (int i = 0; i < n; i++)
            {
                var q = Prim(PrimitiveType.Quad, "Leaf", new Vector3(0, height / 2f, 0), new Vector3(w, height, 1f), mat, g, false, Quaternion.Euler(0, i * 90f, 0));
                q.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
            }
            return g.gameObject;
        }

        /// <summary>Framed picture on a wall. Frame uses the gilt moulding texture when present.</summary>
        GameObject Picture(string name, string tex, Vector3 pos, Vector2 size, Vector3 intoWall, Transform parent = null)
        {
            if (!Tex.Exists(tex)) return null;
            var g = Group(name, parent != null ? parent : cur);
            g.localPosition = pos;
            g.localRotation = Quaternion.LookRotation(intoWall, Vector3.up);
            var frame = Tex.Exists("frame_gilt") ? Mats.Lit("frame_gilt", new Color(0.8f, 0.75f, 0.65f), 0.45f) : Mats.Color(new Color(0.35f, 0.26f, 0.12f), 0.45f);
            float b = Mathf.Clamp(size.x * 0.08f, 0.025f, 0.06f);
            Box("Back", new Vector3(0, 0, 0.004f), new Vector3(size.x, size.y, 0.008f), Mats.Color(new Color(0.1f, 0.08f, 0.06f)), g, false);
            Box("FrameT", new Vector3(0, size.y / 2f + b / 2f, -0.005f), new Vector3(size.x + 2 * b, b, 0.025f), frame, g, false);
            Box("FrameB", new Vector3(0, -size.y / 2f - b / 2f, -0.005f), new Vector3(size.x + 2 * b, b, 0.025f), frame, g, false);
            Box("FrameL", new Vector3(-size.x / 2f - b / 2f, 0, -0.005f), new Vector3(b, size.y, 0.025f), frame, g, false);
            Box("FrameR", new Vector3(size.x / 2f + b / 2f, 0, -0.005f), new Vector3(b, size.y, 0.025f), frame, g, false);
            Prim(PrimitiveType.Quad, "Image", new Vector3(0, 0, -0.001f), new Vector3(size.x, size.y, 1f), Mats.Lit(tex, Color.white, 0.3f), g, false);
            return g.gameObject;
        }

        /// <summary>Night backdrop: a faint sky ring and a ridge of hills, unlit so fog and moonlight don't flatten them.</summary>
        void Backdrop()
        {
            var g = Group("Backdrop", root);
            if (Tex.Exists("night_sky_dome"))
            {
                var sky = Mats.UnlitTinted("night_sky_dome", new Color(0.55f, 0.5f, 0.48f));
                for (int i = 0; i < 12; i++)
                {
                    float a = i * 30f * Mathf.Deg2Rad;
                    var dir = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
                    Prim(PrimitiveType.Quad, "Sky", dir * 62f + new Vector3(0, 22f, 7f), new Vector3(34f, 60f, 1f), sky, g, false, Quaternion.LookRotation(dir));
                }
            }
            if (Tex.Exists("hills_silhouette"))
            {
                var hills = Mats.UnlitTinted("hills_silhouette", new Color(0.55f, 0.52f, 0.5f), true);
                for (int i = 0; i < 10; i++)
                {
                    float a = (i * 36f + 12f) * Mathf.Deg2Rad;
                    var dir = new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
                    Prim(PrimitiveType.Quad, "Hills", dir * 44f + new Vector3(0, 3.5f, 7f), new Vector3(30f, 7.5f, 1f), hills, g, false, Quaternion.LookRotation(dir));
                }
            }
        }

        // ---------------------------------------------------------------- per-room grime

        /// <summary>Stains, damp, cobwebs and scuffs. Positions are world metres (house x∈[-8,8], z∈[0,14]).</summary>
        void Dress()
        {
            cur = Group("Dressing");
            // living room
            Decal("damp_stain_a", new Vector3(-7.4f, 2.35f, 5.885f), new Vector2(1.1f, 0.8f), Vector3.forward, 0f, 0.8f);
            Decal("cobweb_a", new Vector3(-7.72f, 2.62f, 5.72f), new Vector2(0.6f, 0.6f), new Vector3(-1, 0, 1), 0f);
            Decal("water_ring", new Vector3(-6.2f, 0.442f, 3.25f), new Vector2(0.12f, 0.12f), Vector3.down, 30f, 0.7f);
            Decal("scuff_marks", new Vector3(-3.3f, 0.2f, 5.885f), new Vector2(0.7f, 0.35f), Vector3.forward, 0f, 0.7f);
            Decal("crack_plaster", new Vector3(-1.2f, 2.2f, 0.115f), new Vector2(0.8f, 0.8f), Vector3.back, 0f, 0.8f);
            Decal("ceiling_drip", new Vector3(-5.2f, WallH - 0.002f, 1.6f), new Vector2(0.9f, 0.9f), Vector3.up, 0f, 0.8f);

            // kitchen
            Decal("damp_stain_c", new Vector3(7.885f, 1.2f, 4.6f), new Vector2(0.8f, 1.2f), Vector3.right, 0f, 0.85f);
            Decal("mould_corner", new Vector3(7.885f, 2.45f, 0.45f), new Vector2(0.7f, 0.7f), Vector3.right, 0f, 0.9f);
            Decal("ceiling_drip", new Vector3(6.1f, WallH - 0.002f, 2.2f), new Vector2(1.2f, 1.2f), Vector3.up, 60f, 0.9f);
            Decal("scuff_marks", new Vector3(3.5f, 0.003f, 2.6f), new Vector2(1.2f, 0.6f), Vector3.down, 10f, 0.5f);

            // hallway: a handprint at shoulder height, near the basement door
            Decal("handprint_faint", new Vector3(5.4f, 1.3f, 7.885f), new Vector2(0.2f, 0.2f), Vector3.forward, -8f, 0.8f);
            Decal("crack_plaster", new Vector3(-5.6f, 2.3f, 6.115f), new Vector2(0.9f, 0.9f), Vector3.back, 90f, 0.7f);
            Decal("cobweb_b", new Vector3(5.72f, 2.62f, 7.72f), new Vector2(0.55f, 0.55f), new Vector3(1, 0, 1), 0f);
            Decal("damp_stain_b", new Vector3(3f, 0.45f, 6.115f), new Vector2(1.4f, 0.8f), Vector3.back, 0f, 0.8f);

            // study
            Decal("damp_stain_b", new Vector3(-7.885f, 2.2f, 13.2f), new Vector2(1.0f, 1.0f), Vector3.left, 0f, 0.8f);
            Decal("cobweb_a", new Vector3(-0.28f, 2.62f, 13.72f), new Vector2(0.6f, 0.6f), new Vector3(1, 0, 1), 0f);
            Decal("water_ring", new Vector3(-2.1f, 0.788f, 13.55f), new Vector2(0.1f, 0.1f), Vector3.down, 0f, 0.8f);

            // bathroom: it has never quite dried out
            Decal("mould_corner", new Vector3(0.115f, 2.4f, 13.7f), new Vector2(0.8f, 0.8f), Vector3.left, 0f, 1f);
            Decal("mould_corner", new Vector3(3.385f, 2.45f, 8.35f), new Vector2(0.6f, 0.6f), Vector3.right, 90f, 0.9f);
            Decal("ceiling_drip", new Vector3(1.9f, WallH - 0.002f, 12.6f), new Vector2(1.0f, 1.0f), Vector3.up, 0f, 1f);
            Decal("damp_stain_c", new Vector3(1.2f, 1.3f, 13.885f), new Vector2(0.9f, 1.3f), Vector3.forward, 0f, 0.9f);
        }
    }
}
