using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThirdLamp
{
    /// <summary>
    /// Entry point. Creates the systems, builds the villa from code, registers the slice's narrative
    /// data, then either plays the intro or restores a checkpoint.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class SliceBootstrap : MonoBehaviour
    {
        void Awake()
        {
            Game.Reset();
            RoomZone.Clear();
            PlacementSocket.Log.Clear();
            Time.timeScale = 1f;
            Application.targetFrameRate = 120;

            var sys = new GameObject("Systems").transform;
            sys.SetParent(transform, false);
            T Add<T>() where T : Component => new GameObject(typeof(T).Name).AddComponent<T>().Also(c => c.transform.SetParent(sys, false));

            Game.World = new WorldRegistry();
            Game.Catalogue = new CatalogueDatabase();
            Game.State = Add<GameState>();
            Game.Perception = Add<PerceptionManager>();
            Game.Director = Add<EventDirector>();
            Game.Audio = Add<AudioEventManager>();
            Game.Audio.Setup();
            Game.Lighting = Add<LightingStateManager>();
            Game.Phone = Add<PhoneSystem>();
            Game.Inspector = Add<Inspector>();
            Game.Hud = Add<Hud>();
            Game.Fx = Add<ScreenFx>();
            Game.Fx.Setup();
            Game.Save = Add<SaveManager>();

            var world = new GameObject("World").transform;
            world.SetParent(transform, false);
            new VillaBuilder(world).Build();

            SliceContent.RegisterCatalogue(Game.Catalogue);
            SliceContent.RegisterEvents(Game.Director);
            UrpPostFx.Setup(Game.MainCamera, Game.Lighting);

            GameInput.LockCursor(true);
        }

        void Start()
        {
            var pending = SaveManager.Pending;
            SaveManager.Pending = null;
            if (pending != null)
            {
                Game.Save.Apply(pending);
                Game.Fx.SkipIntro();
                Game.Mode = InputMode.Play;
            }
            else
            {
                Game.State.clockRunning = true;
                Game.Fx.StartCoroutine(Game.Fx.Intro());
            }
        }

        void OnApplicationFocus(bool focus)
        {
            if (focus && Game.Mode != InputMode.Paused) GameInput.LockCursor(true);
        }

        /// <summary>Fallback: if the Slice scene's bootstrap reference was lost, create one.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void EnsureBootstrap()
        {
            if (SceneManager.GetActiveScene().name != "Slice") return;
            if (FindFirstObjectByType<SliceBootstrap>() != null) return;
            new GameObject("SliceBootstrap").AddComponent<SliceBootstrap>();
        }
    }

    static class ComponentExtensions
    {
        public static T Also<T>(this T c, System.Action<T> a) where T : Component
        {
            a(c);
            return c;
        }
    }
}
