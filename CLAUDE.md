# Open-Source Workbench: orientation for Claude (and humans)

This repo has two jobs:

1. **Open-source work:** find issues, understand them, reproduce them, fix them, and learn the concepts and the
   architecture behind them.
2. **A self-improving growth system:** get to know Timothy (goals, strengths, gaps, situation, hardware), choose
   issues that fit and stretch him, and turn every issue into a measurable step toward his goal (a .NET role at
   Microsoft). See §9.

It It's published on GitHub as `Timothy-Lee-Grant/playground` (it started as a general
playground; since 2026-09-26 it's organized around open-source work). `README.md` is the public face that
maintainers land on from PR links. **This file is the working guide.** Read it before doing anything here.

**Before you write anything for Timothy** (a conversation answer, a lecture, a report), read [`persona.md`](persona.md).
**Before choosing or scouting issues**, also read [`open_source_persona.md`](open_source_persona.md).
It covers who he is (embedded/firmware engineer moving toward backend and infrastructure), how he learns
(high-level architecture first, then components → interactions → control flow → code → edge cases; personified,
named components; ASCII diagrams and tables; running systems before documents), and process habits to watch for.

> **The system is new (started 2026-09-26) and still changing.** The structure below is current as of
> 2026-09-27. If something here stops matching reality, fix this file in the same session. There's a list of open
> questions about the system at the end.

---

## 1. Repo map

```
exercises/  (GitHub: Timothy-Lee-Grant/playground)
├── README.md               public index: what this repo is + table of every issue worked on
├── CLAUDE.md               ← you are here: layout, workflow, conventions, templates
├── persona.md              public: who Timothy is and how to teach him (living document; read first)
├── open_source_persona.md  public: contributor profile: goals, strengths, issue scoring, machines/hardware,
│                           exposure map, contribution record, study log (the public core of §9)
├── scouting/               issue searches: dated shortlists of candidate issues + a progress tracker
├── iot/                    dotnet/iot work ─────────────── NEW LAYOUT (v2), see §2
│   ├── CLAUDE.md           orientation for the dotnet/iot project itself
│   ├── iot_concepts/       lecture notes on IoT / dotnet/iot concepts, reusable across issues
│   └── 2403_<slug>/        one folder per issue: README, CLAUDE, conversation/, sample/, report/
├── yarp/                   dotnet/yarp work ────────────── LEGACY LAYOUT (v1), see §3; don't restructure
│   └── 1764_websocket_idle_timeout/
├── lectures/               cross-cutting lectures not tied to one upstream project (+ the YARP-era lecture)
│   └── 000-pattern-catalog.md   cross-repo catalog of design patterns and practices seen in the code
├── hand_experiments/       older hand-written practice projects (Kafka, Redis, Rx, RabbitMQ, ...)
│                           ⚠ strict rules: NO AI-written code; review-and-teach only. See its CLAUDE.md.
└── private/                gitignored, never pushed: candid persona, observation log, monthly reviews (§9).
                            Copied by Timothy into a private repo. See private/README.md.
```

Upstream source code is **never** committed here. Forks are cloned outside this repo (see §5, Contribute).

---

## 2. The v2 layout (use this for every new issue)

### 2.1 Upstream-project folder: `<repo>/`

One top-level folder per upstream repository, named after the repo in lowercase (`iot`, `yarp`, `csharp-sdk`,
`opentelemetry-dotnet`, ...).

| Path | What it is |
|---|---|
| `<repo>/CLAUDE.md` | Orientation for the **upstream project**: what it is, how its source is laid out, key types, how to build and test it, its contribution rules and release cadence, maintainers seen so far, and the list of issue folders. Shared context for every issue in that project. |
| `<repo>/<repo>_concepts/` | **Lecture notes** for that project's domain (e.g. `iot_concepts/`: GPIO, drivers, .NET events, Linux GPIO APIs). Written to teach the concept holistically, so they stay useful after the issue that prompted them is closed. Has a `README.md` index. |
| `<repo>/<issue#>_<slug>/` | One workspace per issue being worked on (§2.2). |

