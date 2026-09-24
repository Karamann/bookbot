using UnityEngine;

namespace ThirdLamp
{
    public enum InputMode { Play, Reading, Inspect, Computer, Call, Cinematic, Paused, Ended }

    /// <summary>
    /// Static access point for the slice's single-instance systems. Filled in by SliceBootstrap.
    /// </summary>
    public static class Game
    {
        // Numeric layers so the project needs no TagManager edits. Names are cosmetic only.
        public const int LayerIgnoreRaycast = 2;
        public const int LayerPlayerBody = 27; // seen only by mirrors
        public const int LayerMirror = 28;     // mirror surfaces, hidden from mirror cameras
        public const int LayerMemory = 29;     // "Second Lamp": visible only through lenses and mirrors
        public const int LayerViewmodel = 30;  // held items, main camera only

        public static GameState State;
        public static PerceptionManager Perception;
        public static EventDirector Director;
        public static WorldRegistry World;
        public static LightingStateManager Lighting;
        public static AudioEventManager Audio;
        public static PhoneSystem Phone;
        public static CameraSystem PhotoCamera;
        public static PlayerController Player;
        public static PlayerInteractor Interactor;
        public static Inspector Inspector;
        public static Hud Hud;
        public static ScreenFx Fx;
        public static SaveManager Save;
        public static CatalogueDatabase Catalogue;

        public static Camera MainCamera;
        static InputMode mode = InputMode.Play;

        /// <summary>Frame on which Mode last changed. Input handlers ignore that frame so one key press
        /// can't both close one screen and trigger something in the next.</summary>
        public static int ModeChangedFrame { get; private set; } = -1;

        public static InputMode Mode
        {
            get => mode;
            set
            {
                if (mode == value) return;
                mode = value;
                ModeChangedFrame = Time.frameCount;
            }
        }

        /// <summary>In free play, and not on the frame we just returned to it.</summary>
        public static bool PlayStable => mode == InputMode.Play && ModeChangedFrame != Time.frameCount;
        public static bool ModeJustChanged => ModeChangedFrame == Time.frameCount;

        public static void Reset()
        {
            State = null; Perception = null; Director = null; World = null; Lighting = null;
            Audio = null; Phone = null; PhotoCamera = null; Player = null; Interactor = null;
            Inspector = null; Hud = null; Fx = null; Save = null; Catalogue = null; MainCamera = null;
            mode = InputMode.Play;
            ModeChangedFrame = -1;
        }
    }
}
