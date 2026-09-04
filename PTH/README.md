# Unknown Technology

## 1. Project Overview

**Unknown Technology** is a first-person educational museum sandbox puzzle game. The player takes the role of a museum employee at an exhibition of science, technology, and innovation, investigating nine missing or damaged artifacts and using an upgradable Scepter to find clues, recover fragments, and complete restorations. Each restoration adds historical facts, a short flashback, and a technology timeline, ultimately connecting the three eras: Ancient, Modern, and Future.

The repository already contains a playable first-person foundation slice: the player can reach the main menu from the official entry point, load the Ancient gallery greybox, and move, look around, pause, and adjust basic settings with keyboard/mouse or gamepad. M01–M04 are complete within the current scope, and M05 delivered the first-person controls that support this slice; artifact interaction, restoration, historical content, and the full game loop remain for later modules.

## 2. High Concept

The player investigates missing artifacts in a cozy yet mysterious museum of technology, reconstructing the history of technology through observation, logical reasoning, and spatial assembly. The core experience is not combat but discovery, understanding, and restoration: the player is at once an investigator, an artifact restorer, and an archivist of history.

## 3. Core Experience

The game is built around four experience pillars:

- **Investigation & Discovery**: explore the galleries, read environmental clues, and find artifact fragments and facts.
- **Logic & Restoration**: move, rotate, match, and snap fragments together to rebuild artifacts.
- **Historical Connection**: understand what problem an artifact solved and how it influenced later technology.
- **Gallery Recovery**: empty display cases, lighting, sound, and the timeline gradually recover with progress, providing clear feedback of achievement.

## 4. Target Audience

- Players interested in history, technology, or museum themes.
- Players with limited game experience who need clear guidance and low mechanical demands.
- No strict age range; the final historical texts, question difficulty, and content rating still require review by the content team.

## 5. Target Platforms

- Primary platform: Windows 64-bit.
- Distribution: a WebGL-capable website or itch.io page.
- Input: keyboard/mouse and common gamepads are both within first-release acceptance scope.
- Minimum UI baseline: 960×600.
- Touch, XR, and local multiplayer are not first-release targets.

## 6. Core Features

- First-person museum investigation and environmental interaction.
- A single upgradable artifact-restoration Scepter used throughout the game.
- Three era galleries: Ancient, Modern, and Future.
- Nine unique artifacts, three per era.
- Fragment search, spatial rotation, matching, and snap assembly for restoration.
- Historical fact notes, artifact flashbacks, and a technology timeline.
- A 3-question quiz per era; passing it unlocks the era Scepter.
- Conditional linear NPC dialogue with progressively delivered hints.
- Auto-save, `Continue`, keyboard/mouse and gamepad prompts, and basic accessibility settings.

## 7. Scope and Constraints

The target playtime is about 20 minutes: roughly 2 minutes for the prologue, 5 minutes per era, and 3 minutes for the ending. The first playable target is a 5–6 minute Ancient gallery vertical slice.

The first release explicitly excludes:

- Combat, health, death, time limits, or resource penalties.
- Crouching, sprinting, climbing, and complex movement abilities; only a basic grounded jump remains.
- NPC choice trees, side quests, or behavior trees.
- Freely playable historical flashback levels.
- Touch, XR, networking, cloud saves, or multiplayer features.
- Independent operation mechanics for each of the three Scepters.
- Historical facts that have not been registered to a source and passed content review.

## 8. Core Gameplay Loop

Single-artifact loop:

```text
Discover an empty display case
→ Investigate nearby clues
→ Obtain a historical fact
→ Find 2–4 fragments
→ Go to the restoration bench
→ Move, rotate, and assemble
→ Artifact restored
→ Watch a skippable flashback
→ Record the historical note and timeline entry
```

Single-era loop:

```text
Enter the gallery
→ NPC introduces the era theme
→ Restore three artifacts
→ View the era timeline
→ Complete the 3-question quiz (3/3)
→ Unlock the era Scepter
→ Unlock the next era
```

Full flow:

```text
Main Menu → Prologue → Ancient → Modern → Future → Full Timeline → Ending
```

Detailed rules are in [Core Gameplay](Documentation/01_GameDesign/CoreGameplay.md) and [Player Flow](Documentation/01_GameDesign/PlayerFlow.md).

## 9. Current Repository Status

The following status reflects implementation and verification results as of 2026-08-31.

| Item | Current Status |
|---|---|
| Unity version | `6000.4.10f1` |
| Rendering | URP `17.4.0` installed and configured |
| Input | Input System `1.19.0`; official `Gameplay`, `Restoration`, and `UI` action maps established, with keyboard/mouse and gamepad control schemes |
| Navigation | AI Navigation `2.0.12` installed |
| UI | UI Toolkit (Unity 6 built-in UIDocument/UXML/USS); main menu, pause menu, and a minimal settings panel exist; full M11 not yet implemented. `com.unity.ugui` is kept only for the EventSystem mixed-input bridge |
| Timeline | Timeline `1.8.12` installed; no in-game flashback content yet |
| Tests | Unity Test Framework `1.6.0`; Edit Mode 33/33 and Play Mode 3/3 passing |
| Official scenes | `Bootstrap`, `MainMenu`, `Era_Ancient`, `Era_Modern`, `Era_Future` registered; Modern/Future currently locked |
| SampleScene | File kept, but removed from the official Build Settings |
| Global state | `GamePhase`, read-only snapshots, explicit transition guards, pause/resume, and a single `GameContext` implemented |
| EventBus | Non-static strongly-typed bus with snapshot publishing and `IDisposable` subscriptions implemented; the legacy string-keyed placeholder bus removed |
| Player control | First-person `CharacterController` movement, grounded jump, gravity, collision, camera, greybox hands, and Scepter feedback implemented |
| Environmental interaction | Not implemented |
| Scepter & restoration | Not implemented |
| NPC & dialogue | Not implemented |
| UI & accessibility | UI migrated to UI Toolkit (UIDocument/UXML/USS; dev guidelines in [UiToolkit](Documentation/02_TechnicalDesign/UiToolkit.md)); UI scaling, Y-axis invert, sensitivity, and Reduced Motion completed for the current slice; full M11 not implemented |
| Audio control | Not implemented |
| Saves & progress | Only settings persist via project-specific PlayerPrefs keys; M13 game-progress saves not implemented |
| Three-era content | Ancient is an explorable greybox; Modern/Future are locked placeholder scenes, M07 content data not implemented |
| Windows build | Development build succeeds; an 8-second hidden launch run shows no runtime errors |
| WebGL build | Development build succeeds; `index.html` and WASM return 200 over a local HTTP server |

