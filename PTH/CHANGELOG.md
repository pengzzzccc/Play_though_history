# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.0.1] - 2026-09-10

First tagged version: a playable greybox vertical slice with the final
simplified architecture.

### Added

- Playable slice: main menu → Ancient Exhibition greybox room with walls,
  display plinths, ramp and steps; Modern/Future eras exist as locked
  placeholder scenes.
- First-person controller: `CharacterController` movement with gravity,
  grounded jumping and fall-speed clamping; yaw/pitch camera with ±80° pitch
  clamp; walk bob and sceptre presentation honouring the Reduced Motion
  setting.
- Pause menu with live settings: mouse/gamepad sensitivity, Y-axis inversion,
  UI scale (1.0–1.5), reduced motion and fullscreen; clamped, persisted to
  PlayerPrefs as JSON and broadcast on change; WebGL hides display options.
- UI Toolkit front-end: UXML documents (main menu, era HUD + pause overlay),
  shared USS with design tokens, default runtime theme, PanelSettings at
  960×600 `ScaleWithScreenSize` (match 0.5); focus-driven gamepad navigation.
- Input System asset with three action maps (Gameplay / Restoration / UI) and
  two control schemes (Keyboard&Mouse / Gamepad); per-phase map switching
  deferred to end-of-frame to avoid tearing active callback contexts.
- Device handling: gamepad loss auto-pauses the game and raises a UI notice;
  reconnect shows a restored notice.
- `SceneBuilder` editor tool (menu: Unknown Technology → Build Scenes) that
  generates prefabs and scenes once, reusing existing assets on disk.

### Changed

- Migrated the entire UI from uGUI + TextMeshPro to UI Toolkit; the scene
  `EventSystem` + `InputSystemUIInputModule` is kept solely as the official
  input bridge for focus navigation.
- Rewrote the game architecture from a typed event bus with dependency
  injection, five service interfaces and six assembly definitions (~2,400
  lines) to a static `Game` phase machine with centralised `GameEvents`
  (~1,100 lines, single assembly) — behaviour preserved, ceremony removed.
- Reduced the phase enum from ten reserved states to five shipped ones
  (`Boot/MainMenu/Loading/Exploring/Paused`); future modes will be added as
  they are implemented.
- Flattened the project layout: `Assets/UnknownTechnology/*` moved directly
  under `Assets/` (Scenes, Scripts, UI, Input, Editor, Prefabs, Materials).

### Removed

- Event bus and its 15 message structs, `GameContext` provider, all
  single-implementation service interfaces, and the assembly definitions.
- `SceneFlowConfig` asset, scene routing controller, access policy and
  stubbed progress/continue providers (scene loads now go through
  `Game.LoadScene`).
- The 36-test suite, dropped together with the architecture it pinned down.
- Unity template leftovers: SampleScene, TutorialInfo, recovery files and the
  template input asset.

[0.0.1]: https://github.com/pengzzzccc/Play_though_history/releases/tag/v0.0.1
