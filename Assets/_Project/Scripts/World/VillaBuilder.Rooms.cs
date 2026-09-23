using UnityEngine;

namespace ThirdLamp
{
    public partial class VillaBuilder
    {
        Material WoodDark => Mats.Lit("wood_dark", Color.white, 0.3f);
        Material WoodMid => Mats.Lit("wood_floor", new Color(0.9f, 0.85f, 0.8f), 0.3f);

        void Table(string name, Vector3 topCenter, Vector3 topSize, Material mat, Transform parent = null)
        {
            var g = Group(name, parent != null ? parent : cur);
            g.localPosition = topCenter;
            Box("Top", Vector3.zero, topSize, mat, g);
            float lx = topSize.x / 2f - 0.05f, lz = topSize.z / 2f - 0.05f, lh = topCenter.y;
            foreach (var p in new[] { new Vector3(-lx, 0, -lz), new Vector3(lx, 0, -lz), new Vector3(-lx, 0, lz), new Vector3(lx, 0, lz) })
                Box("Leg", p + new Vector3(0, -lh / 2f, 0), new Vector3(0.05f, lh, 0.05f), mat, g);
        }

        GameObject Chair(string name, Vector3 pos, float yaw, Material mat)
        {
            var g = Group(name, cur);
            g.localPosition = pos;
            g.localRotation = Quaternion.Euler(0, yaw, 0);
            Box("Seat", new Vector3(0, 0.45f, 0), new Vector3(0.44f, 0.04f, 0.44f), mat, g);
            Box("Back", new Vector3(0, 0.72f, -0.2f), new Vector3(0.44f, 0.5f, 0.04f), mat, g);
            foreach (var p in new[] { new Vector3(-0.19f, 0.22f, -0.19f), new Vector3(0.19f, 0.22f, -0.19f), new Vector3(-0.19f, 0.22f, 0.19f), new Vector3(0.19f, 0.22f, 0.19f) })
                Box("Leg", p, new Vector3(0.04f, 0.44f, 0.04f), mat, g);
            return g.gameObject;
        }

        PlacementSocket Socket(string itemId, Vector3 pos, Vector3 labelIntoWall, Vector3 labelOffset)
        {
            var g = Group("Socket_" + itemId, cur);
            g.localPosition = pos;
            var anchor = Group("Anchor", g);
            var trigger = g.gameObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(0.35f, 0.25f, 0.35f);
            trigger.center = new Vector3(0, 0.12f, 0);
            var s = g.gameObject.AddComponent<PlacementSocket>();
            s.Setup("socket_" + itemId, itemId, anchor, trigger);
            Box("Plate", labelOffset, new Vector3(0.1f, 0.035f, 0.004f), Mats.Color(new Color(0.7f, 0.56f, 0.3f), 0.7f), g, false, Quaternion.LookRotation(labelIntoWall));
            Label3D(itemId, labelOffset - labelIntoWall.normalized * 0.004f, labelIntoWall, 0.022f, new Color(0.12f, 0.08f, 0.04f), g);
            Game.World.Register("socket_" + itemId, g.gameObject);
            return s;
        }

