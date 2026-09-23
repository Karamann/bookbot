using UnityEngine;

namespace ThirdLamp
{
    /// <summary>Slow, grounded first-person movement. No jumping, no running through corridors.</summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public float walkSpeed = 1.7f;
        public float briskSpeed = 2.7f;
        public float gravity = -14f;
        public float eyeHeight = 1.62f;
        public float stepLength = 0.72f;

        public Transform CameraPivot { get; private set; }
        public Camera Cam { get; private set; }
        public float Yaw => yaw;
        public float Pitch => pitch;
        public Vector3 Velocity { get; private set; }

        CharacterController cc;
        float yaw, pitch, vy, stepAccumulator;
        Transform viewOverride;
        float overrideBlend;

        public void Setup(Camera cam, float startYaw)
        {
            cc = GetComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.28f;
            cc.center = new Vector3(0, 0.9f, 0);
            cc.stepOffset = 0.3f;
            cc.slopeLimit = 50f;

            CameraPivot = new GameObject("CameraPivot").transform;
            CameraPivot.SetParent(transform, false);
            CameraPivot.localPosition = new Vector3(0, eyeHeight, 0);
            Cam = cam;
            cam.transform.SetParent(CameraPivot, false);
            cam.transform.localPosition = Vector3.zero;
            cam.transform.localRotation = Quaternion.identity;
            yaw = startYaw;
            transform.rotation = Quaternion.Euler(0, yaw, 0);
        }

        public void Teleport(Vector3 position, float newYaw)
        {
            cc.enabled = false;
            transform.position = position;
            yaw = newYaw;
            pitch = 0f;
            transform.rotation = Quaternion.Euler(0, yaw, 0);
            CameraPivot.localRotation = Quaternion.identity;
            cc.enabled = true;
        }

        /// <summary>Smoothly moves the view to a fixed point (e.g. sitting at the computer).</summary>
        public void SetViewOverride(Transform target) => viewOverride = target;
        public void ClearViewOverride() => viewOverride = null;

        void Update()
        {
            bool canLook = Game.Mode == InputMode.Play;
            if (canLook)
            {
                var look = GameInput.LookDelta;
                yaw += look.x;
                pitch = Mathf.Clamp(pitch - look.y, -82f, 82f);
                transform.rotation = Quaternion.Euler(0, yaw, 0);
                CameraPivot.localRotation = Quaternion.Euler(pitch, 0, 0);
            }

            Vector2 input = canLook ? GameInput.Move : Vector2.zero;
            float speed = GameInput.Held(GameKey.Sprint) ? briskSpeed : walkSpeed;
            if (Game.PhotoCamera != null && Game.PhotoCamera.IsRaised) speed = walkSpeed * 0.7f;
            Vector3 move = (transform.right * input.x + transform.forward * input.y) * speed;

            if (cc.isGrounded && vy < 0) vy = -1f;
            vy += gravity * Time.deltaTime;
            move.y = vy;
            var before = transform.position;
            cc.Move(move * Time.deltaTime);
            var delta = transform.position - before;
            delta.y = 0;
            Velocity = delta / Mathf.Max(Time.deltaTime, 0.0001f);

            if (cc.isGrounded && input.sqrMagnitude > 0.01f)
            {
                stepAccumulator += delta.magnitude;
                if (stepAccumulator >= stepLength)
                {
                    stepAccumulator = 0f;
                    Footstep();
                }
            }
        }

        void LateUpdate()
        {
            overrideBlend = Mathf.MoveTowards(overrideBlend, viewOverride != null ? 1f : 0f, Time.deltaTime * 2.2f);
            var camT = Cam.transform;
            if (viewOverride != null || overrideBlend > 0f)
            {
                float t = Mathf.SmoothStep(0, 1, overrideBlend);
                var targetPos = viewOverride != null ? viewOverride.position : CameraPivot.position;
                var targetRot = viewOverride != null ? viewOverride.rotation : CameraPivot.rotation;
                camT.position = Vector3.Lerp(CameraPivot.position, targetPos, t);
                camT.rotation = Quaternion.Slerp(CameraPivot.rotation, targetRot, t);
            }
            else
            {
                camT.localPosition = Vector3.zero;
                camT.localRotation = Quaternion.identity;
            }
        }

        void Footstep()
        {
            var zone = RoomZone.At(transform.position);
            string clip = "step_wood";
            if (zone == null || !zone.indoor) clip = "step_gravel";
            else if (zone.id == "kitchen" || zone.id == "bathroom") clip = "step_tile";
            else if (zone.id == "mind_corridor") clip = "step_stone";
            Game.Audio.PlayAt(clip, transform.position + Vector3.up * 0.05f, 0.32f, Random.Range(0.9f, 1.1f));
        }
    }
}
