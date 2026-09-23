using System;
using System.Collections.Generic;
using UnityEngine;

namespace ThirdLamp
{
    /// <summary>
    /// The numbered place an artifact belongs. Records the order and location of every placement:
    /// this is the hook for the later floor-plan reveal (the placements trace the symbol) and the
    /// secret ending (one socket is deliberately "wrong").
    /// </summary>
    public class PlacementSocket : Interactable
    {
        [Serializable]
        public struct LogEntry
        {
            public string itemId;
            public string socketId;
            public float clockMinutes;
        }

        public static readonly List<LogEntry> Log = new List<LogEntry>();

        public string socketId;
        public string expectedItemId;
        public Transform anchor;
        public InspectableObject Occupant { get; private set; }

        Collider triggerCollider;

        public void Setup(string id, string expected, Transform anchorPoint, Collider trigger)
        {
            socketId = id;
            expectedItemId = expected;
            anchor = anchorPoint;
            triggerCollider = trigger;
        }

        public override bool CanInteract => Occupant == null && Game.Interactor.Held != null;

        public override string Prompt
        {
            get
            {
                var held = Game.Interactor.Held;
                return held == null ? "" : $"Put {held.displayName} here (shelf {expectedItemId})";
            }
        }

        public override void Interact()
        {
            var item = Game.Interactor.TakeHeld();
            if (item != null) Place(item, false);
        }

        public void Place(InspectableObject item, bool restoring)
        {
            Occupant = item;
            item.CurrentSocket = this;
            item.OnReleased(anchor.TransformPoint(item.placeOffset), anchor.rotation * Quaternion.Euler(item.placeEuler), anchor);
            if (triggerCollider != null) triggerCollider.enabled = false;
            if (restoring || !item.IsCatalogueItem) return;

            Log.Add(new LogEntry { itemId = item.itemId, socketId = socketId, clockMinutes = Game.State.clockMinutes });
            Game.State.Add("placements");
            bool correct = item.itemId == expectedItemId;
            Game.State.Clear(correct ? "misplaced_" + item.itemId : "placed_" + item.itemId);
            Game.State.Set(correct ? "placed_" + item.itemId : "misplaced_" + item.itemId);
            Game.Audio.PlayAt("place", anchor.position, 0.6f);
        }

        public void Release(InspectableObject item)
        {
            if (Occupant != item) return;
            Occupant = null;
            item.CurrentSocket = null;
            if (triggerCollider != null) triggerCollider.enabled = true;
        }
    }
}