        // =====================================================================
        void Living()
        {
            cur = Group("Living");
            var fabric = Mats.Lit("fabric", Color.white, 0.05f);
            Box("Rug", new Vector3(-6f, 0.005f, 3f), new Vector3(2.8f, 0.01f, 2.2f), Mats.Lit("rug", Color.white, 0.05f), null, false);

            Box("SofaBase", new Vector3(-7.3f, 0.25f, 3f), new Vector3(0.9f, 0.5f, 2.0f), fabric);
            Box("SofaBack", new Vector3(-7.72f, 0.7f, 3f), new Vector3(0.22f, 0.8f, 2.0f), fabric);
            Box("SofaArmA", new Vector3(-7.3f, 0.45f, 1.93f), new Vector3(0.9f, 0.45f, 0.16f), fabric);
            Box("SofaArmB", new Vector3(-7.3f, 0.45f, 4.07f), new Vector3(0.9f, 0.45f, 0.16f), fabric);
            Table("CoffeeTable", new Vector3(-6.1f, 0.42f, 3f), new Vector3(0.6f, 0.04f, 1.1f), WoodDark);
            Note("Newspaper", new Vector3(-6.05f, 0.448f, 2.8f), new Vector3(0.32f, 0.008f, 0.42f), "newspaper",
                "Read", "the newspaper", SliceText.Newspaper, DocStyle.Newspaper, 0, "read_obituary");

            Box("TVStand", new Vector3(-4.95f, 0.28f, 3f), new Vector3(0.5f, 0.56f, 1.1f), WoodDark);
            Box("TV", new Vector3(-4.97f, 0.83f, 3f), new Vector3(0.5f, 0.5f, 0.6f), Mats.Color(new Color(0.12f, 0.12f, 0.13f), 0.3f));
            Quad("TVScreen", new Vector3(-5.225f, 0.84f, 3f), new Vector2(0.46f, 0.36f), Vector3.right, Mats.Lit("tv_off", Color.white, 0.8f));

            var armchair = Group("Armchair", cur);
            armchair.localPosition = new Vector3(-6.1f, 0, 5.2f);
            Box("Seat", new Vector3(0, 0.25f, 0), new Vector3(0.8f, 0.5f, 0.75f), fabric, armchair);
            Box("Back", new Vector3(0, 0.7f, 0.32f), new Vector3(0.8f, 0.8f, 0.18f), fabric, armchair);

            // bookshelf on the south wall
            Box("Bookshelf", new Vector3(-1.9f, 1.0f, 0.3f), new Vector3(2.0f, 2.0f, 0.36f), WoodDark);
            Quad("Books", new Vector3(-1.9f, 1.0f, 0.485f), new Vector2(1.9f, 1.9f), Vector3.back, Mats.Lit("books", Color.white, 0.1f));

            // display cabinet: home of No. 014
            var cab = Group("DisplayCabinet", cur);
            cab.localPosition = new Vector3(-0.35f, 0, 1.15f);
            Box("Back", new Vector3(0.2f, 0.95f, 0), new Vector3(0.04f, 1.9f, 1.2f), WoodDark, cab);
            Box("SideA", new Vector3(0, 0.95f, -0.58f), new Vector3(0.42f, 1.9f, 0.04f), WoodDark, cab);
            Box("SideB", new Vector3(0, 0.95f, 0.58f), new Vector3(0.42f, 1.9f, 0.04f), WoodDark, cab);
            Box("Top", new Vector3(0, 1.9f, 0), new Vector3(0.42f, 0.04f, 1.2f), WoodDark, cab);
            foreach (float y in new[] { 0.08f, 0.55f, 1.0f, 1.45f })
                Box("Shelf", new Vector3(0, y, 0), new Vector3(0.4f, 0.03f, 1.16f), WoodDark, cab);
            var vase = Mats.Color(new Color(0.55f, 0.32f, 0.2f), 0.3f);
            Cyl("Vase", new Vector3(0, 1.6f, -0.3f), 0.14f, 0.26f, vase, cab);
            Cyl("Bowl", new Vector3(0, 0.62f, 0.25f), 0.24f, 0.08f, vase, cab);
            var prev = cur;
            cur = cab;
            Socket("014", new Vector3(0, 1.015f, -0.1f), Vector3.right, new Vector3(-0.19f, -0.04f, 0));
            cur = prev;

            // the painting, No. 031
            var painting = Group("Painting_031", cur);
            painting.localPosition = new Vector3(-6.4f, 1.6f, 5.86f);
            Box("Frame", Vector3.zero, new Vector3(1.32f, 1.02f, 0.06f), Mats.Color(new Color(0.5f, 0.38f, 0.16f), 0.55f), painting);
            var canvas = Quad("Canvas", new Vector3(0, 0, -0.032f), new Vector2(1.16f, 0.87f), Vector3.forward, Mats.Lit("painting_eight", Color.white, 0.25f), painting);
            var states = painting.gameObject.AddComponent<MaterialStates>();
            states.target = canvas.GetComponent<Renderer>();
            states.Add("eight", Mats.Lit("painting_eight", Color.white, 0.25f));
            states.Add("nine", Mats.Lit("painting_nine", Color.white, 0.25f));
            states.SetState("eight");
            var pInspect = painting.gameObject.AddComponent<InspectableObject>();
            pInspect.itemId = "031";
            pInspect.displayName = "\"Gathering at the Spring\"";
            pInspect.description = "Oil on canvas, unsigned. Figures at a spring at dusk. Leave on the wall.";
            pInspect.canPickUp = false;
            pInspect.displayRenderer = canvas.GetComponent<Renderer>();
            pInspect.symbolUV = new Vector2(262f / 320f, 51f / 240f);
            pInspect.symbolNote = "On the rock by the water, painted small: a circle, a line, a triangle.";
            Game.World.Register("painting_031", painting.gameObject);
            Label3D("031", new Vector3(-6.4f, 0.98f, 5.885f), Vector3.forward, 0.025f, new Color(0.15f, 0.12f, 0.1f));

            // desk key: appears under the painting when the ninth figure does
            var key = Box("DeskKey", new Vector3(-6.1f, 0.012f, 5.55f), new Vector3(0.06f, 0.012f, 0.025f), Mats.Color(new Color(0.75f, 0.6f, 0.3f), 0.8f));
            var keyTrigger = key.GetComponent<BoxCollider>();
            keyTrigger.size = new Vector3(4f, 8f, 8f);
            var kp = key.AddComponent<FlagPickup>();
            kp.flag = "has_desk_key";
            kp.verb = "Pick up";
            kp.itemName = "a small brass key";
            kp.message = "A small brass key. It must have been tucked behind the frame.";
            Game.World.Register("desk_key", key);
            key.SetActive(false);

            Switch("living", new Vector3(-3.25f, 1.25f, 0.11f), Vector3.back);
            Switch("porch", new Vector3(-3.1f, 1.25f, 0.11f), Vector3.back);
            Pendant("living", new Vector3(-4f, 2.35f, 3f), Warm, 1.15f, 7.5f, true);

            // Mind: chalk mark beside the painting
            MindOnly(Quad("Mind_Symbol_Living", new Vector3(-4.9f, 1.8f, 5.885f), new Vector2(0.6f, 0.6f), Vector3.forward, Mats.Lit("symbol_chalk", Color.white, 0.05f)));
        }