### 2.2 Issue folder: `<repo>/<issue#>_<slug>/`

```
<issue#>_<slug>/
├── README.md        public landing page for maintainers (short): claim · status · links to report + sample
├── CLAUDE.md        orientation for this issue: what it is, upstream rules, where things are, how to resume
├── conversation/    the linear log of the work: progress + questions/answers + decisions   (§4.1)
│   └── 001-conversation-log.md
├── sample/          runnable experiments and their captured output                         (§4.2)
│   ├── README.md    what each experiment is and how to run it
│   └── evidence/    raw output of every run the report relies on
└── report/          the finished lab report(s)                                            (§4.3)
    └── README.md    what belongs here + the lab-report outline
```

| Folder | Question it answers | Audience | Edited how |
|---|---|---|---|
| `conversation/` | "What happened, in what order, and why?" | Timothy, and any Claude session picking up the work | Append-only (except the status box and index at the top) |
| `sample/` | "Can I see it happen?" | Timothy, maintainers who want to rerun it | Code changes freely; evidence files are never edited after capture |
| `report/` | "What was found, how was it tested, and what does it mean?" | Anyone: Timothy, maintainers, a future reader | Written when there are results; revised until final |
| `README.md` | "Why is this folder linked from my PR?" | Maintainers | Kept short and current |

### 2.3 Where does a piece of writing go?

| You're writing... | It goes in |
|---|---|
| An answer to Timothy's question about this issue | `conversation/` (new entry) |
| A record of what was just done (ran E1, posted a comment, decided a route) | `conversation/` (📍 or 🧭 entry) |
| A general explanation of a concept (events, GPIO, sysfs vs libgpiod, semver) | `<repo>/<repo>_concepts/NNN-*.md`, linked from the conversation |
| A concept that isn't specific to any one upstream project | root `lectures/` |
| Code that reproduces or tests something | `sample/` |
| Raw output of a run | `sample/evidence/NNN-*.txt` |
| The final write-up of results | `report/NNN-lab-report-*.md` |
| The fix itself | a fork **outside** this repo (§5) |
| Status for a maintainer | the issue `README.md` |

---

## 3. The v1 (legacy) layout: `yarp/1764_websocket_idle_timeout/`

YARP #1764 was the first issue and uses the older layout. **Timothy is still actively working on it, so don't
restructure it.** Plan to migrate it once #1764 is closed. How the v1 pieces map to v2:

| v1 (yarp/1764) | v2 equivalent |
|---|---|
| `concept_notes/001-questions_from_the_issue_shortlist.md` (Q&A log that also tracked progress) | `conversation/001-conversation-log.md` |
| `implementations/001-lab-report-*.md` | `report/001-lab-report-*.md` |
| root `lectures/001-yarp-websocket-activity-timeout.md` | `yarp/yarp_concepts/` (after migration) |
| `README.md` (long public evidence page) | `README.md` (short) + `report/` |
| `sample/`, `sample/evidence/`, `CLAUDE.md` | same |

When working in `yarp/1764_*`, follow **its own** `CLAUDE.md`.

---

## 4. The three working folders in detail

### 4.1 `conversation/`: one linear log

The conversation log is where Timothy thinks out loud with Claude. It mixes questions, answers, progress and
decisions **in the order they happened**, so reading it top to bottom rebuilds the full context. That's
deliberate: a new Claude session (desktop or CLI) can read this one file and know where things stand.

- **File:** `conversation/001-conversation-log.md`. If it gets very long (roughly 1,500+ lines), start
  `002-conversation-log.md` with a one-paragraph recap and a link back. Don't split by topic.
- **Top of file:** a header explaining the file, then **"Where we are now"** (stage, last entry, open decisions,
  next step) and an **Index** table. These two are the only things edited in place. Update them with every new
  entry.
