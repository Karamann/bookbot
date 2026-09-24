using UnityEngine;

namespace ThirdLamp
{
    public partial class VillaBuilder
    {
        static readonly Color Warm = new Color(1f, 0.74f, 0.47f);
        static readonly Color Fluoro = new Color(0.9f, 0.96f, 1f);

        void Environment()
        {
            var L = Game.Lighting;
            L.bulbOnMaterial = Mats.UnlitColor(new Color(1f, 0.93f, 0.78f));
            L.bulbOffMaterial = Mats.Color(new Color(0.75f, 0.73f, 0.68f), 0.6f);

            RenderSettings.skybox = null;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = L.reasonAmbient;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = L.reasonFog;
            RenderSettings.fogDensity = L.reasonFogDensity;

            var moon = new GameObject("Moonlight").AddComponent<Light>();
            moon.transform.SetParent(root, false);
            moon.transform.rotation = Quaternion.Euler(28f, 150f, 0f);
            moon.type = LightType.Directional;
            moon.color = new Color(0.55f, 0.64f, 0.95f);
            moon.intensity = 0.14f;
            moon.shadows = LightShadows.Soft;
            moon.shadowStrength = 0.9f;
        }

        /// <summary>Pale window-shaped patches of moonlight thrown onto the floor from outside.</summary>
        void MoonShafts()
        {
            if (!Tex.Exists("window_cookie")) return;
            var cookie = Tex.Get("window_cookie");
            (Vector3 pos, Vector3 look)[] shafts =
            {
                (new Vector3(-10.2f, 3.6f, 3f), new Vector3(1f, -0.62f, 0f)),     // living, west window
                (new Vector3(-5.5f, 3.4f, 16.2f), new Vector3(0f, -0.55f, -1f)),  // study, north window
                (new Vector3(10.2f, 3.6f, 3f), new Vector3(-1f, -0.62f, 0f)),     // kitchen, east window
            };
            foreach (var (pos, look) in shafts)
            {
                var l = new GameObject("MoonShaft").AddComponent<Light>();
                l.transform.SetParent(root, false);
                l.transform.position = pos;
                l.transform.rotation = Quaternion.LookRotation(look.normalized);
                l.type = LightType.Spot;
                l.spotAngle = 38f;
                l.range = 9f;
                l.intensity = 1.1f;
                l.color = new Color(0.6f, 0.7f, 1f);
                l.cookie = cookie;
                l.shadows = LightShadows.None;
            }
        }

