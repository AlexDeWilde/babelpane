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