- **Entries:** `## #N · YYYY-MM-DD · <type>: <title>`, appended at the bottom. Types:
  - 📍 **Progress:** what was done, with links to evidence or commits.
  - ❓ **Question → 💬 Answer:** Timothy's question in his words, then the answer: short answer first, then the
    explanation, then takeaways / next steps.
  - 🧭 **Decision:** what was decided, the options considered, and why.
  - 🔁 **Correction:** fixes an earlier entry and links back to it. Never edit the old entry.
- **Keep answers issue-focused.** If an answer grows into a general lesson, write it as a concept lecture and link
  it from the entry.
- **End of every working session** (desktop or CLI): append a 📍 entry saying what was done, what's verified, and
  what's next, and update "Where we are now".

### 4.2 `sample/`: experiments and evidence

- **Walking skeleton first:** get the smallest thing running end to end, then add pieces. Build and run early
  and often. Don't design big before running anything.
- Each experiment has an ID (`E1`, `E2`, ...) that's used the same way in the conversation, `sample/README.md`
  and the report.
- **Pin the toolchain** (`global.json`, exact package versions) so a maintainer can reproduce it.
- **Evidence is captured, not remembered.** Save the raw output of every run anything relies on to
  `sample/evidence/NNN-<experiment>-<what>.txt`, with a header: date, OS, `dotnet --version`, package versions,
  upstream commit/tag if relevant, and the exact command. Never edit an evidence file after capture; rerun instead.
- Label anything not backed by a saved run as **unverified**, everywhere.
- Check the state you think you changed (ports, processes, env vars, which package version actually restored)
  before trusting a rerun.

### 4.3 `report/`: the lab report

A finished, standalone write-up in the style of an undergraduate physics/chemistry lab report. It takes a reader
from the problem to the results and explains how every result was obtained, without needing the conversation log.
Write it once there are real results; it's the document an upstream comment or PR links to.

Outline (template in §8.4): Abstract → Background → Question & hypotheses → Apparatus & environment → Method (per
experiment) → Results (tables, figures, evidence links) → Discussion → Conclusion → Limitations & threats to
validity → What was contributed upstream → References → Appendix.

### 4.4 `<repo>_concepts/`: lecture notes

Full lectures on concepts Timothy needs, written for the long term. The issue that prompted a lecture is an
*example inside it*, not its subject. Style, from `persona.md`: why it exists and what problem it solves first;
then architecture → components → interactions → control flow → implementation → edge cases; personified, named
characters; ASCII diagrams and tables; code references to the real upstream files and types; "common mistakes",
"interview relevance" and "real-world usage" sections. Each concepts folder has a `README.md` index table
(number, title, prompted by, status).

---

## 5. Workflow, stage by stage

