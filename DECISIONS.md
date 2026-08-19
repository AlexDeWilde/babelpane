# Product and Build Decisions

Record decisions that materially change the scope, experience, architecture, or
quality of the product. Do not paste full conversations.

## Decision Template

### <Decision title>

- **Context:** What needed to be decided?
- **Claude Code proposal:** What did Claude Code recommend?
- **Team decision:** What was accepted, changed, or rejected?
- **Reason:** Which product or technical evidence informed the decision?
- **Consequence:** What changed in the product or next milestone?

---

### Hotkey behavior: single global hotkey drives a 3-state cycle

- **Context:** Needed a keyboard path to open, trigger, and close the widget pane without conflicting with existing Windows/app shortcuts.
- **Claude Code proposal:** Offered three options — (a) one hotkey cycling open → trigger → close, (b) hotkey toggles open/close only with a separately repeatable trigger, (c) hotkey only opens/closes, trigger always via the `[go]` button.
- **Team decision:** Accepted the 3-state cycle (a).
- **Reason:** Matches the original intent ("same hotkey: opens - triggers - closes") and keeps the interaction model simple for a prototype.
- **Consequence:** To re-translate after moving the pane, the user must close and reopen it (or use the `[go]` button) — re-triggering without closing is not supported by the hotkey alone. Documented as a non-goal to revisit later (repeatable trigger) if it proves limiting.

### Hotkey default binding: `Win+Alt+X`, changeable in settings

- **Context:** First proposal was a fixed `Win+Alt+T` default, not configurable. User found `T` uncomfortable to reach one-handed as part of a three-key chord — too far from the lower-left corner of the keyboard where `Win`/`Alt` sit.
- **Claude Code proposal:** Recommended a bottom-row letter closer to `Ctrl`/`Win`/`Alt` for one-handed comfort (`Z`, `X`, `C`, `V`, in order of proximity), settling on `Win+Alt+X` as a comfortable pick with no existing three-key Windows binding to collide with.
- **Team decision:** Accepted `Win+Alt+X` as the new default, and reversed the earlier "fixed, not configurable" call — the hotkey is now a settings field like endpoint/model/language, superseding that part of the original hotkey-config decision.
- **Reason:** Comfort is user-specific and hard to get right on the first guess; making it configurable removes the need to guess correctly, while still shipping a sensible default.
- **Consequence:** Settings window scope now includes a hotkey field in addition to Ollama endpoint, model name, target language, and request timeout.

### Language scope: target-only, source auto-detected

- **Context:** Whether both source and target language should be user-configurable, or just the target.
- **Claude Code proposal:** Recommended target-only with the model auto-detecting the source language, to keep settings and the translation prompt simple for the prototype.
- **Team decision:** Accepted.
- **Reason:** Reduces settings UI and prompt complexity with no loss of core value — the user's documents are always in an unpredictable mix of foreign languages, translated to one known target.
- **Consequence:** Settings only exposes a target-language field, not a source-language field.

### Multi-monitor support: deferred, single (primary) monitor for the MVP

- **Context:** Multi-monitor was initially put in scope for the MVP (see below), but a critique pass flagged it as the single highest hidden-complexity item in the brief — correctly excluding the overlay's own window from its own screenshot, combined with per-monitor coordinates and differing DPI scaling, risked consuming the entire build budget on its own.
- **Claude Code proposal:** Cut multi-monitor from the MVP, single-primary-monitor only, documented as a known limitation.
- **Team decision:** Accepted — this reverses the earlier "multi-monitor must work in the MVP" decision.
- **Reason:** Protects the prototype time budget; the core journey (private in-place translation) is fully demonstrable on one monitor.
- **Consequence:** Screen-region capture only needs to handle the primary monitor for this build. Moved from Must Demonstrate to Explicit Non-Goals in `PRODUCT_BRIEF.md`; multi-monitor robustness remains a listed item to learn before any Capstone follow-up.

### Mid-request hotkey press cancels and closes

- **Context:** The 3-state hotkey cycle (open → trigger → close) didn't define what happens if the hotkey is pressed again while a translation request is still in flight — a real scenario given Ollama's cold-start slowness.
- **Claude Code proposal:** Flagged the gap during the critique pass without proposing a specific resolution.
- **Team decision:** A new keypress during an in-flight request cancels the request and closes the pane immediately.
- **Reason:** Keeps the interaction predictable — the user always knows what one more press of the same key does, even if a request is stuck.
- **Consequence:** The app must support cancelling an in-flight Ollama request, not just waiting it out. Added to Core Journey, Must Demonstrate, and Acceptance Evidence in `PRODUCT_BRIEF.md`.

### Rendering approach: autofit, not layout-matching

