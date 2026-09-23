using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ThirdLamp
{
    public enum GameKey
    {
        Forward, Back, Left, Right, Sprint,
        Interact, Inspect, CameraToggle, Review, Phone, Lamp, Torch, Drop,
        Cancel, Debug, LoadCheckpoint, Restart,
        NavUp, NavDown, NavLeft, NavRight, Confirm, Choice1, Choice2
    }

    /// <summary>
    /// Thin wrapper so the rest of the code works with either the Input System package
    /// or the legacy Input Manager, whichever the project's Active Input Handling allows.
    /// </summary>
    public static class GameInput
    {
        public static float MouseSensitivity = 0.08f;

#if ENABLE_INPUT_SYSTEM
        static Key Map(GameKey k)
        {
            switch (k)
            {
                case GameKey.Forward: return Key.W;
                case GameKey.Back: return Key.S;
                case GameKey.Left: return Key.A;
                case GameKey.Right: return Key.D;
                case GameKey.Sprint: return Key.LeftShift;
                case GameKey.Interact: return Key.E;
                case GameKey.Inspect: return Key.F;
                case GameKey.CameraToggle: return Key.C;
                case GameKey.Review: return Key.R;
                case GameKey.Phone: return Key.Tab;
                case GameKey.Lamp: return Key.L;
                case GameKey.Torch: return Key.T;
                case GameKey.Drop: return Key.Q;
                case GameKey.Cancel: return Key.Escape;
                case GameKey.Debug: return Key.F1;
                case GameKey.LoadCheckpoint: return Key.F9;
                case GameKey.Restart: return Key.F12;
                case GameKey.NavUp: return Key.UpArrow;
                case GameKey.NavDown: return Key.DownArrow;
                case GameKey.NavLeft: return Key.LeftArrow;
                case GameKey.NavRight: return Key.RightArrow;
                case GameKey.Confirm: return Key.Enter;
                case GameKey.Choice1: return Key.Digit1;
                case GameKey.Choice2: return Key.Digit2;
            }
            return Key.None;
        }

        public static bool Held(GameKey k) { var kb = Keyboard.current; return kb != null && kb[Map(k)].isPressed; }
        public static bool Down(GameKey k) { var kb = Keyboard.current; return kb != null && kb[Map(k)].wasPressedThisFrame; }

        public static Vector2 LookDelta
        {
            get { var m = Mouse.current; return m == null ? Vector2.zero : m.delta.ReadValue() * MouseSensitivity; }
        }
        public static bool PrimaryDown { get { var m = Mouse.current; return m != null && m.leftButton.wasPressedThisFrame; } }
        public static bool PrimaryHeld { get { var m = Mouse.current; return m != null && m.leftButton.isPressed; } }
        public static bool SecondaryDown { get { var m = Mouse.current; return m != null && m.rightButton.wasPressedThisFrame; } }
        public static float Scroll { get { var m = Mouse.current; return m == null ? 0f : m.scroll.ReadValue().y / 120f; } }
#else
        static KeyCode Map(GameKey k)
        {
            switch (k)
            {
                case GameKey.Forward: return KeyCode.W;
                case GameKey.Back: return KeyCode.S;
                case GameKey.Left: return KeyCode.A;
                case GameKey.Right: return KeyCode.D;
                case GameKey.Sprint: return KeyCode.LeftShift;
                case GameKey.Interact: return KeyCode.E;
                case GameKey.Inspect: return KeyCode.F;
                case GameKey.CameraToggle: return KeyCode.C;
                case GameKey.Review: return KeyCode.R;
                case GameKey.Phone: return KeyCode.Tab;
                case GameKey.Lamp: return KeyCode.L;
                case GameKey.Torch: return KeyCode.T;
                case GameKey.Drop: return KeyCode.Q;
                case GameKey.Cancel: return KeyCode.Escape;
                case GameKey.Debug: return KeyCode.F1;
                case GameKey.LoadCheckpoint: return KeyCode.F9;
                case GameKey.Restart: return KeyCode.F12;
                case GameKey.NavUp: return KeyCode.UpArrow;
                case GameKey.NavDown: return KeyCode.DownArrow;
                case GameKey.NavLeft: return KeyCode.LeftArrow;
                case GameKey.NavRight: return KeyCode.RightArrow;
                case GameKey.Confirm: return KeyCode.Return;
                case GameKey.Choice1: return KeyCode.Alpha1;
                case GameKey.Choice2: return KeyCode.Alpha2;
            }
            return KeyCode.None;
        }

        public static bool Held(GameKey k) => Input.GetKey(Map(k));
        public static bool Down(GameKey k) => Input.GetKeyDown(Map(k));

        public static Vector2 LookDelta =>
            new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * (MouseSensitivity * 25f);
        public static bool PrimaryDown => Input.GetMouseButtonDown(0);
        public static bool PrimaryHeld => Input.GetMouseButton(0);
        public static bool SecondaryDown => Input.GetMouseButtonDown(1);
        public static float Scroll => Input.mouseScrollDelta.y;
#endif

        public static Vector2 Move
        {
            get
            {
                var v = Vector2.zero;
                if (Held(GameKey.Forward)) v.y += 1;
                if (Held(GameKey.Back)) v.y -= 1;
                if (Held(GameKey.Right)) v.x += 1;
                if (Held(GameKey.Left)) v.x -= 1;
                return v.sqrMagnitude > 1f ? v.normalized : v;
            }
        }

        public static void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
