# BabelPane

A floating, resizable overlay you point at any text on your screen that shows an
instant translation in place, using a local LLM only — nothing leaves your
machine, and there's no copy-paste.

## Who it's for

Anyone facing a language barrier who can run a small LLM locally and wants
private, in-place translation of whatever is on their screen — contracts,
insurance paperwork, scanned documents, browser pages, messages — without
uploading the content to a cloud translation service.

## Screenshots

| Start state (pane open, empty) | Success state (translation rendered) |
|---|---|
| ![Pane open and empty](assets/start-state.png) | ![Translated text rendered in the pane](assets/success-state.png) |

## Setup

Requires:
- Windows 11
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Ollama](https://ollama.com), running locally, with a vision-capable model pulled
  (e.g. `ollama pull gemma3:4b` or the model of your choice)

```powershell
git clone <this repo>
cd babelpane
dotnet restore BabelPane.sln
```

Settings opens automatically every time you launch BabelPane (reopen it
anytime from the tray icon too). Its right-hand column explains how to use
the app and lists everything you need (Ollama, a vision-capable model, .NET
10) with links to install each one. On startup only, it closes itself after
15 seconds (shown by a countdown next to the buttons) so it doesn't linger
once you already know the ropes — Settings opened manually from the tray
stays open until you close it. On the left, set:
- **Ollama endpoint URL** — defaults to `http://localhost:11434`
- **Model name** — the vision-capable model you pulled in Ollama
- **Target language**, **translation mode** (Literal, the default, or Summary),
  **request timeout**, and the **global hotkey** (default `Win+Alt+X`)

## Run

```powershell
dotnet run --project src/BabelPane/BabelPane.csproj
```

BabelPane starts in the system tray — no window opens until you use it.

## Build a portable .exe

To build a single-file `BabelPane.exe` you can copy to any Windows PC and run
with zero prerequisites — no .NET runtime install, no companion DLLs:

```powershell
dotnet publish src/BabelPane/BabelPane.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

The result is `src/BabelPane/bin/Release/net10.0-windows/win-x64/publish/BabelPane.exe`
(~170MB, since it bundles the whole .NET runtime). Only that one file needs to
go anywhere else — verified by copying just the `.exe` into an empty folder on
its own and launching it from there. Building it still requires the .NET 10
SDK on the machine doing the build (same as `dotnet run` above); the resulting
`.exe` does not, on whatever machine it ends up running on.

Re-run the same command any time the code changes to refresh the package —
nothing is cached or versioned, it just overwrites the `publish/` folder.

## Verify the core journey

1. Press the global hotkey (`Win+Alt+X` by default). The pane opens, empty, at
   its last saved size and position.
2. Drag the pane to reposition it — including onto a different monitor, if
   you have more than one — and resize it from any edge or corner, just like
   a normal window, over some foreign-language text on screen.
3. Press the hotkey again (or click anywhere in the pane without dragging
   it). The region under the pane is captured in memory and sent to your
   local Ollama server for combined OCR + translation. A busy indicator
   shows while waiting. Pressing the hotkey again while a request is in
   flight cancels it and closes the pane immediately.
4. The translated text appears in place, autofit to the available space.
5. Press the hotkey a third time to close the pane, or click the `[Copy]`
   button (bottom-right, visible once the translation renders) to copy the
   text to the clipboard, see a brief "Copied" confirmation, and close in one
   step. Either way, its size and position are saved; its content is not.
6. **Error case:** if Ollama is unreachable or times out, the pane shows a
   clear inline message and stays open at the same position/size so you can
   retry by clicking the pane or pressing the hotkey — no need to reposition.

Press `Escape` at any time to close the pane immediately.

## What's seeded or mocked

Nothing in the running app is seeded or mocked — every translation is a live
call to your local Ollama server. The automated test project
(`tests/BabelPane.Tests`) uses synthetic, hardcoded strings only (sample JSON
bodies, a placeholder endpoint URL) — no real settings file or network call.

## Automated checks

```powershell
dotnet build BabelPane.sln   # build succeeded, 0 warnings, 0 errors
dotnet test BabelPane.sln    # 14/14 tests passed
dotnet format BabelPane.sln  # ran clean; fixed one spacing nit in AssemblyInfo.cs
```

All three commands were run and verified passing in this session. Test coverage is
limited to logic that doesn't require the GUI or a live network call: `AppConfig`
JSON round-tripping, parsing/error-handling of Ollama's response JSON, the
pure geometry-recovery logic used for multi-monitor fallback, and the two
translation-mode prompts' wording. Hotkey registration, screen capture, and
drag/resize are GUI/OS-interop code, exercised manually (see above) rather
than by automated tests. Actual translation *quality* (literal vs. summary,
fluency) can only be judged by a human against the live model — see the
Known Limitations note on that below.

## Known limitations

- Multi-monitor is supported (drag the pane to any connected monitor), but
  only verified with monitors at the same DPI scaling — behavior on a mixed-DPI
  setup (e.g. a laptop panel at 150% next to an external display at 100%) is
  unverified.
- Source language is auto-detected by the model; only the target language is
  configurable.
- Translated text is autofit and wrapped, not matched to the original layout
  or line breaks.
- No translation history, logging, or side-by-side original/translated view.
- A ~120ms synchronous pause during screen capture briefly blocks the UI
  thread on every trigger; not perceptible in normal use (see `DECISIONS.md`).
- Translation quality depends entirely on the local model you configure —
  BabelPane does no post-processing or validation of the model's output.
  **Literal mode**'s prompt is tuned against one real vision model's specific
  failure modes (stilted word-for-word phrasing, bracketed hedging); a
  different model may need different wording to behave the same way.
- The captured region is upscaled 2x before being sent for OCR — a small pane
  captured at its literal on-screen size gave one real model too few pixels
  to read dense text reliably, producing incomplete/garbled output that
  Summary mode's forgiving paraphrasing had been masking (see `DECISIONS.md`).
  A very small pane over dense text may still benefit from being resized
  larger before triggering.
- `TimeoutSeconds` defaults to 120s. Literal mode's longer prompt roughly
  doubles response time versus the old default prompt on one measured setup
  (~7s → ~14s for comparable text); a slow model/server may still need a
  higher value than the default.
