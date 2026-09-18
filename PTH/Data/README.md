# Runtime Data

Everything the game persists lives in this folder. It is generated at runtime
and ignored by git; safe to delete at any time.

| Path | Contents |
|---|---|
| `settings.json` | Player settings (pretty JSON, safe to hand-edit; invalid values are clamped on load) |
| `Saves/` | Save games (planned) |
| `Diagnostics/` | Performance captures and profiling output (planned) |

In the editor the folder sits next to `Assets/`; in builds it is created next
to the executable.
