using System.Collections.Generic;
using UnityEngine;

namespace ThirdLamp
{
    /// <summary>
    /// Axis-aligned room volume. Used for "player is in X" conditions, light-group lookup and
    /// indoor/outdoor ambience. Plain bounds checks, no physics triggers.
    /// </summary>
    public class RoomZone : MonoBehaviour
    {
        public const string Exterior = "exterior";

        public string id;
        public string lightGroup;
        public bool indoor = true;
        [Tooltip("Only counts while the Third Lamp is active (the impossible corridor).")]
        public bool mindOnly;
        public Bounds bounds;

        static readonly List<RoomZone> zones = new List<RoomZone>();

        public static void Clear() => zones.Clear();

        public static RoomZone Create(Transform parent, string id, Vector3 min, Vector3 max, string lightGroup, bool indoor = true)
        {
            var go = new GameObject("Zone_" + id);
            go.transform.SetParent(parent, false);
            var z = go.AddComponent<RoomZone>();
            z.id = id;
            z.lightGroup = lightGroup;
            z.indoor = indoor;
            z.bounds = new Bounds((min + max) * 0.5f, max - min);
            zones.Add(z);
            return z;
        }

        /// <summary>Smallest zone containing the point, or null when outside every zone.</summary>
        public static RoomZone At(Vector3 p)
        {
            RoomZone best = null;
            float bestVol = float.MaxValue;
            foreach (var z in zones)
            {
                if (z == null || !z.bounds.Contains(p)) continue;
                if (z.mindOnly && (Game.Lighting == null || !Game.Lighting.MindActive)) continue;
                var s = z.bounds.size;
                float vol = s.x * s.y * s.z;
                if (vol < bestVol) { bestVol = vol; best = z; }
            }
            return best;
        }

        public static string IdAt(Vector3 p)
        {
            var z = At(p);
            return z == null ? Exterior : z.id;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.8f, 0.3f, 0.4f);
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
    }
}