| Stage | What happens | Output goes to |
|---|---|---|
| **1. Scout** | Search upstream repos for candidate issues. | `scouting/NNN-issue_shortlist_<month_year>.md` (one per pass, dated snapshot; carry unfinished items forward). Keep its **progress tracker** current. |
| **2. Pick** | Move a candidate to "working on it". Re-check the thread (open? claimed? PR up? maintainer comments?) and record the check date. | Create the issue folder (§2.2) from the templates in §8. Create `<repo>/CLAUDE.md` and `<repo>_concepts/` if this is the first issue in that repo. Add a row to the root `README.md` table and update the scouting tracker. |
| **3. Explore** | Read the issue, the thread and the source. Timothy asks questions until the problem makes sense. | `conversation/`: entry #1 is always a briefing (what the issue is, the facts, the cast of characters, control flow, findings, fit check, next steps). |
| **4. Learn** | Turn concepts Timothy is missing into lectures. | `<repo>_concepts/`, linked from the conversation. |
| **5. Experiment** | Reproduce the issue or prove a fix with the smallest running system. | `sample/` + `sample/evidence/`; 📍 entries in the conversation. |
| **6. Report** | Write up the results. | `report/`; update the issue `README.md`. |
| **7. Contribute** | Comment on the issue before substantial work (say what you plan and ask if it's wanted). Implement in a **fork cloned outside this repo**. Open the PR. | Links and outcomes go to the conversation, the issue `CLAUDE.md` status, the root `README.md` table, and the scouting tracker. |
| **8. Reflect** *(after studying or finishing an issue)* | Close the loop (§9.3). | `open_source_persona.md` §7.2 exposure map, §10.1 record, §10.2 study log; `lectures/000-pattern-catalog.md`; `private/001-observations.md`. |

**Contribution rules that apply everywhere:**

- Read the upstream `CONTRIBUTING.md`, PR template and any AI-assistant guidance before the first comment, and
  record the relevant rules in `<repo>/CLAUDE.md`.
- In PR descriptions, link to the issue folder
  (`https://github.com/Timothy-Lee-Grant/playground/tree/main/<repo>/<issue#>_<slug>`) and state the verified
  environment ("verified on .NET 10.0.302 / System.Device.Gpio 4.2.0").
- Disclose AI assistance per the upstream project's policy. If the project has no policy, disclose briefly anyway.
- One active upstream PR at a time.

---

## 6. Working with Claude sessions (desktop and CLI)

**Starting a session on an issue** (e.g. `claude` in the issue folder, or "let's continue on iot#2403"):

1. Read this file, then `<repo>/CLAUDE.md`, then the issue's `CLAUDE.md`.
2. Read `conversation/001-conversation-log.md` **top to bottom**. "Where we are now" says where to pick up.
3. Re-check the upstream issue for new activity if the last check is more than a few days old.
4. Say what you understand the current state to be before changing anything.

**Ending a session:** append a 📍 Progress entry to the conversation and update "Where we are now". Update the
issue `CLAUDE.md` status and the trackers if anything moved. If an issue was studied to a decision or finished,
do the Reflect step (§9.3).

**What Claude may write:** anything in `scouting/`, `<repo>/` (including code in `sample/`), `lectures/`, these
orientation docs, and `private/` (following `private/README.md`). **Nothing** in `hand_experiments/` except reviews
and teaching docs its `CLAUDE.md` allows. Update `persona.md` and `open_source_persona.md` when something durable
and public-safe is learned about Timothy; put evaluations and personal context in `private/` instead (§9.2).

---

## 7. Conventions

- **File naming:** `NNN-title.md`, a three-digit sequence per folder (`001`, `002`, ...), oldest first. Put a short
  metadata block under the title (date, related issue, what the file is).
- **Issue folder names:** `<issue#>_<slug>` where the slug is a short lowercase description. (Existing: `yarp/1764_websocket_idle_timeout`
  uses snake_case; `iot/2403_gpiopin-event-handler-...` uses kebab-case. Either is fine; keep the issue number first.)
- **Dates:** absolute (`2026-09-27`), never "today" or "last week". Say when an upstream fact was checked.
- **Verified vs. unverified:** a claim is verified only if a saved evidence file shows it. Label everything else.
- **Public vs. private:** everything outside `private/` is public on GitHub. Apply the test in §9.2 to every line
  about Timothy. Issue `README.md` files and reports are written for a maintainer audience: neutral, factual, short.
- **Keep the indexes current:** root `README.md` issues table, scouting tracker, `<repo>/CLAUDE.md` issue list,
  each issue's `CLAUDE.md` status, each concepts `README.md`.
- **Process findings count.** When reviewing Timothy's work, include process observations (time between runs, was
  there a walking skeleton, did he get stuck digging) alongside the technical ones. Background:
  `hand_experiments/lectures/engineering-practice/`.

---

## 8. Templates

### 8.1 Issue `README.md` (public landing page)

