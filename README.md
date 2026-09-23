# THE THIRD LAMP

A first-person psychological horror game. Greece, 2002. Alex, a 24-year-old student, takes a
€300 overnight job cataloguing the collection of the late historian Dr. Elias Vardis in an
isolated villa outside Athens. The job's rules:

1. Photograph each numbered object.
2. Enter its catalogue number into the old computer.
3. Return it to the correct location.
4. Keep the generator running.
5. Do not enter the basement.
6. Do not light an old oil lamp if you find one.

This repository holds a **vertical slice** of about 15–20 minutes, built in Unity 6 with URP.

## Running it

1. Open the repository folder with **Unity 6 (6000.0 LTS or newer)**. Let the packages import.
   If Unity asks to enable the new Input System backend, either answer works: the code supports
   both input backends.
2. Open `Assets/_Project/Scenes/Slice.unity`, or use **Tools › The Third Lamp › Open Slice Scene**.
3. Press Play.

The whole villa is built from code at runtime (`VillaBuilder`), so the scene contains only a
`SliceBootstrap` object. If the scene file ever breaks, use **Tools › The Third Lamp › Recreate
Slice Scene**.

**Optional URP post-processing** (warm tungsten grade, grain, vignette, darker grade under the
Third Lamp): create an URP asset (*Create › Rendering › URP Asset (with Universal Renderer)*) and
assign it in *Project Settings › Graphics* and *Quality*. Without it the slice runs on the
built-in pipeline, and a lighter IMGUI grain and vignette overlay is always on.
For standalone builds, run **Tools › The Third Lamp › Include Runtime Shaders In Builds** once.

## Controls

| Key | Action |
| --- | --- |
| WASD / mouse | walk / look (Shift walks faster) |
| E | interact |
| F | turn over the held item (inspect). Scroll to bring it closer |
| Q | put the held item down |
| C or right mouse | raise/lower the digital camera. Left mouse takes a photo, R reviews photos |
| Tab | phone. Arrows/Enter navigate, Enter answers a call, 1/2 choose a reply |
| T | torch (once found) |
| L | light / put out the oil lamp (while holding it) |
| Esc | pause and control list. F9 loads the last checkpoint, F12 restarts |
| F1 | debug overlay (editor/dev builds): flags, counters, hidden perception, clock |

## What's in the slice

Arrival by car at night → key under the blue pot → handwritten instructions → lights, coffee,
camera, torch → log into Vardis's archive (`phos`) → photograph, enter, and return three objects
(bronze disc 014, icon fragment 027, the painting 031) → the generator stalls → restore it in the
shed → the painting now has **nine** figures (earlier photos still show eight) → a brass key behind
the frame → the desk drawer → the oil lamp → texts from an unknown number → the phone call
("How many people were in the painting?") → optionally light the lamp in darkness and walk the
corridor that shouldn't exist.

Perception-driven changes, all silent and missable:

- the hallway photo tilts
- a kitchen chair moves
- the bathroom reflection starts to lag
- the study door you left open is found closed
- footsteps cross the roof
- a figure stands at the end of the hallway, visible only through the camera
- at higher perception, someone stands outside the living-room window, also only through the camera

## Architecture

```
Assets/_Project/
  Scenes/Slice.unity            one GameObject: SliceBootstrap
  Scripts/
    Core/      Game (static service access, input mode), GameInput (Input System or legacy),
               GameState (flags, counters, clock), PerceptionManager (hidden variable),
               WorldRegistry (id → object), RoomZone, SaveManager (checkpoints), SliceBootstrap
    Events/    NarrativeEvent (ScriptableObject), Conditions, Actions, EventDirector, Perceive
    Interaction/ PlayerController, PlayerInteractor, Interactable (+Message/FlagPickup), Openable,
               LightSwitch, NoteReader, CoffeeMachine, InspectableObject, PlacementSocket,
               Inspector, MaterialStates
    Systems/   LightingStateManager (Reason/Memory/Mind, power, light groups), CameraSystem
               (Second Lamp), PhoneSystem (SMS + calls), CatalogueComputer, Generator, OilLamp
               (Third Lamp), Torch, Mirror + ReflectionBody, Apparition, AudioEventManager,
               ProceduralAudio
    UI/        Hud (reticle, prompts, subtitles, documents, pause, debug), ScreenFx, Fonts
    World/     VillaBuilder (+Layout, Rooms, Mind), Tex (texture slots), Pix, Mats, UrpPostFx
    Content/   SliceContent (all events, catalogue records, the call), SliceText (all documents)
  Editor/SliceMenu.cs           Tools › The Third Lamp menu
  Art/asset_manifest.json       texture slots and generation prompts
  Resources/ThirdLamp/{Textures,Audio}/   drop-in overrides for placeholders
```