        // =====================================================================
        void Kitchen()
        {
            cur = Group("Kitchen");
            var counterMat = Mats.Lit("wood_door", new Color(0.85f, 0.82f, 0.75f), 0.2f);
            var marble = Mats.Color(new Color(0.8f, 0.78f, 0.74f), 0.6f);
            Box("CounterBase", new Vector3(5.35f, 0.44f, 5.6f), new Vector3(3.3f, 0.88f, 0.58f), counterMat);
            Box("CounterTop", new Vector3(5.35f, 0.9f, 5.58f), new Vector3(3.34f, 0.04f, 0.64f), marble);
            Box("Sink", new Vector3(5.9f, 0.905f, 5.55f), new Vector3(0.5f, 0.04f, 0.4f), Mats.Color(new Color(0.6f, 0.62f, 0.64f), 0.8f), null, false);
            Cyl("Tap", new Vector3(5.9f, 1.02f, 5.8f), 0.03f, 0.22f, Mats.Color(new Color(0.7f, 0.7f, 0.72f), 0.85f), null, false);
            Box("Hob", new Vector3(6.7f, 0.925f, 5.55f), new Vector3(0.55f, 0.01f, 0.5f), Mats.Color(new Color(0.08f, 0.08f, 0.08f), 0.5f), null, false);
            Box("UpperCabinets", new Vector3(5.35f, 1.95f, 5.72f), new Vector3(3.3f, 0.7f, 0.36f), counterMat);

            var fridge = Box("Fridge", new Vector3(7.45f, 0.9f, 5.55f), new Vector3(0.7f, 1.8f, 0.65f), Mats.Color(new Color(0.86f, 0.85f, 0.8f), 0.4f));
            var hum = Game.Audio.Loop("hum", fridge.transform, new Vector3(0, -0.3f, 0), 0.35f, 1f, 9f);
            Game.Lighting.RegisterPoweredAudio(hum);

            // coffee machine + the mug it makes
            var cm = Box("CoffeeMachine", new Vector3(4.15f, 1.09f, 5.62f), new Vector3(0.22f, 0.34f, 0.26f), Mats.Color(new Color(0.08f, 0.08f, 0.08f), 0.4f));
            Cyl("Carafe", new Vector3(4.15f, 0.99f, 5.47f), 0.13f, 0.14f, Mats.Color(new Color(0.2f, 0.15f, 0.1f), 0.8f), null, false);
            var coffee = cm.AddComponent<CoffeeMachine>();
            coffee.warmLight = PointLight("CoffeeLED", new Vector3(4.15f, 1.0f, 5.45f), new Color(1f, 0.2f, 0.1f), 0.3f, 0.6f, false);
            coffee.warmLight.enabled = false;
            var mug = Cyl("Mug", new Vector3(4.55f, 0.97f, 5.5f), 0.085f, 0.1f, Mats.Color(new Color(0.92f, 0.9f, 0.86f), 0.5f));
            var mugItem = mug.AddComponent<InspectableObject>();
            mugItem.displayName = "mug of coffee";
            mugItem.description = "Hot. Bitter. The mug says 'ΕΛΛΑΣ 1996'.";
            mugItem.holdOffset = new Vector3(0.22f, -0.22f, 0.45f);
            mugItem.placeOffset = new Vector3(0, 0.05f, 0);
            Game.World.Register("coffee_mug", mug);
            mug.SetActive(false);
            coffee.mug = mug;

            // table, chairs, the instructions and the camera
            Table("KitchenTable", new Vector3(3.5f, 0.75f, 2.6f), new Vector3(1.4f, 0.04f, 0.9f), Mats.Lit("wood_floor", Color.white, 0.3f));
            var chairMat = Mats.Lit("wood_door", Color.white, 0.2f);
            Chair("KitchenChairA", new Vector3(3.5f, 0, 1.85f), 0f, chairMat);
            var chairB = Chair("KitchenChairB", new Vector3(3.5f, 0, 3.35f), 180f, chairMat);
            Game.World.Register("kitchen_chair_b", chairB);

            Note("Instructions", new Vector3(3.2f, 0.775f, 2.5f), new Vector3(0.21f, 0.004f, 0.28f), "note_paper",
                "Read", "the instructions", SliceText.Instructions, DocStyle.Handwritten, 0, "read_instructions", Quaternion.Euler(0, -8, 0));

            var cam = Box("Camera", new Vector3(3.95f, 0.81f, 2.75f), new Vector3(0.11f, 0.065f, 0.045f), Mats.Color(new Color(0.55f, 0.56f, 0.58f), 0.6f));
            Cyl("Lens", new Vector3(3.95f, 0.81f, 2.72f), 0.04f, 0.03f, Mats.Color(new Color(0.1f, 0.1f, 0.1f), 0.9f), null, false).transform.localRotation = Quaternion.Euler(90, 0, 0);
            var camPick = cam.AddComponent<FlagPickup>();
            camPick.flag = CameraSystem.HasCameraFlag;
            camPick.itemName = "the digital camera";
            camPick.message = "The agency's camera. Two megapixels. [C] or right mouse to raise it.";

            var torch = Cyl("Torch", new Vector3(6.95f, 0.95f, 5.5f), 0.045f, 0.2f, Mats.Color(new Color(0.15f, 0.2f, 0.45f), 0.5f));
            torch.transform.localRotation = Quaternion.Euler(0, 0, 90);
            var tp = torch.AddComponent<FlagPickup>();
            tp.flag = Torch.HasTorchFlag;
            tp.itemName = "the torch";
            tp.message = "A cheap plastic torch. The batteries rattle. [T] to switch it on.";

            Switch("kitchen", new Vector3(0.11f, 1.25f, 1.8f), Vector3.left);
            var tube = Group("Fluorescent", cur);
            tube.localPosition = new Vector3(4f, 2.74f, 3f);
            var tubeBulb = Box("Tube", Vector3.zero, new Vector3(1.2f, 0.05f, 0.08f), Game.Lighting.bulbOffMaterial, tube, false);
            var tl = PointLight("Light", new Vector3(0, -0.15f, 0), Fluoro, 1.05f, 7.5f, false, tube);
            Game.Lighting.AddLight("kitchen", tl, tubeBulb.GetComponent<Renderer>());
        }

