# Open-Source Workbench: working guide

This repo is where Timothy and Claude find open-source issues, work on them, and learn along the way. It used to
be a general playground; it is now organized around contributing to open source. `README.md` is the public face
(maintainers land there from PR links); this file is the working guide.

**Read `persona.md` first** before writing any lecture, concept note or review. It covers who I am, how I learn
(diagrams, tables, named/personified components, running systems before documents; never ask me to "picture"
something), and my process habits to watch for.

---

## What the repo is for

1. **Scout:** search repos for issues worth doing, and keep a dated shortlist.
2. **Explore:** read the issue, the thread and the relevant source; ask questions until the problem makes sense.
3. **Learn:** turn concepts I don't understand into lectures I can come back to.
4. **Experiment:** build the smallest running system that reproduces the issue or proves a fix, and save the
   output as **evidence**.
5. **Contribute:** comment, open the PR upstream, and link back to the issue folder as evidence.

---

## Areas

| Path | Purpose | Can AI write code here? |
|---|---|---|
| `scouting/` | Issue searches: `NNN-issue_shortlist_<month_year>.md`, one per search pass, with a progress tracker | n/a (docs only) |
| `lectures/<topic>/` | Concept lectures reusable across issues (`lectures/websockets/`, `lectures/async/`, ...) | n/a (docs only) |
| `<repo>/<issue#>_<slug>/` | One workspace per issue I've picked (e.g. `yarp/1764_websocket_idle_timeout/`) | **Yes** |
| `hand_experiments/` | Older by-hand practice projects (Kafka, Redis, Rx, RabbitMQ, ...) | **No.** Strict review-and-teach-only rules; see `hand_experiments/CLAUDE.md` |
| `private/` | Personal notes; gitignored, never pushed | n/a |
| `persona.md` | Who I am and how I learn; a living document (see below) | n/a |

---

## The workflow, stage by stage

### 1. Scout → `scouting/`

- Each search pass is a new file: `scouting/NNN-issue_shortlist_<month_year>.md`. Don't rewrite old shortlists;
  they're dated snapshots. Carry unfinished items forward into the next one.
- For each candidate, record: link, state (open / claimed / closed), intent (🛠️ contribute vs. 📖 learn-only),
  type, effort estimate, hardware needs, and why it's a fit. Then **Problem → Why it fits → How to fix → Steps →
  Done when → Watch out for**.
- Keep the file's **progress tracker** table current as items move.
- Issue state changes fast. Re-check the issue thread (still open? claimed? PR already up?) before starting
  anything, and say what date the check was made.

### 2. Pick → create the issue workspace

When an item moves from "candidate" to "working on it", create `<repo>/<issue#>_<slug>/`:

- `<repo>` is the upstream repo name in lowercase: `yarp`, `iot`, `csharp-sdk`, `opentelemetry-dotnet`, ...
- `<slug>` is a short snake_case description: `1764_websocket_idle_timeout`.
- Create `README.md` and `CLAUDE.md` from the templates at the bottom of this file. Other subfolders only get
  created when there's something to put in them.
- Add a row to the **Issues** table in the root `README.md`.
- Before any PR, read the upstream `CONTRIBUTING.md` (including its AI-disclosure rule) and note the relevant
  rules in the issue's `CLAUDE.md`.

### 3. Explore → `concept_notes/`

- An **append-only** Q&A log: my question, in my words → short answer → explanation → takeaways / next steps.
  Newest at the bottom, with an index table at the top. Don't rewrite earlier answers; if one turns out to be
  wrong, append a correction that links back to it.
- Keep entries short and focused on the issue. If an answer grows into a full lesson, it belongs in a lecture.

### 4. Learn → `lectures/`

Two places, depending on scope:

| Where | When |
|---|---|
| `<issue folder>/lectures/` | Step-by-step write-ups of *this issue's* experiments: what we ran, what happened, what it means |
| `lectures/<topic>/` (root) | A concept that will outlive the issue: WebSockets, cancellation tokens, P/Invoke struct layout, ... |

