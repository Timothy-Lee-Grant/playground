# dotnet/yarp#275: remove Autofac from the tests (desktop orientation)

## Read this first

v2 layout (root [`../../../CLAUDE.md`](../../../CLAUDE.md) §2) **plus the AI workflow**
([`../../../ai-workflow/README.md`](../../../ai-workflow/README.md)). Note: `yarp/1764_*` next door uses the legacy v1 layout;
this folder doesn't.

**Mode P, the default** ([`../../../ai-workflow/default-workflow.md`](../../../ai-workflow/default-workflow.md));
the living plan is [`shared/plan.md`](shared/plan.md). For a new **desktop** session: root `CLAUDE.md` +
`persona.md` → [`../../CLAUDE.md`](../../CLAUDE.md) → this file → `shared/plan.md` → [`shared/STATUS.md`](shared/STATUS.md) + newest [`shared/sessions/`](shared/sessions/) report →
[`conversation/001-conversation-log.md`](conversation/001-conversation-log.md).

## What this is

[dotnet/yarp#275](https://github.com/dotnet/yarp/issues/275) (`help wanted`, `Type: Task`, opened 2020, 0 comments):
Autofac and Moq aren't pulling their weight in the tests. Picked in scouting 002 as the queued second PR; full
briefing: [`../../../scouting/conversation/001-conversation-log.md`](../../../scouting/conversation/001-conversation-log.md)
entry #1 §2. Condensed CLI version: [`shared/01-brief.md`](shared/01-brief.md).

## Where things are

**Since 2026-10-07 this issue uses the YARP project setup** ([`../../current_context/`](../../current_context/)):
one project folder with one clone of the fork; the CLI reaches this folder as
`link/issues/275_remove-autofac-from-tests/`; the branch is registered in
[`../../current_context/04-branches.md`](../../current_context/04-branches.md). Folders: `conversation/` (desktop),
`shared/` (mailbox), `lectures/` (lectures about this issue; created 2026-10-07), `sample/`, `report/`.
`workspace/` and the old `~/Desktop/projects/oss-work/yarp-275/` workspace are **retired** (kept for the record;
don't use them). **Mac or Codespaces only** (SDK 11 RC1; the Linux desktop's CPU can't run .NET 11).

## Status

Set up 2026-09-30. **2026-10-01: switched to mode P** before any CLI session (plan v1 written; waiting for G1).
Timothy already has an **old clone of his fork with earlier changes**; plan Steps 0–1 audit it and bring it to a
standard state without losing anything. The PR waits for iot#2328's PR (one active PR at a time); building and
learning don't.

**If the clone isn't at `develop/yarp`:** `workspace/setup.sh` now refuses to make a second clone and says where
it found the old one; move it to `develop/yarp` and re-run.

## Desktop duties after each CLI session

Same four steps as `iot/2328_*/CLAUDE.md`, plus: read the new Stage 5 entries in `shared/plan.md`.
