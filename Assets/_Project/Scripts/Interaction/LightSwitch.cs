using UnityEngine;

namespace ThirdLamp
{
    public class LightSwitch : Interactable
    {
        public string group;
        public Transform toggle;

        public override string Prompt => Game.Lighting.GroupSwitchedOn(group) ? "Switch the light off" : "Switch the light on";

        public override void Interact()
        {
            Game.Lighting.ToggleGroup(group);
            Game.Audio.PlayAt("switch", transform.position, 0.5f, Random.Range(0.95f, 1.05f));
            if (!Game.Lighting.PowerOn) Game.Hud.Subtitle("Nothing. The power's out.", 2.5f);
            RefreshVisual();
        }

        public void RefreshVisual()
        {
            if (toggle != null)
                toggle.localRotation = Quaternion.Euler(Game.Lighting.GroupSwitchedOn(group) ? -12f : 12f, 0, 0);
        }
    }
}