        // =====================================================================
        void Hallway()
        {
            cur = Group("Hallway");
            Box("Runner", new Vector3(-1f, 0.005f, 7f), new Vector3(12f, 0.01f, 0.8f), Mats.Lit("rug", new Color(0.8f, 0.8f, 0.8f), 0.05f, 6f, 1f), null, false);

            var photo = Group("HallPhoto", cur);
            photo.localPosition = new Vector3(0.2f, 1.6f, 6.115f);
            photo.localRotation = Quaternion.LookRotation(Vector3.back);
            Box("Frame", Vector3.zero, new Vector3(0.4f, 0.3f, 0.025f), Mats.Color(new Color(0.12f, 0.1f, 0.08f), 0.4f), photo);
            Prim(PrimitiveType.Quad, "Photo", new Vector3(0, 0, -0.014f), new Vector3(0.34f, 0.25f, 1f), Mats.Lit("family_photo", Color.white, 0.4f), photo, false);
            Game.World.Register("hall_photo", photo.gameObject);

            // console table: home of No. 027
            Table("Console", new Vector3(-1.5f, 0.8f, 7.72f), new Vector3(1.0f, 0.04f, 0.34f), WoodDark);
            Socket("027", new Vector3(-1.5f, 0.825f, 7.7f), Vector3.forward, new Vector3(0f, -0.04f, -0.17f));

            Switch("hall", new Vector3(-3.25f, 1.25f, 6.11f), Vector3.back);
            Pendant("hall", new Vector3(-4.5f, 2.55f, 7f), Warm, 0.8f, 5f, true, false);
            Pendant("hall", new Vector3(2f, 2.55f, 7f), Warm, 0.8f, 5f, false, false);

            // Second Lamp: the figure at the end of the hallway, only through the lens
            Figure("apparition_hall", new Vector3(5.3f, 0, 7f), -90f, false);

            // Mind: marks on the floor and walls
            MindOnly(Prim(PrimitiveType.Quad, "Mind_Symbol_Floor", new Vector3(-1f, 0.013f, 7f), new Vector3(1.4f, 1.4f, 1f),
                Mats.Lit("symbol_chalk", Color.white, 0.05f), null, false, Quaternion.Euler(90f, -90f, 0f)));
            MindOnly(Quad("Mind_Symbol_HallA", new Vector3(-6f, 1.7f, 6.115f), new Vector2(0.5f, 0.5f), Vector3.back, Mats.Lit("symbol_chalk", Color.white, 0.05f)));
            MindOnly(Quad("Mind_Symbol_HallB", new Vector3(3.8f, 1.5f, 7.885f), new Vector2(0.5f, 0.5f), Vector3.forward, Mats.Lit("symbol_chalk", Color.white, 0.05f)), 10);
        }