        void Grounds()
        {
            cur = Group("Grounds");
            TexBox("Ground", new Vector3(0, -0.2f, 0), new Vector3(200, 0.2f, 200), "dirt", 4f, 0.02f);
            TexBox("Driveway", new Vector3(-4f, -0.095f, -13f), new Vector3(3.4f, 0.01f, 26f), "gravel", 1.5f, 0.02f);
            TexBox("Road", new Vector3(0, -0.095f, -31f), new Vector3(220f, 0.01f, 5.5f), "asphalt", 3f, 0.1f);
            TexBox("FrontStep", new Vector3(-4f, -0.05f, -0.45f), new Vector3(1.8f, 0.1f, 0.9f), "stone", 1f, 0.05f);

            // boundary wall, with gaps for the drive and for the thing that isn't there
            var stoneMat = "stone";
            TexBox("WallS_W", new Vector3(-13.5f, 0.35f, -26f), new Vector3(15f, 0.9f, 0.4f), stoneMat, 1.5f);
            TexBox("WallS_E", new Vector3(8.5f, 0.35f, -26f), new Vector3(23f, 0.9f, 0.4f), stoneMat, 1.5f);
            TexBox("WallW", new Vector3(-21f, 0.35f, -1f), new Vector3(0.4f, 0.9f, 50f), stoneMat, 1.5f);
            TexBox("WallN", new Vector3(0f, 0.35f, 24f), new Vector3(42f, 0.9f, 0.4f), stoneMat, 1.5f);
            TexBox("WallE_S", new Vector3(20f, 0.35f, -12f), new Vector3(0.4f, 0.9f, 28f), stoneMat, 1.5f);
            TexBox("WallE_N", new Vector3(20f, 0.35f, 18f), new Vector3(0.4f, 0.9f, 12f), stoneMat, 1.5f);

            // olive trees
            var bark = Mats.Color(new Color(0.22f, 0.18f, 0.14f), 0.05f);
            var leaves = Mats.Color(new Color(0.16f, 0.2f, 0.15f), 0.05f);
            Vector2[] trees = { new Vector2(-12, -6), new Vector2(-14, 4), new Vector2(-11, 15), new Vector2(10, -11), new Vector2(15, -5),
                                new Vector2(13, 17), new Vector2(-6, 19), new Vector2(4, 20), new Vector2(16, -18), new Vector2(-16, -18) };
            var r = new System.Random(3);
            foreach (var t in trees)
            {
                var g = Group("Olive", cur);
                g.localPosition = new Vector3(t.x, 0, t.y);
                g.localRotation = Quaternion.Euler(0, r.Next(0, 360), 0);
                Cyl("Trunk", new Vector3(0, 0.8f, 0), 0.32f, 1.6f, bark, g);
                string canopy = r.Next(0, 2) == 0 ? "olive_canopy_a" : "olive_canopy_b";
                if (Cross("Canopy", canopy, new Vector3(0, 0.9f, 0), 3.4f + (float)r.NextDouble() * 1.2f, 0f, g, new Color(0.75f, 0.78f, 0.75f)) != null) continue;
                for (int i = 0; i < 4; i++)
                    Prim(PrimitiveType.Sphere, "Crown", new Vector3((float)(r.NextDouble() - 0.5) * 2f, 2.1f + (float)r.NextDouble() * 0.8f, (float)(r.NextDouble() - 0.5) * 2f),
                        Vector3.one * (1.6f + (float)r.NextDouble()), leaves, g, false);
            }

            // cypresses along the north wall, dry weeds along every boundary
            for (float x = -18f; x <= 18f; x += 4.5f)
                Cross("Cypress", "cypress", new Vector3(x + (float)(r.NextDouble() - 0.5), 0, 22.6f), 6.5f + (float)r.NextDouble() * 1.5f, r.Next(0, 90), null, new Color(0.7f, 0.72f, 0.7f));
            for (int i = 0; i < 36; i++)
            {
                float t = (float)r.NextDouble();
                Vector3 p = (i % 4) switch
                {
                    0 => new Vector3(-20.5f, 0, Mathf.Lerp(-25f, 23f, t)),
                    1 => new Vector3(19.5f, 0, Mathf.Lerp(-25f, 23f, t)),
                    2 => new Vector3(Mathf.Lerp(-20f, 19f, t), 0, 23.5f),
                    _ => new Vector3(Mathf.Lerp(-8.5f, 8.5f, t), 0, i % 8 == 3 ? -0.35f : 14.35f),
                };
                Cross("Weeds", "dry_weeds", p, 0.45f + (float)r.NextDouble() * 0.35f, r.Next(0, 90));
            }

            // distant hills (sprite ridge when generated, dark mounds otherwise) and the glow of the city
            if (!Tex.Exists("hills_silhouette"))
            {
                var hill = Mats.UnlitColor(new Color(0.012f, 0.015f, 0.022f));
                Prim(PrimitiveType.Sphere, "HillN", new Vector3(0, -20, 140), new Vector3(260, 70, 60), hill, null, false);
                Prim(PrimitiveType.Sphere, "HillE", new Vector3(150, -25, 30), new Vector3(60, 70, 240), hill, null, false);
            }
            Quad("CityGlow", new Vector3(-90, 8, -150), new Vector2(260, 40), new Vector3(0.5f, 0, -1f).normalized, Mats.Unlit("night_sky_glow"));

            // Second Lamp, later: someone outside the living-room window, looking in
            Figure("apparition_window", new Vector3(-9.4f, -0.1f, 3f), 90f, false, "figure_window_silhouette", 1.85f);

            // Naked eye, much later: far out in the garden, pale, like someone who stepped out of a painting.
            // Gone the moment it is looked at directly.
            var garden = Sprite("GardenFigure", "figure_pale_robed", new Vector3(-17.5f, 0, 2.2f), 1.8f, true,
                Mats.Cutout("figure_pale_robed", new Color(1.35f, 1.35f, 1.3f), 0.5f));
            if (garden != null)
            {
                garden.AddComponent<Glimpse>().id = "garden";
                Game.World.Register("garden_figure", garden);
                garden.SetActive(false);
            }

            Car();
            GeneratorShed();
            PassingCar();

            // the blue pot with the key under it
            var pot = Cyl("BluePot", new Vector3(-5.15f, 0.12f, -0.55f), 0.45f, 0.45f, Mats.Color(new Color(0.12f, 0.25f, 0.55f), 0.6f));
            if (Cross("Geranium", "geranium", new Vector3(-5.15f, 0.3f, -0.55f), 0.55f, 20f) == null)
                Prim(PrimitiveType.Sphere, "Geranium", new Vector3(-5.15f, 0.48f, -0.55f), new Vector3(0.5f, 0.35f, 0.5f), Mats.Color(new Color(0.15f, 0.25f, 0.12f), 0.1f), null, false);
            var potPick = pot.AddComponent<FlagPickup>();
            potPick.flag = "has_front_key";
            potPick.verb = "Lift";
            potPick.itemName = "the blue pot";
            potPick.hideOnPickup = false;
            potPick.sound = "drawer";
            potPick.message = "A key, and a dead beetle. Right where she said.";

            // porch light above the front door (switched from inside)
            Pendant("porch", new Vector3(-4f, 2.35f, -0.35f), Warm, 0.9f, 6f, true, false);
        }

