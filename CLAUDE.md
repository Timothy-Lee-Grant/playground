# Open-Source Workbench: orientation for Claude (and humans)

This repo has two jobs:

1. **Open-source work:** find issues, understand them, reproduce them, fix them, and learn the concepts and the
   architecture behind them.
2. **A self-improving growth system:** get to know Timothy (goals, strengths, gaps, situation, hardware), choose
   issues that fit and stretch him, and turn every issue into a measurable step toward his goal (a .NET role at
   Microsoft). See §9. Part of this is a **learning loop** (§10): every session studies how Timothy learns and
   keeps improving how it teaches him.

It's published on GitHub as `Timothy-Lee-Grant/playground` (it started as a general
playground; since 2026-09-26 it's organized around open-source work). `README.md` is the public face that
maintainers land on from PR links. **This file is the working guide.** Read it before doing anything here.

**Before you write anything for Timothy** (a conversation answer, a lecture, a report), read [`persona.md`](persona.md).
**Before choosing or scouting issues**, also read [`open_source_persona.md`](open_source_persona.md).
It covers who he is (embedded/firmware engineer moving toward backend and infrastructure), how he learns
(high-level architecture first, then components → interactions → control flow → code → edge cases; personified,
named components; ASCII diagrams and tables; running systems before documents), and process habits to watch for.
**Before teaching him anything or writing a lecture**, follow §10: check the learner model and reading ledger in
`private/002-learner-model.md`, and **never assume he has read a document just because it was generated**.

> **The system is new (started 2026-09-26) and still changing.** The structure below is current as of
> 2026-09-29. If something here stops matching reality, fix this file in the same session. There's a list of open
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
├── ai-workflow/            how desktop Claude and the Claude Code CLI share work on an issue (shared/ mailbox,
│                           CLI workspace, /handoff) + the interaction-modes experiment. Read for any coding issue
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
└── private/                gitignored, never pushed: candid persona, observation log, learner model +
                            teach-backs (§10), monthly reviews (§9). Copied by Timothy into a private repo.
                            See private/README.md.
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

**Coding issues add two folders** (since 2026-09-30; see [`ai-workflow/README.md`](ai-workflow/README.md)):
`shared/` (the mailbox between desktop Claude and the CLI: brief, decisions, work order, interaction mode, CLI
status, session reports, evidence) and `workspace/` (the CLI workspace's `CLAUDE.md`, settings, `/handoff`
command and `setup.sh`). For these issues, test/build evidence from the fork goes in `shared/evidence/`;
`sample/` is only for experiments outside the fork.

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
| A lecture about **one issue's change** (mode P: what was built, why, the tests, the comment) | `<issue>/lectures/NNN-*.md` |
| A concept that isn't specific to any one upstream project | root `lectures/` |
| Code that reproduces or tests something | `sample/` |
| Raw output of a run | `sample/evidence/NNN-*.txt` |
| The final write-up of results | `report/NNN-lab-report-*.md` |
| The fix itself | a fork **outside** this repo (§5) |
| Status for a maintainer | the issue `README.md` |
| A lecture he'll **listen to** via NotebookLM (only when he asks for audio) | `<repo>/<repo>_concepts/audio/NNN-audio-*.md` + `.prompt.md` (§10.7) |
| A teach-back transcript and its analysis | `private/teachbacks/NNN-*.md` (§10.4) |
| An observation about how Timothy learns (a question, what landed, what didn't) | `private/002-learner-model.md` §7 (§10.3) |

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
(number, title, prompted by, status). Every lecture ends with a **teach-back checklist**: the 5–10 key ideas
Timothy should be able to say back in his own words. Teach-backs are scored against it (§10.4). Prefer shorter
lectures, and split long topics: whether 1,000-line lectures actually get read is an open question the
reading ledger is tracking (§10.2). Once Timothy has read a lecture, **don't edit it** (§10.6). Audio lectures for
NotebookLM follow a different format (§10.7).

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

**For coding issues, the desktop + CLI split in [`ai-workflow/README.md`](ai-workflow/README.md) applies:** the CLI
runs in a workspace outside this repo and sees only the fork and the issue's `shared/` folder (it follows
`shared/00-start-here.md`, not this file). Desktop sessions read `shared/STATUS.md` and the newest session report
before anything else, and after each CLI session do the four duties listed in the issue's `CLAUDE.md`.

