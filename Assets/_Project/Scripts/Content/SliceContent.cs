using UnityEngine;

namespace ThirdLamp
{
    /// <summary>
    /// The vertical slice's narrative events and catalogue records, defined as data.
    /// Every beat is gated by progression + perception + location + object state. No random scares.
    /// </summary>
    public static class SliceContent
    {
        const string C = GameState.CataloguedCounter;

        public static void RegisterCatalogue(CatalogueDatabase db)
        {
            db.Add(new CatalogueRecord
            {
                itemId = "014",
                title = "Votive disc, bronze",
                period = "c. 5th–4th c. BC (?)",
                provenance = "Attica. Acquired 1968.",
                notes = "Diam. 11 cm. Concentric rays, worn. A small mark near the rim, deliberately cut. See 027, 031.",
            }, true);
            db.Add(new CatalogueRecord
            {
                itemId = "027",
                title = "Icon fragment, egg tempera on wood",
                period = "15th c. (?)",
                provenance = "Monastery on Chios (per dealer). Acquired 1981.",
                notes = "Upper right corner lost. Saint unidentified. Unusual device painted into the lower border. See 014.",
            }, true);
            db.Add(new CatalogueRecord
            {
                itemId = "031",
                title = "\"Gathering at the Spring\", oil on canvas",
                period = "c. 1890",
                provenance = "Unsigned. Bought at auction, Smyrna estate, 1976.",
                notes = "Eight figures at a spring, dusk. Good condition, craquelure. Mark on the rock, right.",
                altFlag = "painting_changed",
                notesAlt = "Nine figures at a spring, dusk. Good condition, craquelure. Mark on the rock, right.",
            }, true);
            db.Add(new CatalogueRecord
            {
                itemId = "001",
                title = "Oil lamp, clay, with bronze fittings",
                period = "Unknown",
                provenance = "—",
                notes = "Not for sale. Not to be catalogued.",
            }, false);
        }

