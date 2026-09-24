using UnityEngine;

namespace ThirdLamp
{
    /// <summary>Anything the centre-screen reticle can act on with [E].</summary>
    public abstract class Interactable : MonoBehaviour
    {
        public virtual string Prompt => "Interact";
        public virtual bool CanInteract => true;
        public abstract void Interact();
    }

    /// <summary>Shows a line of inner monologue. Used for the car, locked cupboards and the like.</summary>
    public class MessageInteractable : Interactable
    {
        public string prompt = "Look";
        [TextArea] public string message;
        public string flagOnUse;

        public override string Prompt => prompt;

        public override void Interact()
        {
            Game.Hud.Subtitle(message, 4f);
            if (!string.IsNullOrEmpty(flagOnUse)) Game.State.Set(flagOnUse);
        }
    }

    /// <summary>A small thing that goes into Alex's pocket and sets a flag (keys, torch, camera).</summary>
    public class FlagPickup : Interactable
    {
        public string flag;
        public string verb = "Take";
        public string itemName = "item";
        [TextArea] public string message;
        public bool hideOnPickup = true;
        public GameObject hideTarget;
        public string sound = "pickup";

        public override string Prompt => $"{verb} {itemName}";
        public override bool CanInteract => !Game.State.Has(flag);

        public override void Interact()
        {
            Game.State.Set(flag);
            if (!string.IsNullOrEmpty(message)) Game.Hud.Subtitle(message, 4.5f);
            if (!string.IsNullOrEmpty(sound)) Game.Audio.PlayAt(sound, transform.position, 0.7f);
            RestoreFromFlags();
        }

        public void RestoreFromFlags()
        {
            if (hideOnPickup && Game.State.Has(flag)) (hideTarget != null ? hideTarget : gameObject).SetActive(false);
        }
    }
}
