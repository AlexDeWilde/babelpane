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

On first run, open Settings from the tray icon and set:
- **Ollama endpoint URL** — defaults to `http://localhost:11434`
- **Model name** — the vision-capable model you pulled in Ollama
- **Target language**, **request timeout**, and the **global hotkey** (default `Win+Alt+X`)

## Run

```powershell
dotnet run --project src/BabelPane/BabelPane.csproj
```

BabelPane starts in the system tray — no window opens until you use it.

## Verify the core journey

1. Press the global hotkey (`Win+Alt+X` by default). The pane opens, empty, at
   its last saved size and position.
2. Drag the pane's border to reposition it, and its bottom-right corner to
   resize it, over some foreign-language text on screen.
3. Press the hotkey again (or click `[go]` in the pane's corner). The region
   under the pane is captured in memory and sent to your local Ollama server
   for combined OCR + translation. A busy indicator shows while waiting.
   Pressing the hotkey again while a request is in flight cancels it and
   closes the pane immediately.
4. The translated text appears in place, autofit to the available space.
5. Press the hotkey a third time to close the pane. Its size and position are
   saved; its content is not.
6. **Error case:** if Ollama is unreachable or times out, the pane shows a
   clear inline message and stays open at the same position/size so you can
   retry with `[go]` or the hotkey — no need to reposition.

Press `Escape` at any time to close the pane immediately.

## What's seeded or mocked

Nothing in the running app is seeded or mocked — every translation is a live
call to your local Ollama server. The automated test project
(`tests/BabelPane.Tests`) uses synthetic, hardcoded strings only (sample JSON
bodies, a placeholder endpoint URL) — no real settings file or network call.

## Automated checks

```powershell
dotnet build BabelPane.sln   # build succeeded, 0 warnings, 0 errors
dotnet test BabelPane.sln    # 7/7 tests passed
```

Both commands were run and verified passing in this session. Test coverage is
limited to logic that doesn't require the GUI or a live network call: `AppConfig`
JSON round-tripping, and parsing/error-handling of Ollama's response JSON.
Hotkey registration, screen capture, and drag/resize are GUI/OS-interop code,
exercised manually (see above) rather than by automated tests.

## Known limitations

- Single (primary) monitor only — no multi-monitor support.
- Source language is auto-detected by the model; only the target language is
  configurable.
- Translated text is autofit and wrapped, not matched to the original layout
  or line breaks.
- No translation history, logging, or side-by-side original/translated view.
- Dev-process only — no packaged, standalone `.exe`.
- A ~120ms synchronous pause during screen capture briefly blocks the UI
  thread on every trigger; not perceptible in normal use (see `DECISIONS.md`).
- Translation quality depends entirely on the local model you configure —
  BabelPane does no post-processing or validation of the model's output.