```markdown
# <repo>#<issue>: <short title>

**Upstream issue:** <link> · **Related PR:** <link or "not yet"> · **Status:** <one line, dated>

## Claim
What this folder demonstrates, in one or two sentences. ("Nothing verified yet" is a valid claim.)

## Where to look
- Full write-up: `report/NNN-lab-report-*.md`
- Run it yourself: `sample/README.md`
- Raw output: `sample/evidence/`

## Environment
OS · `dotnet --version` · package versions · upstream commit/tag
```

### 8.2 Issue `CLAUDE.md` (orientation)

```markdown
# <repo>#<issue>: <short title>

## What this is
The issue in plain words, and why it was picked (link to the scouting entry).

## Read this first
Order for a new session: this file → conversation log top to bottom.

## Upstream facts (checked YYYY-MM-DD)
State, labels, assignee, maintainer comments that matter, related issues/PRs.

## Upstream rules
CONTRIBUTING notes, AI-disclosure rule, PR template quirks, breaking-change policy.

## Where things are
Folder-by-folder. Upstream source files that matter. Where the fork is cloned (path on Timothy's machine).

## Status
Dated bullet log: what's verified, what's posted upstream.

## Next steps
Ordered; each step should produce something visible.
```

### 8.3 Conversation log

Header, "Where we are now" and Index as described in §4.1. Copy the top of
`iot/2403_*/conversation/001-conversation-log.md` as the model.

### 8.4 Lab report

```markdown
# Lab Report NNN: <title>

| | |
|---|---|
| Issue | <link> |
| Author | Timothy Grant (with Claude) |
| Date | YYYY-MM-DD |
| Upstream version tested | <commit/tag, package versions> |
| Status | draft / final |

## Abstract
Five sentences: problem, method, key result, conclusion, what was contributed.

## 1. Background
The system, the concepts needed, and the issue as reported. Link concept lectures rather than repeating them.

## 2. Question and hypotheses
The precise question, and each hypothesis stated so an experiment can prove it wrong.

## 3. Apparatus and environment
Hardware, OS, SDK, package versions, upstream commit. Diagram of the setup.

## 4. Method
One subsection per experiment (E1, E2, ...): setup, procedure (exact commands), what was measured, controls.

## 5. Results
Per experiment: observed output (tables, figures), evidence file links, expected vs. observed.

## 6. Discussion
What the results mean, how they answer the question, surprises, corrections to earlier beliefs.

## 7. Conclusion

## 8. Limitations and threats to validity
What wasn't tested (drivers, hardware, OS) and why it might matter.

## 9. Upstream contribution
What was posted (comments, PRs), with links and outcomes.

## References

## Appendix
Full logs, extra figures, code listings.
```

### 8.5 Concept lecture

```markdown
# Lecture NNN: <concept>

> Prompted by: <repo>#<issue> · Date: YYYY-MM-DD · Prerequisites: <lectures>

## What problem does this solve?
## The cast of characters
## How they interact (diagram)
## Control flow, step by step
## In the real code (file and type references)
## Edge cases and gotchas
## Common mistakes
## Interview relevance
## Real-world production usage
## Check yourself (questions)
```

---

## 9. The growth system

### 9.1 The loop

```
        ┌──────────────────────────────── WHO I AM / WHERE I'M GOING ─────────────────────────────┐
        │ public:  persona.md (how I learn) · open_source_persona.md (goals, strengths, scoring,    │
        │          machines & hardware, exposure map, record, study log, preference signals)        │
        │ private: private/persona_private.md (full picture) · private/001-observations.md (log)    │
        └───────────────┬──────────────────────────────────────────────────────────────▲──────────┘
                        │ informs                                                        │ updates
                        ▼                                                                │
   SCOUT: score issues on exposure value,              REFLECT (§9.3): study-log row, exposure map,
   strength fit, feasibility (machines/hardware),      pattern catalog, contribution record,
   scope, career signal ─► scouting/NNN-*.md            private observations; monthly review
                        │                                                                ▲
                        ▼                                                                │
   WORK THE ISSUE: conversation/ ─► concepts lectures ─► sample/ + evidence ─► report/ ─► comment / PR
```

