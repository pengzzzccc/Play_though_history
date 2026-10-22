# Runtime Data

Settings and diagnostics live in this folder. It is generated at runtime and
ignored by git; safe to delete at any time.

| Path | Contents |
|---|---|
| `settings.json` | Player settings (pretty JSON, safe to hand-edit; invalid values are clamped on load) |
| `Diagnostics/` | Performance captures and profiling output (planned) |

Save games are kept beside this folder in `Saves/slot_{1,2,3}.json` (also
generated at runtime and ignored by git). Deleting a file there empties that
slot; an unreadable save is renamed to `slot_{n}.corrupt.json`.

In the editor both folders sit next to `Assets/`; in builds they are created
next to the executable. WebGL builds store them in the browser instead
(`Application.persistentDataPath`).
