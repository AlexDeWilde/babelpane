# Project Instructions for Claude Code

Keep this file short and current.

## Product
- Target user: anyone facing a language barrier who runs a local LLM and wants private, in-place translation of on-screen text (see `PRODUCT_BRIEF.md` for full detail).
- User problem: no fast, private way to translate arbitrary on-screen content (contracts, scans, browser pages, messages) without uploading it to a cloud service.
- Intended outcome: point a floating overlay at foreign-language text on screen and get a translation rendered in place, entirely offline.
- Core journey: hotkey opens pane → drag/resize over text → hotkey/`[go]` triggers capture+translate → translated text renders in place → hotkey closes.
- Current milestone: M4 — automated checks + walkthrough (M1-M3 done; settings window + pane-geometry persistence, both in the brief's Must Demonstrate list but not in the original 4 milestones, are also done and verified).

## Scope
Required now (M1):
- Tray icon with "open widget pane" / "open settings" menu items.
- Borderless, transparent-center pane: draggable, resizable, closeable with the mouse.
- Global hotkey (default `Win+Alt+X`) cycling open → (trigger, stubbed) → close.

Non-goals (whole project, see `PRODUCT_BRIEF.md` for the full list):
- Packaged `.exe` — dev process only.
- Configurable source language, multi-monitor, layout-matched rendering, history/logging, non-Windows support.


Do not add features outside the current milestone without explaining why they are necessary.

## Technical Context
- Stack: C# / .NET 10 (net10.0-windows), WPF + WinForms interop (for the tray icon). Chosen over Python/Electron because native APIs cover every "must demonstrate" item (transparent click-through window, tray icon, global hotkey, screen capture) with the fewest third-party dependencies — see `DECISIONS.md`. Targets net10.0 (not net9.0) because a .NET 10 desktop runtime was already present on this machine and mixing SDK/runtime majors caused a real `TypeLoadException` in WinForms' tray-icon code (`System.Private.Windows.Core` resolving from the wrong major version) — see `DECISIONS.md`.
- Architecture: single WPF app project (`src/BabelPane`). `System.Drawing.Common` (bundled with the net10.0 desktop runtime, no separate package needed) provides `Graphics.CopyFromScreen` for region capture. `HttpClient` calls a local Ollama server for OCR+translate. `AppConfig.Current` holds settings + pane geometry, persisted to `%AppData%\BabelPane\settings.json`; edited via `SettingsWindow`.
- Important paths: `src/BabelPane/` (app source), `src/BabelPane/BabelPane.csproj` (project + deps), `BabelPane.sln` (solution).
- Data: synthetic / seeded only; no real personal documents in the repo.
- Environment: Windows 11, PowerShell. No `&&` chaining. Paths use `\`. Building/running from Bash needs `dotnet` on PATH (`export PATH="/c/Program Files/dotnet:$PATH"`) since it was just installed this session.

## Commands
- Install: `dotnet restore BabelPane.sln` (requires .NET 10 SDK; also installed .NET 9 SDK this session for the first build attempt — kept, unused by this project)
- Run: `dotnet run --project src/BabelPane/BabelPane.csproj`
- Test: *(none yet — no test project created)*
- Lint or format: `dotnet format BabelPane.sln`
- Build: `dotnet build BabelPane.sln`

Never report a command as passing unless it actually ran successfully. `dotnet run` launches a GUI window — verify it visually, don't assume from exit code alone (a backgrounded GUI process won't exit until closed).

## Product and Design Rules
- Optimize the primary journey for the target user.
- Keep the interface responsive and accessible (keyboard, labels, contrast, non-color cues) where reasonable for a mouse-first floating widget.
- Include useful loading, empty, success, and error states.
- Use realistic, non-sensitive demonstration content.
- Label mocked, generated, or estimated output clearly.
- Prefer a small coherent product over broad unfinished functionality.
- Never write the captured screen region to disk, temp files included.

## Working Agreement
- Inspect the repo and `PRODUCT_BRIEF.md` before proposing changes.
- For a substantial milestone: present the plan, assumptions, expected file changes, and verification approach before editing.
- Stay inside the agreed milestone.
- Explain new dependencies and meaningful architecture decisions.
- Run the relevant checks afterwards and report what is unverified.
- Never commit, push, deploy, delete data, or do anything irreversible without explicit permission.
- Never expose secrets or put sensitive data in files, logs, or screenshots.

## Definition of Done
Acceptance criteria met, relevant checks pass, journey exercised by a human, docs match actual behavior, remaining limitations stated.
