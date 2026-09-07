# Unknown Technology

## 1. Project Overview

**Unknown Technology** is a first-person educational museum sandbox puzzle game. The player takes the role of a museum employee in a technology and innovation exhibition, investigating nine missing or damaged artifacts and using an upgradeable staff to uncover clues, recover fragments, and complete restorations. Each restoration unlocks historical facts, short flashbacks, and timeline entries that ultimately connect the Ancient, Modern, and Future eras.

The repository already contains a playable first-person vertical slice. Players can enter through the main menu and load the Ancient Museum greybox, using keyboard and mouse or a controller to move, look around, pause, and adjust basic settings. M01–M04 are complete within the current scope, and M05 has completed the supporting first-person controller. Artifact interaction, restoration mechanics, historical content, and the complete gameplay loop remain for later modules.

## 2. High Concept

Players investigate missing artifacts inside a cozy yet mysterious technology museum, reconstructing the history of innovation through observation, logical reasoning, and spatial assembly. The core experience is not combat, but discovery, understanding, and restoration—the player serves as both investigator and conservator, rebuilding history piece by piece.

## 3. Core Experience

The game is built around four experience pillars:

- **Investigation & Discovery:** Explore exhibits, read environmental clues, and find artifact fragments and historical facts.
- **Logic & Restoration:** Move, rotate, match, and snap fragments together to rebuild artifacts.
- **Historical Connections:** Learn what problem each artifact solved and how it influenced later technologies.
- **Museum Recovery:** Empty display cases, lighting, sound, and timeline displays gradually return as progress is made, providing clear achievement feedback.

## 4. Target Audience

- Players interested in history, technology, or museum-themed experiences.
- Players with limited gaming experience who benefit from clear guidance and low-complexity controls.
- No strict age target; historical text, quiz difficulty, and content ratings will be reviewed by the content team.

## 5. Target Platforms

- Primary platform: Windows 64-bit.
- Release platforms: WebGL-supported websites or itch.io.
- Input: Keyboard and mouse plus standard gamepad are included in first-release acceptance.
- Minimum UI baseline: 960×600.
- Touch, XR, and local multiplayer are not first-release targets.

## 6. Core Features

- First-person museum exploration and environmental interaction.
- A single upgradeable restoration staff used throughout the game.
- Three museum eras: Ancient, Modern, and Future.
- Nine unique artifacts, with three per era.
- Fragment searching, spatial rotation, matching, and snap-based restoration.
- Historical fact notes, artifact flashbacks, and technology timelines.
- One three-question quiz per era; passing unlocks that era's staff upgrade.
- Conditional linear NPC dialogue with progressive hints.
- Autosave, Continue, keyboard and mouse plus controller prompts, and basic accessibility settings.

## 7. Scope and Constraints

The target playtime is approximately 20 minutes:

- Prologue: ~2 minutes
- Each era: ~5 minutes
- Ending: ~3 minutes

The first playable milestone is a **5–6 minute Ancient Museum vertical slice**.

The first release explicitly excludes:

- Combat, health, death, timers, or resource penalties.
- Crouching, sprinting, climbing, or advanced movement abilities; only grounded jumping remains.
- Branching NPC dialogue trees, side quests, or behavior trees.
- Fully playable historical flashback levels.
- Touch support, XR, networking, cloud saves, or multiplayer.
- Separate control systems for each staff.
- Historical facts that have not been sourced and reviewed.

## 8. Core Gameplay Loop

### Single Artifact Loop

```text
Discover an empty display case
→ Investigate nearby clues
→ Learn historical facts
→ Find 2–4 fragments
→ Go to the restoration station
→ Move, rotate, and assemble pieces
→ Restore the artifact
→ Watch an optional flashback
→ Record it in the history notes and timeline
```

### Single Era Loop

```text
Enter the museum wing
→ NPC introduces the era
→ Restore three artifacts
→ Review the era timeline
→ Complete the three-question quiz (3/3)
→ Unlock the era staff
→ Open the next era
```

### Full Game Flow

```text
Main Menu → Prologue → Ancient → Modern → Future → Complete Timeline → Ending
```

Detailed rules are documented in `Documentation/01_GameDesign/CoreGameplay.md` and `Documentation/01_GameDesign/PlayerFlow.md`.

## 9. Current Repository Status

The following status reflects the implementation verified on **2026-08-31**.