**Starting a session on an issue** (e.g. `claude` in the issue folder, or "let's continue on iot#2403"):

1. Read this file, then `<repo>/CLAUDE.md`, then the issue's `CLAUDE.md`.
2. Read `conversation/001-conversation-log.md` **top to bottom**. "Where we are now" says where to pick up.
3. Re-check the upstream issue for new activity if the last check is more than a few days old.
4. Say what you understand the current state to be before changing anything.
5. Skim `private/002-learner-model.md` §1–§5 and its reading ledger (§8) before teaching or writing a lecture
   (§10). Don't assume any document has been read unless the ledger or Timothy says so.

**Ending a session:** append a 📍 Progress entry to the conversation and update "Where we are now". Update the
issue `CLAUDE.md` status and the trackers if anything moved. Append learning-log rows to
`private/002-learner-model.md` §7 for anything learning-relevant that happened (questions he asked, what landed,
techniques tried) and update its reading ledger (§10.3). If an issue was studied to a decision or finished,
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
## Teach-back checklist
The 5–10 key ideas to say back in your own words, each one line. Start with what the thing is *for*.
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
3. **Contribution record:** `open_source_persona.md` §10.1 for anything posted upstream.
4. **Pattern catalog:** add the architecture patterns and practices this repo showed you to
   `lectures/000-pattern-catalog.md`.
5. **Private observations:** append dated rows to `private/001-observations.md` §7 (Observed / Inferred), covering
   the process as well as the result.
6. **Preference signals:** if a pattern is emerging in what Timothy found interesting, update
   `open_source_persona.md` §10.3.
7. **Learning:** review the issue's teach-backs and learning-log rows; update the learner model's technique
   ledger and hypotheses (§10.6).

### 9.4 Cadence

| When | What |
|---|---|
| Every working session | 📍 entry in the issue conversation log; learning-log rows in `private/002-learner-model.md` §7 |
| Whenever Timothy sends a teach-back | Full analysis + reply (§10.4) |
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

## 10. The learning loop: how Claude teaches Timothy, and keeps getting better at it

Timothy asked (2026-09-29) that every AI session **continually study how he learns** and use it to teach him
better: what confuses him, which explanations land, what his follow-up questions show about whether a concept
stuck, and what his own explain-it-back sessions reveal. This section is the protocol. Instructions live here
(public); **all evidence and evaluation goes in `private/`** (§9.2).

```
   Claude explains ──► Timothy asks questions ──► Timothy reads (when he says so) ──► teach-back (voice, on a walk)
        ▲                      │                                                              │
        │                      ▼                                                              ▼
        │            learning-log rows (§10.3)                              analysis + reply (§10.4)
        │                      │                                                              │
        └──── technique ledger, hypotheses, lessons for FUTURE lectures (§10.6) ◄───────────────────────────┘
                         private/002-learner-model.md
```

| File (all private) | What it holds |
|---|---|
| `private/002-learner-model.md` | The living model: confusion patterns, motivation signals, **technique ledger**, open hypotheses, append-only **learning log**, **reading ledger** |
| `private/teachbacks/NNN-YYYY-MM-DD-<topic>.md` | One file per teach-back: verbatim transcript + analysis. Template in `private/teachbacks/README.md` |
| `private/persona_private.md`, `private/001-observations.md` | The background the model was seeded from; broader coaching notes |

### 10.1 Stance

- Observe as a **learning educator**: what he says, asks, gets right, and gets wrong, and what that implies about
  the teaching. Behavioral and educational observation only. **No clinical claims or diagnoses.** Don't speculate
  about neurotype unprompted (his standing instruction), and don't use it to explain things with ordinary causes.
- Tag everything **Observed** or **Inferred**, with a date and source. Keep hypotheses as hypotheses until
  evidence settles them.
- Record strengths and improvements as carefully as gaps. Candid, specific, never flattering.
- **Zero extra work for Timothy.** He talks; Claude analyzes and files.

