using UnityEngine;

namespace ThirdLamp
{
    /// <summary>Visibility tests used by "is the player looking at X" conditions.</summary>
    public static class Perceive
    {
        static readonly Plane[] planes = new Plane[6];

        public static int OcclusionMask =>
            ~((1 << Game.LayerIgnoreRaycast) | (1 << Game.LayerPlayerBody) | (1 << Game.LayerViewmodel) |
              (1 << Game.LayerMemory) | (1 << Game.LayerMirror));

        public static bool TryGetBounds(GameObject go, out Bounds b)
        {
            b = default;
            if (go == null) return false;
            bool any = false;
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                if (!r.enabled || !r.gameObject.activeInHierarchy) continue;
                if (!any) { b = r.bounds; any = true; }
                else b.Encapsulate(r.bounds);
            }
            return any;
        }

        /// <summary>
        /// True if any part of the object is inside the camera frustum, within range and not fully
        /// hidden behind walls. Used to make sure changes happen only while unobserved.
        /// </summary>
        public static bool CanSee(Camera cam, GameObject target, float maxDistance)
        {
            if (cam == null || !cam.isActiveAndEnabled || !TryGetBounds(target, out var b)) return false;
            Vector3 eye = cam.transform.position;
            if (Vector3.Distance(eye, b.ClosestPoint(eye)) > maxDistance) return false;
            GeometryUtility.CalculateFrustumPlanes(cam, planes);
            if (!GeometryUtility.TestPlanesAABB(planes, b)) return false;

            // Sample centre and corners; one clear line is enough to count as visible.
            Vector3 c = b.center, e = b.extents * 0.9f;
            if (HasLine(eye, c, target)) return true;
            for (int i = 0; i < 8; i++)
            {
                var p = c + new Vector3((i & 1) == 0 ? -e.x : e.x, (i & 2) == 0 ? -e.y : e.y, (i & 4) == 0 ? -e.z : e.z);
                if (HasLine(eye, p, target)) return true;
            }
            return false;
        }

        /// <summary>Stricter test: the object's centre is near the middle of the view and unobstructed.</summary>
        public static bool IsFocused(Camera cam, GameObject target, float maxDistance, float viewportMargin = 0.22f)
        {
            if (cam == null || !cam.isActiveAndEnabled || !TryGetBounds(target, out var b)) return false;
            Vector3 eye = cam.transform.position;
            if (Vector3.Distance(eye, b.ClosestPoint(eye)) > maxDistance) return false;
            var vp = cam.WorldToViewportPoint(b.center);
            if (vp.z <= 0f || vp.x < viewportMargin || vp.x > 1f - viewportMargin ||
                vp.y < viewportMargin || vp.y > 1f - viewportMargin) return false;
            return HasLine(eye, b.center, target);
        }

        static bool HasLine(Vector3 from, Vector3 to, GameObject target)
        {
            Vector3 d = to - from;
            float dist = d.magnitude;
            if (dist < 0.01f) return true;
            if (!Physics.Raycast(from, d / dist, out var hit, dist, OcclusionMask, QueryTriggerInteraction.Ignore))
                return true;
            return hit.transform.IsChildOf(target.transform) || hit.distance > dist - 0.15f;
        }

        /// <summary>Visible to the player by any means: eyes, or the raised camera's viewfinder.</summary>
        public static bool PlayerCanSee(GameObject target, float maxDistance)
        {
            if (CanSee(Game.MainCamera, target, maxDistance)) return true;
            var pc = Game.PhotoCamera;
            return pc != null && pc.IsRaised && CanSee(pc.LensCamera, target, maxDistance);
        }

        public static bool PlayerFocusedOn(GameObject target, float maxDistance)
        {
            if (Game.Inspector != null && Game.Inspector.IsInspecting(target)) return true;
            if (IsFocused(Game.MainCamera, target, maxDistance)) return true;
            var pc = Game.PhotoCamera;
            return pc != null && pc.IsRaised && IsFocused(pc.LensCamera, target, maxDistance);
        }
    }
}