| Item | Current Status |
|------|----------------|
| Unity Version | `6000.4.10f1` |
| Rendering | URP `17.4.0` installed and configured |
| Input | Input System `1.19.0`; official `Gameplay`, `Restoration`, and `UI` Action Maps with keyboard/mouse and gamepad schemes |
| Navigation | AI Navigation `2.0.12` installed |
| UI | uGUI `2.0.0` and TMP; Main Menu, Pause Menu, and minimal Settings panel implemented; full M11 pending |
| Timeline | Timeline `1.8.12` installed; no gameplay flashbacks yet |
| Testing | Unity Test Framework `1.6.0`; Edit Mode 33/33 and Play Mode 3/3 passing |
| Official Scenes | `Bootstrap`, `MainMenu`, `Era_Ancient`, `Era_Modern`, and `Era_Future` registered; Modern and Future currently locked |
| SampleScene | File retained but removed from Build Settings |
| Global State | `GamePhase`, read-only snapshots, explicit transition guards, pause recovery, and singleton `GameContext` implemented |
| EventBus | Non-static strongly typed bus with snapshot publishing and `IDisposable` subscriptions implemented; legacy string-based bus removed |
| Player Controller | First-person `CharacterController` movement, grounded jump, gravity, collisions, camera, greybox hands, and staff feedback implemented |
| Environmental Interaction | Not implemented |
| Staff & Restoration | Not implemented |
| NPC & Dialogue | Not implemented |
| UI & Accessibility | Current slice includes UI scaling, Y-axis inversion, sensitivity, and Reduced Motion; full M11 pending |
| Audio Controls | Not implemented |
| Save & Progress | Project-specific PlayerPrefs keys used for settings persistence only; M13 save system not implemented |
| Three-Era Content | Ancient is an explorable greybox; Modern and Future are locked placeholder scenes; M07 content data pending |
| Windows Build | Development build successful; hidden startup completed with no runtime errors after 8 seconds |
| WebGL Build | Development build successful; `index.html` and WASM verified with local HTTP 200 responses |

```text
Project Stage: Playable Foundation / Vertical Slice

Current Playable State: Main Menu → Ancient Greybox

Current Milestone: M01–M04 Complete; M05 First-Person Slice Complete
```

## 10. Module Completion Status

Status progression follows:

`Planned → In Progress → Review → Complete`

| Module | Status | Current Completion | Next Step |
|--------|--------|-------------------|-----------|
| M01: Bootstrap & Global State | Complete | State snapshots, transition guards, pause recovery, singleton Bootstrap/Context, and era scene fallback | Future modules integrate through public state interfaces |
| M02: Event Bus | Complete | Strongly typed publish/subscribe, disposable handles, subscription snapshots, exception logging, and test cleanup | Add new read-only event messages |
| M03: Scene Flow | Complete | Five-scene routing, asynchronous single transactions, gating, spawn restoration, failure events, and minimal menu entry | Replace gates and Continue adapter after M09/M13 |
| M04: Input & Settings | Complete | Three Action Maps, keyboard/mouse and gamepad support, device events, safe end-of-frame map switching, sensitivity, inversion, Reduced Motion, and settings persistence | Connect full UI and audio consumers in M11/M12 |
| M05: Player Controller | Complete | Required movement, grounded jump, collisions, camera, lock state, and greybox presentation | Only add interaction hooks later |
| M06: Environmental Interaction | Planned | None | Design unified focus and interaction rules |
| M07: Content Data | Planned | None | Design era, artifact, and historical fact data |
| M08: Staff & Artifact Restoration | Planned | None | Design scanning, fragments, and assembly sessions |
| M09: Progress & Quiz | Planned | None | Design gating, quizzes, and reward states |
| M10: NPC & Dialogue | Planned | None | Design conditional linear dialogue and navigation states |
| M11: UI & Accessibility | In Progress | Main Menu, Pause/Settings panels, UI scaling, and Reduced Motion interfaces exist | Complete all panels, focus navigation, subtitles, and 960×600 validation |
| M12: Audio & Flashbacks | Planned | None | Design audio events and flashback presentation |
| M13: Save System | Planned | None | Define save boundaries for progress and settings |
| M14: Validation & Testing | In Progress | M01–M05 automated tests, Windows/WebGL builds, and startup checks established | Extend validation to M06–M13 without overstating current coverage |

Individual module status format:

```text
Module:

Phase:

Status:

Completed:

Known Issues:

Next Step:
```

## 11. Planned Implementation Phases

1. **Completed – Playable Foundation:** M01, M02, M03, M04, and the current scope of M05.
2. **Next Phase – Exploration & Interaction:** M06 and M07 establish the unified interaction framework and artifact data.
3. **Core Gameplay Loop:** M08 and M09 complete the staff, fragment restoration, quizzes, and progression gates.
4. **Presentation & Guidance:** M10, M11, and M12 add NPCs, complete accessibility UI, audio, and flashbacks.
5. **Progress & Content:** M13 implements the full save system and configures all three eras with nine artifacts.
6. **Validation & Release:** Complete M14 coverage, performance validation, and itch.io release preparation.

## 12. Documentation Index

- `Documentation/01_GameDesign/CoreGameplay.md`
- `Documentation/02_TechnicalDesign/ArchitectureOverview.md`
- `Documentation/02_TechnicalDesign/Modules/README.md`
- `Documentation/03_Content/ContentBible.md`
- `Documentation/05_QA/TestStrategy.md`

Any `TBD` entries indicate either missing reliable historical sources or unfinished product decisions. They must not be replaced with unsourced information.

## 13. Build and Development Requirements

- **Unity Editor:** `6000.4.10f1`
- **Target Builds:** Windows 64-bit and WebGL
- Package versions are defined in `Packages/manifest.json`
- Scenes, assets, code, and tests are located under `Assets/UnknownTechnology`; TMP base resources remain in `Assets/TextMesh Pro`
- Technical implementation follows the one-way dependency flow: **Core → Gameplay → Presentation → Validation → Tests**
- Development builds output to Git-ignored folders:
  - `Builds/Development/Windows`
  - `Builds/Development/WebGL`
- Automated test results output to `Temp/TestResults` and are never included in production assets
- All releases must pass the `Documentation/05_QA/ReleaseChecklist.md` checklist before publication.