Lecture style (from `persona.md`): purpose before mechanism; high-level architecture → components → interactions
→ control flow → implementation → edge cases; personified named characters; ASCII diagrams and tables; "common
mistakes" and "interview relevance" sections. When a lecture explains upstream code, reference the real file and
type names.

### 5. Experiment → `sample/` and `sample/evidence/`

- **Walking skeleton first:** get the smallest thing running end to end before adding pieces. Get a
  build/run result early and often. Don't write a large design without running anything.
- Pin the toolchain (`global.json`, exact package versions) so a maintainer can reproduce it.
- **Evidence is captured, not remembered.** Save the raw output of every run the README relies on to
  `sample/evidence/NNN-<what>.txt`. Put a header on each file with the date, OS, `dotnet --version`, package
  versions, the upstream commit/tag if relevant, and the exact command(s).
- Label anything not backed by a saved run as **unverified**, in the README and in lectures alike.
- Always check the state you think you changed (ports, processes, env vars) before trusting a re-run.
  `lectures/001` in the YARP folder has a concrete example with a stale `dotnet run` child process.

### 6. Contribute

- Comment on the issue before substantial work: say what you plan to do and ask whether it's wanted.
- The fix itself goes in a **fork cloned outside this repo**. Upstream source never gets committed here.
- In the PR description, link to the issue folder (`https://github.com/Timothy-Lee-Grant/playground/tree/main/<repo>/<issue#>_<slug>`)
  and state the verified environment ("verified on .NET 10.0.302 / Yarp.ReverseProxy 2.3.0").
- Follow the upstream project's AI-disclosure policy.
- Record the comment/PR links and the outcome in the issue's `CLAUDE.md`, the root `README.md` table, and the
  scouting tracker.

---

## Conventions

- **Markdown naming:** `NNN-title.md`, a three-digit sequence per folder (`001`, `002`, ...), oldest first. Put a
  short metadata block under the title (date, related issue, what the file is).
- **Public vs. private.** Issue-folder `README.md` files are written for a maintainer audience: neutral, factual,
  and short. Learning notes can be personal, but anything that shouldn't be public goes in `private/`.
- **Keep indexes current:** the root `README.md` issues table, the scouting tracker, and each issue's `CLAUDE.md`
  status section.
- **`persona.md` is a living document.** Update it when you learn something worth keeping about me: new projects,
  skills I've demonstrated, patterns in how I work, changes in my goals. I use it across several projects.
- **Process findings count.** When reviewing my work, include process observations (time between runs, whether
  there was a walking skeleton, whether I got stuck digging) alongside the technical ones. See
  `hand_experiments/lectures/engineering-practice/` for background.

---

## Templates

### Issue `README.md` (public evidence page)

```markdown
# <repo>#<issue>: <short title>

**Upstream issue:** <link> · **Related PR:** <link or "not yet"> · **Status:** <one line>

## Claim
What this folder demonstrates, in one or two sentences.

## Environment
OS · `dotnet --version` · package versions · upstream commit/tag

## Setup
ASCII diagram of the pieces and how they connect.

## Reproduce
Exact commands, from a clean clone.

## Results
| Experiment | Configuration | Expected | Observed | Evidence file |

## Notes
Anything a maintainer should know (caveats, what's *not* verified).
```

### Issue `CLAUDE.md` (working notes)

```markdown
# <repo>#<issue>: <short title>

## What this is
The issue in plain words, and why I picked it (link to the scouting entry).

## Upstream rules
CONTRIBUTING.md notes, AI-disclosure rule, where docs/code live, PR template quirks.

## Structure
What's in each subfolder.

## Status
Dated bullet log: what's verified, what's posted upstream.

## Next steps
Ordered; each step should produce something visible.
```