```text
Project Stage: Playable foundation / Vertical slice
Current Playable State: Main Menu → Ancient greybox is playable
Current Milestone: M01–M04 complete; M05 first-person slice complete
```

## 10. Module Completion Status

All modules use the unified statuses `Planned → In Progress → Review → Complete`.

| Module | Status | Completed | Next Step |
|---|---|---|---|
| M01: Bootstrap & Global State | Complete | State snapshots, transition guards, pause/resume, single Bootstrap/Context, era-scene dev fallback | Later modules connect only through public state interfaces |
| M02: Event Bus | Complete | Strongly-typed pub/sub, disposable handles, subscription snapshots, exception logging, and test cleanup | Later modules add read-only event messages |
| M03: Scene Flow | Complete | Five-scene routing, async single-transaction loading, gates, spawn point recovery, failure events, and a minimal menu entry | Replace gates and the Continue adapter after M09/M13 complete |
| M04: Input & Settings | Complete | Three action maps, keyboard/mouse and gamepad support, device events, safe end-of-frame map switching, sensitivity, invert, Reduced Motion, settings persistence | M11/M12 hook up the full UI and volume consumers |
| M05: Player Control | Complete | Movement required for the current slice, grounded jump, collision, camera, locking, and greybox presentation completed | Later add only interaction mount points; no combat movement expansion |
| M06: Environmental Interaction | Planned | None | Design unified focus and interaction rules |
| M07: Content Data | Planned | None | Design era, artifact, and fact data |
| M08: Scepter & Artifact Restoration | Planned | None | Design detection, fragment, and assembly sessions |
| M09: Progress & Quiz | Planned | None | Design gate, quiz, and reward states |
| M10: NPC & Dialogue | Planned | None | Design conditional linear dialogue and navigation states |
| M11: UI & Accessibility | In Progress | UI migrated to UI Toolkit (UIDocument/UXML/USS): main menu, pause/settings panels, HUD, and the UI scaling and Reduced Motion interfaces are available | Complete all panels, focus navigation, subtitles, and 960×600 acceptance |
| M12: Audio & Flashback | Planned | None | Design audio events and flashback presentation |
| M13: Saves | Planned | None | Design save boundaries for progress and settings |
| M14: Verification & Testing | In Progress | Automated tests for M01–M05 and Windows/WebGL build and startup checks established | Add matching verification for M06–M13 later; do not label current coverage as complete M14 |

Per-module status records follow this format:

```text
Module:
Phase:
Status:
Completed:
Known Issues:
Next Step:
```

## 11. Planned Implementation Phases

1. **Done — playable foundation**: M01, M02, M03, M04, and M05 within the current scope.
2. **Next — exploration interaction**: M06 and M07, establishing the unified interaction contract and artifact data.
3. **Core loop**: M08 and M09, completing the Scepter, fragment restoration, quizzes, and gates.
4. **Presentation & guidance**: M10, M11, M12, completing NPCs, full accessible UI, audio, and flashbacks.
5. **Progress & content**: M13 official saves, and configuring the nine artifacts across the three eras.
6. **Verification & release**: full M14 module coverage, performance acceptance, and itch.io release checks.

## 12. Documentation Index

- [Core Gameplay Design](Documentation/01_GameDesign/CoreGameplay.md)
- [Technical Architecture Overview](Documentation/02_TechnicalDesign/ArchitectureOverview.md)
- [Technical Module Index](Documentation/02_TechnicalDesign/Modules/README.md)
- [Content Bible](Documentation/03_Content/ContentBible.md)
- [Test Strategy](Documentation/05_QA/TestStrategy.md)

Any `TBD` appearing in the documentation means reliable content has not yet been secured or a product decision is still open; it must not later be replaced with unreviewed facts.

## 13. Build and Development Requirements

- Unity Editor: `6000.4.10f1`.
- Target builds: Windows 64-bit and WebGL.
- Primary package versions follow `Packages/manifest.json`.
- Scenes, assets, code, and tests live under `Assets/UnknownTechnology`; UI Toolkit assets (UXML/USS/themes/PanelSettings) live under `Assets/UnknownTechnology/UI`.
- Implementation must respect the one-way dependencies Core → Gameplay → Presentation → Validation, plus Tests.
- Development builds output to the git-ignored `Builds/Development/Windows` and `Builds/Development/WebGL` folders.
- Automated test results output to `Temp/TestResults` and never enter the official asset directories.
- The [Release Checklist](Documentation/05_QA/ReleaseChecklist.md) must pass before release.
