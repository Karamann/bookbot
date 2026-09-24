using UnityEngine;

namespace ThirdLamp
{
    /// <summary>Turns about the vertical axis to face the player's eye. Used for sprite figures and foliage.</summary>
    public class Billboard : MonoBehaviour
    {
        void LateUpdate()
        {
            var cam = Game.MainCamera;
            if (cam == null) return;
            Vector3 to = cam.transform.position - transform.position;
            to.y = 0f;
            if (to.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.LookRotation(-to.normalized, Vector3.up);
        }
    }

    /// <summary>
    /// Something the naked eye catches at the edge of vision. It stays while the player isn't looking
    /// straight at it, and is gone the moment they do. Sets glimpsed_&lt;id&gt; once.
    /// </summary>
    public class Glimpse : MonoBehaviour
    {
        public string id;
        public float range = 30f;
        public int perception = 2;
        float seenFor;

        void Update()
        {
            var cam = Game.MainCamera;
            if (cam == null) return;
            if (Perceive.IsFocused(cam, gameObject, range, 0.3f))
            {
                // one or two frames is enough to register; then it isn't there
                seenFor += Time.deltaTime;
                if (seenFor < 0.12f) return;
                if (!Game.State.Has("glimpsed_" + id))
                {
                    Game.State.Set("glimpsed_" + id);
                    Game.Perception.Add(perception, "glimpse_" + id);
                    UrpPostFx.Pulse(0.6f);
                }
                gameObject.SetActive(false);
            }
            else seenFor = 0f;
        }
    }

    /// <summary>Candle-like flicker for a point light.</summary>
    public class FlameFlicker : MonoBehaviour
    {
        public float baseIntensity = 0.6f, amount = 0.3f, speed = 7f;
        Light l;
        float seed;

        void Awake() { l = GetComponent<Light>(); seed = Random.value * 10f; }

        void Update()
        {
            if (l == null) return;
            float n = Mathf.PerlinNoise(Time.time * speed, seed);
            l.intensity = baseIntensity + (n - 0.5f) * 2f * amount;
        }
    }

    /// <summary>The living-room TV. SetParam("on", 1) makes it hiss into static by itself.</summary>
    public class TvStatic : MonoBehaviour, IParamReceiver
    {
        public Renderer screen;
        public Material offMaterial, staticMaterial;
        public Light glow;
        AudioSource hiss;
        bool on;

        public void SetParam(string key, float value)
        {
            if (key != "on") return;
            on = value > 0.5f;
            screen.sharedMaterial = on ? staticMaterial : offMaterial;
            if (glow != null) glow.enabled = on;
            if (on && hiss == null) hiss = Game.Audio.Loop("hum", transform, Vector3.zero, 0.3f, 0.5f, 7f);
            if (hiss != null) { if (on) hiss.Play(); else hiss.Stop(); }
            if (on) Game.Audio.PlayAt("switch", transform.position, 0.5f, 0.6f);
        }

        void Update()
        {
            if (!on) return;
            // roll the snow texture around so it crawls, and let the glow breathe with it
            staticMaterial.mainTextureOffset = new Vector2(Random.value, Random.value);
            if (glow != null) glow.intensity = 0.35f + Random.value * 0.25f;
            if (hiss != null) hiss.pitch = 1.8f + Random.value * 0.2f;
        }
    }
}
