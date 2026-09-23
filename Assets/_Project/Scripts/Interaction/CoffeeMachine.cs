using System.Collections;
using UnityEngine;

namespace ThirdLamp
{
    /// <summary>Filter coffee machine. Entirely mundane, which is the point.</summary>
    public class CoffeeMachine : Interactable
    {
        public const string MadeFlag = "made_coffee";
        public GameObject mug;
        public Light warmLight;
        bool brewing;

        public override string Prompt
        {
            get
            {
                if (brewing) return "Coffee machine (brewing)";
                if (!Game.Lighting.PowerOn) return "Coffee machine (no power)";
                return Game.State.Has(MadeFlag) ? "Make more coffee" : "Make coffee";
            }
        }

        public override bool CanInteract => !brewing;

        public override void Interact()
        {
            if (!Game.Lighting.PowerOn)
            {
                Game.Hud.Subtitle("No power.", 2f);
                return;
            }
            if (Game.State.Has(MadeFlag) && mug != null && mug.activeSelf)
            {
                Game.Hud.Subtitle("One's enough. It's going to be a long night either way.", 3.5f);
                return;
            }
            StartCoroutine(Brew());
        }

        IEnumerator Brew()
        {
            brewing = true;
            if (warmLight != null) warmLight.enabled = true;
            Game.Audio.PlayAt("switch", transform.position, 0.5f);
            Game.Audio.PlayAt("coffee", transform.position, 0.6f, 1f, 0.3f);
            Game.Hud.Subtitle("The machine wheezes into life.", 3f);
            yield return new WaitForSeconds(8f);
            if (warmLight != null) warmLight.enabled = false;
            if (mug != null) mug.SetActive(true);
            Game.State.Set(MadeFlag);
            Game.Hud.Subtitle("Coffee. Strong enough to strip paint.", 3.5f);
            brewing = false;
        }
    }
}
