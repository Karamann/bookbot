using UnityEngine;

namespace ThirdLamp
{
    /// <summary>The weak kitchen-drawer torch. Counts as electric light, so it suppresses the Third Lamp.</summary>
    public class Torch : MonoBehaviour
    {
        public const string HasTorchFlag = "has_torch";
        public Light beam;
        public bool On { get; private set; }

        void Update()
        {
            if (!Game.PlayStable || !Game.State.Has(HasTorchFlag)) return;
            if (GameInput.Down(GameKey.Torch)) Set(!On);
            if (On) beam.intensity = 1.4f + Mathf.PerlinNoise(Time.time * 3f, 0.3f) * 0.25f;
        }

        public void Set(bool on)
        {
            On = on;
            beam.enabled = on;
            Game.Audio.PlayAt("switch", transform.position, 0.35f, 1.3f);
        }
    }
}
