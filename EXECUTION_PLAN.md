# Execution Plan — Claude Code Product Showcase

**Portable plan.** Copy this single file into your new Windows project folder and
work top to bottom. It is self-contained: every template you need is in the
appendices.

- **Setup:** Windows 11, PowerShell, VS Code, solo
- **Target:** one working prototype today (~4 h), extensible later
- **Stack:** decided in Phase 2, after the idea is known

## Rules of engagement

| Claude Code owns | You own |
|---|---|
| Inspecting, planning, coding, testing, debugging, docs | Problem, scope, priorities, acceptance, permissions, truthfulness |

Three non-negotiables:

1. **You write the product brief first.** Claude critiques it, does not author it.
2. **No milestone is done until you exercise it as a user.** Passing tests ≠ working product.
3. **Never claim a check passed unless it actually ran.**

## Time budget

| Phase | What | Time | Output |
|---|---|---|---|
| 0 | Workspace ready | 15 min | Repo runs, Claude connected |
| 1 | Frame the product | 30 min | `PRODUCT_BRIEF.md` |
| 2 | Stack + context | 20 min | `CLAUDE.md`, milestone list |
| 3 | Build in 4 milestones | 105 min | Working app |
| 4 | Critical review | 25 min | Fixed issues, known limits |
| 5 | Package showcase | 25 min | `README.md`, `SHOWCASE.md`, screenshots |

If you fall behind: cut features, never cut Phase 1 or Phase 4.

## The loop (repeat per milestone)

**Frame** → **Inspect** → **Plan** → **Delegate** → **Verify** → **Decide**

1. **Frame** — state user, outcome, scope boundary, acceptance check.
2. **Inspect** — let Claude read the repo before proposing.
3. **Plan** — challenge scope, assumptions, dependencies. Cut before approving.
4. **Delegate** — one coherent milestone, not the whole app.
5. **Verify** — run the checks, then click through it yourself.
6. **Decide** — accept / revise / simplify / revert. Log it if it mattered.

Large one-shot prompts hide mistakes. Small reviewable steps expose them.

---

## Phase 0 — Workspace ready (15 min)

### Windows / PowerShell gotchas

| Issue | Fix |
|---|---|
| Long paths, sync conflicts | Use `C:\dev\<project>`. **Not** OneDrive/Desktop/Documents. |
| `npm : running scripts is disabled` | `Set-ExecutionPolicy -Scope CurrentUser RemoteSigned` |
| `&&` fails | PowerShell 5.1 has no `&&`. Use `;` or separate lines. Check: `$PSVersionTable.PSVersion` |
| CRLF noise in every diff | `git config core.autocrlf false` + `.gitattributes` (below) |
| Port already in use | `Get-NetTCPConnection -LocalPort 5173` then `Stop-Process -Id <PID>` |

### Steps

```powershell
node -v; npm -v; git --version        # install anything missing first
New-Item -ItemType Directory C:\dev\<project> -Force
cd C:\dev\<project>
git init -b main
git config core.autocrlf false
code .
```

Create `.gitattributes` with one line: `* text=auto eol=lf`

Create `.gitignore` — at minimum:

```gitignore
node_modules/
dist/
build/
.env
.env.*
!.env.example
*.log
*.sqlite
__pycache__/
.venv/
.claude/settings.local.json
```

**Done when:** `git status` is clean, VS Code is open in `C:\dev\<project>`, Claude
Code responds in the terminal.

---

## Phase 1 — Frame the product (30 min)

### 1a. Pressure-test the idea (10 min)

> **PROMPT**
>
> ```
> Act as a critical product partner, not a cheerleader.
>
> My idea: [2-4 sentences]
> Domain I actually understand: [context]
> Constraint: solo, ~2 hours of build time, synthetic data only, no paid services.
>
> Answer briefly, bullets only, under 250 words:
> 1. The single most specific user and their recurring pain.
> 2. The one journey that proves the value: start -> steps -> success state.
> 3. Three assumptions that would need real user research.
> 4. Three things to cut to fit the time budget.
> 5. Anything here that needs real personal data, credentials, or paid APIs.
>
> Ask me at most 3 questions if the idea is ambiguous. Do not write code.
> ```

