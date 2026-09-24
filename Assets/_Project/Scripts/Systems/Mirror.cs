using System.Collections.Generic;
using UnityEngine;

namespace ThirdLamp
{
    /// <summary>
    /// Planar mirror using an off-axis projection that exactly frames the mirror quad from the
    /// reflected eye, so the render texture maps straight onto the quad. Mirrors are a Second Lamp:
    /// they also show Memory-layer objects. Parameter "lag" delays the reflected body.
    /// </summary>
    public class Mirror : MonoBehaviour, IParamReceiver
    {
        public float width = 0.7f, height = 0.9f;
        public float activeRange = 7f;
        public ReflectionBody body;

        Camera reflCam;
        RenderTexture rt;

        public void Setup(float w, float h, Renderer surface, Material baseUnlit, ReflectionBody reflectionBody)
        {
            width = w;
            height = h;
            body = reflectionBody;
            rt = new RenderTexture(512, Mathf.RoundToInt(512 * h / w), 16) { name = "MirrorRT" };
            var go = new GameObject("MirrorCamera");
            go.transform.SetParent(transform, false);
            reflCam = go.AddComponent<Camera>();
            reflCam.targetTexture = rt;
            reflCam.clearFlags = CameraClearFlags.SolidColor;
            reflCam.backgroundColor = Color.black;
            reflCam.depth = -5;
            reflCam.cullingMask = ~((1 << Game.LayerMirror) | (1 << Game.LayerViewmodel) | (1 << Game.LayerIgnoreRaycast));
            reflCam.enabled = false;

            var mat = new Material(baseUnlit) { mainTexture = rt };
            mat.mainTextureScale = new Vector2(-1, 1);
            mat.mainTextureOffset = new Vector2(1, 0);
            surface.sharedMaterial = mat;
        }

        public void SetParam(string key, float value)
        {
            if (key == "lag" && body != null) body.lag = value;
        }

        void LateUpdate()
        {
            var main = Game.MainCamera;
            if (main == null || reflCam == null) return;
            Vector3 eye = main.transform.position;
            Vector3 n = -transform.forward; // faces into the room
            Vector3 c = transform.position;
            float side = Vector3.Dot(eye - c, n);
            bool active = side > 0.02f && Vector3.Distance(eye, c) < activeRange;
            reflCam.enabled = active;
            if (!active) return;

            Vector3 pe = eye - 2f * side * n;
            Vector3 vn = -n;
            Vector3 vu = transform.up;
            Vector3 vr = Vector3.Cross(vn, vu);

            Vector3 pa = c - vr * (width / 2f) - vu * (height / 2f);
            Vector3 pb = c + vr * (width / 2f) - vu * (height / 2f);
            Vector3 pc = c - vr * (width / 2f) + vu * (height / 2f);
            Vector3 va = pa - pe, vb = pb - pe, vc = pc - pe;
            float d = -Vector3.Dot(va, vn);
            float near = Mathf.Max(0.02f, d);
            float far = 40f;
            float l = Vector3.Dot(vr, va) * near / d;
            float r = Vector3.Dot(vr, vb) * near / d;
            float b = Vector3.Dot(vu, va) * near / d;
            float t = Vector3.Dot(vu, vc) * near / d;

            reflCam.transform.SetPositionAndRotation(pe, Quaternion.LookRotation(-vn, vu));
            var view = Matrix4x4.identity;
            view.SetRow(0, new Vector4(vr.x, vr.y, vr.z, -Vector3.Dot(vr, pe)));
            view.SetRow(1, new Vector4(vu.x, vu.y, vu.z, -Vector3.Dot(vu, pe)));
            view.SetRow(2, new Vector4(vn.x, vn.y, vn.z, -Vector3.Dot(vn, pe)));
            view.SetRow(3, new Vector4(0, 0, 0, 1));
            reflCam.worldToCameraMatrix = view;
            reflCam.projectionMatrix = Matrix4x4.Frustum(l, r, b, t, near, far);
        }

        void OnDestroy()
        {
            if (rt != null) rt.Release();
        }
    }

    /// <summary>
    /// Alex's body, visible only to mirrors. Follows the player through a short history buffer so
    /// the reflection can fall behind by <see cref="lag"/> seconds.
    /// </summary>
    public class ReflectionBody : MonoBehaviour
    {
        public float lag;
        public Transform head;

        struct Pose { public float time; public Vector3 pos; public float yaw; public float pitch; }
        readonly List<Pose> history = new List<Pose>();

        void LateUpdate()
        {
            var p = Game.Player;
            if (p == null) return;
            float now = Time.time;
            history.Add(new Pose { time = now, pos = p.transform.position, yaw = p.Yaw, pitch = p.Pitch });
            while (history.Count > 2 && history[1].time < now - 3f) history.RemoveAt(0);

            float target = now - lag;
            Pose a = history[0], b = history[history.Count - 1];
            for (int i = history.Count - 1; i >= 0; i--)
            {
                if (history[i].time <= target)
                {
                    a = history[i];
                    b = i + 1 < history.Count ? history[i + 1] : history[i];
                    break;
                }
            }
            float k = b.time > a.time ? Mathf.InverseLerp(a.time, b.time, target) : 1f;
            transform.position = Vector3.Lerp(a.pos, b.pos, k);
            transform.rotation = Quaternion.Euler(0, Mathf.LerpAngle(a.yaw, b.yaw, k), 0);
            if (head != null) head.localRotation = Quaternion.Euler(Mathf.Lerp(a.pitch, b.pitch, k) * 0.6f, 0, 0);
        }
    }
}
