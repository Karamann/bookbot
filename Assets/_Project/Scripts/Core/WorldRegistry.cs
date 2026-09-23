using System.Collections.Generic;
using UnityEngine;

namespace ThirdLamp
{
    /// <summary>Names a scene object so data-driven events can find it by id, even while inactive.</summary>
    public class WorldTag : MonoBehaviour
    {
        public string id;
    }

    public class WorldRegistry
    {
        readonly Dictionary<string, GameObject> objects = new Dictionary<string, GameObject>();

        public void Register(string id, GameObject go)
        {
            if (string.IsNullOrEmpty(id) || go == null) return;
            var tag = go.GetComponent<WorldTag>();
            if (tag == null) tag = go.AddComponent<WorldTag>();
            tag.id = id;
            if (objects.ContainsKey(id)) Debug.LogWarning($"[ThirdLamp] Duplicate world id '{id}'");
            objects[id] = go;
        }

        public GameObject Find(string id)
        {
            if (id != null && objects.TryGetValue(id, out var go) && go != null) return go;
            Debug.LogWarning($"[ThirdLamp] World id '{id}' not found");
            return null;
        }

        public T Get<T>(string id) where T : Component
        {
            var go = Find(id);
            return go == null ? null : go.GetComponentInChildren<T>(true);
        }

        public IEnumerable<KeyValuePair<string, GameObject>> All => objects;
    }
}
