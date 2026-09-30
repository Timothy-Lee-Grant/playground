# dotnet/yarp#275: remove Autofac from the tests (desktop orientation)

## Read this first

v2 layout (root [`../../CLAUDE.md`](../../CLAUDE.md) §2) **plus the AI workflow**
([`../../ai-workflow/README.md`](../../ai-workflow/README.md)). Note: `yarp/1764_*` next door uses the legacy v1 layout;
this folder doesn't.

For a new **desktop** session: root `CLAUDE.md` + `persona.md` → [`../CLAUDE.md`](../CLAUDE.md) → this file →
[`shared/STATUS.md`](shared/STATUS.md) + newest [`shared/sessions/`](shared/sessions/) report →
[`conversation/001-conversation-log.md`](conversation/001-conversation-log.md).

## What this is

[dotnet/yarp#275](https://github.com/dotnet/yarp/issues/275) (`help wanted`, `Type: Task`, opened 2020, 0 comments):
Autofac and Moq aren't pulling their weight in the tests. Picked in scouting 002 as the queued second PR; full
briefing: [`../../scouting/conversation/001-conversation-log.md`](../../scouting/conversation/001-conversation-log.md)
entry #1 §2. Condensed CLI version: [`shared/01-brief.md`](shared/01-brief.md).

## Where things are

Same as `iot/2328_*/CLAUDE.md`: `conversation/` (desktop), `shared/` (mailbox), `workspace/` (CLI config +
`setup.sh`), `sample/`, `report/`.

**CLI workspace:** `~/Desktop/projects/oss-work/yarp-275/` (fork at `develop/yarp`, origin
`Timothy-Lee-Grant/yarp`, upstream `dotnet/yarp`). **Mac or Codespaces only** (SDK 11 RC1; the Linux desktop's CPU
can't run .NET 11).

## Status

Set up 2026-09-30. Queued behind iot#2328 (one active PR at a time). The "is this still wanted?" comment is not
posted yet. Until a maintainer answers, only WO-1 (setup + read-only tour) is allowed.

## Desktop duties after each CLI session

Same four steps as `iot/2328_*/CLAUDE.md`.