Decide what you accept. Cut hard now — cutting later costs build time.

### 1b. Write the brief yourself (15 min)

Create `PRODUCT_BRIEF.md` from **Appendix A**. Rough sentences are fine; your
reasoning must be in it, not Claude's.

Sanity test — a stranger reading it can answer:

- [ ] Who is the user, in one specific phrase (not "everyone")?
- [ ] What can they do afterwards that they couldn't before?
- [ ] What is the smallest journey that proves it?
- [ ] What evidence will show the journey works?
- [ ] What is explicitly out of scope?

### 1c. Let Claude attack it (5 min)

> **PROMPT**
>
> ```
> Read PRODUCT_BRIEF.md. Find: ambiguity, hidden complexity, risky assumptions,
> and the two biggest scope cuts available. Max 150 words, ranked. Do not rewrite
> the file. Do not write code.
> ```

Accept or reject each point, then update the brief yourself.

**Done when:** the brief describes one user, one journey, one success state, one
failure/empty state.

---

## Phase 2 — Stack and persistent context (20 min)

### 2a. Choose the stack (10 min)

> **PROMPT**
>
> ```
> Read PRODUCT_BRIEF.md.
>
> Propose the simplest credible stack for a ~2-hour solo prototype on
> Windows 11 / PowerShell that demonstrates this journey.
>
> Hard constraints:
> - No paid services, no accounts, no cloud infra, no Docker.
> - One install command, one run command.
> - Seeded or synthetic data only.
> - Prefer zero-config over configurable.
>
> Return under 200 words:
> - A table: Option | Why it fits this journey | Main risk
> - Your recommendation, and every dependency with one line on why it is needed.
> - 4 milestones, each with the check that proves it works.
>
> Do not write code yet.
> ```

Your job: **delete infrastructure**. No auth, no database, no state library, no
component library unless the journey collapses without it.

### 2b. Write `CLAUDE.md` (10 min)

Create `CLAUDE.md` from **Appendix B**. Fill in the approved stack and the real
install/run/test/lint commands.

Keep it short. Stable operating rules go here; evolving scope stays in
`PRODUCT_BRIEF.md`. Never let it become a transcript.

**Done when:** `CLAUDE.md` has real commands, and you have 4 named milestones.

---

## Phase 3 — Build in 4 milestones (105 min)

### Reusable milestone prompt

> **PROMPT**
>
> ```
> Milestone: [name]
> What the user can do after it: [outcome]
> In scope: [...]
> Out of scope: [...]
>
> Step 1 - before editing, tell me: the files you expect to change, your
> assumptions, new dependencies and why, and the checks that will prove this
> works. Then stop and wait for my go-ahead.
>
> Step 2 - after I approve: implement it, run the checks, and report what is
> verified versus unverified. Add nothing outside the scope above.
> ```

### When something breaks

> **PROMPT**
>
> ```
> Observed: I did [action] and got [behavior].
> Expected: [behavior].
> Output:
> [paste the exact error]
>
> Find the root cause before proposing a fix. Give me the cause in two sentences,
> then the smallest fix that addresses it. Do not refactor beyond the fix.
> ```

Do not take over the keyboard at the first error. Hand the error back.

### Milestone sequence

| # | Milestone | Time | Verify by |
|---|---|---|---|
| M1 | Runnable shell + visual direction | 25 min | App starts on one command; layout readable at 375 px and 1440 px |
| M2 | **Core journey, end to end, seeded data** | 45 min | You complete the journey start → success without touching code |
| M3 | Empty, loading, error, invalid-input states | 20 min | Each state reachable on demand; messages say what to do next |
| M4 | Automated checks + walkthrough | 15 min | Checks run green; you narrate the journey out loud once |

**M2 is the project.** If time runs short, M2 gets the remaining minutes and M3
shrinks to just the empty and error states.