        // =====================================================================
        void Study()
        {
            cur = Group("Study");
            Box("PaperN", new Vector3(-4f, 1.4f, 13.895f), new Vector3(7.8f, 2.8f, 0.01f), Mats.Lit("wallpaper", Color.white, 0.05f, 8f, 3f), null, false);
            Box("PaperE", new Vector3(-0.105f, 1.4f, 11f), new Vector3(0.01f, 2.8f, 5.8f), Mats.Lit("wallpaper", Color.white, 0.05f, 6f, 3f), null, false);

            // desk with a locked drawer
            var desk = Group("Desk", cur);
            desk.localPosition = new Vector3(-2.5f, 0, 13.45f);
            Box("Top", new Vector3(0, 0.76f, 0), new Vector3(1.6f, 0.05f, 0.8f), WoodDark, desk);
            Box("LegA", new Vector3(-0.75f, 0.37f, -0.35f), new Vector3(0.06f, 0.74f, 0.06f), WoodDark, desk);
            Box("LegB", new Vector3(-0.75f, 0.37f, 0.35f), new Vector3(0.06f, 0.74f, 0.06f), WoodDark, desk);
            Box("PedSideA", new Vector3(0.33f, 0.37f, 0), new Vector3(0.03f, 0.74f, 0.76f), WoodDark, desk);
            Box("PedSideB", new Vector3(0.77f, 0.37f, 0), new Vector3(0.03f, 0.74f, 0.76f), WoodDark, desk);
            Box("PedBack", new Vector3(0.55f, 0.37f, 0.37f), new Vector3(0.44f, 0.74f, 0.02f), WoodDark, desk);
            Box("PedLower", new Vector3(0.55f, 0.21f, -0.37f), new Vector3(0.44f, 0.42f, 0.02f), WoodDark, desk);
            Box("PedShelf", new Vector3(0.55f, 0.42f, 0), new Vector3(0.42f, 0.02f, 0.74f), WoodDark, desk);

            var tray = Group("Drawer", desk);
            tray.localPosition = new Vector3(0.55f, 0.43f, 0f);
            Box("Front", new Vector3(0, 0.15f, -0.37f), new Vector3(0.42f, 0.3f, 0.02f), WoodDark, tray);
            Box("Bottom", new Vector3(0, 0.01f, 0), new Vector3(0.4f, 0.01f, 0.72f), WoodDark, tray, false);
            Box("SideA", new Vector3(-0.19f, 0.08f, 0), new Vector3(0.01f, 0.14f, 0.72f), WoodDark, tray, false);
            Box("SideB", new Vector3(0.19f, 0.08f, 0), new Vector3(0.01f, 0.14f, 0.72f), WoodDark, tray, false);
            Prim(PrimitiveType.Sphere, "Knob", new Vector3(0, 0.16f, -0.39f), Vector3.one * 0.035f, Mats.Color(new Color(0.7f, 0.55f, 0.25f), 0.7f), tray, false);
            var drawer = tray.gameObject.AddComponent<Openable>();
            drawer.displayName = "desk drawer";
            drawer.kind = Openable.Kind.Slide;
            drawer.openOffset = new Vector3(0, 0, -0.34f);
            drawer.locked = true;
            drawer.unlockFlag = "has_desk_key";
            drawer.lockedText = "Locked. A small brass keyhole.";
            drawer.unlockText = "The little key fits.";
            drawer.Setup(tray, false);
            Game.World.Register("desk_drawer", tray.gameObject);
            OilLampIn(tray);
            Box("Matches", new Vector3(0.11f, 0.03f, -0.25f), new Vector3(0.05f, 0.015f, 0.035f), Mats.Color(new Color(0.7f, 0.2f, 0.1f)), tray, false);

            // computer
            var beige = Mats.Color(new Color(0.78f, 0.75f, 0.66f), 0.3f);
            var monitor = Group("Computer", cur);
            monitor.localPosition = new Vector3(-2.75f, 0.785f, 13.55f);
            Box("CRT", new Vector3(0, 0.22f, 0.05f), new Vector3(0.42f, 0.38f, 0.42f), beige, monitor);
            Box("Stand", new Vector3(0, 0.01f, 0.05f), new Vector3(0.25f, 0.02f, 0.2f), beige, monitor);
            var screen = Prim(PrimitiveType.Quad, "Screen", new Vector3(0, 0.23f, -0.162f), new Vector3(0.31f, 0.24f, 1f), Mats.Unlit("crt_off"), monitor, false);
            Box("Keyboard", new Vector3(0, 0.01f, -0.42f), new Vector3(0.45f, 0.025f, 0.15f), beige, monitor);
            Box("Tower", new Vector3(-0.1f, -0.55f, 0.05f), new Vector3(0.2f, 0.42f, 0.45f), beige, monitor);
            var view = Group("ViewPoint", monitor);
            view.localPosition = new Vector3(0, 0.26f, -0.62f);
            view.localRotation = Quaternion.Euler(2f, 0, 0);
            var comp = monitor.gameObject.AddComponent<CatalogueComputer>();
            comp.viewPoint = view;
            comp.screenRenderer = screen.GetComponent<Renderer>();
            comp.screenOn = Mats.Unlit("crt_on");
            comp.screenOff = Mats.Unlit("crt_off");
            comp.fanLoop = Game.Audio.Loop("fan", monitor, new Vector3(-0.1f, -0.55f, 0.05f), 0.25f, 0.5f, 5f);
            Game.Lighting.RegisterPoweredAudio(comp.fanLoop);
            comp.Setup();
            Chair("DeskChair", new Vector3(-2.6f, 0, 12.65f), 0f, WoodDark);

            Note("Notebook", new Vector3(-1.95f, 0.795f, 13.25f), new Vector3(0.16f, 0.02f, 0.22f), "note_paper",
                "Read", "Vardis's notebook", SliceText.Notebook, DocStyle.Handwritten, 2, "read_notebook", Quaternion.Euler(0, 12, 0));
            var lamp = Group("DeskLamp", cur);
            lamp.localPosition = new Vector3(-3.15f, 0.785f, 13.6f);
            Cyl("Base", new Vector3(0, 0.01f, 0), 0.14f, 0.02f, Mats.Color(new Color(0.2f, 0.3f, 0.2f), 0.6f), lamp);
            Cyl("Stem", new Vector3(0, 0.2f, 0), 0.02f, 0.38f, Mats.Color(new Color(0.7f, 0.6f, 0.3f), 0.8f), lamp, false);
            Cyl("Shade", new Vector3(0, 0.4f, 0), 0.22f, 0.12f, Mats.Color(new Color(0.15f, 0.35f, 0.2f), 0.5f), lamp, false);
            var dl = PointLight("Light", new Vector3(0, 0.3f, -0.05f), new Color(1f, 0.7f, 0.42f), 0.7f, 2.8f, false, lamp);
            Game.Lighting.AddLight("study", dl);

            // work table with the objects to catalogue
            Table("WorkTable", new Vector3(-6.2f, 0.76f, 9.4f), new Vector3(1.6f, 0.04f, 0.8f), WoodMid);
            var crate = Group("Crate", cur);
            crate.localPosition = new Vector3(-6.55f, 0.78f, 9.55f);
            var crateWood = Mats.Lit("wood_door", new Color(0.9f, 0.8f, 0.65f), 0.05f);
            Box("Bottom", new Vector3(0, 0.01f, 0), new Vector3(0.6f, 0.02f, 0.4f), crateWood, crate);
            Box("SideA", new Vector3(0, 0.12f, -0.2f), new Vector3(0.6f, 0.24f, 0.02f), crateWood, crate);
            Box("SideB", new Vector3(0, 0.12f, 0.2f), new Vector3(0.6f, 0.24f, 0.02f), crateWood, crate);
            Box("SideC", new Vector3(-0.3f, 0.12f, 0), new Vector3(0.02f, 0.24f, 0.4f), crateWood, crate);
            Box("SideD", new Vector3(0.3f, 0.12f, 0), new Vector3(0.02f, 0.24f, 0.4f), crateWood, crate);
            Label3D("ΠΡΟΣ ΚΑΤΑΓΡΑΦΗ\nTO CATALOGUE: 014, 027", new Vector3(0, 0.13f, -0.215f), Vector3.forward, 0.07f, new Color(0.15f, 0.1f, 0.05f), crate);

            Artifacts();

            Note("Receipt", new Vector3(-5.7f, 0.782f, 9.25f), new Vector3(0.15f, 0.003f, 0.2f), "note_paper",
                "Read", "a receipt", SliceText.HeatingReceipt, DocStyle.Typed, 0, "read_receipt", Quaternion.Euler(0, 20, 0));
            Note("Postcard", new Vector3(-5.55f, 0.782f, 9.6f), new Vector3(0.15f, 0.003f, 0.1f), "family_photo",
                "Read", "a postcard", SliceText.Postcard, DocStyle.Postcard, 0, "read_postcard", Quaternion.Euler(0, -15, 0));

            // bookshelves on the west wall, one book loose
            Box("Shelves", new Vector3(-7.75f, 1.2f, 11f), new Vector3(0.4f, 2.4f, 3.2f), WoodDark);
            Quad("ShelfBooks", new Vector3(-7.545f, 1.2f, 11f), new Vector2(3.1f, 2.3f), Vector3.left, Mats.Lit("books", Color.white, 0.1f));
            Note("LooseBook", new Vector3(-7.47f, 1.33f, 10.2f), new Vector3(0.2f, 0.24f, 0.05f), "wood_door",
                "Pull out", "a loose book", SliceText.Letter, DocStyle.Letter, 2, "read_letter");

            Switch("study", new Vector3(-3.3f, 1.25f, 8.11f), Vector3.forward);
            Pendant("study", new Vector3(-4f, 2.4f, 11f), Warm, 0.85f, 6.5f, true);

            // Mind: the mark over the desk, and a doorway where the wall should be
            MindOnly(Quad("Mind_Symbol_Study", new Vector3(-2.5f, 2.05f, 13.88f), new Vector2(0.7f, 0.7f), Vector3.forward, Mats.Lit("symbol_chalk", Color.white, 0.05f)));
            var arch = Group("Mind_Doorway", cur);
            Quad("Void", new Vector3(-0.115f, 1.05f, 11f), new Vector2(1.0f, 2.1f), Vector3.right, Mats.UnlitColor(Color.black), arch);
            TexBox("JambA", new Vector3(-0.14f, 1.1f, 10.45f), new Vector3(0.06f, 2.2f, 0.12f), "stone", 1f, 0.05f, arch);
            TexBox("JambB", new Vector3(-0.14f, 1.1f, 11.55f), new Vector3(0.06f, 2.2f, 0.12f), "stone", 1f, 0.05f, arch);
            TexBox("Lintel", new Vector3(-0.14f, 2.22f, 11f), new Vector3(0.06f, 0.14f, 1.22f), "stone", 1f, 0.05f, arch);
            Quad("Mark", new Vector3(-0.115f, 2.55f, 11f), new Vector2(0.4f, 0.4f), Vector3.right, Mats.Lit("symbol_chalk", Color.white, 0.05f), arch);
            MindOnly(arch.gameObject, 20);
        }

