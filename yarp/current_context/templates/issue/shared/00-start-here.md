# 00 — Start here: dotnet/yarp#{{ISSUE}} (issue-specific operating notes)

> Owner: desktop Claude (or the CLI when Timothy works without desktop). **The general rules are in
> `link/current_context/01-operating-manual.md`** (how sessions start, the branch check, the autonomous run,
> commits, gates, never-list) and they win on anything about git, branches and layout. This file holds only what
> is **specific to this issue**: extra constraints, things to watch, deviations from the defaults.

## 1. The issue in two lines

*(Desktop: what it is and what "done" looks like. Details in `01-brief.md`.)*

## 2. Issue-specific rules and traps

*(Desktop: anything the CLI must do differently here, e.g. "refactor: no test may change what it proves",
"platform: these tests are Windows-only", "don't touch public API". "None" if nothing.)*

## 3. Who owns which file in this folder

| File | Owner | Others may |
|---|---|---|
| `00-start-here.md`, `01-brief.md` | desktop (or CLI if Timothy says so) | read |
| `plan.md` | body: the planner (versioned) · entries: append-only by author | the CLI appends Stage 5 entries and **CHANGE REQUEST**s |
| `04-interaction-mode.md` | Timothy | the CLI changes the Current mode block only when he says so, and logs it |
| `02-decisions.md` | append-only, anyone | never edit an old entry |
| `STATUS.md`, `sessions/`, `evidence/` | CLI | read |
| `../lectures/` | whoever writes the lecture (CLI on `/lecture`, or desktop) | never edit a lecture Timothy has read |