### After each milestone

- [ ] Ran the check yourself — not just read Claude's summary
- [ ] Clicked the journey as a user
- [ ] Inspected the diff: `git diff` (look for scope creep and new deps)
- [ ] Logged anything consequential in `DECISIONS.md` (**Appendix C**)
- [ ] Committed: `git add -A; git commit -m "M2: core journey"`

Commit per milestone. It is your only cheap undo.

---

## Phase 4 — Critical review (25 min)

### 4a. Ask for findings, not fixes (10 min)

> **PROMPT**
>
> ```
> Final critical review of this repo against PRODUCT_BRIEF.md.
> Max 2 findings per lens, ranked, each with file:line and a one-line fix:
>
> 1. Target user - is the next action obvious? Is the result understandable?
> 2. Product - does any feature fail to serve the stated outcome?
> 3. Quality - empty, invalid, repeated, or unexpected input.
> 4. Accessibility - keyboard reachable, labels, contrast, non-color cues.
> 5. Trust - is generated, mocked, or estimated output clearly labelled?
> 6. Operations - can a stranger install and run this from README.md alone?
> 7. Security and privacy - secrets, real personal data, unsafe demo content.
>
> Split into CONFIRMED (you verified it in the code) and SUSPECTED.
> Fix nothing yet.
> ```

### 4b. You triage (15 min)

- Spot-check 2–3 CONFIRMED findings yourself. Claude's confidence is not evidence.
- Fix only what blocks the journey, the quality gate, or safety.
- Everything else → **Known Limitations** in `SHOWCASE.md`. Stating a limit
  honestly beats a rushed fix.

---

## Phase 5 — Package the showcase (25 min)

### 5a. Screenshots (5 min)

`Win` + `Shift` + `S` → save into `assets/`. Minimum two: the **start state** and
the **success state**. Check them for anything personal before saving.

### 5b. README (10 min)

> **PROMPT**
>
> ```
> Write README.md for someone who was not here and has never seen this repo.
>
> Sections: what it is and who it is for; screenshots (assets/...); setup; run;
> "verify the core journey" as numbered steps; what data is seeded or mocked;
> known limitations.
>
> Under 150 lines. Windows PowerShell commands. State only checks that actually
> ran in this session - no aspirational claims.
> ```

Then follow your own README from a fresh PowerShell window. If a step fails, the
README is wrong, not you.

### 5c. Showcase (10 min)

Fill `SHOWCASE.md` (**Appendix D**) yourself. The two answers that carry weight:

- one place Claude Code clearly accelerated the work;
- one place your review **changed or rejected** its proposal.

---

## Quality gate

Ship only when every line is true:

- [ ] A new reviewer can explain the user and the value
- [ ] Core journey works from a clean start (fresh terminal, fresh install)
- [ ] You followed your own setup instructions successfully
- [ ] Empty, error, and narrow-screen behavior reviewed
- [ ] Diff and dependencies inspected — nothing unexplained
- [ ] No secrets, no real personal data, no private URLs
- [ ] Every test/build claim matches a check that actually ran
- [ ] Known limitations and unverified assumptions written down
- [ ] `PRODUCT_BRIEF.md`, `CLAUDE.md`, `DECISIONS.md`, `SHOWCASE.md` all filled in

## Guardrails

- Read commands before approving them; be slow with anything destructive or irreversible.
- Secrets live in `.env` (git-ignored). Commit only `.env.example` with placeholders.
- Synthetic or public data only.
- Ask Claude to justify any dependency you do not recognise.
- Review `git status` and `git diff` before every commit.
- Never present generated or mocked behavior as validated real-world results.

## Parking lot — after the gate passes

Only add things that deepen the evidence, not surface area.

- Short usability test with one real person → prioritized changes
- Replace seeded data with a documented public API + fallback
- Persistence with a demo reset path
- Automated browser test for the core journey
- Accessibility audit and confirmed fixes
- A second user role, only if it changes the product model
- Capstone proposal: research questions, success measures, data, architecture, risks

