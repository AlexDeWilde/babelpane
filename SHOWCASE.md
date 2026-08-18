# Product Showcase

## Product Story

**User and problem:** Anyone facing a language barrier who runs a local LLM and
wants private, in-place translation of on-screen text — contracts, insurance
paperwork, scans, browser pages, messages — without uploading it to a cloud
translation service.

**Value demonstrated:** Point a floating, resizable overlay at foreign-language
text anywhere on screen; get a translation rendered in place, entirely
offline, with no copy-paste and no captured image ever written to disk.

**Why this scope:** The four milestones (runnable shell → core journey →
empty/error states → automated checks + walkthrough) cover the brief's full
"Must Demonstrate" list, plus an unplanned-but-required fifth milestone for
the settings window and pane-geometry persistence (see `DECISIONS.md`).
Multi-monitor support, layout-matched rendering, and a packaged `.exe` were
cut deliberately — each was flagged as adding real complexity without adding
to the core value being proven (see `PRODUCT_BRIEF.md` non-goals).

## Core Journey

Open (`Win+Alt+X`) → drag/resize over foreign text → trigger (hotkey or
`[go]`) → busy indicator → translated text renders in place, autofit → close
(hotkey or `Escape`). A forced-unreachable-endpoint test confirmed the
error/retry branch: pane stays open at the same position/size with a clear
message and retry path.

![Pane open and empty](assets/start-state.png)
![Translated text rendered in the pane](assets/success-state.png)

## Evidence

### Product

- **Acceptance criteria checked:** All rows in `PRODUCT_BRIEF.md`'s Acceptance
  Evidence table were exercised live — tray icon menu, borderless
  transparent-center pane (drag/resize/close by mouse), 3-state hotkey cycle,
  `[go]` button, pane geometry persisted across restarts, in-memory capture,
  settings window (endpoint/model/language/timeout/hotkey), busy indicator,
  inline retryable error state, autofit rendering.
- **Feedback or observations:** The first live walkthrough attempt actually
  succeeded (the configured Ollama endpoint was reachable), so the error path
  wasn't exercised naturally — it had to be forced by deliberately pointing
  Settings at an unreachable address, then reverted. A full-desktop screenshot
  taken early in the session (to self-verify the GUI) incidentally captured
  unrelated content from other open windows; it was deleted immediately and
  the rest of the walkthrough was driven by the user directly instead (see
  "Most important lesson" below).
- **Edge cases reviewed:** no-legible-text case (explicit model sentinel, not
  a blank-string check — see `DECISIONS.md`), malformed/unexpected Ollama
  response JSON (now a clear retryable message, covered by tests), settings
  field validation (empty model/language, non-positive timeout, invalid
  hotkey key/URL).

### Technical

- **Install and run verification:** `dotnet restore`, `dotnet build`,
  `dotnet run` all executed and confirmed working in this session (app
  launched to the tray with no startup errors, twice, across two separate
  runs).
- **Tests, lint, type checks, or build commands completed:** `dotnet build
  BabelPane.sln` — 0 warnings, 0 errors. `dotnet test BabelPane.sln` — 7/7
  passed (`AppConfig` JSON round-trip incl. null geometry; Ollama response
  parsing incl. malformed-JSON and missing-field cases). `dotnet format
  BabelPane.sln` — ran clean, fixed one spacing nit in the WPF-scaffolded
  `AssemblyInfo.cs`.
- **Not verified:** Hotkey registration, screen capture, and
  drag/resize are GUI/OS-interop paths not covered by automated tests —
  verified manually instead.

## Working with Claude Code

**Where it accelerated the work:** Scaffolding the xUnit test project and
identifying the one refactor needed to make Ollama's response parsing
testable without mocking `HttpClient`; running a structured 7-lens critical
review that caught a real issue (a real home-LAN IP address hardcoded as the
shipped default setting) that a normal read-through could easily miss.

**Where my review changed or rejected its proposal:** Rejected the proposed
approach of Claude taking full-desktop screenshots to self-verify the GUI
walkthrough, after one such screenshot exposed unrelated window content —
switched to driving the app directly instead. Also chose to log one review
finding (a small synchronous UI-thread pause during capture) as a known
limitation rather than have it fixed, since it's immaterial in practice.

**Most important lesson about directing it:** A model narrating "this should
work" from reading the code is not the same as a human exercising the
journey — the live walkthrough's happy path succeeded on the first try, which
would have left the error/retry path unverified if not deliberately forced.
Automated GUI verification (screenshots, simulated input) also carries a real
privacy cost on a real desktop that a sandboxed test environment wouldn't
have — worth deciding deliberately, not defaulting into.

## Known Limitations

- Single (primary) monitor only.
- Source language auto-detected; only the target language is configurable.
- Autofit + wrap rendering, not layout-matched to the source.
- No translation history, logging, or side-by-side view.
- Dev-process only, no packaged `.exe`.
- ~120ms synchronous UI-thread pause during every capture (see `DECISIONS.md`).
- No validation of translation quality/accuracy — entirely dependent on the
  configured local model.

## Bridge to the Capstone

**Worth carrying forward:** The offline-first, in-place translation journey
itself — the core privacy value proposition (nothing leaves the machine)
doesn't require any of the cut scope to be compelling.

**Research or validation still required:** Real-world OCR+translate quality on
dense, legally consequential text (contracts, insurance) at production scale,
not just the synthetic/casual content tested here; whether autofit reads as
usable rather than cramped on genuinely long documents.

**Data, architecture, testing, accessibility, security, or governance work
needed:** Multi-monitor-aware capture architecture; a packaging/distribution
pipeline; an accessibility audit beyond the one keyboard affordance added
this session (`Escape` to close); automated UI/interop testing if this moves
past prototype stage.

**Recommended next product experiment:** A short usability test with one real
person translating a real (but non-sensitive) foreign-language document
end-to-end, to learn whether autofit rendering and the 3-state hotkey model
hold up outside a scripted walkthrough.