        public static void RegisterEvents(EventDirector d)
        {
            // ---------- arrival: completely mundane ----------
            d.Register(NarrativeEvent.Create("sms_agent_arrival")
                .When(new TimeSinceFlagCondition("intro_done", 5f))
                .Do(new SendSmsAction("Kanellou", "Hi Alex. Key is under the blue pot by the front door. Prof's instructions on the kitchen table. Thank you! M.K.")));

            d.Register(NarrativeEvent.Create("entered_villa")
                .When(new FlagCondition("has_front_key"), new IndoorsCondition(true))
                .Do(new SetFlagAction("entered_villa"), new CheckpointAction("Inside the villa")));

            d.Register(NarrativeEvent.Create("sms_mum")
                .When(new TimeSinceFlagCondition("entered_villa", 150f))
                .Do(new SendSmsAction("Mum", "Did you get there ok? Don't drink coffee all night. Call tomorrow. Filia")));

            d.Register(NarrativeEvent.Create("sms_agent_generator")
                .When(new CounterCondition(C, 1))
                .After(20f)
                .Do(new SendSmsAction("Kanellou", "Forgot to say. Generator is old, if power goes pull the cord a few times. Shed is on the east side. Sorry!")));

            // ---------- the painting ----------
            d.Register(NarrativeEvent.Create("painting_first_seen")
                .When(new LookingAtCondition("painting_031", true, 4f, 1f))
                .Do(new SetFlagAction("painting_first_seen")));

            d.Register(NarrativeEvent.Create("painting_ninth_figure")
                .Note("No sound, no notification. The player's own discovery is the scare. " +
                      "Photos taken earlier keep showing eight.")
                .When(new CounterCondition(C, 3), new FlagCondition("painting_first_seen"),
                      new LookingAtCondition("painting_031", false))
                .Do(new SetMaterialStateAction("painting_031", "nine"),
                    new SetFlagAction("painting_changed"),
                    new SetActiveAction("desk_key", true)));

            d.Register(NarrativeEvent.Create("painting_discovered")
                .When(new FlagCondition("painting_changed"), new LookingAtCondition("painting_031", true, 3.5f, 2f))
                .Do(new SetFlagAction("saw_ninth"), new AddPerceptionAction(3, "saw_ninth"), new PulseAction(0.7f)));

            // ---------- power ----------
            d.Register(NarrativeEvent.Create("power_cut")
                .When(new CounterCondition(C, 2), new ClockCondition(22, 10), new FlagCondition("entered_villa"),
                      new IndoorsCondition(true), new PlayModeCondition())
                .Do(new PowerAction(false),
                    new SetFlagAction("power_cut_happened"),
                    PlaySoundAction.At("generator_die", "generator", 1f)));

            d.Register(NarrativeEvent.Create("sms_nikos")
                .When(new TimeSinceFlagCondition(Generator.RestoredFlag, 40f))
                .Do(new SendSmsAction("Nikos", "u still at the dead professors house?? dont let the ghosts take ur 300 euro lol")));

            // ---------- small inconsistencies (perception) ----------
            d.Register(NarrativeEvent.Create("hall_photo_tilts")
                .When(new PerceptionCondition(10), new LookingAtCondition("hall_photo", false))
                .Do(new NudgeTransformAction("hall_photo", Vector3.zero, new Vector3(0, 0, 4f))));

            d.Register(NarrativeEvent.Create("kitchen_chair_moves")
                .When(new CounterCondition(C, 2), new PerceptionCondition(5), new ZoneCondition("kitchen", false),
                      new LookingAtCondition("kitchen_chair_b", false))
                .Do(new NudgeTransformAction("kitchen_chair_b", new Vector3(0, 0, 0.18f), new Vector3(0, 7f, 0))));

            d.Register(NarrativeEvent.Create("mirror_lag")
                .When(new PerceptionCondition(10))
                .Do(new SetParamAction("bath_mirror", "lag", 0.28f)));

            d.Register(NarrativeEvent.Create("study_door_closes")
                .Note("Silent. The player left it open; now it isn't.")
                .When(new PerceptionCondition(8), new DoorOpenCondition("door_study", true),
                      new ZoneCondition("study", false), new ZoneCondition("hall", false),
                      new LookingAtCondition("door_study", false))
                .Do(new SetDoorAction("door_study", false)));

            d.Register(NarrativeEvent.Create("roof_footsteps")
                .When(new PerceptionCondition(12), new ZoneCondition("hall"), new FlagCondition(Generator.RestoredFlag))
                .Do(new SoundSequenceAction()
                    .Add("footstep_heavy", new Vector3(-3f, 3.2f, 7f), 0f, 0.45f)
                    .Add("footstep_heavy", new Vector3(-1.8f, 3.2f, 7.1f), 0.9f, 0.45f)
                    .Add("footstep_heavy", new Vector3(-0.6f, 3.2f, 6.9f), 1.8f, 0.45f)
                    .Add("footstep_heavy", new Vector3(0.6f, 3.2f, 7f), 2.7f, 0.45f)
                    .Add("knock", new Vector3(3.2f, 0.5f, 13.7f), 5.5f, 0.5f)));

            // ---------- Second Lamp: figures seen only through the lens ----------
            d.Register(NarrativeEvent.Create("apparition_hall")
                .When(new FlagCondition(Generator.RestoredFlag),
                      new AnyCondition(new FlagCondition("saw_ninth"), new PerceptionCondition(10)))
                .Do(new SetActiveAction("apparition_hall", true)));

            d.Register(NarrativeEvent.Create("apparition_window")
                .When(new PerceptionCondition(20), new ZoneCondition("living", false))
                .Do(new SetActiveAction("apparition_window", true)));

            // ---------- the house keeps changing (perception) ----------
            d.Register(NarrativeEvent.Create("tv_static")
                .Note("Heard from another room first: a click, then hiss. Nobody switched it on.")
                .When(new PerceptionCondition(14), new PowerCondition(true), new IndoorsCondition(true),
                      new ZoneCondition("living", false), new LookingAtCondition("tv", false))
                .Do(new SetParamAction("tv", "on", 1f), new SetFlagAction("tv_static_on")));

            d.Register(NarrativeEvent.Create("tv_static_stops")
                .Note("It cuts out the moment the player has had a proper look.")
                .When(new FlagCondition("tv_static_on"), new LookingAtCondition("tv", true, 7f, 1.2f))
                .Do(new SetParamAction("tv", "on", 0f), new SetFlagAction("tv_static_seen"), new PulseAction(0.5f)));

            d.Register(NarrativeEvent.Create("kitchen_cupboard_opens")
                .Note("Heard, not seen. Inside, one of the clay figures from the corridor.")
                .When(new PerceptionCondition(12), new ZoneCondition("kitchen", false), new IndoorsCondition(true),
                      new LookingAtCondition("kitchen_cabinet", false))
                .Do(new SetDoorAction("kitchen_cabinet", true), PlaySoundAction.At("creak", "kitchen_cabinet", 0.8f)));

            d.Register(NarrativeEvent.Create("garden_figure")
                .Note("Naked eye, peripheral. Pale, robed, like the ninth figure. Gone when looked at.")
                .When(new PerceptionCondition(15), new ZoneCondition("living"), new LookingAtCondition("garden_figure", false))
                .Do(new SetActiveAction("garden_figure", true, false)));

            d.Register(NarrativeEvent.Create("wet_footprints")
                .Note("Bare, wet, leading to the basement door. The player has been wearing shoes all night.")
                .When(new PerceptionCondition(18), new FlagCondition(Generator.RestoredFlag),
                      new ZoneCondition("hall", false), new LookingAtCondition("wet_footprints", false))
                .Do(new SetActiveAction("wet_footprints", true)));

            d.Register(NarrativeEvent.Create("hall_photo_scratched")
                .When(new PerceptionCondition(25), new ZoneCondition("hall", false), new LookingAtCondition("hall_photo", false))
                .Do(new SetMaterialStateAction("hall_photo", "scratched")));

            d.Register(NarrativeEvent.Create("hall_portrait_scratched")
                .When(new PerceptionCondition(30), new ZoneCondition("hall", false), new LookingAtCondition("hall_portrait", false))
                .Do(new SetMaterialStateAction("hall_portrait", "scratched")));

            d.Register(NarrativeEvent.Create("chalk_in_kitchen")
                .Note("First chalk mark outside the lamp's light.")
                .When(new PerceptionCondition(35), new ZoneCondition("kitchen", false), new LookingAtCondition("chalk_reason_kitchen", false))
                .Do(new SetActiveAction("chalk_reason_kitchen", true)));

            d.Register(NarrativeEvent.Create("passing_car")
                .Note("Headlights on the road below. Mundane. Probably.")
                .When(new TimeSinceFlagCondition(Generator.RestoredFlag, 90f))
                .Do(new SetActiveAction("passing_car", true, false)));

            // ---------- messages from an unknown number ----------
            const string unknown = "+30 6944 ...812";
            d.Register(NarrativeEvent.Create("sms_dont_use_lamp")
                .When(new AnyCondition(new FlagCondition("saw_ninth"), new FlagCondition("lamp_found")))
                .After(15f)
                .Do(new SendSmsAction(unknown, "Don't use the lamp.", "sms_lamp")));

            d.Register(NarrativeEvent.Create("sms_not_discover")
                .When(new FlagCondition("sms_lamp"), new TimeSinceFlagCondition("lamp_found", 20f), new TimeSinceFlagCondition("sms_lamp", 15f))
                .Do(new SendSmsAction(unknown, "Vardis wasn't trying to discover the ritual.", "sms_discover")));

            d.Register(NarrativeEvent.Create("sms_finish")
                .When(new TimeSinceFlagCondition("sms_discover", 25f))
                .Do(new SendSmsAction(unknown, "He was trying to finish it.", "sms_finish")));

            var call = new CallScript { id = "call_painting", caller = "Unknown number" };
            call.intro.Add(new CallLine("[Silence. Someone breathing, slowly.]", 3.5f));
            call.intro.Add(new CallLine("[A man's voice. Calm. Very quiet.]", 2f));
            call.question = "\"How many people were in the painting?\"   [1] Eight   [2] Nine";
            var eight = new CallOption { label = "Eight.", flag = "call_answer_8" };
            eight.response.Add(new CallLine("[He waits. You hear him waiting.]", 4f));
            eight.response.Add(new CallLine("[The line goes dead.]", 2f));
            var nine = new CallOption { label = "Nine.", flag = "call_answer_9" };
            nine.response.Add(new CallLine("[A pause.]", 2f));
            nine.response.Add(new CallLine("\"Then they've already started.\"", 3.5f));
            nine.response.Add(new CallLine("[Click.]", 1.5f));
            call.options.Add(eight);
            call.options.Add(nine);

            d.Register(NarrativeEvent.Create("call_painting")
                .When(new TimeSinceFlagCondition("sms_finish", 35f), new PlayModeCondition())
                .Do(new StartCallAction(call)));

            // ---------- ending the slice ----------
            d.Register(NarrativeEvent.Create("end_after_lamp")
                .When(new TimeSinceFlagCondition("lamp_out_after_mind", 6f), new TimeSinceFlagCondition("call_painting_done", 8f),
                      new PlayModeCondition())
                .Do(new EndSliceAction()));

            d.Register(NarrativeEvent.Create("end_clock")
                .When(new ClockCondition(23, 50), new PlayModeCondition())
                .Do(new EndSliceAction()));
        }
    }
}