        void Artifacts()
        {
            // No. 014, votive disc
            var disc = Cyl("Disc_014", new Vector3(-6.0f, 0.79f, 9.3f), 0.12f, 0.012f, Mats.Lit("disc_014", Color.white, 0.55f));
            var d = disc.AddComponent<InspectableObject>();
            d.itemId = "014";
            d.displayName = "bronze votive disc";
            d.description = "Heavy for its size. Green in the grooves.";
            d.symbolLocalDir = Vector3.up;
            d.inspectEuler = new Vector3(-70f, 0, 0);
            d.symbolNote = "Cut into the rim, very fine: a circle, a line, a triangle.";
            d.holdEuler = new Vector3(-50f, 0, 0);
            d.placeOffset = new Vector3(0, 0.006f, 0);
            Game.World.Register("item_014", disc);

            // No. 027, icon fragment
            var icon = Box("Icon_027", new Vector3(-6.45f, 0.8f, 9.5f), new Vector3(0.18f, 0.24f, 0.02f), Mats.Lit("icon_027", Color.white, 0.35f), null, true, Quaternion.Euler(90, 0, 0));
            var i = icon.AddComponent<InspectableObject>();
            i.itemId = "027";
            i.displayName = "icon fragment";
            i.description = "A saint without a name. One corner broken away.";
            i.symbolLocalDir = Vector3.back;
            i.inspectEuler = Vector3.zero;
            i.symbolNote = "Painted into the border, almost hidden: a circle, a line, a triangle.";
            i.holdEuler = new Vector3(10f, -15f, 0);
            i.placeOffset = new Vector3(0, 0.12f, 0);
            i.placeEuler = new Vector3(-6f, 0, 0);
            Game.World.Register("item_027", icon);
        }

