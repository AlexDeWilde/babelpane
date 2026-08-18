# Product Brief

## Product Summary

**Working name:** BabelPane

**One-sentence value proposition:** A floating, resizable overlay you point at any text on your screen that shows an instant translation in place, using a local LLM only — nothing leaves your machine, and there's no copy-paste.

## User and Problem

**Primary user:** Anyone facing a language barrier who can run a small LLM locally and wants private, in-place translation of whatever is on their screen — not just me, but anyone unwilling or unable to trust cloud translation services with the content in question.

**Situation and current behavior:** To translate this kind of content today, people copy-paste text into a web-based translator, or screenshot and upload it, source by source (PDF viewer, scanner app, browser, messaging app) — a convoluted process repeated every time, and one that requires trusting a third-party service with the content.

**Problem or unmet need:** There's no fast, private way to translate arbitrary on-screen content in place. Web-based translation services are inconvenient for this workflow and, more importantly, not something people can comfortably use for sensitive personal material — contracts, insurance info, private correspondence, or anything else they'd rather not upload.

**Evidence we have:** My own recurring experience across multiple real document types — legal/insurance paperwork, private emails, scanned documents, browser pages, and social media messages — where I can't comfortably use web-based translation.

**Assumptions that still require research:**
- The local model (`hf.co/unsloth/gemma-4-E4B-it-qat-GGUF:UD-Q4_K_XL` via Ollama) has been tried standalone for combined OCR + translation and worked; whether that quality holds up once wired into this app's actual capture pipeline, and for dense or legally consequential text specifically, is still to be seen.
- Autofit text (font size shrunk to the available space, fixed line spacing) reads as usable rather than cramped or illegible for dense document content.
- Multi-monitor support was verified only where all connected monitors share the same DPI scaling (both monitors on the dev machine run at 100%); behavior with monitors at different scaling factors (e.g. a laptop panel at 150% next to an external display at 100%) is unverified.

## Core Journey

**Starting point:** BabelPane is running in the background; its icon is in the system tray; the widget pane is closed.

**Key user actions:**
1. Press the global hotkey (default `Win+Alt+X`, changeable in settings) — the pane opens, empty, at its last saved size and position.
2. Drag and resize the borderless, transparent-center pane — from any edge or corner, like any normal window — over the foreign-language text, on any on-screen source (PDF, scanned image, browser, app, social media), on any monitor connected to the system.
3. Press the same hotkey again (or click anywhere in the pane without dragging it) — the region under the pane is captured in memory and sent to the local Ollama model for combined OCR and translation. The captured image is never written to disk and is discarded once the request completes. A busy indicator shows while waiting. Pressing the hotkey again while the request is still in flight cancels it and closes the pane immediately.
4. The translated text appears inside the pane, in place, autofit to the available space (font size scaled down as needed, fixed line spacing), wrapped rather than matched to the original layout.
5. Press the hotkey a third time to close the pane, or click the `[Copy]` button that appears once a translation is showing — this copies the translated text to the clipboard, flashes a brief "Copied" confirmation, and closes the pane in one step. Either way, its size and position are saved; its content is not.

**Successful outcome:** I can read a trustworthy-enough translation of sensitive on-screen text, in place, without the content or a captured image of it ever leaving memory or touching disk.

**Important empty or failure state:** Ollama is unreachable, times out, or returns nothing usable — the pane shows a clear inline status/error message and stays open, at the same position and size, so I can retry without losing my setup.

## Scope

### Must Demonstrate

- Tray icon with a menu: open widget pane / open settings.
- Borderless pane with a transparent center (only a contour visible), movable, resizable from any edge or corner like a normal window, and closeable with the mouse.
- Global hotkey (default `Win+Alt+X`, changeable in settings) driving a 3-state cycle: open → trigger capture+translate → close. A new keypress while a request is in flight cancels it and closes the pane.
- Clicking anywhere in the pane without dragging it is a manual trigger alternative to the hotkey's trigger step.
- Pane size and position persisted across app restarts; pane always opens empty. If the saved position falls outside every currently connected monitor (e.g. an external monitor was unplugged), the pane falls back to a centered, reachable position on the primary monitor instead of opening off-screen.
- Works across all monitors connected to the system — the pane can be dragged to and captures/translates correctly on any of them, not just the primary.
- Screen-region capture of whatever is beneath the pane, on any connected monitor. The captured image exists only in memory for the duration of the request and is never written to disk.
- Call to a local Ollama server (endpoint and model name both configurable) for combined OCR + translation.
- Settings window exposing: Ollama endpoint URL, model name, target language, translation mode (literal / summary), request timeout, and the global hotkey.
- Visible busy/activity indicator while waiting on the model.
- Inline, recoverable error/timeout state in the pane.
- Translated text rendered in place, autofit to the available space (font size scaled to fit, fixed line spacing), wrapped rather than layout-matched.
- Two translation modes, selectable in settings: **Literal translation** (default) — a complete, sentence-by-sentence, faithful translation in fluent target-language phrasing, with deterministic output (temperature 0); and **Summary interpretation** — a more freely interpreted rendering.
- `[Copy]` button, visible only once a translation is showing, that copies the translated text to the clipboard, confirms with a brief "Copied" message, and closes the pane.