Every issue should leave three things behind: **a public action** (a comment, a reproduction or a PR), **at least
one concept Timothy can now explain**, and **at least one architecture pattern** added to the catalog.

### 9.2 Public vs. private: the one rule

> **Would Timothy be comfortable if a hiring manager read this line on his public GitHub?**
> Yes → a public file. No, or "rather not" → `private/`.

| Public (tracked, pushed) | Private (`private/`, gitignored) |
|---|---|
| Goals, target areas, projects, skills demonstrated, contribution record | Weaknesses and gaps phrased as weaknesses, AI evaluations of how he works |
| How-to-teach-me *instructions* ("use diagrams", "running system first") | The *reasons* behind them (health, cognition, feedback from colleagues) |
| Machines and hardware inventory, time budget | Wellbeing, family, employer specifics, salary, interview outcomes |
| Exposure map, study log, pattern catalog | Observation log, monthly reviews, growth plans |

When in doubt, write it in `private/`. It can always be made public later; the reverse can't be undone (git history).

### 9.3 Reflect: closing the loop after each issue

Do this when an issue has been studied to a decision (pursue / park / skip) and again when it's finished:

1. **Study log:** add or update a row in `open_source_persona.md` §10.2. Ask Timothy for the two 1–5 ratings
   (interest, growth). Don't guess them.
2. **Exposure map:** update §7.2 (area, issue, depth: Read → Reproduced → Contributed → Can explain).
3. **Contribution record:** §10.1 for anything posted upstream.
4. **Pattern catalog:** add the architecture patterns and practices this repo showed you to
   `lectures/000-pattern-catalog.md`.
5. **Private observations:** append dated rows to `private/001-observations.md` §7 (Observed / Inferred), covering
   the process as well as the result.
6. **Preference signals:** if a pattern is emerging in what Timothy found interesting, update §10.3.

### 9.4 Cadence

| When | What |
|---|---|
| Every working session | 📍 entry in the issue conversation log |
| End of each issue (studied or finished) | Reflect (§9.3) |
| Monthly (next: late October 2026) | `private/reviews/YYYY-MM.md` from the template in `private/README.md`; then a new scouting pass weighted by the study log, exposure-map gaps and preference signals |
| When hardware, machines or goals change | Update `open_source_persona.md` §8 (machines and hardware) or §2/§4 (goals and venues) |

### 9.5 Guardrails (so the system serves the work)

- **The system exists to produce public work and learning, not documents.** The monthly scoreboard tracks public
  actions next to planning documents; if planning outpaces public actions, say so plainly.
- **Keep the structure stable.** Change it only at the monthly review, and only when something clearly didn't
  work. Park ideas in the review's "changes to the system" section until then.
- **Studying counts** (📖 Learn items are real progress), but pair roughly every two Learn items with one
  Contribute action, even a small one.

## 10. Open questions about the system

These aren't settled yet. Revisit them as the system matures.

1. **Migrating `yarp/1764`** to the v2 layout once #1764 is done (and moving its lecture into `yarp/yarp_concepts/`).
2. **What root `lectures/` is for** long term: cross-cutting concepts only, or retired in favor of `<repo>_concepts/`.
3. **Slug style:** snake_case vs kebab-case for issue folders.
4. **Whether `scouting/` should also split per upstream repo** once there are several shortlists.
5. **Public git history:** candid self-assessment was in the public `persona.md` (and in
   `hand_experiments/lectures/engineering-practice/002`, `003` and `hand_experiments/CLAUDE.md`) from 2026-08. It's
   been moved out of `persona.md`, but it stays in the GitHub history until the history is rewritten or the repo is
   made private. Timothy to decide.