- **Context:** The original idea called for translated text to be "shrunk to fit... including carriage returns where appropriate to resemble the origin text," which bundles two hard, unverified capabilities (OCR+translate, and reproducing source layout) into one assumption.
- **Claude Code proposal:** Flagged the bundling as a risk during the critique pass.
- **Team decision:** Drop layout-matching. Autofit only — font size scaled down to the available space, fixed line spacing (1), plain wrapping. No attempt to reproduce the original text's line breaks.
- **Reason:** Simpler to build and verify; the value is a readable translation in place, not a pixel-faithful reproduction of the source layout.
- **Consequence:** Removed "approximated line breaks" language from `PRODUCT_BRIEF.md`; replaced with an autofit rendering requirement and a matching acceptance-evidence check.

### Stack: C# / .NET 9 WPF over Python or Electron

- **Context:** Every "must demonstrate" item is deeply OS-integrated — tray icon, borderless transparent-center click-through overlay that must not capture itself, global hotkey, in-memory screen-region capture, local HTTP call to Ollama, and persisted settings/geometry.
- **Claude Code proposal:** Compared C#/.NET WPF, Python (PyQt/PySide + pystray + mss + keyboard), and Electron. Recommended WPF because each requirement maps to a first-party Windows API (`AllowsTransparency`, `RegisterHotKey`, `NotifyIcon`, `Graphics.CopyFromScreen`) rather than a third-party binding, with only one added dependency (`System.Drawing.Common`).
- **Team decision:** Accepted.
- **Reason:** Fewest moving parts for the riskiest requirement (excluding the overlay's own window from its own capture); Python/Electron both needed more glue or had known friction on frameless translucent click-through windows.
- **Consequence:** .NET 9 SDK installed via `winget` (was missing, only a runtime stub was present). Scaffolded `src/BabelPane` (WPF app, net9.0-windows) and `BabelPane.sln`; added `System.Drawing.Common` NuGet package; build verified green. `CLAUDE.md` written with this stack and the real `dotnet` commands.

### Retarget net9.0-windows -> net10.0-windows during M1

- **Context:** M1's tray-icon code (`NotifyIcon`/`ContextMenuStrip`, WinForms interop inside a WPF app) crashed at startup with a `TypeLoadException`: `System.Private.Windows.Core, Version=10.0.0.0` failed to load while the rest of the app ran on the .NET 9 desktop runtime. Root cause: a .NET 10 desktop runtime was already installed on this machine (only its SDK was missing) alongside the newly installed .NET 9 SDK/runtime, and the WPF+WinForms interop assembly resolved from the wrong major version — a genuine side-by-side resolution bug, not fixable by `RollForward` policy tweaks (tried `LatestMinor`, still crashed).
- **Claude Code proposal:** Install the .NET 10 SDK (matching the runtime already present) and retarget the project to `net10.0-windows` instead of chasing the 9/10 split further.
- **Team decision:** Accepted.
- **Reason:** Eliminates the mixed-major-version resolution entirely rather than working around a framework bug; verified by running the app after retargeting — starts cleanly, no exception, process stays alive.
- **Consequence:** `BabelPane.csproj` now targets `net10.0-windows`. Removed the explicit `System.Drawing.Common` PackageReference — it ships inside the net10.0 desktop runtime and NuGet flagged it as redundant (`NU1510`). The unused .NET 9 SDK install was left in place (harmless, no cleanup needed). `CLAUDE.md` updated accordingly.

### M2 model override: `gemma4-e4b-110k:latest`, not the hf.co tag named in the brief

- **Context:** `PRODUCT_BRIEF.md`'s research-assumptions note names `hf.co/unsloth/gemma-4-E4B-it-qat-GGUF:UD-Q4_K_XL` as the model tried standalone. That exact tag is loaded on the Ollama server with a 131072-token context window.
- **Claude Code proposal:** Proposed using the brief's named tag directly for M2's `AppConfig.ModelName`.
- **Team decision:** Rejected — use `gemma4-e4b-110k:latest` instead (same underlying weights per the server's model list, retagged with a 110k context window).
- **Reason:** The original hf.co tag's context window is unnecessarily large for a single-region OCR+translate call and adds avoidable overhead.
- **Consequence:** `AppConfig.ModelName` is `gemma4-e4b-110k:latest`. Verified directly against the Ollama server with a synthetic French-text image before wiring it into the app — returned a correct English translation.

### Autofit implementation: computed font size, not Viewbox scaling

- **Context:** First M2 implementation autofit the translated text with a `Viewbox` (`Stretch="Uniform"`) wrapping a `TextBlock` with a fixed `MaxWidth="800"` for word-wrap. Manual test showed the text rendering far smaller than necessary, with empty space left in the pane.
- **Claude Code proposal:** Diagnosed the cause — `Viewbox` uniform-scales to the wrapped text's natural aspect ratio, which depends on the arbitrary wrap width and rarely matches the pane's actual aspect ratio, so the pane's shorter axis constrains the scale regardless of space available on the other axis. Replaced it with a binary search over `TextBlock.FontSize` (measuring wrapped height at the pane's actual width) so the text fills the container directly, and re-runs the fit on resize.
- **Team decision:** Accepted after visual confirmation — text now fills the available space at a comfortable reading size.
- **Reason:** Direct visual defect caught in manual milestone verification, not a hypothetical.
- **Consequence:** `MainWindow.xaml.cs` has an `AutoFitText()` helper; `OutputContainer.SizeChanged` re-triggers it so resizing the pane after a translation keeps the fit correct. The `[go]` button was also relocated to bottom-right (left of the resize grip) after the same test showed it overlapping the top-left of the translated text.

### M3: "no text detected" needs an explicit model sentinel, not an empty-string check

- **Context:** M3 added a retryable empty/failure state per the brief ("returns nothing usable" should stay open, not close). Manual testing over a blank area showed the model doesn't reliably return an empty string when there's no text in the image — it sometimes echoed fragments of the prompt instead, which slipped past an `IsNullOrWhiteSpace` check and got rendered as if it were a real translation.
- **Claude Code proposal:** Added an explicit instruction to the prompt — respond with a fixed sentinel (`NO_TEXT_FOUND`) when no legible text is present — and check the response for that sentinel (via `Contains`, not exact match, since instruction-following isn't perfectly literal) in addition to the whitespace check.
- **Team decision:** Accepted after retest confirmed the blank-area case now shows the correct "no text detected" message.
- **Reason:** A model's natural-language response can't be trusted to reliably signal "nothing to report" on its own; giving it an explicit, checkable token is more robust than inferring intent from prose.
- **Consequence:** `OllamaClient.NoTextSentinel` is the contract between the prompt and `MainWindow`'s result handling — if the prompt ever changes, the sentinel instruction and the check in `RunTriggerAsync` must change together.

### Added an unplanned milestone for settings window + geometry persistence

- **Context:** A gap check against `PRODUCT_BRIEF.md`'s Must Demonstrate list, done before starting M4, found two required items with no home in the original 4 named milestones (M1-M4 from the execution-plan template): the settings window (endpoint/model/target language/timeout/hotkey) and pane size/position persisting across app restarts. Both had been stubbed out (a "not implemented" message; no saved geometry at all) since M1/M2.
- **Claude Code proposal:** Build both now, as an extra milestone before M4, rather than either silently skipping the gap or quietly folding scope into M4 unannounced.
- **Team decision:** Accepted — build now.
- **Reason:** Both items are explicitly required by the brief's Must Demonstrate list and tested rows in its Acceptance Evidence table; M4 is meant to validate a feature-complete build against the brief, not to discover missing features.
- **Consequence:** `AppConfig` became a mutable, persisted settings class (`%AppData%\BabelPane\settings.json`) instead of hardcoded constants; new `SettingsWindow`; pane geometry is saved on app exit (both explicit tray-Exit and `OnExit`) and restored on the next launch. Verified: settings changes (target language, hotkey) took effect immediately, and geometry (position + size) was confirmed identical after a real exit-and-relaunch cycle.

### Phase 4 critical review finding: synchronous 120ms UI-thread block during capture — left as-is

- **Context:** Phase 4's critical review flagged that `ScreenCapture.CapturePaneRegion`'s `Thread.Sleep(120)` (a wait for the compositor to redraw the region after hiding the pane) runs synchronously on the UI thread on every trigger, briefly freezing the app — e.g. a hotkey-cancel landing in that exact window can't be processed.
- **Claude Code proposal:** Rated it low severity — the freeze is short, happens while the pane is already hidden for its own capture, and isn't perceptible in normal use. Suggested moving the capture off the UI thread only if it ever proves material.
- **Team decision:** Leave as-is; documented as a known limitation rather than fixed now.
- **Reason:** Immaterial in practice at this scale; not worth the added complexity of marshalling the capture off the UI thread for a demo-scale prototype.
- **Consequence:** No code change. Revisit if the delay ever grows (e.g. multi-monitor/DPI handling) or cancel-during-capture responsiveness becomes a real complaint.

### Multi-monitor support: reversed from deferred (M1) to built, post-deliverable

- **Context:** Multi-monitor was deferred at M1 (see above) as the highest hidden-complexity item — correctly excluding the pane's own window from its own capture, plus per-monitor coordinates and differing DPI scaling, risked the whole build budget. With all 4 milestones plus packaging shipped, the user picked this up from their post-deliverable backlog as the first item to build.
- **Claude Code proposal:** Declare `PerMonitorV2` DPI awareness explicitly via the MSBuild `ApplicationHighDpiMode` property (a hand-written `app.manifest` was tried first but triggered the WinForms SDK analyzer's `WFO0003`, since `UseWindowsForms` is also true for the tray icon); add a pure `ScreenGeometry.EnsureVisible` helper so saved pane geometry that falls outside every currently connected monitor (e.g. an external monitor was unplugged) falls back to a centered position instead of opening off-screen; leave `ScreenCapture.CapturePaneRegion`'s existing per-window-DPI math untouched, since it was already written in a per-monitor-aware style.
- **Team decision:** Accepted.
- **Reason:** The existing capture code's approach was already theoretically correct for multi-monitor; the missing pieces were an explicit DPI-awareness declaration and off-screen-geometry recovery, both small additions rather than a rewrite.
- **Consequence:** `BabelPane.csproj` sets `ApplicationHighDpiMode=PerMonitorV2`; new `src/BabelPane/ScreenGeometry.cs` + `tests/BabelPane.Tests/ScreenGeometryTests.cs`. Verified: `dotnet build`/`dotnet test` clean (11/11 passing); live, dragging the pane onto the real secondary monitor and triggering a translation there captured/rendered that monitor's content correctly, and geometry saved there reopened in the same spot after a relaunch; a saved position manually set outside every connected monitor (9999, 9999) fell back to a centered, reachable position on the primary monitor on relaunch, confirmed by the user. **Caveat:** both monitors on the dev machine run at 100% DPI scaling — mixed-DPI-per-monitor setups remain unverified and are documented as an open assumption in `PRODUCT_BRIEF.md`. Moved "Multi-monitor support" from `PRODUCT_BRIEF.md`'s Explicit Non-Goals and `CLAUDE.md`'s non-goals list; updated `README.md`/`SHOWCASE.md` known limitations accordingly.

### Remove `[go]` button (click-to-trigger) + native edge/corner resize

- **Context:** Next two items from the user's post-deliverable `addons.md` backlog. First request was resize knobs on all sides, less obtrusive than the existing single bottom-right grip; the user then explicitly corrected that to no knobs at all — resize should feel like any normal Windows window. Alongside that, the `[go]` button (a manual trigger alternative) was to be replaced by clicking anywhere in the pane.
- **Claude Code proposal:** For resize, use `System.Windows.Shell.WindowChrome` (built into `PresentationFramework`, no new dependency) instead of custom `Thumb` controls or manual `WM_NCHITTEST` hooking — WPF's first-party mechanism for native edge/corner resize on a chromeless (`WindowStyle="None"`) window, entirely invisible, with correct OS cursors. For click-to-trigger, reuse the existing `Chrome_MouseLeftButtonDown` handler: since `DragMove()` blocks until the mouse button is released, comparing `Left`/`Top` before and after tells us whether an actual drag happened versus a stationary click, without any new state machine.
- **Team decision:** Accepted.
- **Reason:** `WindowChrome` is the standard, idiomatic WPF answer for exactly this scenario and needed zero custom resize math (unlike the old single-corner `Thumb`, which only handled one edge). The click-vs-drag distinction reuses `RunTriggerAsync()`'s existing `State == Open` guard verbatim from the old `GoButton_Click`, so behavior elsewhere is unchanged.
- **Consequence:** `MainWindow.xaml`: `ResizeMode` changed to `CanResize`, `<WindowChrome.WindowChrome ResizeBorderThickness="6" .../>` added, `GoButton` and `ResizeGrip` removed entirely, `CloseButton` marked `WindowChrome.IsHitTestVisibleInChrome="True"` (it sits within the resize-border thickness). `MainWindow.xaml.cs`: `Chrome_MouseLeftButtonDown` rewritten to detect click-vs-drag; `GoButton_Click` and `ResizeGrip_DragDelta` deleted; the two retry messages that said "...or `[go]` to retry" now say "...or click the pane to retry." Verified live: a stationary click triggers a translation, a drag only repositions, all 4 edges and 4 corners resize with native cursors and no visible grip, and the close button (inside the resize-border zone) still closes rather than resizing. `PRODUCT_BRIEF.md`, `CLAUDE.md`, `README.md`, and `SHOWCASE.md` updated to describe the new interaction; the two `assets/*.png` screenshots still show the old `[go]` button and grip and are now stale, pending a retake.

### Copy-to-clipboard button

- **Context:** Next `addons.md` backlog item: a `[Copy]` button, visible once a translation renders, that copies the text, confirms with a brief "Copied" flash, and closes the pane.
- **Claude Code proposal:** First attempt showed the "Copied" flash *inside* the pane (a small overlay, same visual pattern as the existing busy indicator) for ~700ms before closing. The user tried it and found it hard to read — too brief, inside a small pane that was about to disappear anyway. Revised to close the pane immediately on copy, then show the confirmation in a separate small `CopiedToast` window (black background box, positioned centered on where the pane was), which auto-dismisses itself after ~1.2s.
- **Team decision:** Accepted the revised (post-close) version after live-testing the first attempt and rejecting it.
- **Reason:** Direct user feedback from testing the first attempt — a real usability defect caught by trying it, not a hypothetical.
- **Consequence:** New `CopyButton` in `MainWindow.xaml` (bottom-right, `Visibility="Collapsed"` by default, `WindowChrome.IsHitTestVisibleInChrome="True"` since it sits in the resize-border zone, made visible in `RunTriggerAsync`'s success branch, reset to collapsed in `ResetForOpen`). New `CopiedToast.xaml`/`.xaml.cs` — a borderless, `ShowActivated="False"` (so it doesn't steal focus), `Topmost` window sized to its content, shown via `new CopiedToast(centerX, centerY).Show()` from `CopyButton_Click` after `Hide()`/`State = Closed`. Verified live: copy, close, and toast timing/position/readability all confirmed; one apparent text mismatch between the displayed and copied translation on a first try turned out to be two separate (non-deterministic) Ollama translation calls being compared, not a defect — confirmed by no duplicate `BabelPane` process running and a clean match on retry. `PRODUCT_BRIEF.md`, `CLAUDE.md`, `README.md`, `SHOWCASE.md` updated.

### Literal vs. Summary translation mode + temperature control

- **Context:** The user observed the model producing an interpreted/summarized translation despite the existing prompt already asking it to "translate all the text you find" — a real instruction/behavior gap for the configured vision model. Requested: a settings toggle between a literal, sentence-by-sentence translation (no opinion) and today's more interpretive behavior, with Literal as the default, plus `temperature=0` for determinism.
- **Claude Code proposal:** New `TranslationMode` enum (`Literal`/`Summary`) on `AppConfig`, a settings radio-button pair, and `OllamaClient.BuildPrompt(mode, targetLanguage)` returning one of two prompts. Summary mode keeps today's exact existing wording unchanged; Literal mode gets new wording plus `options.temperature = 0` on the request (asked and confirmed with the user: temperature=0 applies only to Literal, not Summary).
- **Team decision:** Accepted the toggle design; the exact Literal-mode prompt wording went through two live-tested revisions before the user confirmed it worked (see below) — this was iterative discovery, not a one-shot decision.
  1. **First wording** ("translate ... literally and faithfully, sentence by sentence... provide a direct, literal translation") produced stilted, grammatically broken output that mirrored the source German's word order into English — "literal" was interpreted by the model as word-for-word, not "complete and faithful in content."
  2. **Revised wording** made this explicit: translate every sentence completely (nothing summarized/condensed/omitted) but in natural, fluent, grammatically correct target-language phrasing — "reorder words and phrases as needed," explicitly "do not preserve the source's sentence structure or word order." This fixed the fluency, but a new issue surfaced: the model bracketing hedged word choices in the output (e.g. `[held]`), which would land in the clipboard as-is.
  3. **Final fix:** added an explicit instruction against bracketed alternates/hedging/meta-commentary to *both* modes' prompts (an output-cleanliness concern, not specific to literal-vs-summary), confirmed clean on retest.
- **Reason:** Each revision was driven by a concrete, observed failure on the real model via live testing — not a hypothetical prompt-engineering guess.
- **Consequence:** `AppConfig.TranslationMode` (default `Literal`), new `SettingsWindow` radio-button row, `OllamaClient.TranslateImageAsync` takes a `TranslationMode` parameter and builds its request payload as a `Dictionary<string, object>` (rather than an anonymous type) so `options.temperature=0` can be added only for Literal. `OllamaClient.BuildPrompt` extracted as a public static method, unit-tested for both modes' key phrasing. `PRODUCT_BRIEF.md` (new Must-Demonstrate bullet + two Acceptance Evidence rows; also fixed a stale "primary monitor only" line in Constraints and Risks left over from the multi-monitor work), `CLAUDE.md`, `README.md`, `SHOWCASE.md` updated. **Caveat, stated in `README.md`:** the Literal prompt is tuned to this specific model's observed failure modes (word-for-word phrasing, bracketed hedging) — a different model may need different wording to behave the same way.

### Custom tray icon: procedurally-drawn chili pepper

- **Context:** Ahead of a colleague demo, the generic `SystemIcons.Application` tray icon felt uninspired; the user wanted a distinctive one — a bright yellow chili pepper, curved like a hook, green tip pointing up.
- **Claude Code proposal:** No image-generation or design tool was available, so the icon is drawn procedurally at startup: a `System.Drawing.Bitmap` filled via `Graphics`, with the pepper's body built from overlapping filled circles of shrinking radius along a cubic Bezier spine (a dark-orange layer first for a thin outline, then bright yellow on top), plus a small green triangular calyx at the top. Converted to an `Icon` via `Bitmap.GetHicon()`, cloned, and the native handle destroyed (`user32!DestroyIcon`) to avoid a GDI handle leak.
- **Team decision:** Accepted after visual confirmation in the running tray.
- **Reason:** No design-tool dependency needed; fully self-contained in code, easy to re-tune (spine control points, radii, colors) without external asset files.
- **Consequence:** New `TrayIconFactory.cs` (`CreateChiliIcon()`); `App.xaml.cs`'s `CreateTrayIcon` uses it instead of `SystemIcons.Application`. Verified live: rebuilt, relaunched, user confirmed the icon reads as a yellow chili with a green tip in the tray.

### Bug: translation silently produced an empty pane with no Copy button

- **Context:** During a colleague demo, translations started coming back as a blank pane — busy indicator showed, then nothing: no text, no error, no `[Copy]` button, pane stuck open. User reported it as a regression ("it was working before").
- **Root cause:** `RunTriggerAsync`'s `catch (OperationCanceledException)` assumed the *only* way that exception could fire was the user pressing the hotkey a second time to cancel (in which case `CycleState`'s Busy branch already hides the pane and resets state — so doing nothing in the catch was correct). But `HttpClient`'s own request timeout *also* throws `OperationCanceledException` (`TaskCanceledException`, a subtype) when its `Timeout` elapses — a completely different situation where nothing else resets the UI. That case fell into the same silent-no-op branch, leaving `State` stuck at `Busy`, `OutputText` empty, and `CopyButton` hidden forever, with no feedback at all.
- **Claude Code proposal:** Split the catch: only treat the cancellation as "already handled, stay silent" when `_cts.IsCancellationRequested` is actually true (i.e. *our own* token was the one cancelled by a second hotkey press); otherwise treat it as a real failure — show a "Translation timed out" message and return to `Open` so it's retryable.
- **Team decision:** Accepted.
- **Reason:** The two situations were being conflated by exception type alone; distinguishing by *which token* actually fired makes the silent case correct again without breaking the real-timeout case.
- **Consequence:** `MainWindow.xaml.cs`'s `RunTriggerAsync` now has two `catch (OperationCanceledException)` clauses, the first guarded by `when (_cts is { IsCancellationRequested: true })`. Verified live: after the fix, a genuine timeout now shows a clear, retryable message instead of a silently stuck empty pane.

### Slow model exposed by the fix above: timeout default raised 60s → 120s

- **Context:** Once the silent-failure bug above was fixed, the *real* timeouts it had been hiding became visible. Timing the user's actual Ollama server (`gemma4-e4b-110k:latest` on a LAN box) directly: a trivial text-only prompt took 28s; the same literal-mode instructions that used to run in ~7s (the old, shorter default prompt) took ~14s on comparable text, because the Literal-mode prompt is far longer (more instruction tokens to process) — pushing real image-based OCR+translate requests close to or past the 60s default.
- **Claude Code proposal:** Raise `AppConfig.TimeoutSeconds`'s default to 120s.
- **Team decision:** Accepted.
- **Reason:** Direct, measured evidence of this specific model/server's latency profile, not a guess — 60s was simply too tight a margin once Literal mode's longer prompt became the default.
- **Consequence:** `AppConfig.cs` default changed 60 → 120; user's own `settings.json` updated to match. `README.md`'s Known Limitations already notes Literal mode's prompt is tuned to observed model behavior — extended to note the longer prompt is also slower.

### Settings window: two-column layout with always-visible onboarding help, opened automatically on launch

- **Context:** Next `addons.md` backlog item: Settings should explain how to work with the app, and a related item asked for a help link (Ollama install, model pull, exposing Ollama to the LAN) surfaced when a translation errors on run.
- **Claude Code proposal:** Drafted the exact help copy first for approval — a "How to use" numbered walkthrough of the core journey, and a "Make sure you have" checklist (Windows 11, .NET 10 Desktop Runtime, Ollama installed and running, a vision-capable model pulled and matching the Model name field, the correct endpoint) with clickable links to each install source. Once approved, put it in a right-hand column of the Settings window (existing fields stay on the left, divided by a vertical rule) rather than a separate help pane or on-error popup, and made Settings open automatically on app startup alongside the tray icon so a first-time user sees it immediately without needing to already know the tray menu exists.
- **Team decision:** Accepted, including the two-column layout suggested by the user once the text's length made a single stacked column impractical.
- **Reason:** The help content only needs to be read once or twice, not on every error, so always-available-but-out-of-the-way (a side column, on first launch) fit better than gating it behind a failure state; that also fully covers the "Ollama/model/WAN" instructional content originally requested behind an error-triggered link.
- **Consequence:** `SettingsWindow.xaml` restructured into a `Grid` (left: `ScrollViewer` of existing fields; divider; right: `ScrollViewer` of the two help sections with `Hyperlink`s opening via `Process.Start`/`UseShellExecute`), window resized to 780x640 and made resizable (`CanResizeWithGrip`) since the content no longer fits a `SizeToContent` single column. `App.xaml.cs`'s `OnStartup` now calls `OpenSettingsWindow()` after creating the tray icon. The still-open remainder of the original backlog item — deep-linking from an in-flight translation error straight into Settings — is left in `addons.md` for later, since the always-visible version already covers the informational need. Verified: `dotnet build` clean; live visual check of the running Settings window confirmed the two-column layout, wrapping, and auto-open-on-launch behavior. **Process note:** two self-verification attempts via full-desktop screenshots each captured unrelated, sensitive window content (a colleague video call, then a private WhatsApp conversation) instead of just the Settings window — both deleted immediately; verification was handed back to the user's direct visual confirmation instead, consistent with the same lesson already recorded in `SHOWCASE.md` from M4.

### Milestone non-goals lifted; open build phase, backlog tracked in `addons.md`

- **Context:** M1's non-goals (packaged `.exe`, cloud LLM support, etc.) existed because development time wasn't guaranteed to cover even the required scope. All 4 milestones are now done and verified, well past that risk.
- **Team decision:** The user explicitly lifted the original scope limits: "regardless of any original forcefully limited goals... now it's build build build." New features are no longer screened against the old non-goals list. `addons.md` (a plain, untracked-until-now backlog file) is adopted as the running list of build opportunities, starting with "portable exe" and "allow selecting cloud LLM via API."
- **Reason:** The non-goals existed to protect a deadline that has passed; keeping them would block requested work for no remaining benefit.
- **Consequence:** `CLAUDE.md`'s Scope section rewritten to drop the hard non-goals list in favor of a note that scope is now open and tracked via `addons.md`. Architecture decisions and dependency call-outs (Working Agreement) still apply — this lifts scope limits, not the process around changing the codebase.

### Settings auto-close: 15s countdown on first-launch auto-open only

- **Context:** The Settings window now opens automatically on every app startup (previous decision above). Left open indefinitely, that's an extra manual step for a returning user who doesn't need the onboarding help again.
- **Claude Code proposal:** Give `SettingsWindow` an `autoCloseOnFirstLaunch` constructor flag. When true, a `DispatcherTimer` ticks once per second, updates a countdown label ("Closing automatically in Ns...") next to the Cancel/Save buttons, and calls `Close()` at zero. `App.xaml.cs` passes `true` only from `OnStartup`'s automatic open; the tray menu's "Open settings" keeps calling it with the default `false`, so a manually-reopened Settings window never times out.
- **Team decision:** Accepted as specified — 15s, first-launch-only, no timer reset on user interaction (kept simple; the window is read-only informational content on first launch, not a form the user is expected to be mid-edit in).
- **Reason:** Matches the user's stated intent directly: help content that's useful once at startup and gets out of the way on its own, without adding a timeout to the case where someone deliberately opened Settings to change something.
- **Consequence:** `SettingsWindow.xaml` row 1 changed from a right-aligned button `StackPanel` to a two-column `Grid` (countdown `TextBlock`, collapsed unless auto-close is active, on the left; buttons on the right). `SettingsWindow.xaml.cs` gained the timer, stopped in the window's `Closed` handler to avoid ticking a disposed window. `App.xaml.cs`'s `OpenSettingsWindow` took an `autoCloseOnFirstLaunch` parameter, `true` only at the `OnStartup` call site. Verified: `dotnet build` clean; live run confirmed by the user directly (countdown visible and ticking, window closes itself around 15s on startup, manual tray reopen has no countdown and stays open).

### Desktop launcher + single-instance enforcement

- **Context:** The user wanted a `.bat` file on the Desktop to launch BabelPane on demand, then asked for it to show the app's real icon (a `.bat` can't carry a custom icon in Windows — only a `.lnk` shortcut can), and while testing both, ended up with two `BabelPane.exe` processes running side by side, prompting a request to cap the app at one running instance.
- **Claude Code proposal:** `Launch BabelPane.bat` on the Desktop starts `BabelPane.exe` from its build output path (checks the exe exists first, points to a build command if not). `BabelPane.ico` generated directly from `TrayIconFactory.CreateChiliIcon()` (loaded via `Add-Type -Path` against the built DLL) so it's pixel-identical to the tray icon, not a hand-drawn approximation. `BabelPane.lnk` shortcut targets the `.bat`, carries that icon via `IconLocation`, and is the thing meant to actually be double-clicked. All three live only on the Desktop, not inside the repo, so none of this touches the frozen `babelpane` codebase. Separately, `App.OnStartup` now acquires a named `Mutex` (`BabelPane-SingleInstance-<guid>`) before creating any window or the tray icon; if another instance already holds it, it shows a short "already running" `MessageBox` and calls `Shutdown()` immediately.
- **Team decision:** Accepted, single-instance explicitly requested ("the app should have a maximum on 1 instance running") — an exception to the standing "no changes to babelpane" freeze from earlier in this session, since the user asked for this specific change directly.
- **Reason:** A `.bat`'s fixed generic icon isn't fixable in the file itself, only via a shortcut pointing at it; a per-user named mutex is the standard, dependency-free way to detect and block a second instance of a Windows desktop app before it does any real work (registers the hotkey, creates a tray icon, etc.).
- **Consequence:** `App.xaml.cs` gained `SingleInstanceMutexName`, `_singleInstanceMutex`, `_isPrimaryInstance`; a non-primary instance returns from `OnStartup` immediately after the message box, before touching `_mainWindow`/hotkey/tray/Settings, so `OnExit`'s existing null-checks (`SaveGeometry`, `_trayIcon?.Dispose()`, etc.) stay safe for that short-lived path; the mutex is only released by the instance that owns it. The mutex name is BabelPane-specific so a future BabelPaneSky instance (a separate product, separate process) isn't blocked by it. Verified: `dotnet build` clean; repeated launches confirmed only one full-footprint `BabelPane.exe` persists after a second launch attempt (the second appears briefly as a much lighter process showing the message, then exits) — but the dialog's own dismiss-by-click behavior couldn't be confirmed by simulated input in this automated environment (no true interactive desktop session for `SendKeys`/`AppActivate`), consistent with the same class of verification limit already noted for screenshots elsewhere in this file; left for the user's direct confirmation.

### Portable exe: framework-dependent publish, not self-contained

- **Context:** Backlog item "portable exe, simple" from `addons.md` — the app should run as a packaged `.exe` instead of only via `dotnet run`, now that the original brief's "no packaged exe" non-goal no longer constrains new work.
- **Claude Code proposal:** `dotnet publish src/BabelPane/BabelPane.csproj -c Release -r win-x64 --self-contained false`, producing `BabelPane.exe` + dependency DLLs in `bin/Release/net10.0-windows/win-x64/publish/`. Offered self-contained single-file as the alternative (bundles the .NET runtime, larger output, runs with no runtime installed anywhere).
- **Team decision:** Framework-dependent, chosen directly by the user over self-contained.
- **Reason:** Smaller output; the target machine (the user's own) already has the .NET 10 desktop runtime installed, since that's the same requirement `dotnet run` has today — no new constraint introduced. Nothing is baked into the `.csproj` (the runtime identifier is passed on the publish command line only), so day-to-day `dotnet build`/`dotnet run` are unaffected, and refreshing the package after future changes is just re-running the same publish command.
- **Consequence:** No `.csproj` or code changes. Verified: `dotnet build` clean; ran the published exe directly. First run collided with an already-running Debug-build instance (started earlier the same day) — the new Release exe hit the single-instance mutex (same fixed name regardless of build flavor) and exited within ~2s trying to show the "already running" dialog, which itself couldn't render/be dismissed in this non-interactive environment (same class of limitation already noted for the single-instance dialog above). After closing the Debug instance, the published exe launched alone and stayed running (process persisted, working set consistent with tray icon + auto-opened Settings window). Visual confirmation of the tray icon and window rendering is left for the user directly.

### Bug: real (small/dense) captures produced incomplete, garbled OCR — not a token-limit truncation

- **Context:** Even after the two fixes above, real translations at the user's actual (small, ~286×182) pane size kept coming back visibly cut off mid-sentence — and the copied clipboard text matched the display exactly, proving the data itself was incomplete, not a rendering bug.
- **Root cause, found by direct experiment against the real Ollama server:** Replicating the app's exact request (same prompt, same `temperature=0`, a real rendered image) with a large, roomy synthetic image came back complete every time (`done_reason: "stop"`, full coverage). The *same source text* rendered into an image matching the user's actual small pane size came back garbled and incomplete — still `done_reason: "stop"` (the model decided on its own it was finished), meaning the vision model's own OCR reading of the small/dense image was incomplete, not a generation-length cap. Re-rendering the identical on-screen content at 2x the pixel density fixed it completely. Summary mode's forgiving, paraphrase-anything style had likely been masking this same limitation all along — a summary of a partially-read image still reads as a plausible complete summary, while Literal mode's "translate everything completely" instruction faithfully (and visibly) reflects whatever incomplete reading the model produced.
- **Claude Code proposal:** Upscale the captured region 2x (`InterpolationMode.HighQualityBicubic`) before encoding to PNG and sending it to Ollama, giving the vision model more actual pixels for the same on-screen content.
- **Team decision:** Accepted after live confirmation on the user's real pane and content.
- **Reason:** Reproduced with a controlled experiment (same text, same request shape, only the image's pixel density varied) before proposing a fix — not a guess.
- **Consequence:** `ScreenCapture.cs` gained a `private const int UpscaleFactor = 2` and an `Upscale()` helper using `Graphics.DrawImage` with high-quality bicubic interpolation, applied to every capture before it's PNG-encoded. Verified live on the user's actual failing case: the same source text that previously came back cut off ("...the location where the data is processed" now included in full, vs. previously ending mid-clause) now translates completely and matches the clipboard-copied text.