        void OilLampIn(Transform tray)
        {
            var g = Group("OilLamp", tray);
            g.localPosition = new Vector3(-0.05f, 0.02f, -0.2f);
            var brass = Mats.Color(new Color(0.55f, 0.42f, 0.2f), 0.65f);
            var clay = Mats.Color(new Color(0.45f, 0.28f, 0.18f), 0.2f);
            Cyl("Base", new Vector3(0, 0.03f, 0), 0.14f, 0.06f, brass, g);
            Prim(PrimitiveType.Sphere, "Font", new Vector3(0, 0.1f, 0), new Vector3(0.14f, 0.1f, 0.14f), clay, g, false);
            Cyl("Chimney", new Vector3(0, 0.2f, 0), 0.07f, 0.14f, Mats.Color(new Color(0.8f, 0.82f, 0.78f), 0.9f), g, false);
            var flame = Prim(PrimitiveType.Sphere, "Flame", new Vector3(0, 0.19f, 0), new Vector3(0.025f, 0.05f, 0.025f), Mats.UnlitColor(new Color(1f, 0.7f, 0.3f)), g, false);
            var light = PointLight("Flame", new Vector3(0, 0.22f, 0), new Color(1f, 0.58f, 0.25f), 1.2f, 4.8f, true, g);
            var col = g.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0, 0.13f, 0);
            col.size = new Vector3(0.16f, 0.27f, 0.16f);
            var lamp = g.gameObject.AddComponent<OilLamp>();
            lamp.itemId = "";
            lamp.displayName = "old oil lamp";
            lamp.description = "Clay and brass. Older than anything else in the house. It still smells of oil.";
            lamp.holdOffset = new Vector3(-0.22f, -0.28f, 0.5f);
            lamp.holdEuler = Vector3.zero;
            lamp.inspectEuler = new Vector3(-10f, 0, 0);
            var hiss = Game.Audio.Loop("lamp_hiss", g, new Vector3(0, 0.2f, 0), 0.25f, 0.3f, 4f, false);
            lamp.SetupLamp(light, flame, hiss);
            Game.Lighting.RegisterLamp(lamp);
            Game.World.Register("oil_lamp", g.gameObject);
        }

