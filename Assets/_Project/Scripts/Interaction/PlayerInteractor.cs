using UnityEngine;

namespace ThirdLamp
{
    /// <summary>Centre-screen interaction, holding one item at a time, putting things down.</summary>
    public class PlayerInteractor : MonoBehaviour
    {
        public float reach = 2.1f;
        public Transform HoldAnchor { get; private set; }
        public InspectableObject Held { get; private set; }
        public Interactable Focus { get; private set; }

        Camera cam;
        bool shownHoldTip;

        int Mask => ~((1 << Game.LayerIgnoreRaycast) | (1 << Game.LayerPlayerBody) | (1 << Game.LayerViewmodel) |
                      (1 << Game.LayerMemory) | (1 << Game.LayerMirror));

        public void Setup(Camera camera)
        {
            cam = camera;
            HoldAnchor = new GameObject("HoldAnchor").transform;
            HoldAnchor.SetParent(cam.transform, false);
        }

        void Update()
        {
            Focus = null;
            if (Game.Mode != InputMode.Play) return;

            if (Physics.Raycast(cam.transform.position, cam.transform.forward, out var hit, reach, Mask, QueryTriggerInteraction.Collide))
            {
                var i = hit.collider.GetComponentInParent<Interactable>();
                if (i != null && i.isActiveAndEnabled && i.CanInteract) Focus = i;
            }

            if (!Game.PlayStable) return;
            if (GameInput.Down(GameKey.Interact) && Focus != null) Focus.Interact();
            else if (GameInput.Down(GameKey.Drop) && Held != null) Drop();
            else if (GameInput.Down(GameKey.Inspect) && Held != null) Game.Inspector.OpenHeld(Held);
            else if (GameInput.Down(GameKey.Lamp) && Held is OilLamp lamp) lamp.Toggle();
        }

        public void PickUp(InspectableObject item)
        {
            if (Held != null)
            {
                Game.Hud.Subtitle("My hands are full.", 2f);
                return;
            }
            Held = item;
            item.OnPickedUp(HoldAnchor);
            Game.Audio.PlayAt("pickup", cam.transform.position, 0.45f);
            if (!shownHoldTip)
            {
                shownHoldTip = true;
                Game.Hud.Tip("[F] turn it over    [Q] put it down", 6f);
            }
        }

        /// <summary>Removes the held item from the hand without placing it anywhere (caller places it).</summary>
        public InspectableObject TakeHeld()
        {
            var item = Held;
            Held = null;
            return item;
        }

        public void Drop()
        {
            var item = TakeHeld();
            if (item == null) return;
            Vector3 origin = cam.transform.position;
            Vector3 pos;
            if (Physics.Raycast(origin, cam.transform.forward, out var hit, reach, Mask, QueryTriggerInteraction.Ignore) && hit.normal.y > 0.6f)
                pos = hit.point;
            else
            {
                Vector3 ahead = origin + transform.forward * 0.6f;
                pos = Physics.Raycast(ahead, Vector3.down, out var down, 3f, Mask, QueryTriggerInteraction.Ignore)
                    ? down.point
                    : new Vector3(ahead.x, transform.position.y, ahead.z);
            }
            var rot = Quaternion.Euler(0, transform.eulerAngles.y, 0);
            item.OnReleased(pos + rot * item.placeOffset + Vector3.up * 0.005f, rot * Quaternion.Euler(item.placeEuler), null);
            Game.Audio.PlayAt("place", pos, 0.5f);
        }
    }
}
