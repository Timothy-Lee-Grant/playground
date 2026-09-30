# dotnet/iot#2328: `GpioButton.IsPressed` not initialized (desktop orientation)

## Read this first

This issue uses the v2 layout (root [`../../CLAUDE.md`](../../CLAUDE.md) §2) **plus the AI workflow**
([`../../ai-workflow/README.md`](../../ai-workflow/README.md)): development happens in Claude Code (the CLI) on a
fork, and the two sessions talk only through [`shared/`](shared/).

For a new **desktop** session, read in this order:

1. Root [`../../CLAUDE.md`](../../CLAUDE.md), [`../../persona.md`](../../persona.md), [`../CLAUDE.md`](../CLAUDE.md).
2. This file.
3. [`shared/STATUS.md`](shared/STATUS.md) and the newest file in [`shared/sessions/`](shared/sessions/) (what the CLI did).
4. [`conversation/001-conversation-log.md`](conversation/001-conversation-log.md), top to bottom.

The CLI never reads this file; it reads `shared/00-start-here.md` (via `workspace/CLAUDE.md`).

## What this is

[dotnet/iot#2328](https://github.com/dotnet/iot/issues/2328): `ButtonBase.IsPressed` starts `false` and only changes
on pin edges, so a button already held at startup is misreported. Picked in
[`../../scouting/002-code_pr_shortlist_week_of_2026-09-29.md`](../../scouting/002-code_pr_shortlist_week_of_2026-09-29.md)
as the first code PR (target: PR opened by Sun 2026-10-04). Full briefing:
[`../../scouting/conversation/001-conversation-log.md`](../../scouting/conversation/001-conversation-log.md) entry #1 §1.
The condensed version the CLI works from is [`shared/01-brief.md`](shared/01-brief.md).

## Where things are

| Path | Contents | Owner |
|---|---|---|
| `conversation/` | Desktop conversation log (Timothy ↔ desktop Claude) | desktop |
| `shared/00–04` | Operating agreement, brief, decisions, work order, interaction mode | desktop / Timothy |
| `shared/STATUS.md`, `shared/sessions/`, `shared/evidence/` | CLI state, session reports, test output | CLI |
| `workspace/` | The CLI workspace's `CLAUDE.md`, settings, `/handoff` command, `setup.sh` | desktop |
| `sample/` | Experiments outside the fork (e.g. a Pi console app with a real button), if needed | either |
| `report/` | Lab report, once there are results | desktop |

**CLI workspace:** `~/Desktop/projects/oss-work/iot-2328/` (fork at `develop/iot`, origin
`Timothy-Lee-Grant/iot`, upstream `dotnet/iot`).

## Status

Set up 2026-09-30. Upstream comment not posted yet. Fork not cloned yet (run `workspace/setup.sh`).

## Desktop duties after each CLI session

1. Read `shared/STATUS.md` + the newest session report.
2. File its "Timothy's learning" section into `private/002-learner-model.md` §7.
3. Add its mode scores to `../../ai-workflow/interaction-modes.md` §4.
4. Record any decisions Timothy made in the conversation log; write the next work order in `shared/03-next.md`.
