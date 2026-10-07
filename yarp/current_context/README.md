# current_context: the living configuration of the YARP CLI project

> **What this folder is:** everything a Claude session needs to know about *how the YARP project works*: who the
> CLI is, how it operates, where everything is, which branch belongs to which issue, who Timothy is and how to
> teach him, and what he currently understands about YARP. Issue-specific state lives in each issue folder
> (`../issues/<issue>/`), not here.
>
> **Why it exists** (Timothy's design, 2026-10-07): the CLI starts in the project folder, where a few **static**
> files are copied in once (`../static_files/`). Static files can't keep up with a system that keeps changing, so
> they contain almost nothing except "go and read `link/current_context/`". This folder is reached through the
> `link` symlink, lives in the `exercises` repo, is versioned there, and **both desktop Claude and the CLI can
> update it**. Static entry point → dynamic configuration.

## The files

| File | What it answers | Loaded at CLI startup? |
|---|---|---|
| [`01-operating-manual.md`](01-operating-manual.md) | Who the CLI is, how a session runs, the autonomous run, gates, commits, what it must never do | **yes** |
| [`02-layout.md`](02-layout.md) | Where everything is: the project folder, the `link`, what's versioned where, path cheat sheet | **yes** |
| [`03-yarp-project.md`](03-yarp-project.md) | Upstream facts: what YARP is, source map, build and test, CI, contribution rules | on demand (`/issue` reads it) |
| [`04-branches.md`](04-branches.md) | The branch registry: issue ↔ branch ↔ what it's for; protected branches | **yes** |
| [`05-timothy.md`](05-timothy.md) | Who Timothy is and how to explain things to him | **yes** |
| [`06-competency-yarp.md`](06-competency-yarp.md) | What Timothy currently understands about YARP and the concepts under it; what he's read | on demand (before any explanation or lecture) |
| [`07-lectures.md`](07-lectures.md) | How to write the lecture types (the change; how to test and verify it; concept; audio) | on demand (`/lecture`) |
| [`procedures/`](procedures/) | The real instructions behind the slash commands: `issue`, `handoff`, `lecture`, `new-issue` | when the command runs |
| [`git-hooks/`](git-hooks/) | `commit-msg`, `pre-commit`, `pre-push` for the clone (wired in with `core.hooksPath`) | run by git |
| [`templates/issue/`](templates/issue/) + [`new-issue.sh`](new-issue.sh) | The standard issue folder and the script that creates one | when a new issue starts |

"Loaded at startup" = imported by the static `CLAUDE.md` in the project root. If the CLI ever starts without them
in context, it reads them itself and says so.

## Who may edit what

The CLI and desktop Claude both write here. Small, careful edits only; each one gets a changelog line below.

| File | Desktop Claude | CLI | Notes |
|---|---|---|---|
| `01`, `02`, `03`, `07`, `procedures/`, `templates/`, `new-issue.sh` | edits | **proposes** (writes the proposal in its session report, or edits directly if Timothy says so in the session) | The rules of the game. Changing them silently would change behavior for every issue |
| `git-hooks/` | edits | proposes only | Safety net; never weakened without Timothy |
| `04-branches.md` | edits | **edits** (adds a row when it creates a branch, updates status at `/handoff`) | Never deletes a row; finished branches move to "Done" |
| `05-timothy.md` | edits | edits only to record something **Timothy said about himself** in a session, quoted and dated | No evaluations of him here (this repo is public; candid notes go in `exercises/private/`, desktop only) |
| `06-competency-yarp.md` | edits | **edits**, evidence only (he said he read X; he explained Y correctly; he asked Z) | Never mark something understood because a document was generated |

If two sessions disagree about a rule, Timothy decides; record the decision in the changelog.

## Changelog

| Date | Who | Change |
|---|---|---|
| 2026-10-07 | desktop | v1. Created from Timothy's design: static entry files + one `link` symlink; dynamic context here; CLI waits for the issue at startup, verifies the branch before work, runs the approved plan autonomously, commits on its own (references need Timothy's OK), writes lectures on request. Builds on the iot `cli-project` (2026-10-05) and `ai-workflow/` (2026-09-30) |
