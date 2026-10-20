# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Sceptre grab-and-carry: holding the tool button grabs the nearest carryable
  within range (the one under the crosshair takes priority, seated ones
  included) and pulls it in front of the camera (kinematic follow, colliders
  ignored against the player); pointing the sceptre at a grabbable lights its
  edges — a tight fresnel plus an inverted-hull silhouette outline, both
  driven by the per-renderer `_RimStrength` (`Custom/CarryableRim` shader now
  also samples base, occlusion and roughness maps). While the hand is full
  every slot glows, the aimed one brightens, and releasing then makes the
  item fly into it — the flight animation is owned by the carryable itself
  (`FlyTo`, no slot reference on items). Items carry an optional id; a slot
  with a matching required id flashes the placed item gold (shader
  `_FlashAmount`, ~0.6 s decay), otherwise nothing changes. Slots are
  idempotent, hold one item, eject the previous occupant upward when a new
  one arrives. Slots pulse an emissive highlight
  while their item is carried, brightening near the snap point (steady glow
  under Reduced Motion). New `Scepter` (Player root), `CarryableItem` and
  `ItemSlot` (Interaction) components, `CarryableGrabbed`/`CarryablePlaced`
  events; manual scene wiring guide in
  `Documentation/02_TechnicalDesign/Modules/Scepter.md`.
- Bonsai exhibit prop: `Assets/Model` import cleanup (roughness/AO imported as
  linear data maps, embedded material remapped to `tree.mat`) and a one-shot
  Unknown Technology → Setup Bonsai Grabbable menu that builds a grabbable
  prefab (convex MeshCollider + Rigidbody + CarryableItem) and places an
  instance in the open scene.
- Enabled Bloom on the default volume (intensity 0 → 0.25) so emissive slot
  highlights read as a glow.
- Level transition trigger prefab: `LevelTransition` is an invisible trigger
  volume created as a prefab under `Assets/Prefabs`; drag a `.unity` scene into
  its Target Scene field and walking into it fades the screen out, loads the
  target scene, respawns the player at that scene's `PlayerSpawn` and fades
  back in. `SceneTransition`/`ScreenFade` own the persistent overlay and reject
  invalid targets (missing scene, scene not in Build Settings, repeated
  triggers) without leaving the player stuck on a black screen.
- Sceptre beam: while an item is carried, a `ScepterBeam` component (Player
  root) draws a LineRenderer from the sceptre tip to the item's bounds centre
  with the new additive `Custom/ScepterBeam` shader (`Materials/ScepterBeam.mat`):
  white core, cyan glow, pulses running towards the item and two
  counter-phase filaments, HDR for bloom. The line itself follows a travelling
  sine wave pinned at both ends. It fades in on grab and fades out on release,
  following the item into its slot. Reduced Motion flattens the wave and stops
  the pulses.

### Changed

- Aim highlight fades instead of switching: `CarryableItem.SetHighlight`
  eases `_RimStrength` in (0.2 s) and out (0.35 s), and `Scepter` scales the
  target by distance — full up to `highlightFullDistance` (4 m), easing to
  nothing at the edge of grab range (the Player prefab uses 10 m).

- `Custom/CarryableRim` aim highlight now reads as an edge glow instead of a
  lit surface: a thin fresnel edge (`_RimPower` 6) that shifts to white at the
  silhouette and outputs HDR for bloom (`_RimIntensity`, `_RimCoreWhite`), plus
  an additive halo hull pushed out in world units that fades outwards
  (`_OutlineWidth`, `_GlowIntensity`, `_GlowFalloff`). The base texture stays
  untouched. `_UseCenterNormals` drives both from object-centre directions so
  hard-edged convex meshes (the fragment cubes) get a smooth gradient and a
  closed hull instead of whole lit faces and torn corners. The passes are
  tagged `UniversalForward` / `SRPDefaultUnlit` and the shader renders at
  `Transparent-10` so the depth-less halo is never painted over by later
  geometry or the skybox. Property names used by `Scepter` and `ItemSlot` are
  unchanged.

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
  UI scale (1.0–1.5), reduced motion and fullscreen; clamped, persisted as
  pretty JSON in the local `Data` folder (which also hosts future saves and
  performance diagnostics) and broadcast on change; WebGL hides display
  options.
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
