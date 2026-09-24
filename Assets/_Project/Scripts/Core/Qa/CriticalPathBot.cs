#if UNITY_EDITOR
using System.Collections;
using UnityEngine;

namespace ThirdLamp.Qa
{
    /// <summary>
    /// Plays the slice's critical path through the real game logic (the event director stays on),
    /// then forces the later-night perception events and checks each one fires and changes the world.
    /// Every step is PASS/FAIL with its time; failures are screenshotted.
    /// </summary>
    public class CriticalPathBot : QaRunner
    {
        protected override string Kind => "bot";
        protected override float MaxSeconds => 900f;

        const string C = GameState.CataloguedCounter;

        CatalogueComputer computer;
        CameraSystem cam => Game.PhotoCamera;

        protected override IEnumerator Run()
        {
            QaUtil.SkipIntro();
            yield return null;

            // ---------------------------------------------------------------- arrival
            yield return Step("Arrival view", () => QaUtil.Teleport(new Vector3(-2.7f, 0, -13.2f), 8f), () => true, 0.5f, true);

            var pot = QaUtil.FindPickup<FlagPickup>(p => p.flag == "has_front_key");
            yield return Step("Lift the blue pot, find the key",
                () => { QaUtil.Teleport(new Vector3(-5.15f, 0, -1.7f), 0f, 25f); pot.Interact(); },
                () => Flag("has_front_key"));

            yield return Step("Unlock and open the front door",
                () => Game.World.Get<Openable>("door_front").Interact(),
                () => Game.World.Get<Openable>("door_front").IsOpen, 3f);

            yield return Step("Enter the villa (checkpoint)",
                () => QaUtil.Teleport(new Vector3(-4.5f, 0, 1.4f), 0f),
                () => Flag("entered_villa"), 5f, true);

            // ---------------------------------------------------------------- kitchen
            var instructions = QaUtil.FindPickup<NoteReader>(n => n.flagOnRead == "read_instructions");
            yield return Step("Read the instructions",
                () => { QaUtil.Teleport(new Vector3(3.2f, 0, 1.8f), 0f, 45f); instructions.Interact(); },
                () => Game.Mode == InputMode.Reading && Flag("read_instructions"), 3f, true);
            CloseDocument();
            yield return null;

            yield return Step("Switch the lights on", () => QaUtil.LightsAll(true), () => Game.Lighting.IsGroupLit("kitchen"), 2f);
            yield return Step("Take the camera",
                () => QaUtil.FindPickup<FlagPickup>(p => p.flag == CameraSystem.HasCameraFlag).Interact(),
                () => Flag(CameraSystem.HasCameraFlag));
            yield return Step("Take the torch",
                () => QaUtil.FindPickup<FlagPickup>(p => p.flag == Torch.HasTorchFlag).Interact(),
                () => Flag(Torch.HasTorchFlag));

            // ---------------------------------------------------------------- computer
            computer = Object.FindAnyObjectByType<CatalogueComputer>();
            yield return SitAtComputer();
            yield return Step("Log in with 'phos'", () => TypeAndSubmit("phos"), () => Flag("computer_logged_in"), 3f, true);
            StandUp();
            yield return null;

            // ---------------------------------------------------------------- photographs
            yield return Photograph("014", new Vector3(-6.0f, 0, 8.2f), Game.World.Find("item_014").transform.position);
            // the icon lies inside the crate: stand close so the lens sees over the crate side
            yield return Photograph("027", new Vector3(-6.45f, 0, 9.05f), Game.World.Find("item_027").transform.position);
            yield return Photograph("031", new Vector3(-6.4f, 0, 4.45f), Game.World.Find("painting_031").transform.position);
            yield return Step("Painting noticed (looked at for 1 s)", null, () => Flag("painting_first_seen"), 4f);

            // ---------------------------------------------------------------- catalogue
            yield return SitAtComputer();
            foreach (var id in new[] { "014", "027", "031" })
            {
                yield return Step($"Look up No. {id}", () => TypeAndSubmit(id), () => Page() == "Record", 3f);
                yield return Step($"Save record No. {id}", () => Submit(), () => Flag("catalogued_" + id), 3f, id == "031");
                Submit(); // back to lookup (or the completion page)
                yield return null;
                if (Page() == "Complete") { Submit(); yield return null; }
            }
            yield return Step("Three objects catalogued", null, () => Game.State.Get(C) >= 3, 1f);
            StandUp();
            yield return null;

            // ---------------------------------------------------------------- return objects
            yield return Return("014", new Vector3(-0.9f, 0, 1.05f), 90f);
            yield return Return("027", new Vector3(-1.5f, 0, 7.0f), 0f);

            // ---------------------------------------------------------------- power
            yield return Step("Power cuts out at 22:10",
                () => { QaUtil.Teleport(new Vector3(3.5f, 0, 1.6f), 0f); Game.State.clockMinutes = 22 * 60 + 10; },
                () => Flag("power_cut_happened") && !Game.Lighting.PowerOn, 5f, true);

            var gen = Game.World.Get<Generator>("generator");
            QaUtil.Teleport(new Vector3(11.2f, 0, -4.4f), 0f, 20f);
            yield return Shot("generator_shed");
            for (int i = 0; i < 3; i++)
            {
                yield return Step($"Pull the generator cord ({i + 1}/3)", () => gen.Interact(), () => true, 0.1f);
                yield return new WaitForSeconds(1.3f);
            }
            yield return Step("Generator restored", null, () => Flag(Generator.RestoredFlag) && Game.Lighting.PowerOn, 5f, true);

            // ---------------------------------------------------------------- the ninth figure
            yield return Step("Painting changes while unobserved",
                () => QaUtil.Teleport(new Vector3(3.5f, 0, 2.5f), 180f),
                () => Flag("painting_changed"), 5f);
            yield return Step("See the ninth figure",
                () => { QaUtil.Teleport(new Vector3(-6.4f, 0, 4.45f), 0f); QaUtil.AimAt(Game.World.Find("painting_031").transform.position); },
                () => Flag("saw_ninth"), 5f, true);

            yield return Step("Take the brass key", () => Game.World.Get<FlagPickup>("desk_key").Interact(), () => Flag("has_desk_key"));
            yield return Step("Unlock the desk drawer",
                () => { QaUtil.Teleport(new Vector3(-2.0f, 0, 12.6f), 0f, 30f); Game.World.Get<Openable>("desk_drawer").Interact(); },
                () => Game.World.Get<Openable>("desk_drawer").IsOpen, 3f);
            var lamp = Game.World.Get<OilLamp>("oil_lamp");
            yield return Step("Take the oil lamp", () => Game.Interactor.PickUp(lamp), () => Flag("lamp_found") && lamp.IsHeld, 3f, true);

            // ---------------------------------------------------------------- texts and the call
            QaUtil.Teleport(new Vector3(-4f, 0, 3f), 90f);
            yield return Step("Texts arrive (lamp, discover, finish)", null, () => Flag("sms_finish"), 150f);
            yield return Step("The phone rings", null, () => QaUtil.FieldString(Game.Phone, "screen") == "Ringing", 60f);
            yield return Step("Answer the call",
                () => { Game.Phone.SetOut(true); QaUtil.Call(Game.Phone, "Answer"); },
                () => Game.Mode == InputMode.Call, 2f, true);
            yield return Step("Asked how many figures", null, () => QaUtil.Get<bool>(Game.Phone, "awaitingChoice"), 30f);
            yield return Step("Answer: nine", () => QaUtil.Call(Game.Phone, "Choose", 1), () => Flag("call_answer_9"), 2f);
            yield return Step("Call ends", null, () => Flag("call_painting_done") && Game.Mode == InputMode.Play, 40f);
            Game.Phone.SetOut(false);

            // ---------------------------------------------------------------- later-night events
            yield return ScarePass();

            // ---------------------------------------------------------------- the Third Lamp
            yield return Step("Light the lamp in the dark hallway (Mind)",
                () =>
                {
                    QaUtil.LightsAll(false);
                    var t = Game.Player.GetComponent<Torch>();
                    if (t.On) t.Set(false);
                    QaUtil.Teleport(new Vector3(-2f, 0, 7f), 90f);
                    if (!lamp.Lit) lamp.Toggle();
                },
                () => Game.Lighting.MindActive && Flag("mind_seen"), 4f, true);
            yield return Step("Walk into the corridor", () => QaUtil.Teleport(new Vector3(13f, 0, 7f), 90f), () => Game.Lighting.MindActive, 2f, true);
            yield return Step("Reach the great mark", () => QaUtil.Teleport(new Vector3(50f, 0, 7f), 90f, -3f), () => Game.Lighting.MindActive, 2f, true);
            yield return Step("Walk back to the hallway", () => QaUtil.Teleport(new Vector3(-2f, 0, 7f), -90f), () => Game.Lighting.MindActive, 2f);

            yield return Step("Put the lamp out", () => lamp.Toggle(), () => Flag("lamp_out_after_mind"), 3f);
            yield return Step("The slice ends", null, () => Game.Mode == InputMode.Ended, 25f);
            yield return new WaitForSeconds(3f);
            yield return Shot("ending");
        }

