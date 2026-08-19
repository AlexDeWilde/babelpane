# Project Instructions for Claude Code

Keep this file short and current.

## Product
- Target user: anyone facing a language barrier who runs a local LLM and wants private, in-place translation of on-screen text (see `PRODUCT_BRIEF.md` for full detail).
- User problem: no fast, private way to translate arbitrary on-screen content (contracts, scans, browser pages, messages) without uploading it to a cloud service.
- Intended outcome: point a floating overlay at foreign-language text on screen and get a translation rendered in place, entirely offline.
- Core journey: hotkey opens pane → drag/resize (any edge/corner) over text → hotkey or a click on the pane triggers capture+translate → translated text renders in place → hotkey closes, or `[Copy]` copies it to the clipboard and closes.
- Current milestone: all 4 original milestones (M1-M4) done and verified, plus settings window + pane-geometry persistence from the brief's Must Demonstrate list, plus post-deliverable multi-monitor support, click-to-trigger/native resize/copy-to-clipboard, a literal/summary translation mode toggle, a post-demo bug-hunt fixing a silent-timeout bug, a too-tight default timeout, and incomplete OCR on small captures, a two-column Settings window (fields + always-visible onboarding help) that opens automatically on launch, and a 15s self-closing countdown on that first-launch auto-open only (manual reopens from the tray stay open indefinitely), and single-instance enforcement (a second launch shows an "already running" message and exits instead of running alongside the first).

## Scope
Required now (M1):
- Tray icon with "open widget pane" / "open settings" menu items.
- Borderless, transparent-center pane: draggable, resizable, closeable with the mouse.
- Global hotkey (default `Win+Alt+X`) cycling open → (trigger, stubbed) → close.

The original M1-only non-goals list (packaged `.exe`, cloud LLM support, etc.) applied while development time was uncertain. That milestone is done and verified, so it no longer constrains new work: the project is now in an open build phase, adding features beyond the original brief on request. Backlog ideas (portable exe, cloud LLM API selection, etc.) are tracked in `addons.md` rather than treated as out of scope.

Still explain new dependencies and meaningful architecture decisions (see Working Agreement) — "build build build" doesn't mean skip that.

## Technical Context
- Stack: C# / .NET 10 (net10.0-windows), WPF + WinForms interop (for the tray icon). Chosen over Python/Electron because native APIs cover every "must demonstrate" item (transparent click-through window, tray icon, global hotkey, screen capture) with the fewest third-party dependencies — see `DECISIONS.md`. Targets net10.0 (not net9.0) because a .NET 10 desktop runtime was already present on this machine and mixing SDK/runtime majors caused a real `TypeLoadException` in WinForms' tray-icon code (`System.Private.Windows.Core` resolving from the wrong major version) — see `DECISIONS.md`.
- Architecture: single WPF app project (`src/BabelPane`). `System.Drawing.Common` (bundled with the net10.0 desktop runtime, no separate package needed) provides `Graphics.CopyFromScreen` for region capture. `HttpClient` calls a local Ollama server for OCR+translate. `AppConfig.Current` holds settings + pane geometry, persisted to `%AppData%\BabelPane\settings.json`; edited via `SettingsWindow`. `ApplicationHighDpiMode=PerMonitorV2` (csproj) plus `System.Windows.Forms.Screen.AllScreens` and the pure `ScreenGeometry.EnsureVisible` helper give multi-monitor support: correct per-monitor DPI-aware capture, and a fallback to a centered position if saved geometry falls outside every currently connected monitor. `System.Windows.Shell.WindowChrome` (built into `PresentationFramework`, no new package) gives the borderless pane native edge/corner resize with no visible grip; the pane's `Chrome` border distinguishes a click (triggers translate) from a drag (repositions) by comparing `Left`/`Top` before and after the blocking `DragMove()` call. `CopyButton` (visible only once a translation renders) copies via `System.Windows.Clipboard`, closes the pane immediately, then shows `CopiedToast` — a small separate borderless window, positioned where the pane was — since an in-pane flash before closing proved too hard to read. `AppConfig.TranslationMode` (`Literal` default, or `Summary`) picks between two prompts built by `OllamaClient.BuildPrompt`: Literal asks for a complete, sentence-by-sentence, fluent (not word-for-word) translation with no bracketed hedging, and sends `temperature=0`; Summary keeps the original, more interpretive prompt and default model temperature. The exact literal-mode wording came from live iteration against the real model (see `DECISIONS.md`) — it's tuned to this specific failure mode, not a first guess. `ScreenCapture` upscales every capture 2x (`InterpolationMode.HighQualityBicubic`) before PNG-encoding it — a small pane captured at literal on-screen size gave one real model too few pixels for reliable OCR, confirmed by direct experiment against the live server (see `DECISIONS.md`). `RunTriggerAsync`'s cancellation handling distinguishes "our own `_cts` was cancelled" (a user hotkey-press mid-request, silent by design) from any other `OperationCanceledException` (e.g. `HttpClient`'s own timeout) — conflating the two once left the pane silently stuck empty. `AppConfig.TimeoutSeconds` defaults to 120s, not 60s — Literal mode's longer prompt measurably roughly doubles response time on this project's test hardware. `SettingsWindow` opens automatically from `App.OnStartup` (alongside tray-icon creation) and lays out in two columns via a plain `Grid` (no new dependency): existing settings fields in a left `ScrollViewer`, a vertical divider, and always-visible onboarding help (a "How to use" walkthrough and a "Make sure you have" install checklist with `Hyperlink`s opened via `Process.Start`) in a right `ScrollViewer` — chosen over gating the same content behind an error state so it's available the moment a first-time user sees the app. That startup open passes `autoCloseOnFirstLaunch: true` to `SettingsWindow`'s constructor, which starts a `DispatcherTimer` ticking once per second, showing a "Closing automatically in Ns..." countdown next to the buttons and calling `Close()` at zero; manual reopens from the tray menu use the default `false` and never start the timer. `App.OnStartup` also opens a named `Mutex` (`BabelPane-SingleInstance-<guid>`) before creating any window or the tray icon; if it's already held, the app shows a short "already running" message and calls `Shutdown()` immediately instead of starting a second instance. The mutex name is fixed and BabelPane-specific so a future BabelPaneSky fork can run alongside it under its own name.
- Important paths: `src/BabelPane/` (app source), `src/BabelPane/BabelPane.csproj` (project + deps), `BabelPane.sln` (solution).
- Data: synthetic / seeded only; no real personal documents in the repo.
- Environment: Windows 11, PowerShell. No `&&` chaining. Paths use `\`. Building/running from Bash needs `dotnet` on PATH (`export PATH="/c/Program Files/dotnet:$PATH"`) since it was just installed this session.

## Commands
- Install: `dotnet restore BabelPane.sln` (requires .NET 10 SDK; also installed .NET 9 SDK this session for the first build attempt — kept, unused by this project)
- Run: `dotnet run --project src/BabelPane/BabelPane.csproj`
- Test: `dotnet test BabelPane.sln`
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
