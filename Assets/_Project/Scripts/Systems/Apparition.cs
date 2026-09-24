using UnityEngine;

namespace ThirdLamp
{
    /// <summary>
    /// A figure on the Memory layer: invisible to the eye, visible through the camera and mirrors.
    /// Watching it through the lens long enough raises perception; lowering the camera afterwards
    /// removes it. Entirely missable.
    /// </summary>
    public class Apparition : MonoBehaviour
    {
        public string id = "figure";
        public float watchSeconds = 3f;
        public int perception = 3;
        public float range = 25f;
        public bool vanishWhenLensLowered = true;

        float watched;
        bool seen;

        void OnEnable()
        {
            watched = 0f;
        }

        void Update()
        {
            var cam = Game.PhotoCamera;
            bool raised = cam != null && cam.IsRaised && !cam.IsReviewing;
            if (raised && Perceive.CanSee(cam.LensCamera, gameObject, range))
            {
                Game.State.Set("glimpsed_" + id);
                watched += Time.deltaTime;
                if (!seen && watched >= watchSeconds)
                {
                    seen = true;
                    Game.State.Set("saw_" + id);
                    UrpPostFx.Pulse(0.8f);
                    Game.Perception.Add(perception, "apparition_" + id);
                }
            }

            if (vanishWhenLensLowered && Game.State.Has("glimpsed_" + id) && (cam == null || !cam.IsRaised))
            {
                Game.State.Set("gone_" + id);
                gameObject.SetActive(false);
            }
        }
    }

    /// <summary>Moves an object along a straight line once, then disables it (the passing car).</summary>
    public class Mover : MonoBehaviour
    {
        public Vector3 from, to;
        public float duration = 12f;
        float t;

        void OnEnable()
        {
            t = 0f;
            transform.position = from;
        }

        void Update()
        {
            t += Time.deltaTime / duration;
            transform.position = Vector3.Lerp(from, to, t);
            if (t >= 1f) gameObject.SetActive(false);
        }
    }
}