        // ------------------------------------------------------------------ scares

        IEnumerator ScarePass()
        {
            Game.Perception.Add(45, "qa_scare_pass");
            // living room, looking east: away from the garden, not in kitchen or hall
            QaUtil.Teleport(new Vector3(-3f, 0, 3f), 90f);
            yield return Step("Cupboard creaks open (heard from elsewhere)", null, () => Game.World.Get<Openable>("kitchen_cabinet").IsOpen, 6f);
            yield return Step("Wet footprints appear", null, () => Game.World.Find("wet_footprints").activeSelf, 6f);
            yield return Step("Hall photo faces scratched", null, () => Game.World.Get<MaterialStates>("hall_photo").Current == "scratched", 6f);
            yield return Step("Wedding portrait faces scratched", null, () => Game.World.Get<MaterialStates>("hall_portrait").Current == "scratched", 6f);
            yield return Step("Chalk mark in the kitchen", null, () => Game.World.Find("chalk_reason_kitchen").activeSelf, 6f);
            yield return Step("Figure appears in the garden", null, () => Flag("evt_garden_figure"), 6f);
            var garden = Game.World.Find("garden_figure");
            yield return Step("Garden figure vanishes when looked at",
                () => { QaUtil.Teleport(new Vector3(-6.6f, 0, 3.1f), -90f); QaUtil.AimAt(garden.transform.position + Vector3.up * 1f); },
                () => !garden.activeSelf && Flag("glimpsed_garden"), 3f, true);

            yield return Step("TV turns itself on (player in the kitchen)",
                () => QaUtil.Teleport(new Vector3(5f, 0, 2f), 90f),
                () => Flag("tv_static_on"), 6f);
            yield return Step("TV static seen",
                () => { QaUtil.Teleport(new Vector3(-6.9f, 0, 4.6f), 131f); QaUtil.AimAt(new Vector3(-5.25f, 0.84f, 2.94f)); },
                () => true, 0.8f, true);
            yield return Step("TV cuts out once looked at", null, () => Flag("tv_static_seen"), 4f);

            yield return Step("Open cupboard (clay figure inside)",
                () => { QaUtil.Teleport(new Vector3(4.6f, 0, 4.4f), -20f); QaUtil.AimAt(new Vector3(3.98f, 1.8f, 5.7f)); }, () => true, 0.6f, true);
            yield return Step("Footprints lead to the basement",
                () => QaUtil.Teleport(new Vector3(-4.3f, 0, 7f), 90f, 24f), () => true, 0.6f, true);
        }