### Explicit Non-Goals

- Packaged standalone `.exe` — runs as a script/dev process for this prototype.
- Configurable source language — auto-detected by the model, only the target language is a setting.
- Matching the original text's line breaks/layout — autofit + plain wrap only.
- Side-by-side original/translated view.
- Translation history or logging.
- Non-Windows support.

## Acceptance Evidence

| Criterion | How we will verify it |
|---|---|
| Tray icon opens pane and settings | Click the tray icon; confirm both menu items appear and each opens the right window |
| Pane is borderless, transparent-center, mouse-movable/resizable/closeable | Manually drag, resize from each edge and corner, and close the pane; confirm no window chrome and that content underneath is visible through the center |
| Hotkey 3-state cycle works | From a closed state, press the configured hotkey three times in a row over sample foreign text; confirm open → translate → close behavior each time |
| Mid-request cancel works | Trigger a translation, press the hotkey again before it completes; confirm the request is abandoned and the pane closes immediately |
| Captured image is never persisted to disk | Watch the filesystem (temp folders included) during and after a translation; confirm no image file is written at any point |
| Geometry persists, content does not | Move/resize the pane, fully close the app, relaunch, reopen the pane; confirm same size/position and empty content |
| Real translation via local Ollama | Point the pane at a real foreign-language sample (e.g. a scanned contract clause or a foreign-language email) and trigger; confirm a plausible translation appears in place |
| Autofit rendering is legible | Trigger a translation on a dense block of text; confirm the result is scaled to fit the pane, fixed line spacing, readable rather than cramped |
| Timeout/error handling is visible, not silent | Stop Ollama or block the connection, trigger a translation; confirm the busy indicator appears, then a clear in-pane error/timeout message, with geometry intact. Also confirm a genuinely slow response (exceeding the configured timeout, not a user-cancelled one) shows the same clear message rather than leaving a stuck, empty pane |
| Settings changes take effect | Change endpoint, model, target language, hotkey, and timeout in settings; confirm the next translation uses the updated values |
| Copy-to-clipboard works | Trigger a translation, click `[Copy]`; confirm the pane closes, a brief "Copied" confirmation appears, and the exact translated text is on the clipboard |
| Works on any connected monitor | Drag the pane onto a secondary monitor, trigger a translation over real text there; confirm the captured/rendered content matches that monitor. Save geometry there, relaunch, confirm it reopens in the same spot |
| Recovers from a changed monitor arrangement | Set saved pane geometry to coordinates outside every currently connected monitor, relaunch; confirm the pane falls back to a centered, reachable position instead of opening off-screen |
| Literal mode is faithful, fluent, and clean | Trigger a translation in Literal mode (the default) on real multi-sentence foreign text; confirm every sentence is translated completely (nothing summarized/omitted), reads as natural fluent phrasing (not word-for-word source order), and contains no bracketed alternates or hedging |
| Summary mode is selectable and different | Switch to Summary interpretation in settings, retranslate the same text; confirm the output is noticeably more interpretive than Literal mode's output on the same input |
| Small panes still OCR completely | Trigger a translation over dense multi-sentence text with a small pane; confirm the full text is translated (nothing cut off mid-sentence), matching what the copied clipboard text shows |

## Constraints and Risks

- Data and privacy: All OCR and translation happen locally against a local Ollama server (`localhost:11434`); no screenshot or extracted text is sent to any third-party or cloud service. The captured screen region is held in memory only and is never written to disk. This is the core value proposition, not an add-on — sensitive personal documents must never leave the local network or persist anywhere after translation.
- Accessibility: Primary interactions (move, resize, close the pane) are mouse-driven by design, consistent with a floating widget. The hotkey gives a keyboard path for open/trigger/close. Settings window keyboard-only operation is not a goal for this prototype.
- Technical constraints: Windows 11 only; requires a reachable Ollama server running a vision-capable model; works across all connected monitors, verified only where they share the same DPI scaling (mixed-DPI setups unverified — see Assumptions above); the overlay must capture actual on-screen content beneath it without capturing its own transparent window.
- Product or trust risks: OCR/translation quality from a local quantized model is unverified for dense or legally consequential text. A mistranslated clause in a contract or insurance document has real consequences, so results should be treated as a reading aid, not an authoritative translation, even though no on-screen disclaimer is required at this prototype stage.

## Capstone Potential

**Why this direction may deserve further investment:** It addresses a real, recurring, privacy-sensitive need — reading legal, financial, and personal documents in a foreign language — that cloud-based translation tools can't comfortably serve. A local-LLM screen-overlay translator is a distinctive, demonstrable use of on-device AI with a genuine privacy argument, not just a technical exercise.

**What must be learned before treating it as a Capstone candidate:** Real-world OCR + translation accuracy on dense/legal text versus casual text; whether an unlabeled AI translation is trustworthy enough for consequential documents without a confidence or disclaimer indicator; robustness of multi-monitor and DPI handling; and how large a value gap remains for the pane trigger/keystroke to be truly "free" from copy-paste.
