# Unknown Technology — Developer Handover

Prepared from the repository on **16 November 2026**. The Unity project folder is `PTH`; the game presents itself as **Unknown Technology**. Paths below are relative to `PTH/` unless stated otherwise.

This is a source-based maintenance guide. The code, scene configuration, prefabs and package manifest were inspected; the game, automated tests and a fresh build were **not run** when preparing it. Implemented features below describe the source, rather than certify a release. Read the [Sponsor Guide](SPONSOR_GUIDE.md) for player instructions and reporting problems.

## Contents

- [Current scope](#current-scope)
- [Open and run the project](#open-and-run-the-project)
- [Repository map](#repository-map)
- [Architecture and execution flow](#architecture-and-execution-flow)
- [Gameplay systems](#gameplay-systems)
- [UI, settings and audio](#ui-settings-and-audio)
- [Saves and compatibility](#saves-and-compatibility)
- [Common development tasks](#common-development-tasks)
- [Tests and release checks](#tests-and-release-checks)
- [Troubleshooting](#troubleshooting)
- [Known limitations and follow-up work](#known-limitations-and-follow-up-work)
- [Ownership and handover checklist](#ownership-and-handover-checklist)

## Current scope

A first-person museum puzzle game about historical technology. The current content centres on an Ancient exhibition, a papyrus artefact, a narrated flashback, a wall restoration puzzle and an NPC quiz. A sceptre lets the player carry and place objects.

| Scene | Purpose / current state |
|---|---|
| `Bootstrap` | Startup; creates the persistent game root and routes to the menu. |
| `MainMenu` | Play, three save slots, settings and Quit. |
| `Era_Ancient` | Main authored exhibition and papyrus activity. |
| `Era_Modern`, `Era_Future` | Placeholder rooms containing a `Locked Exhibit`; their inclusion in the build does not mean they have finished gameplay. |
| `Test` | Development scene; excluded from the configured build list. |
| `Assets/_Recovery/0.unity` | Recovery scene; excluded from the configured build list. |

The parent repository's `README.md` and this project's [CHANGELOG.md](CHANGELOG.md) include historical descriptions. Some are superseded: saves, flashbacks, the papyrus puzzle and a Canvas quiz now exist; there are six phases; the UI uses both UI Toolkit and uGUI. In particular, **do not follow the old README's instruction to regenerate scenes during normal setup**.

## Open and run the project

1. Obtain the complete Git repository and install Git LFS. The parent `.gitattributes` uses LFS for models, audio and other binary assets, including everything under `PTH/Assets/Model/`.
2. From the repository root, run `git lfs install` and `git lfs pull`. A file containing `version https://git-lfs.github.com/spec/v1` is an LFS pointer, not the asset itself. Resolve incomplete downloads before diagnosing Unity import failures.
3. Install **Unity 6000.4.10f1**, the exact version in [ProjectVersion.txt](ProjectSettings/ProjectVersion.txt), through Unity Hub, with the build module for the intended platform. Start with this version; test an editor upgrade on a separate branch.
4. In Unity Hub, add/open the **`PTH` folder**, which contains `Assets`, `Packages` and `ProjectSettings`. Do not select the parent repository as the Unity project.
5. Allow package restoration and asset import to finish. An initial import needs access to package registries and may take time. Inspect Console errors before entering Play mode.
6. Open `Assets/Scenes/Bootstrap.unity` and press Play. Expect the main menu. Select Play and an empty save slot to start in Ancient, or an occupied slot to continue.

[PlayModeStartup.cs](Assets/Editor/PlayModeStartup.cs) sets the editor's Play Mode start scene to Bootstrap, even if another scene is open. [DevSceneSetup.cs](Assets/Scripts/DevSceneSetup.cs) also supports creating the game root in an era scene when running directly, but this does **not** create a save session. Use the menu/slot flow when testing saves.

**Do not run `Unknown Technology > Build Scenes` as a repair or setup step.** [SceneBuilder.cs](Assets/Editor/SceneBuilder.cs) is a historical generator that writes scenes and player/game-root prefabs. Running it can overwrite hand-authored content with the earlier greybox layout. Use it only in an isolated, backed-up checkout when deliberately rebuilding that layout. `Unknown Technology > Setup Bonsai Grabbable` also modifies a prefab and adds a scene object.

### Toolchain and dependencies

C# runtime code uses Unity's default `Assembly-CSharp`; editor scripts/tests are under `Assets/Editor`. No custom assembly definitions were found. Unity generates the solution/project files; do not maintain `.csproj` or `.slnx` files as source.

Key direct package versions from [manifest.json](Packages/manifest.json):

| Package | Version | Role |
|---|---|---|
| Universal Render Pipeline | 17.4.0 | Rendering and materials. |
| Input System | 1.19.0 | Player input and UI input bridge. |
| uGUI | 2.0.0 | Canvas-based quiz, dialogue and wall UI; TextMesh Pro assets are present. |
| Test Framework | 1.6.0 | NUnit editor tests. |
| ProBuilder | 6.0.9 | Level authoring. |
| Timeline | 1.8.12 | Authored timeline assets. |
| AI Navigation | 2.0.12 | Installed navigation package. |

Other packages are listed in the manifest. Commit both the manifest and `packages-lock.json` when intentionally changing dependencies. Active Input Handling is configured for the new Input System. The custom runtime systems inspected use local files; no custom backend service or credentials were found in those scripts. Installed Unity service packages alone do not establish a server requirement.

## Repository map

| Location | What a maintainer should look for |
|---|---|
| `Assets/Scenes/` | Authored levels and entry scenes. |
| `Assets/Scripts/` | Global game state, bootstrap, settings and scene transitions. |
| `Assets/Scripts/Player/` | Movement, camera, presentation, sceptre and beam. |
| `Assets/Scripts/Interaction/` | Carryable objects, slots, gates, proximity interaction and papyrus puzzle. |
| `Assets/Scripts/UI/` | Menu/pause controllers, settings binder, flashback, Canvas quiz and helper UI. |
| `Assets/Scripts/Save/` | Save data model, capture/restore and hierarchy-based IDs. |
| `Assets/Scripts/Audio/` | Audio manager and mixer volume handling. |
| `Assets/Editor/` | Startup configuration, legacy scene generation and editor tests. |
| `Assets/Prefabs/` | Reusable objects: `GameRoot`, `Player`, `Fragment`, `slot`, `TriggerGate`, `LevelTransition`, `PapyrusWallPuzzle`, `QuizUIPrefab`, `Bonsai`. |
| `Assets/UI/` | UXML layouts, USS styling, runtime theme, `PanelSettings` and images. |
| `Assets/Audio/`, `Assets/Model/`, `Assets/Materials/`, `Assets/Shaders/`, `Assets/Settings/` | Content, mixer, graphics and render-pipeline configuration. |
| `Packages/`, `ProjectSettings/` | Required project configuration. |
| `Data/`, `Saves/` | Local runtime settings/save files; ignored by Git, except `Data/README.md`. |
| `Library/`, `Temp/`, `Logs/`, `obj/`, generated solution files | Local/generated Unity state; not the source of the project. |

Keep assets and their `.meta` files together. Move or rename Unity assets inside the editor so references survive. Scenes and prefabs contain essential Inspector wiring as well as content; copying only C# scripts is insufficient to reproduce the game.

The project's `.gitignore` ignores `/Documentation/`. These two manuals live at the project root so they can be included in the handover.

## Architecture and execution flow

Most runtime classes use the `UnknownTechnology` namespace; `AudioManager` uses `UnknownTechnology.Audio`, while `QuizManager` is in the global namespace.

```text
Bootstrap scene -> persistent GameRoot / GameBootstrap -> MainMenu
MainMenu -> choose save slot -> Game.LoadScene -> Era_Ancient
Exploring -> artefact flashback -> Exploring -> papyrus puzzle -> NPC quiz
Exploring -> Paused -> Resume, Settings, or Save & Quit -> MainMenu
```

### Global state

[Game.cs](Assets/Scripts/Game.cs) is the static entry point for settings, runtime access, phases and asynchronous scene loading. Use its guarded methods; do not write phase state directly or bypass the save capture with arbitrary scene loads.

| Phase | Allowed changes to a different phase |
|---|---|
| `Boot` | `MainMenu`, `Loading`, `Exploring`. |
| `MainMenu` | `Loading`. |
| `Loading` | `MainMenu`, `Exploring`. |
| `Exploring` | `Loading`, `Flashback`, or `Paused` through `TryPause()`. |
| `Flashback` | `Exploring`, `Loading`; cannot pause. |
| `Paused` | `Loading`, or back to `Exploring` through `TryResume()`. |

Setting the current phase again succeeds without another phase event. Entering pause records `ResumePhase`. Leaving pause through a scene load clears it. `Game.LoadScene()` captures the outgoing scene, enters Loading and completes into the requested phase.

[GameEvents.cs](Assets/Scripts/GameEvents.cs) centralises global events: phase changes, jump/cancel, device changes, carryable grab/place/vacate and flashback finish/skip. Pair subscriptions with unsubscriptions. Static state/events are reset through runtime initialization hooks when a Play session starts.

**Pause blocks player input; it does not set `Time.timeScale` to zero.** Physics and coroutines can continue. Avoid assuming every simulation stops when the pause overlay appears.

### Bootstrap and input

[GameBootstrap.cs](Assets/Scripts/GameBootstrap.cs) is a singleton with `DontDestroyOnLoad`. It owns `PlayerInput`, exposes Move/Look/ToolHeld and adds `SaveRuntime`. Input map changes are deferred to `LateUpdate` to avoid invalidating an active input callback.

- Exploring enables `Gameplay`, except while `CanvasQuizUI.IsQuizOpen`, which uses `UI`.
- MainMenu, Paused and Flashback enable `UI`.
- Boot/Loading disable maps.
- `Restoration` exists in the input asset, but the current phase mapping never enables it. The wall puzzle uses the sceptre in Exploring.
- Device loss raises a notice and attempts to pause; reconnecting raises another notice. Pausing still depends on the current phase allowing it.

Bindings are stored in [UnknownTechnologyActions.asset](Assets/Input/UnknownTechnologyActions.asset):

| Action | Keyboard / mouse | Gamepad |
|---|---|---|
| Move / look | WASD or arrows / mouse | Left stick / right stick. |
| Jump | Space | South button (A / Cross). |
| Tool, hold to carry | F or right mouse button | Right trigger. |
| Pause while exploring | Escape | Start. |
| Menu navigation / confirm | WASD or arrows / Enter or mouse click | Stick or D-pad / South. |
| UI cancel | Escape or Backspace | East button (B / Circle). |
| NPC / artefact / begin wall puzzle | **T, read directly by interaction scripts** | No equivalent wired in those scripts. |
| Check wall answers | Aim at Check, left click | Aim at Check, South. |

`Gameplay/Interact` has E/West bindings but is not wired to the T-based interactions. `Notebook` bindings also exist without an implemented notebook handler in the inspected runtime. Do not advertise these as working features. `toggleInteraction` exists in settings but is not used by the current tool loop.

## Gameplay systems

### Player and sceptre

[FirstPersonPlayer.cs](Assets/Scripts/Player/FirstPersonPlayer.cs) coordinates `PlayerMotor`, `FirstPersonCameraController`, `PlayerAnimationController` and `Scepter`. It places the player at a scene object named **`PlayerSpawn`**, controls cursor locking and applies restored player poses with the CharacterController temporarily disabled.

Movement/look are gated by Exploring, a closed quiz and no active scene transition. Motor movement uses `CharacterController`; the camera applies mouse/gamepad sensitivity and Y inversion. Reduced Motion is used by presentation and some puzzle animations, rather than stopping all animation globally.

[Scepter.cs](Assets/Scripts/Player/Scepter.cs) handles targeting, grab/carry/release and slot highlighting. [CarryableItem.cs](Assets/Scripts/Interaction/CarryableItem.cs) owns item physics, highlights and flight into a slot. [ItemSlot.cs](Assets/Scripts/Interaction/ItemSlot.cs) owns occupancy, placement validation, ejection and correct-placement flashing.

- Aim at a carryable, hold the tool input, then release toward a slot to place it. A nearby eligible object may be selected when none is directly aimed at.
- Carrying uses a dynamic Rigidbody without gravity, velocity toward a hold point and continuous collision. Walls can stop the object; a blocked camera-to-item connection breaks the carry.
- Remote placement flies toward the aimed slot; a nearby slot can snap the item into place. Blocked flight drops the item.
- A generic slot accepts items unless a `PlacementValidator` rejects them. `requiredItemId` controls correctness feedback; it is not, by itself, a universal placement restriction.
- Placing a different item in an occupied slot ejects the old item. Grabbing an item vacates its slot.
- [CarryPhysics.cs](Assets/Scripts/Interaction/CarryPhysics.cs) defines shared obstruction rules; triggers, the item itself and the destination slot are ignored where appropriate.

`TriggerGate` listens globally for placed/vacated items matching its configured `itemID`, and tweens toward its `target` or starting pose. It does not restrict the event to a particular slot. If several unrelated puzzles reuse an ID, they can unintentionally affect the same gate.

### Papyrus activity and NPC quiz

The intended guided sequence is:

1. Enter the exhibition and interact with the NPC using T. Complete/close the introduction.
2. Interact with the papyrus artefact using T. `ArtefactInteraction.onInteract` calls the configured flashback.
3. The five-part `ArtefactSlideshow` presents images/narration. Next advances; Finish, Skip or Escape closes it. Finishing **or skipping** raises the event that unlocks the wall puzzle.
4. Approach the wall puzzle and press T to begin. Use the sceptre to put answer fragments in all blanks, then aim at Check and click.
5. Incorrect checks consume one of three chances; after three, replay/skip the artefact narration to reset the attempt. Correct completion enables the NPC quiz after the introduction has been seen.
6. Talk to the NPC again using T. `QuizManager` advances through questions and allows three incorrect answers across an attempt. Failure closes/resets the quiz so the player can try again. Success shows the completion panel.

Relevant implementation:

| File | Responsibility |
|---|---|
| [ExhibitionGuidance.cs](Assets/Scripts/Interaction/ExhibitionGuidance.cs) | Proximity and activity guidance. |
| [PapyrusPuzzleState.cs](Assets/Scripts/Interaction/PapyrusPuzzleState.cs) | Plain C# state: unlock/start, answers, correctness and chances. |
| [PapyrusWallPuzzle.cs](Assets/Scripts/Interaction/PapyrusWallPuzzle.cs) | Inspector wiring, physical cards/blanks, paragraph layout, check feedback and reset animations. |
| [CanvasQuizUI.cs](Assets/Scripts/UI/CanvasQuizUI.cs) | NPC introduction, quiz eligibility, UI visibility and cursor/player control. |
| [QuizManager.cs](Assets/Scripts/UI/QuizManager.cs) | Question array, correct answer index, chances and completion. |

The wall prefab currently has four blanks and eight answer cards. Correct IDs are `nile`, `3000-bce`, `crossed-layers`, `scrolls`. Paragraph markup links such as `gap-0` map text to the corresponding blank array index; preserve that relationship when editing. Wrong answer cards are intentionally valid placements until Check is pressed.

NPC naming is inconsistent: prompts/code refer to **Jimmy**, the dialogue speaker and model refer to **Timmy**. These currently refer to the same guided activity. The quiz prefab also contains a placeholder third question, **“The answer is C”**, with four identical C choices; only answer index 2 is marked correct.

### Level transitions

`LevelTransition` is a trigger requiring a player collider. Its editor `SceneAsset` populates a serialized `targetSceneName`. The destination must be enabled in the build scene list. `SceneTransition` checks availability/busy state and starts the persistent `ScreenFade`; `Game.LoadScene` performs the load. The destination player starts at `PlayerSpawn`, except when a continued save applies a pending player pose.

Transition code and a prefab exist; confirm actual trigger placement and destination wiring in the scene being released. Their existence does not establish a completed Ancient-to-Modern-to-Future campaign.

## UI, settings and audio

There are **two UI systems**:

- **UI Toolkit:** main menu, save selection, pause/settings, HUD, flashback and transition fade. Edit `MainMenuPage.uxml`, `GameHUD.uxml`, `SettingPage.uxml` and `Main.uss`. Controllers query elements by name; renaming an element requires updating its code queries.
- **uGUI / TextMesh Pro:** NPC introduction, quiz/completion and wall puzzle visuals. Edit prefab/scene components and their Inspector references. Several helpers create UI at runtime; those objects will not exist in the saved hierarchy outside Play mode.

Keep the `EventSystem` / `InputSystemUIInputModule` bridge correctly wired and avoid competing EventSystems. The UI Toolkit `PanelSettings` uses a 960 × 600 reference resolution; `UiScaleSettingsApplier` changes its scale. This does not automatically scale every Canvas or proximity prompt. Test different aspect ratios and 100–150% UI scale: some menu offsets are hard-coded.

[GameSettings.cs](Assets/Scripts/GameSettings.cs) uses schema **2** and local JSON. Update fields then call `Save()` to sanitize, write, apply engine options and emit `Changed`. The binder saves changes as controls are changed; no separate Apply button is required.

| Setting | Supported range / behaviour |
|---|---|
| Mouse / gamepad sensitivity | 0.02–1 / 30–360. |
| UI scale | 1–1.5; UI Toolkit panel. |
| Volumes | Master, music, SFX and UI, each 0–1. |
| Other options | Invert Y, Reduced Motion, quality level, fullscreen. |
| Invalid JSON / unsupported schema | Falls back to defaults; no settings migration. |
| WebGL display options | Display settings are hidden/limited by platform checks. |

`AudioManager` applies volume through `Assets/Audio/Mixers/MainAudioMixer.mixer`, using exposed parameters `MasterVolume`, `MusicVolume`, `SFXVolume`, `UIVolume`. Music/SFX/UI sources and clips are Inspector assignments. It handles menu music, footsteps, clicks and sceptre pickup/carry/drop/place feedback; the carry loop has its own generated source. The flashback has a separate AudioSource: verify its mixer routing and volume behaviour in the release scene.

## Saves and compatibility

Read [GameData.cs](Assets/Scripts/GameData.cs), [SaveSystem.cs](Assets/Scripts/Save/SaveSystem.cs), [SaveData.cs](Assets/Scripts/Save/SaveData.cs), [SaveIds.cs](Assets/Scripts/Save/SaveIds.cs) and [SaveRuntime.cs](Assets/Scripts/Save/SaveRuntime.cs) together.

### Locations and triggers

| Environment | Settings | Saves |
|---|---|---|
| Unity editor | `PTH/Data/settings.json` | `PTH/Saves/slot_1.json` through `slot_3.json`. |
| Desktop build | `Data/settings.json` beneath `GameData.GameDirectory` | `Saves/slot_n.json` beneath the same directory. On Windows this is beside the executable. |
| WebGL code path | Under `Application.persistentDataPath` | Under the same browser-backed root. Persistence across browser reloads needs platform testing. |

`GameDirectory` is the parent of `Application.dataPath` outside WebGL. Do not assume the Windows layout applies identically to a macOS application bundle; validate write access on every supported target. A protected installation folder can prevent saves/settings from being written. This implementation does not fall back to a user-writable desktop data directory.

- The menu selects one of **three** slots. New games start in `Era_Ancient`.
- A scene is saveable when its name begins `Era_`.
- Loading a destination era restores its state one frame later, then calls `SaveNow()`.
- `Game.LoadScene` captures the outgoing era in memory. Capture alone is not a disk write.
- Pause > **Save & Quit** calls `SaveNow()` before returning to the main menu, where the active session is cleared.
- There is no periodic autosave or application-close/crash save hook in the inspected save runtime. Changes since the last write can be lost.

### Data contract and restoration

Save schema is **1**, separate from settings schema 2. A save contains timestamps, slot, play time, current scene, player position/yaw/pitch and a list of scene records. A scene record contains carryable poses/slot IDs and trigger-gate poses.

On restore, gates are restored first, then items are placed through matching slots or restored free when placement fails; a pending continued-player pose is applied afterwards. `IsRestoring` suppresses some sound/flash feedback. Missing object IDs are skipped. If a continued scene is unavailable in the build, Continue falls back to Ancient and clears the pending player pose.

**Important persistence gap:** the schema does not store `PapyrusPuzzleState`, narration unlocks, NPC introduction state, quiz answers/chances or quiz completion. The wall puzzle starts locked, and its placement validator can reject saved cards before the puzzle has been started. Consequently, restoring object transforms is not equivalent to restoring a playable puzzle session. Test saves before, during and after this activity; extending the schema and initialization order is necessary for full progression persistence.

`SaveIds.FromHierarchy` derives IDs from the full object-name hierarchy, with occurrence suffixes for same-named siblings. Renaming/reparenting objects, renaming ancestors or changing the ordering/count of duplicate names can invalidate old save references. `itemId` is a puzzle matching value, **not** a stable save ID. Consider explicit persistent GUIDs and a migration plan before expanding released content.

Writes use a `.tmp` file then replacement. A platform fallback deletes/moves the destination and is less robust than atomic replacement; this is not a backup system. Unparseable/unsupported-version saves are moved to `slot_n.corrupt.json` and appear empty. Another failed read can replace an existing corrupt backup. Read validation checks the schema version, not every nested field, so malformed same-version data can still fail later.

For recovery: close the game, copy the entire `Saves` folder to a dated backup, preserve any corrupt file and log, and restore a known-good `slot_n.json` only while the game is closed. Do not recommend editing JSON to a nontechnical user. Back up `Data/settings.json` separately before renaming it to reset settings.

## Common development tasks

### Change dialogue, quiz or flashback content

1. Create a branch and inspect the relevant prefab **and** any scene-instance overrides.
2. Quiz: edit `QuizManager.questions` in `QuizUIPrefab`; maintain four answers and a zero-based correct index (0=A, 1=B, 2=C, 3=D). Keep its button/text/panel references assigned. Replace the placeholder question before a public demonstration.
3. Introduction dialogue is currently the `IntroductionLines` array in `CanvasQuizUI`, rather than an external content asset.
4. Flashback slides are serialized on `ArtefactSlideshow` in Ancient: title, narration AudioClip and optional image Texture2D. The scene currently assigns five narration/image pairs. Check the artefact UnityEvent calls `Begin()` on the intended slideshow and the wall references that slideshow.
5. Test opening/closing dialogue, finishing/skipping narration, correct/wrong quiz answers, failure/retry and completion.

### Change the wall puzzle

Edit `PapyrusWallPuzzle.prefab`, checking scene overrides before applying changes. Keep fragment and blank arrays assigned, matching `ItemId` / `RequiredItemId` values, colliders, Rigidbody/renderers, text references, screen content and the linked paragraph markup. Array indices must agree with `gap-n` links. Check answer targeting after changing fonts or paragraph layout: code recalculates physical blank positions from TMP text. Test three failed checks, narration replay, replacement/ejection and completion.

### Add a carryable object, slot or gate

Use existing prefabs as examples. A carryable needs `CarryableItem`, a Rigidbody, a suitable collider and renderer; moving mesh colliders normally need a convex configuration. A slot needs a hittable collider, `ItemSlot`, a snap point and optional highlight renderer/required ID. Configure the gate's target pose and matching item ID; test both placement and removal. Use unique hierarchy names where possible and assess old-save compatibility before moving objects.

### Add a new exhibition

Create an authored scene with player, `PlayerSpawn`, era HUD/pause UI, the required input bridge and bootstrap support. Use an `Era_` name for the current save convention. Add it to the enabled build scene list, configure transition targets, and test entry/return/save/continue. Reusing a locked placeholder scene still requires authoring its content and progression; scene registration alone does not create gameplay.

### Change input or presentation

Update the action asset and all direct keyboard/mouse checks together. To support controller-only play, route T-based interactions through a shared action and test UI/gameplay map switching. If changing the UI, check both UI Toolkit and Canvas screens. Keep old-save compatibility, aspect ratios, Reduced Motion and controller disconnection in the review checklist.

## Tests and release checks

### Existing automated tests

Open Unity's Test Runner (normally **Window > General > Test Runner**) and run **EditMode** tests under `Assets/Editor/Tests/`:

| Test file | Coverage |
|---|---|
| [GamePhaseTests.cs](Assets/Editor/Tests/GamePhaseTests.cs) | Phase transition matrix, pause/resume and leaving pause for a load. |
| [GameSettingsTests.cs](Assets/Editor/Tests/GameSettingsTests.cs) | Settings clamping and invalid/old-version fallback. Backs up/restores the real local settings file. |
| [SaveSystemTests.cs](Assets/Editor/Tests/SaveSystemTests.cs) | Summary reads and corrupt/version handling. Creates/cleans slots 901–905. |

Run these in a development copy with data backed up. Existing tests do not establish correct scene wiring, physical interaction, full save/restore, papyrus progression, quiz behaviour or build/platform support. No dedicated tests for those behaviours were found in this test folder. Record results; no pass result is claimed by this manual.

### Build a Windows handover candidate

1. Resolve Console compilation/import errors and complete the EditMode tests.
2. Open **File > Build Profiles**. Select/add the intended Windows profile and its build support module. Inspect that profile's actual scene list, including any overrides.
3. Match [EditorBuildSettings.asset](ProjectSettings/EditorBuildSettings.asset): Bootstrap first, then MainMenu, Era_Ancient, Era_Modern, Era_Future, all enabled. Exclude Test/recovery scenes.
4. Confirm player/company names, graphics quality and the target architecture. Current Player Settings use `DefaultCompany` / `PTH`; changing these can change standard Unity log locations.
5. Build to a separate output folder outside `Assets`, such as `Builds/Windows/`. Create a Development Build for diagnosis if needed, and a separate release candidate for sponsor testing.
6. Launch the resulting executable outside Unity from a writable folder. Give the sponsor the **entire output folder**, including the executable, matching `_Data` folder, Unity player libraries and any other emitted files.
7. Test on the actual sponsor computer using the checklist below. Record the commit, editor version, platform, build date and results alongside the delivered build. A browser release needs a separately tested WebGL build and hosting workflow.

### Manual acceptance checklist

- [ ] Fresh launch reaches menu; settings and all three slots are usable.
- [ ] Empty slot starts Ancient; occupied slot continues; delete requires a second confirming press within three seconds.
- [ ] Movement, look, jump, carry/drop/place and wall obstruction behave correctly.
- [ ] NPC introduction, artefact narration, Skip/Finish, wall unlock, all blanks and Check work in sequence.
- [ ] Wrong checks, three-chance reset, quiz failure/retry and completion behave correctly.
- [ ] Save & Quit, close/relaunch and Continue restore the intended position and generic objects. Record the known puzzle/progression gap explicitly.
- [ ] Test before/during/after puzzle completion and after returning to a scene; do not sign off full persistence based only on player position.
- [ ] Pause/resume, settings changes, controller loss/reconnection and keyboard/mouse fallback work.
- [ ] Menus, wall text, dialogue and quiz fit the target screen, window mode and UI scales; audio controls and Reduced Motion behave as described.
- [ ] Actual configured level-transition triggers load the correct scene and clear the fade.
- [ ] No unexplained Console/player-log errors, and local files are actually written after saving.
- [ ] Package is complete, content/asset permissions recorded, and sponsor can repeat the basic walkthrough.

## Troubleshooting

In Unity, open **Window > General > Console**, reproduce once and capture the first relevant error plus its stack trace. Later errors may be consequences. Keep the full log and note the scene, active phase, input device and save slot. In a built Windows player, the usual log location for the current names is `%USERPROFILE%\AppData\LocalLow\DefaultCompany\PTH\Player.log`; it can change with Player Settings or launch options. Copy it before relaunching. Standard Windows editor logs are normally under `%LOCALAPPDATA%\Unity\Editor\Editor.log`.

| Symptom / message | Check first |
|---|---|
| Missing assets / import failures on a fresh clone | Complete LFS download, exact editor version, package restoration and preserved `.meta` files. |
| `No GameBootstrap found...` / `Game services are unavailable.` | Bootstrap startup, `GameRoot` prefab with `PlayerInput`, and `DevSceneSetup` reference when running directly. |
| Scene cannot load / persistent loading or fade | Scene name and enabled build list; `LevelTransition.targetSceneName`; first exception in `Game.LoadScene` / fade runner. |
| Movement works but E/controller interaction does nothing | Current NPC/artefact/wall interactions poll T directly. Read the input section. |
| Flashback controls missing | `GameHUD.uxml` names, UIDocument and referenced PanelSettings/theme. |
| Papyrus stays off | Finish/skip the configured slideshow, then press T near the wall; check flashback reference and trigger. After three failures, replay narration. |
| Check does nothing | All blanks must be occupied, no fragment carried/flying, Exploring active, quiz closed, and Check collider reachable by the camera ray. |
| Quiz will not start | Introduction seen, wall puzzle complete and result animation finished; required puzzle/panel references assigned. |
| `PapyrusWallPuzzle: assign...` / `CanvasQuizUI: Quiz Panel is not assigned.` | Inspector arrays/references and scene overrides. |
| Saves/settings disappear or revert | Correct executable folder and slot; actual JSON write; directory permissions; corrupt/version warnings. `Save & Quit` currently ignores the boolean save result. |
| Continue restores the room but not the activity | Known progression persistence gap; extend save model and restore puzzle state before validating item placement. |
| Objects fail to restore / duplicate save ID warning | Hierarchy changes, duplicate names/IDs and missing slots; compare backed-up JSON IDs with current object IDs. |
| Silent audio / volume control mismatch | OS output, settings, assigned sources/clips, mixer references/parameters and each source's output group. |
| Pause still allows object motion | Current input-based pause does not stop physics/coroutines. |
| Pink materials or absent highlights | URP assets/quality assignment, shader compile errors and materials supporting the rim/emission properties. |

`Data/Diagnostics` is only a defined/planned location. No automatic diagnostic report uploader or sponsor-facing error dashboard was found. File operations generally log warnings; the UI may continue without clearly reporting a write failure.

## Known limitations and follow-up work

These are observations from source/configuration, not results of a new playtest. They should become tracked issues with an owner and reproduction evidence.

| Priority | Finding | Recommended next work |
|---|---|---|
| High | Puzzle unlock/start/completion, NPC introduction and quiz progress are absent from saves; restored card placement can be rejected while locked. | Define the intended persistence contract, extend/version the data model and test initialization/restore ordering. |
| High | Save & Quit returns to the menu even if `SaveNow()` returns false; settings writes also lack a visible failure response. | Surface failure, offer a retry, and use/validate a user-writable storage strategy. |
| High before content release | Quiz question 3 is placeholder content with identical labels and only one accepted index. | Replace and review all questions/answers with the sponsor. |
| Medium | T-based interactions leave controller-only play incomplete. | Consolidate actions and test the full activity using only a controller. |
| Medium | Pause does not freeze the simulation. | Decide desired semantics; test held objects, gates and feedback timers while paused. |
| Medium | Hierarchy-derived IDs and no save migration make scene/schema changes fragile. | Add stable IDs, stronger validation, compatibility policy and restore tests. |
| Medium | Modern/Future are unfinished placeholders. | Agree scope and author content before advertising a complete three-era game. |
| Medium | NPC has Jimmy/Timmy labels; fixed UI offsets and mixed scaling need review. | Standardise naming and test target displays/accessibility. |
| Verification needed | Desktop/WebGL branches exist, but no platform certification or fresh build was performed for this handover guide. | Record target-machine tests and browser persistence/hosting checks if WebGL is offered. |
| Maintenance | Legacy README setup can regenerate authored scenes; diagnostics remain planned. | Keep setup instructions aligned and decide whether to retire the generator / add diagnostics. |

## Ownership and handover checklist

Before final acceptance, the project owner should record the following in the delivery record or issue tracker:

- Repository owner, sponsor access, default branch and exact handed-over commit/tag.
- Source archive and a separately labelled, tested playable build with these manuals.
- Named maintenance contact and the agreed issue-reporting channel; none is assigned by these manuals.
- Supported operating systems, actual test hardware and any agreed minimum requirements; no measured minimum hardware specification is established here.
- Known issues accepted for delivery, release-test results and any deferred work.
- An inventory of models, images, fonts, audio and other third-party content with their licences, attribution and redistribution permissions. Preserve bundled notices; repository access alone does not document every asset's reuse rights.
- Local save/settings backup procedure and compatibility expectations for future updates. Runtime data and generated build folders are ignored by Git, so a source handover does not automatically include them.

Future developers should update both manuals when controls, save behaviour, scene scope, support contacts or release procedures change.
