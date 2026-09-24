using UnityEngine;

namespace ThirdLamp
{
    /// <summary>Old petrol generator in the shed. The villa has no grid connection.</summary>
    public class Generator : Interactable
    {
        public const string RestoredFlag = "generator_restored";
        int pulls;
        float lastPull;

        public override string Prompt => Game.Lighting.PowerOn ? "Generator" : "Pull the starter cord";

        public override void Interact()
        {
            if (Game.Lighting.PowerOn)
            {
                Game.Hud.Subtitle("Running. Loud, but running.", 2.5f);
                return;
            }
            if (Time.time - lastPull < 1.1f) return;
            lastPull = Time.time;
            pulls++;
            Game.Audio.PlayAt("cord_pull", transform.position, 0.9f);
            if (pulls < 3)
            {
                Game.Audio.PlayAt("sputter", transform.position, 0.9f, 1f, 0.35f);
                Game.Hud.Subtitle(pulls == 1 ? "It coughs and dies." : "Almost.", 2f);
                return;
            }
            pulls = 0;
            Game.Audio.PlayAt("generator_start", transform.position, 1f, 1f, 0.3f);
            Invoke(nameof(Restore), 1.1f);
        }

        void Restore()
        {
            Game.Lighting.SetPower(true);
            Game.State.Set(RestoredFlag);
            Game.Save.Checkpoint("Generator restored");
        }
    }
}
