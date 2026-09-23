using UnityEngine;

namespace ThirdLamp
{
    /// <summary>Doors (hinged) and drawers (sliding). Can be locked behind a flag.</summary>
    public class Openable : Interactable
    {
        public enum Kind { Hinge, Slide }

        public string displayName = "door";
        public Kind kind = Kind.Hinge;
        public Transform moving;
        public Vector3 openEuler = new Vector3(0, 95, 0);
        public Vector3 openOffset = new Vector3(0, 0, 0.35f);
        public float speed = 1.6f;
        public bool locked;
        public string unlockFlag;
        [TextArea] public string lockedText = "Locked.";
        [TextArea] public string unlockText;
        public string unlockedFlagOnOpen;

        public bool IsOpen => target > 0.5f;

        Quaternion closedRot;
        Vector3 closedPos;
        float t, target;
        bool ready;

        public void Setup(Transform movingPart, bool startOpen)
        {
            moving = movingPart;
            closedRot = moving.localRotation;
            closedPos = moving.localPosition;
            t = target = startOpen ? 1f : 0f;
            ready = true;
            Apply();
        }

        public override string Prompt
        {
            get
            {
                if (locked && !(unlockFlag != null && Game.State.Has(unlockFlag))) return $"Try the {displayName}";
                return (IsOpen ? "Close the " : "Open the ") + displayName;
            }
        }

        public override void Interact()
        {
            if (locked)
            {
                if (!string.IsNullOrEmpty(unlockFlag) && Game.State.Has(unlockFlag))
                {
                    locked = false;
                    Game.Audio.PlayAt("unlock", transform.position, 0.7f);
                    if (!string.IsNullOrEmpty(unlockText)) Game.Hud.Subtitle(unlockText, 3.5f);
                }
                else
                {
                    Game.Audio.PlayAt("door_locked", transform.position, 0.6f);
                    Game.Hud.Subtitle(lockedText, 3.5f);
                    return;
                }
            }
            SetOpen(!IsOpen, false, false);
        }

        public void SetOpen(bool open, bool instant, bool silent)
        {
            if (open && locked && !silent) return;
            target = open ? 1f : 0f;
            if (instant) { t = target; Apply(); }
            if (!silent)
            {
                string clip = kind == Kind.Slide ? "drawer" : (open ? "door_open" : "door_close");
                Game.Audio.PlayAt(clip, transform.position, 0.55f, Random.Range(0.95f, 1.05f));
            }
            if (open && !string.IsNullOrEmpty(unlockedFlagOnOpen)) Game.State.Set(unlockedFlagOnOpen);
        }

        void Update()
        {
            if (!ready || Mathf.Approximately(t, target)) return;
            t = Mathf.MoveTowards(t, target, Time.deltaTime * speed);
            Apply();
        }

        void Apply()
        {
            float e = Mathf.SmoothStep(0, 1, t);
            if (kind == Kind.Hinge) moving.localRotation = closedRot * Quaternion.Euler(openEuler * e);
            else moving.localPosition = closedPos + openOffset * e;
        }
    }
}
