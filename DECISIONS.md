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