### 10.2 Never assume a document has been read

Generating a lecture or briefing says nothing about whether Timothy has read it. He often asks for a document and
keeps working, planning to read it later.

- He'll say explicitly: **"I read X and have questions"** or **"I want to summarize X"** (a teach-back). Only then
  mark it read in the **reading ledger** (`private/002-learner-model.md` §8).
- If he asks something a document he hasn't read already covers, **answer it directly in chat.** Don't reply
  "as covered in the lecture". Point to the section if useful. Log that the question came up before reading;
  it shows what's salient to him before reading.
- Don't quiz him on unread material, and don't count it against him.

### 10.3 Continuous observation (every interaction)

After each exchange where Timothy is learning, note (privately, at session end at the latest) in the learning log:

| Look at | What it can show |
|---|---|
| **The kind of question** (purpose/why, mechanism/how, value/should-I, scope, verification) | Altitude: does he start from purpose or mechanism? |
| **The follow-up** | Builds on the answer (transfer, next level) → landed. Re-asks in other words → didn't land. Drops down a level → descending pattern |
| **The technique used** in the answer (table, cast, analogy, diagram, running code) | Feeds the technique ledger |
| **What he skipped** (questions left unanswered, decisions not made) | Friction, fatigue, or low interest; note it, don't assume |
| **Self-corrections and "that doesn't sound right" moments** | His gap detector at work; the most valuable signal |

Don't narrate this in chat each time; just do it. Mention it only when an observation should change what happens next.

### 10.4 Teach-backs

**What they are:** Timothy explains a lecture or concept back in his own words, usually **voice-to-text recorded
on a walk**: stream of consciousness, transcription errors, restarts, tangents. Talking out loud is his own
technique for finding gaps: when he can't articulate something cleanly or it sounds wrong, he knows the model is
off and reasons toward what makes sense.

**When one arrives:**

1. **Save it verbatim first** in `private/teachbacks/NNN-YYYY-MM-DD-<topic>.md`. Never clean up the transcript.
2. **Read it charitably.** Separate transcription noise ("nu int" for `nuint`) from conceptual errors. Only
   conceptual errors count.
3. **Score it** against the lecture's teach-back checklist: ✅ correct · 🟡 partial/vague · ❌ missing · ⚠️ misconception.
4. **Analyze how he reasoned:** what his first sentence was about (purpose or mechanism); where he caught his own
   gaps; whether his **repairs** (what he reasoned his way to after a gap) are right. Wrong repairs are the
   highest-value thing to catch. Also note the analogies he used unprompted (what stuck) and where he went vague.
5. **Decide whose gap it is:** the lecture's (unclear, missing, too long, wrong order) → record a **lesson for
   future lectures** in `private/002-learner-model.md` §10, and **don't edit the lecture** (§10.6); a missing
   prerequisite → suggest the next lecture; a misconception → state the exact wrong belief and the corrected one
   in the reply.
6. **Reply in chat**, in this shape:
   - one-line verdict;
   - what he got right, quoting a few of his own words;
   - gaps and misconceptions as a table: *concept · what you said · what's actually true · why it matters*;
   - the corrected model as a small diagram, if one is needed;
   - 1–3 **retrieval questions** for the next walk (optional; he chooses);
   - what I'll do differently in future lectures, if anything.
7. **Update** the learner model (log row, technique ledger, hypotheses, reading ledger) and the teachbacks index.

### 10.5 Lectures and answers: what this changes

- Every lecture ends with a **teach-back checklist** (§4.4, §8.5).
- **Before writing any lecture**, read the learner model's technique ledger (§4) and **lessons for future lectures**
  (§10), and apply them. That's where the teach-backs pay off.
- When he returns to an old topic, a **re-test** (1–2 retrieval questions a week or more later) is the best
  evidence of retention. Offer it; don't force it.

### 10.6 Self-improvement: updating the techniques

- **Every teach-back** updates the technique ledger (Effect / Confidence), the hypotheses, and the list of lessons
  for future lectures.