        // =====================================================================
        void Bathroom()
        {
            cur = Group("Bathroom");
            var porcelain = Mats.Color(new Color(0.92f, 0.92f, 0.9f), 0.7f);
            Box("Tub", new Vector3(1.75f, 0.3f, 13.45f), new Vector3(3.2f, 0.6f, 0.85f), porcelain);
            Box("Toilet", new Vector3(0.4f, 0.22f, 11.6f), new Vector3(0.4f, 0.44f, 0.55f), porcelain);
            Box("Cistern", new Vector3(0.18f, 0.7f, 11.6f), new Vector3(0.16f, 0.4f, 0.45f), porcelain);
            Box("SinkPedestal", new Vector3(3.2f, 0.4f, 10f), new Vector3(0.2f, 0.8f, 0.2f), porcelain);
            Box("Basin", new Vector3(3.15f, 0.85f, 10f), new Vector3(0.45f, 0.12f, 0.55f), porcelain);

            var mirrorGroup = Group("Mirror", cur);
            mirrorGroup.localPosition = new Vector3(3.385f, 1.55f, 10f);
            mirrorGroup.localRotation = Quaternion.LookRotation(Vector3.right);
            Box("Frame", new Vector3(0, 0, 0.004f), new Vector3(0.68f, 0.88f, 0.02f), Mats.Color(new Color(0.3f, 0.28f, 0.25f), 0.4f), mirrorGroup, false);
            var surface = Prim(PrimitiveType.Quad, "Surface", new Vector3(0, 0, -0.008f), new Vector3(0.6f, 0.8f, 1f), Mats.UnlitColor(Color.gray), mirrorGroup, false);
            surface.layer = Game.LayerMirror;
            var mirror = surface.AddComponent<Mirror>();
            mirror.Setup(0.6f, 0.8f, surface.GetComponent<Renderer>(), Mats.UnlitColor(Color.white), BuildReflectionBody());
            Game.World.Register("bath_mirror", surface);

            Switch("bath", new Vector3(2.45f, 1.25f, 8.11f), Vector3.forward);
            Pendant("bath", new Vector3(1.75f, 2.5f, 11f), new Color(0.95f, 0.92f, 0.86f), 0.9f, 4.5f, false, false);
        }

        ReflectionBody BuildReflectionBody()
        {
            var g = new GameObject("ReflectionBody").transform;
            g.SetParent(root, false);
            var jacket = Mats.Color(new Color(0.2f, 0.22f, 0.26f), 0.1f);
            var skin = Mats.Color(new Color(0.78f, 0.62f, 0.5f), 0.2f);
            var hair = Mats.Color(new Color(0.12f, 0.08f, 0.05f), 0.1f);
            Prim(PrimitiveType.Capsule, "Torso", new Vector3(0, 1.12f, 0), new Vector3(0.42f, 0.42f, 0.26f), jacket, g, false);
            Prim(PrimitiveType.Capsule, "Legs", new Vector3(0, 0.45f, 0), new Vector3(0.34f, 0.45f, 0.22f), Mats.Color(new Color(0.15f, 0.17f, 0.25f)), g, false);
            var head = Group("Head", g);
            head.localPosition = new Vector3(0, 1.62f, 0);
            Prim(PrimitiveType.Sphere, "Face", Vector3.zero, new Vector3(0.19f, 0.24f, 0.21f), skin, head, false);
            Prim(PrimitiveType.Sphere, "Hair", new Vector3(0, 0.05f, -0.03f), new Vector3(0.2f, 0.2f, 0.2f), hair, head, false);
            Box("EyeL", new Vector3(-0.04f, 0.01f, 0.1f), new Vector3(0.025f, 0.012f, 0.01f), Mats.Color(Color.black), head, false);
            Box("EyeR", new Vector3(0.04f, 0.01f, 0.1f), new Vector3(0.025f, 0.012f, 0.01f), Mats.Color(Color.black), head, false);
            SetLayerRecursive(g.gameObject, Game.LayerPlayerBody);
            var rb = g.gameObject.AddComponent<ReflectionBody>();
            rb.head = head;
            return rb;
        }

        void Storage()
        {
            cur = Group("Storage");
            var cardboard = Mats.Color(new Color(0.55f, 0.42f, 0.28f), 0.05f);
            Box("BoxA", new Vector3(6.5f, 0.3f, 12.8f), new Vector3(0.6f, 0.6f, 0.6f), cardboard);
            Box("BoxB", new Vector3(7.2f, 0.25f, 12.9f), new Vector3(0.5f, 0.5f, 0.5f), cardboard);
            Box("BoxC", new Vector3(6.8f, 0.8f, 12.85f), new Vector3(0.45f, 0.4f, 0.45f), cardboard);
        }
    }
}