---

# Appendix A — `PRODUCT_BRIEF.md`

```markdown
# Product Brief

## Summary
**Working name:**
**One-sentence value proposition:**

## User and Problem
**Primary user:**
**Situation and current behavior:**
**Problem or unmet need:**
**Evidence I have:**
**Assumptions still needing research:**

## Core Journey
**Starting point:**
**Key user actions:**
**Successful outcome:**
**Important empty or failure state:**

## Scope
### Must demonstrate
-
### Explicit non-goals
-

## Acceptance Evidence
| Criterion | How I will verify it |
|---|---|
|  |  |

## Constraints and Risks
- Data and privacy:
- Accessibility:
- Technical constraints:
- Product or trust risks:

## Capstone Potential
**Why this direction may deserve more investment:**
**What must be learned first:**
```

# Appendix B — `CLAUDE.md`

```markdown
# Project Instructions for Claude Code

Keep this file short and current.

## Product
- Target user: <specific user>
- User problem: <task that needs improvement>
- Intended outcome: <what the user can accomplish>
- Core journey: <start, steps, success state>
- Current milestone: <next reviewable outcome>

## Scope
Required now:
- <requirement>

Non-goals:
- <non-goal>

Do not add features outside the current milestone without explaining why they
are necessary.

## Technical Context
- Stack: <stack and versions>
- Architecture: <main components in 2 lines>
- Important paths: <source, tests, data>
- Data: synthetic / seeded only
- Environment: Windows 11, PowerShell. No `&&` chaining. Paths use `\`.

## Commands
- Install: `<command>`
- Run: `<command>`
- Test: `<command>`
- Lint or format: `<command>`
- Build: `<command>`

Never report a command as passing unless it actually ran successfully.

## Product and Design Rules
- Optimize the primary journey for the target user.
- Keep the interface responsive and accessible (keyboard, labels, contrast,
  non-color cues).
- Include useful loading, empty, success, and error states.
- Use realistic, non-sensitive demonstration content.
- Label mocked, generated, or estimated output clearly.
- Prefer a small coherent product over broad unfinished functionality.

## Working Agreement
- Inspect the repo and PRODUCT_BRIEF.md before proposing changes.
- For a substantial milestone: present the plan, assumptions, expected file
  changes, and verification approach before editing.
- Stay inside the agreed milestone.
- Explain new dependencies and meaningful architecture decisions.
- Run the relevant checks afterwards and report what is unverified.
- Never commit, push, deploy, delete data, or do anything irreversible without
  explicit permission.
- Never expose secrets or put sensitive data in files, logs, or screenshots.

## Definition of Done
Acceptance criteria met, relevant checks pass, journey exercised by a human,
docs match actual behavior, remaining limitations stated.
```

# Appendix C — `DECISIONS.md`

```markdown
# Product and Build Decisions

One entry per decision that changed scope, experience, architecture, or quality.
No transcripts.

### <Decision title>
- **Context:** what needed deciding
- **Claude Code proposal:** what it recommended
- **My decision:** accepted / changed / rejected
- **Reason:** which product or technical evidence decided it
- **Consequence:** what changed in the product or next milestone
```

# Appendix D — `SHOWCASE.md`

```markdown
# Product Showcase

## Product Story
**User and problem:**
**Value demonstrated:**
**Why this scope:**

## Core Journey
<Uninterrupted journey, plus links to 2 screenshots or a recording.>

## Evidence
### Product
- Acceptance criteria checked:
- Feedback or observations:
- Edge cases reviewed:

### Technical
- Install and run verification:
- Tests, lint, type checks, or build commands completed:
- Not verified:

## Working with Claude Code
**Where it accelerated the work:**
**Where my review changed or rejected its proposal:**
**Most important lesson about directing it:**

## Known Limitations
-

## Bridge to the Capstone
**Worth carrying forward:**
**Research or validation still required:**
**Data, architecture, testing, accessibility, security, or governance work needed:**
**Recommended next product experiment:**
```