        void Car()
        {
            var g = Group("Car", cur);
            g.localPosition = new Vector3(-4.3f, 0, -13f);
            var paint = Tex.Exists("car_paint") ? Mats.Lit("car_paint", Color.white, 0.55f, 2f, 2f) : Mats.Color(new Color(0.1f, 0.14f, 0.25f), 0.55f);
            var glass = Mats.Glass(new Color(0.06f, 0.08f, 0.1f, 0.55f));
            var trim = Mats.Color(new Color(0.06f, 0.06f, 0.06f), 0.3f);
            var seat = Mats.Lit("fabric", new Color(0.35f, 0.35f, 0.38f), 0.05f);
            var tyre = Mats.Color(new Color(0.05f, 0.05f, 0.05f), 0.1f);
            Box("Body", new Vector3(0, 0.55f, 0), new Vector3(1.66f, 0.6f, 3.8f), paint, g);
            Box("Cabin", new Vector3(0, 1.1f, -0.2f), new Vector3(1.5f, 0.52f, 2.1f), glass, g);
            Box("Roof", new Vector3(0, 1.37f, -0.25f), new Vector3(1.46f, 0.04f, 1.8f), paint, g);
            foreach (float x in new[] { -0.73f, 0.73f })
                foreach (float z in new[] { -1.2f, 0.12f, 0.8f })
                    Box("Pillar", new Vector3(x, 1.1f, z), new Vector3(0.05f, 0.52f, 0.06f), paint, g, false);
            Box("BumperF", new Vector3(0, 0.36f, 1.93f), new Vector3(1.7f, 0.14f, 0.1f), trim, g, false);
            Box("BumperR", new Vector3(0, 0.38f, -1.93f), new Vector3(1.7f, 0.14f, 0.1f), trim, g, false);
            Box("PlateF", new Vector3(0, 0.36f, 1.985f), new Vector3(0.44f, 0.1f, 0.01f), Mats.Color(new Color(0.85f, 0.85f, 0.8f), 0.3f), g, false);
            Box("PlateR", new Vector3(0, 0.5f, -1.915f), new Vector3(0.44f, 0.1f, 0.01f), Mats.Color(new Color(0.85f, 0.85f, 0.8f), 0.3f), g, false);
            Box("Grille", new Vector3(0, 0.55f, 1.905f), new Vector3(0.7f, 0.12f, 0.02f), trim, g, false);
            Box("SeatFL", new Vector3(-0.38f, 0.95f, 0.2f), new Vector3(0.5f, 0.12f, 0.5f), seat, g, false);
            Box("SeatFR", new Vector3(0.38f, 0.95f, 0.2f), new Vector3(0.5f, 0.12f, 0.5f), seat, g, false);
            Box("BackFL", new Vector3(-0.38f, 1.2f, -0.05f), new Vector3(0.5f, 0.5f, 0.1f), seat, g, false);
            Box("BackFR", new Vector3(0.38f, 1.2f, -0.05f), new Vector3(0.5f, 0.5f, 0.1f), seat, g, false);
            Box("RearSeat", new Vector3(0, 1.0f, -0.85f), new Vector3(1.3f, 0.35f, 0.45f), seat, g, false);
            Box("Dash", new Vector3(0, 1.0f, 0.75f), new Vector3(1.4f, 0.14f, 0.3f), trim, g, false);
            foreach (float x in new[] { -0.86f, 0.86f })
                Box("Mirror", new Vector3(x, 1.02f, 0.72f), new Vector3(0.1f, 0.08f, 0.04f), trim, g, false);
            foreach (var p in new[] { new Vector3(-0.78f, 0.3f, 1.2f), new Vector3(0.78f, 0.3f, 1.2f), new Vector3(-0.78f, 0.3f, -1.2f), new Vector3(0.78f, 0.3f, -1.2f) })
                Prim(PrimitiveType.Cylinder, "Wheel", p, new Vector3(0.58f, 0.1f, 0.58f), tyre, g, false, Quaternion.Euler(0, 0, 90));
            var lampMat = Mats.UnlitColor(new Color(1f, 0.95f, 0.8f));
            // the cabin must not hide the seats behind an opaque block
            g.Find("Cabin").GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var tail = Mats.UnlitColor(new Color(0.5f, 0.02f, 0.02f));
            var lights = new System.Collections.Generic.List<Light>();
            foreach (float x in new[] { -0.55f, 0.55f })
            {
                Box("Headlamp", new Vector3(x, 0.62f, 1.91f), new Vector3(0.32f, 0.14f, 0.02f), lampMat, g, false);
                Box("Tail", new Vector3(x, 0.66f, -1.91f), new Vector3(0.26f, 0.12f, 0.02f), tail, g, false);
                var beam = new GameObject("Beam").AddComponent<Light>();
                beam.transform.SetParent(g, false);
                beam.transform.localPosition = new Vector3(x, 0.62f, 2f);
                beam.transform.localRotation = Quaternion.Euler(5f, 0, 0);
                beam.type = LightType.Spot;
                beam.spotAngle = 58f;
                beam.range = 24f;
                beam.intensity = 2.2f;
                beam.color = new Color(1f, 0.93f, 0.78f);
                beam.shadows = x < 0 ? LightShadows.Soft : LightShadows.None;
                lights.Add(beam);
            }
            var car = g.gameObject.AddComponent<CarInteractable>();
            car.headlights = lights;
            Game.Audio.Loop("engine_tick", g, new Vector3(0, 0.5f, 1.5f), 0.5f, 1f, 8f);
        }

