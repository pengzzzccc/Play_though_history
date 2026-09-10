# Unknown Technology

**Version 0.0.1** · Unity 6000.4 · URP · Input System · UI Toolkit

A first-person museum puzzle game. You are a night curator investigating missing
technology relics across the museum's three exhibition eras — Ancient, Modern and
Future — and restoring them with a mysterious sceptre. The current release is a
playable greybox vertical slice: the core loop, input model and UI framework are
in place, while content and the restoration gameplay are still ahead.

## What's in v0.0.1

- Main menu with New Game / Continue (disabled until saves exist) / Quit.
- First-person controller: WASD movement, grounded jumps, mouse/gamepad look with
  ±80° pitch clamp, head-bob presentation that respects Reduced Motion.
- Pause menu with a live settings panel: mouse/gamepad sensitivity, Y-axis
  inversion, UI scale (100%–150%), reduced motion and fullscreen. All settings
  persist to PlayerPrefs and survive restarts.
- Full keyboard & mouse + gamepad support with automatic control-scheme
  switching; losing a gamepad auto-pauses the game and shows a reconnect notice.
- UI Toolkit front-end (UXML/USS, shared PanelSettings at a 960×600 baseline)
  with focus-based gamepad navigation.

## Project Structure

```text
Play_though_history/
├── README.md
├── CHANGELOG.md
└── PTH/                            # Unity 6000.4 project (open this folder)
    ├── ProjectSettings/            # player, physics, URP quality settings
    ├── Packages/                   # manifest: URP, Input System, UI Toolkit
    ├── Documentation/              # design documents (gitignored)
    └── Assets/
        ├── Scenes/                 # Bootstrap, MainMenu, Era_Ancient/Modern/Future
        ├── Scripts/                # all runtime code (~1,100 lines, no asmdefs)
        │   ├── Game.cs             # static phase machine + scene loads
        │   ├── GameEvents.cs       # every global event
        │   ├── GameSettings.cs     # persisted player settings
        │   ├── GameBootstrap.cs    # persistent input root (+ DevSceneSetup)
        │   ├── Player/             # motor, first-person player, camera, animation
        │   └── UI/                 # UI Toolkit controllers (menu, pause, HUD, scale)
        ├── UI/                     # UXML / USS / theme / PanelSettings
        ├── Input/                  # InputActionAsset (Gameplay / Restoration / UI maps)
        ├── Editor/                 # SceneBuilder (one-shot scene generator)
        ├── Prefabs/                # Player, GameRoot
        ├── Materials/              # greybox floor / wall / accent materials
        ├── Settings/               # URP render pipeline assets
        └── TextMesh Pro/           # TMP essentials (kept to avoid import dialogs)
```

## Getting Started

1. Install Unity **6000.4.10f1** or newer.
2. Open the `PTH/` folder as the Unity project.
3. Run the menu command **Unknown Technology → Build Scenes** once. This
   (re)generates the prefabs, the five scenes and the Build Settings list.
4. Open the **Bootstrap** scene and press Play — the game routes itself to the
   main menu. Era scenes can also be opened directly for level work; a
   `DevSceneSetup` component spawns the game root automatically.

## Controls

| Action | Keyboard & Mouse | Gamepad |
|---|---|---|
| Move | WASD / arrows | Left stick |
| Look | Mouse | Right stick |
| Jump | Space | A / South |
| Tool (hold) | F / right mouse | Right trigger |
| Pause | Esc | Start |
| UI navigate | Arrows | Left stick / D-pad |
| UI confirm | Enter | A / South |
| UI cancel | Backspace / Esc | B / East |

## Architecture

The runtime is deliberately small and flat. A static `Game` class owns a
five-phase state machine (`Boot → MainMenu → Loading → Exploring ⇄ Paused`) and
all scene loads; phases only change through guarded methods
(`SetPhase`/`TryPause`/`TryResume`). `GameEvents` centralises every global
notification, `GameSettings` handles persisted options, and the persistent
`GameBootstrap` routes Input System actions and enables exactly one action map
per phase — pausing therefore disables gameplay input structurally, not by
scattered checks. Presentation is UI Toolkit: structure in UXML, styling in USS,
thin controllers that query elements by name. There are no assembly definitions
and no dependency injection; the whole runtime is ~1,100 lines you can read in
an afternoon.

## Roadmap

- Relic restoration mini-game (move / rotate / snap fragments with the sceptre).
- Interaction system and exhibit content for the three eras.
- NPC dialogue, history quizzes and progression gates.
- Audio design, flashbacks and a save system.
