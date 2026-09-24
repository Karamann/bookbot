using System.Collections.Generic;
using UnityEngine;

namespace ThirdLamp
{
    /// <summary>
    /// A catalogue artifact or other object Alex can pick up and turn over in the hands.
    /// Fixed objects (the painting) are inspected in place instead.
    /// </summary>
    public class InspectableObject : Interactable
    {
        [Tooltip("Catalogue number, e.g. \"014\". Empty for ordinary objects like the coffee mug.")]
        public string itemId;
        public string displayName = "object";
        [TextArea] public string description;
        public bool canPickUp = true;

        [Header("Hidden symbol")]
        [Tooltip("Local direction of the face carrying the symbol (3D pickups). Zero = none.")]
        public Vector3 symbolLocalDir;
        [Tooltip("UV position of the symbol on DisplayTexture (fixed objects). Negative = none.")]
        public Vector2 symbolUV = new Vector2(-1, -1);
        [TextArea] public string symbolNote = "A small mark: a circle, a line, a triangle.";

        [Header("Holding")]
        public Vector3 holdOffset = new Vector3(0.2f, -0.2f, 0.42f);
        public Vector3 holdEuler = new Vector3(15f, -20f, 0f);
        public float inspectDistance = 0.42f;
        [Tooltip("Starting rotation in front of the camera when inspecting. Identity shows the local -Z face.")]
        public Vector3 inspectEuler;
        public Renderer displayRenderer;
        [Tooltip("Offset/rotation when set down, so the object rests on the surface rather than in it.")]
        public Vector3 placeOffset;
        public Vector3 placeEuler;

        public PlacementSocket CurrentSocket { get; set; }
        public bool IsHeld { get; private set; }
        /// <summary>True once the player has picked it up at least once (checkpoints only move touched items).</summary>
        public bool HasBeenMoved { get; set; }
        public bool IsCatalogueItem => !string.IsNullOrEmpty(itemId);
        public bool HasSymbol => symbolLocalDir != Vector3.zero || symbolUV.x >= 0f;
        public string SymbolFlag => "symbol_seen_" + (IsCatalogueItem ? itemId : name);

        readonly Dictionary<Transform, int> originalLayers = new Dictionary<Transform, int>();
        Transform worldParent;

        public override string Prompt
        {
            get
            {
                string label = IsCatalogueItem ? $"{displayName} (No. {itemId})" : displayName;
                return canPickUp ? $"Pick up {label}" : $"Look closely at {label}";
            }
        }

        public override void Interact()
        {
            if (canPickUp) Game.Interactor.PickUp(this);
            else Game.Inspector.OpenFixed(this);
        }

        public Texture DisplayTexture
        {
            get
            {
                var ms = GetComponentInChildren<MaterialStates>();
                if (ms != null && ms.CurrentTexture != null) return ms.CurrentTexture;
                return displayRenderer != null && displayRenderer.sharedMaterial != null ? displayRenderer.sharedMaterial.mainTexture : null;
            }
        }

        public virtual void OnPickedUp(Transform holdAnchor)
        {
            if (worldParent == null) worldParent = transform.parent;
            if (CurrentSocket != null) CurrentSocket.Release(this);
            IsHeld = true;
            HasBeenMoved = true;
            transform.SetParent(holdAnchor, false);
            transform.localPosition = holdOffset;
            transform.localRotation = Quaternion.Euler(holdEuler);
            SetPhysical(false);
            if (IsCatalogueItem) Game.State.Set("picked_" + itemId);
        }

        public virtual void OnReleased(Vector3 position, Quaternion rotation, Transform parent)
        {
            IsHeld = false;
            transform.SetParent(parent != null ? parent : worldParent, true);
            transform.SetPositionAndRotation(position, rotation);
            SetPhysical(true);
        }

        void SetPhysical(bool physical)
        {
            foreach (var c in GetComponentsInChildren<Collider>(true)) c.enabled = physical;
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                if (!physical)
                {
                    if (!originalLayers.ContainsKey(t)) originalLayers[t] = t.gameObject.layer;
                    t.gameObject.layer = Game.LayerViewmodel;
                }
                else if (originalLayers.TryGetValue(t, out var layer))
                {
                    t.gameObject.layer = layer;
                }
            }
            foreach (var r in GetComponentsInChildren<Renderer>(true))
                r.shadowCastingMode = physical ? UnityEngine.Rendering.ShadowCastingMode.On : UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        public void RevealSymbol()
        {
            if (Game.State.Has(SymbolFlag)) return;
            Game.State.Set(SymbolFlag);
            Game.Perception.Add(1, SymbolFlag);
            Game.Hud.Subtitle(symbolNote, 5f);
        }
    }
}