- **Never edit a lecture Timothy has already read** (his rule, 2026-09-29). Hunting for what changed in a document
  he's read is a poor use of his time. The teach-back's purpose is to analyze his thinking and write *better future
  lectures*. Correct misconceptions in the chat reply (and the teach-back file). If a read lecture turns out to
  contain an outright factual error, tell him in chat and let him decide.
- **When the evidence changes an instruction** (e.g. "shorter lectures retain better"), update the public
  instruction too: `persona.md` "Working With Me" and §4.4 / §8.5 here, without the private reasons.
- **Monthly review** (§9.4): add a "Learning" section: teach-backs done vs lectures generated, patterns
  improving/unchanged/resolved, hypotheses settled, technique changes made.
- **Guardrail:** the loop is judged by teach-backs that actually happen, not by documents produced. If lectures
  pile up unread, generate fewer and shorter ones, and say so plainly.

### 10.7 Audio lectures (for NotebookLM)

Some lectures are meant to be **listened to**, not read: Timothy loads them into Google **NotebookLM** and generates
an Audio Overview (a podcast-style conversation between two AI hosts) to listen to on walks.

**Trigger: only when he explicitly asks** for an audio / listening / NotebookLM / podcast lecture. A plain request
for "a lecture" always means the normal reading lecture (§4.4).

**Where:** `<repo>/<repo>_concepts/audio/NNN-audio-<topic>.md` (own numbering), plus a sibling
`NNN-audio-<topic>.prompt.md` with a suggested **NotebookLM customization prompt** (pasted into the Audio Overview
"Customize" box, **not** uploaded as a source). List both in the concepts `README.md` under "Audio lectures".

**How to write one.** The reader is a pair of AI podcast hosts, and the listener can't see anything:

| Do | Don't |
|---|---|
| Plain Markdown headings + prose paragraphs; short sentences | Tables, ASCII diagrams, emoji, status icons, checkboxes |
| A narrative arc: the problem → the characters → one event traveling through the system, step by step → why it's designed this way → mistakes → recap | Reference-style lists of facts; "see §3" or links that carry meaning |
| Introduce the **personified characters** by name and job, and keep the names consistent (they survive audio well) | "Picture this" / "imagine" (he has no voluntary imagery); describe **sequences and relationships in words** instead |
| Describe code in words: "the plus-equals operator", "the question-mark-dot operator". Spell awkward identifiers once ("GpioController, the G-P-I-O controller") | Code blocks longer than a line; symbols that only work visually |
| Explicit "the key idea is…" sentences, contrasts ("X, not Y"), and "a common misconception is… actually…" (the hosts pick these up and discuss them) | Nuance buried in parentheses |
| Analogies to embedded C and firmware | |
| A spoken recap at the end of each section and at the end, covering the same 5–10 key ideas as a teach-back checklist | |
| Say out loud what's verified and what isn't ("this hasn't been tested yet") | |

**Length:** start around 2,000–4,000 words and tune from Timothy's feedback on how the audio came out.

**Caveat for teach-backs:** the NotebookLM hosts paraphrase and can get things wrong. When a teach-back follows an
audio lecture (mode "NotebookLM audio"), check whether a misconception could have come from the podcast rather than
from him, and ask if unsure. Mark audio lectures **Listened** (not Read) in the reading ledger.

---

## 11. Open questions about the system

These aren't settled yet. Revisit them as the system matures.

1. **Migrating `yarp/1764`** to the v2 layout once #1764 is done (and moving its lecture into `yarp/yarp_concepts/`).
2. **What root `lectures/` is for** long term: cross-cutting concepts only, or retired in favor of `<repo>_concepts/`.
3. **Slug style:** snake_case vs kebab-case for issue folders.
4. **Whether `scouting/` should also split per upstream repo** once there are several shortlists.
5. **Public git history:** candid self-assessment was in the public `persona.md` (and in
   `hand_experiments/lectures/engineering-practice/002`, `003` and `hand_experiments/CLAUDE.md`) from 2026-08. It's
   been moved out of `persona.md`, but it stays in the GitHub history until the history is rewritten or the repo is
   made private. Timothy to decide.
6. **Learning loop (§10), started 2026-09-29:** is one teach-back per lecture realistic? Does lecture length need a
   hard cap? Revisit after the first three teach-backs.