### Data-driven events

Every scripted beat is a `NarrativeEvent`: conditions (all must hold) plus actions. Beats are
defined in `SliceContent.cs` and can also be authored as assets (*Create › Third Lamp ›
Narrative Event*). Nothing is random. Example:

```csharp
d.Register(NarrativeEvent.Create("painting_ninth_figure")
    .When(new CounterCondition("catalogued_items", 3),
          new FlagCondition("painting_first_seen"),
          new LookingAtCondition("painting_031", looking: false))   // only while unobserved
    .Do(new SetMaterialStateAction("painting_031", "nine"),       // no sound, no notification
        new SetFlagAction("painting_changed"),
        new SetActiveAction("desk_key", true)));
```

Conditions: flag, counter, perception range, clock, time-since-flag, zone, indoors, looking-at
(with hold time, or "not visible to eye or lens"), camera raised, lamp mode, power, door state,
play mode, any-of.
Actions: set flag, add perception, set active, material state, nudge transform, set parameter,
door, sound, sound sequence, SMS, call, power, subtitle, checkpoint, end slice.
State-changing actions are replayed silently when a checkpoint loads.

### Perception

The hidden `perception` value never appears in the UI. Each source counts once.

| Source | + |
| --- | --- |
| Vardis's notebook, the letter in the loose book | 2 each |
| Studying the hidden mark on 014, 027, 031 (inspect, close up) | 1 each |
| Noticing the ninth figure | 3 |
| Watching the hallway figure through the lens for 3 s | 3 |
| Each time the Third Lamp takes hold in darkness | 5 |

Stages: 0–9, 10–19, 20–34, 35–49, 50+. Stage 1 unlocks the small inconsistencies. At 20 the
window figure and a Mind-only doorway in the study appear. Curiosity makes the night worse.

### The three lamps

- **Reason**: electric light. Rooms have light groups on switches. The generator powers
  everything.
- **Memory**: the camera's LCD renders a second, low-res camera that also sees the `Memory`
  layer (29). Mirrors see it too. Photos are frozen copies, so they keep showing what was true
  when they were taken.
- **Mind**: the oil lamp, lit, near the player, with no torch and the room's electric light off.
  Objects tagged Mind-only appear and Reason-only ones vanish. The basement wall opens onto a
  stone corridor far larger than the house. Putting the lamp out inside the corridor returns you
  to the hallway.

## Art and audio

The PixelLab MCP wasn't reachable from the build environment (its network policy blocked
`api.pixellab.ai`), so every texture is a **procedural placeholder** behind a named slot, and
every sound is synthesised. To replace them:

- Textures: generate the slots listed in `Assets/_Project/Art/asset_manifest.json` (it includes
  the prompts) and save them to `Assets/_Project/Resources/ThirdLamp/Textures/<name>.png`.
- Audio: drop clips into `Assets/_Project/Resources/ThirdLamp/Audio/<name>`.

No code changes are needed either way.

## Status and scope

The runtime code is compile-checked against Unity reference assemblies, for both input backends
and the URP branch. It hasn't been play-tested inside the Unity Editor yet, so expect a tuning
pass on light levels, text sizes and a few positions.

Not in this slice (hooks exist): the Polaroid event (the clock and `ClockCondition` are ready),
the floor-plan reveal (`PlacementSocket.Log` records every placement's order and location), the
basement, the cassette, and the three endings.