        // ------------------------------------------------------------------ helpers

        IEnumerator SitAtComputer()
        {
            yield return Step("Sit at the computer",
                () => { QaUtil.Teleport(new Vector3(-2.75f, 0, 12.7f), 0f); computer.Interact(); },
                () => Game.Mode == InputMode.Computer && Page() != "Boot", 6f);
        }

        IEnumerator Photograph(string id, Vector3 stand, Vector3 target)
        {
            yield return Step($"Photograph No. {id}",
                () =>
                {
                    QaUtil.Teleport(stand, 0f);
                    QaUtil.AimAt(target);
                    cam.SetRaised(true);
                },
                () => true, 0.1f);
            yield return new WaitForSeconds(0.6f); // let the lens settle
            yield return Step($"Photo of No. {id} recognised",
                () => cam.StartCoroutine((IEnumerator)QaUtil.Call(cam, "Capture")),
                () => Flag("photographed_" + id), 3f, true);
            cam.SetRaised(false);
            yield return null;
        }

        IEnumerator Return(string id, Vector3 stand, float yaw)
        {
            var item = Game.World.Get<InspectableObject>("item_" + id);
            var socket = Game.World.Get<PlacementSocket>("socket_" + id);
            yield return Step($"Pick up No. {id}", () => Game.Interactor.PickUp(item), () => item.IsHeld, 2f);
            yield return Step($"Return No. {id} to its place",
                () => { QaUtil.Teleport(stand, yaw); QaUtil.AimAt(socket.transform.position); socket.Interact(); },
                () => Flag("placed_" + id), 2f, true);
        }

        string Page() => QaUtil.FieldString(computer, "page");

        void TypeAndSubmit(string text)
        {
            QaUtil.Set(computer, "input", text);
            Submit();
        }

        void Submit() => QaUtil.Call(computer, "Submit");

        void StandUp()
        {
            if (QaUtil.Get<bool>(computer, "active")) QaUtil.Call(computer, "StandUp");
        }

        static void CloseDocument()
        {
            try { QaUtil.Set(Game.Hud, "doc", null); } catch (System.MissingFieldException) { }
            Game.Mode = InputMode.Play;
        }
    }
}
#endif