        void GeneratorShed()
        {
            var g = Group("Shed", cur);
            g.localPosition = new Vector3(11.2f, 0, -2.5f);
            var wood = Mats.Lit("wood_door", new Color(0.7f, 0.7f, 0.7f), 0.1f);
            TexBox("Slab", new Vector3(0, -0.05f, 0), new Vector3(2.6f, 0.1f, 2.2f), "stone", 1f, 0.05f, g);
            foreach (var p in new[] { new Vector3(-1.2f, 1.1f, -1f), new Vector3(1.2f, 1.1f, -1f), new Vector3(-1.2f, 1.1f, 1f), new Vector3(1.2f, 1.1f, 1f) })
                Box("Post", p, new Vector3(0.1f, 2.2f, 0.1f), wood, g);
            var roofMat = Tex.Exists("corrugated_iron") ? Mats.Lit("corrugated_iron", Color.white, 0.25f, 2f, 2f) : Mats.Color(new Color(0.35f, 0.32f, 0.3f), 0.2f);
            Box("Roof", new Vector3(0, 2.25f, 0), new Vector3(2.8f, 0.08f, 2.4f), roofMat, g, true, Quaternion.Euler(0, 0, 6));
            Box("BackWall", new Vector3(1.25f, 1.0f, 0), new Vector3(0.06f, 2.0f, 2.1f), wood, g);

            var gen = Box("Generator", new Vector3(0.2f, 0.35f, 0), new Vector3(0.6f, 0.55f, 0.9f), Mats.Color(new Color(0.55f, 0.1f, 0.06f), 0.4f), g);
            Box("Engine", new Vector3(0.1f, 0.72f, 0.1f), new Vector3(0.35f, 0.2f, 0.45f), Mats.Color(new Color(0.15f, 0.15f, 0.15f), 0.3f), gen.transform.parent);
            Box("Can", new Vector3(0.6f, 0.2f, 0.7f), new Vector3(0.2f, 0.4f, 0.3f), Mats.Color(new Color(0.2f, 0.35f, 0.15f), 0.3f), g);
            gen.AddComponent<Generator>();
            Game.World.Register("generator", gen);
            var loop = Game.Audio.Loop("generator", gen.transform, Vector3.zero, 0.75f, 2f, 45f);
            Game.Lighting.RegisterPoweredAudio(loop);
        }

