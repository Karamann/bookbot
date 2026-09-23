using UnityEngine;

namespace ThirdLamp
{
    public partial class VillaBuilder
    {
        /// <summary>
        /// Third Lamp placeholder: with the lamp lit in darkness, the hallway's end wall and the basement
        /// door are gone and the corridor keeps going: narrow, then enormous, far beyond the house walls.
        /// </summary>
        void MindGeometry()
        {
            cur = Group("MindCorridor");
            const string stone = "stone";
            // narrow continuation, x 8..12
            TexBox("NarrowFloor", new Vector3(10f, -0.04f, 7f), new Vector3(4f, 0.1f, 2f), stone, 1.5f, 0.1f);
            TexBox("NarrowWallS", new Vector3(10f, 1.4f, 5.9f), new Vector3(4f, 2.8f, 0.2f), stone, 1.5f);
            TexBox("NarrowWallN", new Vector3(10f, 1.4f, 8.1f), new Vector3(4f, 2.8f, 0.2f), stone, 1.5f);
            TexBox("NarrowCeiling", new Vector3(10f, WallH + 0.075f, 7f), new Vector3(4f, 0.15f, 2.4f), stone, 1.5f);

            // the hall, x 12..62
            TexBox("HallFloor", new Vector3(37f, -0.04f, 7f), new Vector3(50f, 0.1f, 8f), stone, 2f, 0.15f);
            TexBox("HallWallS", new Vector3(37f, 4f, 2.9f), new Vector3(50f, 8f, 0.2f), stone, 2f);
            TexBox("HallWallN", new Vector3(37f, 4f, 11.1f), new Vector3(50f, 8f, 0.2f), stone, 2f);
            TexBox("HallEnd", new Vector3(62.1f, 4f, 7f), new Vector3(0.2f, 8f, 8.4f), stone, 2f);
            TexBox("EntryS", new Vector3(12f, 4f, 4.5f), new Vector3(0.2f, 8f, 3f), stone, 2f);
            TexBox("EntryN", new Vector3(12f, 4f, 9.5f), new Vector3(0.2f, 8f, 3f), stone, 2f);
            TexBox("EntryLintel", new Vector3(12f, 5.4f, 7f), new Vector3(0.2f, 5.2f, 2f), stone, 2f);
            TexBox("HallCeiling", new Vector3(37f, 8.075f, 7f), new Vector3(50.4f, 0.15f, 8.4f), stone, 2f);
            for (float x = 15f; x < 60f; x += 6f)
            {
                TexBox("ColumnS", new Vector3(x, 4f, 4.2f), new Vector3(0.8f, 8f, 0.8f), stone, 1.5f);
                TexBox("ColumnN", new Vector3(x, 4f, 9.8f), new Vector3(0.8f, 8f, 0.8f), stone, 1.5f);
            }
            Quad("GreatMark", new Vector3(61.99f, 3.6f, 7f), new Vector2(5f, 5f), Vector3.right, Mats.Lit("symbol_large", Color.white, 0.05f));
            PointLight("FarGlow", new Vector3(57f, 3f, 7f), new Color(0.8f, 0.42f, 0.18f), 0.7f, 14f, false);
            MindOnly(cur.gameObject);

            Game.Lighting.corridorBounds = new[]
            {
                new Bounds(new Vector3(9f, 1.5f, 7f), new Vector3(6f, 3f, 2f)),
                new Bounds(new Vector3(37f, 4.5f, 7f), new Vector3(50f, 9f, 8.2f)),
            };
            Game.Lighting.corridorExitPosition = new Vector3(5.2f, 0f, 7f);
            Game.Lighting.corridorExitYaw = -90f;
        }

        void Zones()
        {
            var z = Group("Zones");
            RoomZone.Create(z, "living", new Vector3(-8, -0.5f, 0), new Vector3(0, 3, 6), "living");
            RoomZone.Create(z, "kitchen", new Vector3(0, -0.5f, 0), new Vector3(8, 3, 6), "kitchen");
            RoomZone.Create(z, "hall", new Vector3(-8, -0.5f, 6), new Vector3(6, 3, 8), "hall");
            RoomZone.Create(z, "study", new Vector3(-8, -0.5f, 8), new Vector3(0, 3, 14), "study");
            RoomZone.Create(z, "bathroom", new Vector3(0, -0.5f, 8), new Vector3(3.5f, 3, 14), "bath");
            RoomZone.Create(z, "storage", new Vector3(3.5f, -0.5f, 8), new Vector3(8, 3, 14), null);
            RoomZone.Create(z, LightingStateManager.MindCorridorZone, new Vector3(6, -0.5f, 6), new Vector3(12, 3, 8), null).mindOnly = true;
            RoomZone.Create(z, LightingStateManager.MindCorridorZone, new Vector3(12, -0.5f, 3), new Vector3(62, 9, 11), null).mindOnly = true;
        }

        void Player()
        {
            var go = new GameObject("Player");
            go.transform.SetParent(root, false);
            go.transform.position = PlayerStart;
            go.layer = Game.LayerIgnoreRaycast;
            go.AddComponent<CharacterController>();
            var pc = go.AddComponent<PlayerController>();

            var camGo = new GameObject("MainCamera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            camGo.AddComponent<AudioListener>();
            cam.fieldOfView = 62f;
            cam.nearClipPlane = 0.03f;
            cam.farClipPlane = 250f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.012f, 0.018f, 0.035f);
            cam.cullingMask = ~((1 << Game.LayerMemory) | (1 << Game.LayerPlayerBody));
            pc.Setup(cam, PlayerStartYaw);
            Game.Player = pc;
            Game.MainCamera = cam;

            var interactor = go.AddComponent<PlayerInteractor>();
            interactor.Setup(cam);
            Game.Interactor = interactor;

            var beamGo = new GameObject("TorchBeam");
            beamGo.transform.SetParent(cam.transform, false);
            beamGo.transform.localPosition = new Vector3(0.18f, -0.15f, 0.1f);
            var beam = beamGo.AddComponent<Light>();
            beam.type = LightType.Spot;
            beam.spotAngle = 42f;
            beam.innerSpotAngle = 18f;
            beam.range = 14f;
            beam.color = new Color(1f, 0.9f, 0.72f);
            beam.shadows = LightShadows.Soft;
            beam.enabled = false;
            var torch = go.AddComponent<Torch>();
            torch.beam = beam;
            Game.Lighting.RegisterTorch(torch);

            var phoneLight = new GameObject("PhoneBacklight").AddComponent<Light>();
            phoneLight.transform.SetParent(cam.transform, false);
            phoneLight.transform.localPosition = new Vector3(0.25f, -0.25f, 0.35f);
            phoneLight.type = LightType.Point;
            phoneLight.color = new Color(0.55f, 0.85f, 0.5f);
            phoneLight.intensity = 0.45f;
            phoneLight.range = 2.6f;
            phoneLight.enabled = false;
            Game.Phone.backlight = phoneLight;

            var camSys = go.AddComponent<CameraSystem>();
            camSys.Setup(cam);
            Game.PhotoCamera = camSys;
        }
    }
}
