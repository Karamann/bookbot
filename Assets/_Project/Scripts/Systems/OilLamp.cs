using UnityEngine;

namespace ThirdLamp
{
    /// <summary>
    /// The Third Lamp. Placeholder: lighting it in a dark room switches the world to Mind mode
    /// (LightingStateManager), revealing LampVisibility "mind only" geometry.
    /// </summary>
    public class OilLamp : InspectableObject
    {
        public Light flame;
        public GameObject flameVisual;
        public bool Lit { get; private set; }
        public int IgnitionCount { get; private set; }
        public float nearRadius = 3.5f;

        AudioSource hiss;
        bool tipShown;

        public void SetupLamp(Light light, GameObject visual, AudioSource hissLoop)
        {
            flame = light;
            flameVisual = visual;
            hiss = hissLoop;
            flame.enabled = false;
            flameVisual.SetActive(false);
        }

        public bool IsNear(Vector3 p) => IsHeld || Vector3.Distance(transform.position, p) <= nearRadius;

        public override void OnPickedUp(Transform holdAnchor)
        {
            base.OnPickedUp(holdAnchor);
            Game.State.Set("lamp_found");
            if (!tipShown)
            {
                tipShown = true;
                Game.Hud.Tip("[L] light / put out", 5f);
            }
        }

        public void Toggle()
        {
            Lit = !Lit;
            flame.enabled = Lit;
            flameVisual.SetActive(Lit);
            if (Lit)
            {
                IgnitionCount++;
                Game.State.Set("lamp_lit");
                Game.Audio.PlayAt("match", transform.position, 0.8f);
                if (hiss != null) hiss.Play();
                if (!Game.Lighting.IsDarkAt(Game.Player.transform.position))
                    Game.Hud.Subtitle("The flame looks thin under the electric light.", 3f);
            }
            else
            {
                Game.State.Set("lamp_extinguished");
                if (Game.State.Has("mind_seen")) Game.State.Set("lamp_out_after_mind");
                Game.Audio.PlayAt("lamp_out", transform.position, 0.7f);
                if (hiss != null) hiss.Stop();
            }
        }

        void Update()
        {
            if (!Lit) return;
            float n = Mathf.PerlinNoise(Time.time * 6f, 0.7f);
            flame.intensity = 1.1f + n * 0.5f;
            flame.range = 4.5f + n * 0.6f;
        }
    }
}