        void PassingCar()
        {
            var g = Group("PassingCar", cur);
            var mover = g.gameObject.AddComponent<Mover>();
            mover.from = new Vector3(-95f, 0.6f, -31.5f);
            mover.to = new Vector3(95f, 0.6f, -31.5f);
            mover.duration = 15f;
            foreach (float z in new[] { -0.55f, 0.55f })
            {
                var beam = new GameObject("Beam").AddComponent<Light>();
                beam.transform.SetParent(g, false);
                beam.transform.localPosition = new Vector3(2f, 0, z);
                beam.transform.localRotation = Quaternion.Euler(4f, 90f, 0);
                beam.type = LightType.Spot;
                beam.spotAngle = 60f;
                beam.range = 40f;
                beam.intensity = 2.5f;
                beam.color = new Color(1f, 0.95f, 0.85f);
            }
            Game.Audio.Loop("engine", g, Vector3.zero, 0.8f, 4f, 70f);
            Game.World.Register("passing_car", g.gameObject);
            g.gameObject.SetActive(false);
        }

        // =====================================================================
        // house shell
        // =====================================================================

        void Shell()
        {
            cur = Group("House");
            // floors
            TexBox("Floor_Living", new Vector3(-4, -0.05f, 3), new Vector3(8, 0.1f, 6), "wood_floor", 1.6f, 0.25f);
            TexBox("Floor_Kitchen", new Vector3(4, -0.05f, 3), new Vector3(8, 0.1f, 6), "tile_kitchen", 1.2f, 0.35f);
            TexBox("Floor_Hall", new Vector3(0, -0.05f, 7), new Vector3(16, 0.1f, 2), "wood_floor", 1.6f, 0.25f);
            TexBox("Floor_Study", new Vector3(-4, -0.05f, 11), new Vector3(8, 0.1f, 6), "wood_dark", 1.6f, 0.2f);
            TexBox("Floor_Bath", new Vector3(1.75f, -0.05f, 11), new Vector3(3.5f, 0.1f, 6), "tile_bath", 0.8f, 0.4f);
            TexBox("Floor_Storage", new Vector3(5.75f, -0.05f, 11), new Vector3(4.5f, 0.1f, 6), "stone", 1.5f, 0.05f);
            TexBox("Ceiling", new Vector3(0, WallH + 0.075f, 7), new Vector3(16, 0.15f, 14), "ceiling", 2f, 0.02f);
            TexBox("Roof", new Vector3(0, WallH + 0.25f, 7), new Vector3(16.4f, 0.2f, 14.4f), "plaster_ext", 2f, 0.02f);
            TexBox("ParapetS", new Vector3(0, 3.35f, -0.1f), new Vector3(16.4f, 0.4f, 0.2f), "plaster_ext", 2f);
            TexBox("ParapetN", new Vector3(0, 3.35f, 14.1f), new Vector3(16.4f, 0.4f, 0.2f), "plaster_ext", 2f);
            TexBox("ParapetW", new Vector3(-8.1f, 3.35f, 7), new Vector3(0.2f, 0.4f, 14.4f), "plaster_ext", 2f);
            TexBox("ParapetE", new Vector3(8.1f, 3.35f, 7), new Vector3(0.2f, 0.4f, 14.4f), "plaster_ext", 2f);

            // exterior walls
            Wall("Wall_S", -8, 0, 8, 0, "plaster_ext", WindowOp(1.6f), DoorOp(4f, 1.0f), WindowOp(10.5f), WindowOp(13.5f));
            Wall("Wall_N", -8, 14, 8, 14, "plaster_ext", WindowOp(2.5f), WindowOp(9.75f, 0.6f, 1.5f, 2.1f), WindowOp(13.5f, 0.6f, 1.6f, 2.1f));
            Wall("Wall_W", -8, 0, -8, 14, "plaster_ext", WindowOp(3f), WindowOp(7f, 0.6f, 1.2f, 2.0f));
            Wall("Wall_E_Kitchen", 8, 0, 8, 6, "plaster_ext", WindowOp(3f));
            ReasonOnly(Wall("Wall_E_Stair", 8, 6, 8, 8, "plaster_ext"));
            Wall("Wall_E_Storage", 8, 8, 8, 14, "plaster_ext");

            // interior walls
            Wall("Wall_LivingHall", -8, 6, 8, 6, "plaster", DoorOp(4f), DoorOp(11f));
            Wall("Wall_HallNorth", -8, 8, 8, 8, "plaster", DoorOp(4f), DoorOp(9.75f, 0.8f), DoorOp(13f));
            Wall("Wall_LivingKitchen", 0, 0, 0, 6, "plaster", DoorOp(3f, 1.4f, 2.25f));
            Wall("Wall_StudyBath", 0, 8, 0, 14, "plaster");
            Wall("Wall_BathStorage", 3.5f, 8, 3.5f, 14, "plaster");
            var endWall = Wall("Wall_HallEnd", 6, 6, 6, 8, "plaster", DoorOp(1f));
            ReasonOnly(endWall);

            // doors
            var front = Door("door_front", new Vector3(-4.5f, 0, 0), true, 1.0f, -1f, false, "front door");
            front.locked = true;
            front.unlockFlag = "has_front_key";
            front.lockedText = "Locked. The key should be under a blue pot.";
            front.unlockText = "The key turns, stiffly.";
            Door("door_living_hall", new Vector3(-4.45f, 0, 6f), true, 0.9f, 1f, true);
            Door("door_kitchen_hall", new Vector3(2.55f, 0, 6f), true, 0.9f, 1f, true);
            Door("door_study", new Vector3(-4.45f, 0, 8f), true, 0.9f, -1f, false, "study door");
            Door("door_bath", new Vector3(1.35f, 0, 8f), true, 0.8f, -1f, false, "bathroom door");
            var storage = Door("door_storage", new Vector3(4.55f, 0, 8f), true, 0.9f, -1f, false, "storage door");
            storage.locked = true;
            storage.lockedText = "Locked. Not mentioned in the instructions.";

            var basementGroup = Group("BasementDoorGroup", cur);
            var prev = cur;
            cur = basementGroup;
            var basement = Door("door_basement", new Vector3(6f, 0, 6.55f), false, 0.9f, 1f, false, "basement door",
                Mats.Lit("wood_door", new Color(0.55f, 0.5f, 0.45f), 0.2f));
            basement.locked = true;
            basement.lockedText = "The basement. Instruction five.";
            Label3D("ΥΠΟΓΕΙΟ", new Vector3(5.88f, 2.25f, 7f), Vector3.right, 0.07f, new Color(0.2f, 0.18f, 0.15f));
            cur = prev;
            ReasonOnly(basementGroup.gameObject);
        }
    }

    /// <summary>Alex's car. Headlights were left on.</summary>
    public class CarInteractable : Interactable
    {
        public System.Collections.Generic.List<Light> headlights = new System.Collections.Generic.List<Light>();
        bool On => headlights.Count > 0 && headlights[0].enabled;

        public override string Prompt => On ? "Switch off the headlights" : "Switch on the headlights";

        public override void Interact()
        {
            bool next = !On;
            foreach (var l in headlights) l.enabled = next;
            Game.Audio.PlayAt("switch", transform.position, 0.5f, 0.8f);
            if (!Game.State.Has("car_used"))
            {
                Game.State.Set("car_used");
                Game.Hud.Subtitle("Three hundred euros. One night. Then home.", 3.5f);
            }
        }
    }
}
