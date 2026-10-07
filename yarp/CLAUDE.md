# dotnet/yarp: project orientation (desktop Claude)

> Shared context for YARP work. **CLI sessions:** your instructions are in [`current_context/`](current_context/),
> not here; you can ignore this file.
>
> **Since 2026-10-07 the YARP work runs on the project setup Timothy designed:** a project folder (on his SSD)
> where the CLI starts, holding static entry files, one `link` symlink to this folder, and the single clone of his
> fork. Everything that changes lives here, in `current_context/`, and both desktop and the CLI keep it current.

## 1. What's in this folder

| Path | What it is |
|---|---|
| [`current_context/`](current_context/) | **The living configuration**: operating manual, layout, upstream facts, branch registry, Timothy's profile, his YARP competency, lecture specs, procedures behind the slash commands, git hooks, issue template. Start with its [`README.md`](current_context/README.md) |
| [`static_files/`](static_files/) | Master copies of the project root's static files (`CLAUDE.md`, `.claude/`) + [`SETUP.md`](static_files/SETUP.md), the commands Timothy runs to build the project folder |
| [`issues/`](issues/) | One folder per issue. [`275_remove-autofac-from-tests/`](issues/275_remove-autofac-from-tests/): #275, building privately (moved here 2026-10-07). `1234_…`, `1235_…`: Timothy's placeholder examples (empty) |
| [`shared/`](shared/) | Project-level desktop ↔ CLI mailbox (proposals, questions, setup notes) |
| [`yarp_scouting/`](yarp_scouting/) | Searches for the next YARP issue |
| [`yarp_concepts/`](yarp_concepts/) | Long-lived concept lectures (reading + `audio/`) |
| [`1764_websocket_idle_timeout/`](1764_websocket_idle_timeout/) | #1764 docs PR (legacy v1 layout). **Stays here**: its files are linked from a public docs PR |
| `_to_delete/` | Leftovers from the 2026-10-07 restructuring (old template pieces). Safe to delete |

## 2. Desktop duties in this setup

- **Before planning or teaching:** read `current_context/README.md`, `05-timothy.md`, `06-competency-yarp.md`;
  for an issue, its `CLAUDE.md`, `shared/plan.md`, `shared/STATUS.md` and newest session report.
- **New issue:** `bash current_context/new-issue.sh <issue#> <slug> "<title>"`, add the branch row to
  `current_context/04-branches.md`, then brief + plan (Stages 1–3) → G1.
- **After a CLI session:** read STATUS + the session report; file the "Timothy's learning" section into
  `exercises/private/002-learner-model.md` §7 (private stays private: never copy evaluations into
  `current_context/`); add a row to `../ai-workflow/interaction-modes.md` §4.
- **Changing the rules:** edit `current_context/` and log it in its `README.md` changelog. If a change needs the
  static files to change too, edit `static_files/` and tell Timothy to re-copy (SETUP.md "Later").

## 3. Facts about YARP

In [`current_context/03-yarp-project.md`](current_context/03-yarp-project.md) (what it is, source map, build/test,
contribution rules, people), so desktop and CLI share one copy.
