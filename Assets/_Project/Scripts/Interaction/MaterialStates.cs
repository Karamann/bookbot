using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdLamp
{
    /// <summary>Named material variants for one renderer (e.g. the painting's "eight" and "nine").</summary>
    public class MaterialStates : MonoBehaviour
    {
        [Serializable]
        public struct Entry
        {
            public string name;
            public Material material;
        }

        public Renderer target;
        public List<Entry> states = new List<Entry>();
        public string Current { get; private set; }

        public void Add(string stateName, Material material) => states.Add(new Entry { name = stateName, material = material });

        public void SetState(string stateName)
        {
            foreach (var s in states)
            {
                if (s.name != stateName) continue;
                target.sharedMaterial = s.material;
                Current = stateName;
                return;
            }
            Debug.LogWarning($"[ThirdLamp] Material state '{stateName}' missing on {name}");
        }

        public Texture CurrentTexture => target != null && target.sharedMaterial != null ? target.sharedMaterial.mainTexture : null;
    }
}
