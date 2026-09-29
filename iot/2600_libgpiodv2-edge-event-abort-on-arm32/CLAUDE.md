# dotnet/iot#2600: `LibGpiodV2Driver` aborts the process on 32-bit ARM (null edge event)

## Read this first

You're in an issue workspace that uses the v2 layout (root [`../../CLAUDE.md`](../../CLAUDE.md) §2). For a new
session, read in this order:

1. Root [`../../CLAUDE.md`](../../CLAUDE.md) (workflow, conventions) and [`../../persona.md`](../../persona.md) (how Timothy learns).
2. [`../CLAUDE.md`](../CLAUDE.md): the dotnet/iot project: source map, build/test, contribution rules.
3. This file.
4. [`conversation/001-conversation-log.md`](conversation/001-conversation-log.md), **top to bottom**. Its "Where we
   are now" box says where to pick up.

At the end of a session: append a 📍 Progress entry to the conversation log, update its "Where we are now" box,
and update **Status** below.

## What this is

[dotnet/iot#2600](https://github.com/dotnet/iot/issues/2600): on a Raspberry Pi 2 (armv7l), a process watching a pin
with `LibGpiodV2Driver` is killed by a native `abort()` (`gpiod_edge_event_copy: Assertion 'event' failed`). The
likely root cause (proposed in the thread by wolfgang-knobloch, 2026-09-23) is an **ABI mismatch**: the V2 binding
declares C `unsigned long` as C# `ulong`, which is wrong on 32-bit ARM. It's item **C** in
[`../../scouting/001-issue_shortlist_sept_2026.md`](../../scouting/001-issue_shortlist_sept_2026.md), picked as a
native-interop bug that matches Timothy's firmware background. The scouting entry predates PR #2601, so the scope
changed: see conversation #1 §10.

## Upstream facts (checked 2026-09-29)

- Open. Label `untriaged`. **Assigned to krwq and Copilot.** Opened 2026-08-19 by kai-melchior.
- **PR #2601** (Copilot agent for krwq, opened 2026-08-20): null check in `EdgeEventBuffer.GetEvent`, loop bounded by
  `GetNumEvents()`, and disposal of the event copies. Approved twice by raffaeler; auto-merge enabled; **not merged**; last
  activity 2026-08-27. It does **not** change the P/Invoke types.
- pgrawehr (2026-09-17, triage): will try to reproduce; didn't understand how the handle could become invalid.
- wolfgang-knobloch (2026-09-23): root cause is `ulong` vs C `unsigned long` on ARM32 (AAPCS even-register rule);
  proposes `nuint` throughout the V2 binding. **No reply yet.**
- Same bug family in V1: #2604 / PR #2605 (32-bit `time_t` layout), open.

## Upstream rules that apply

- Don't compete with #2601: comment on #2600 first and offer the root-cause fix as a follow-up.
- The fix is **internal only**, so not a breaking change (see `../CLAUDE.md` §5 on breaking changes).
- PR description starts with `Fixes #2600` (or `Contributes to #2600` if #2601 closes the issue first); avoid
  force-pushing during review.
- Unit tests for anything testable without hardware; `.github/copilot-instructions.md` asks for that too.
- No explicit AI-disclosure rule upstream; disclose briefly anyway.

## Where things are

| Path | Contents |
|---|---|
| `README.md` | Public landing page for maintainers (claim, status, where to look). |
| `conversation/001-conversation-log.md` | The linear log: briefing, questions and answers, progress, decisions. |
| `sample/` | Experiments E1–E4 (planned in conversation #1 §11). See `sample/README.md`. |
| `sample/evidence/` | Raw run output (none yet). |
| `report/` | The lab report, once there are results. See `report/README.md`. |

**Upstream files that matter** (paths in dotnet/iot, under `src/System.Device.Gpio/`):
`Interop/Unix/libgpiod/V2/Binding/Interop.libgpiod.cs` (the declarations),
`Interop/Unix/libgpiod/V2/Proxies/EdgeEventBuffer.cs` (`GetEvent`), `.../Proxies/EdgeEvent.cs` (seqno getters),
`.../Proxies/LineSettings.cs` and `LineInfo.cs` (debounce), `.../Proxies/LibGpiodProxyFactory.cs` (buffer capacity 10),
`.../Proxies/LibGpiodProxyBase.cs` (`CallLibgpiod` wraps exceptions in `GpiodException`),
`.../V2/Binding/Handles/EdgeEventNotFreeable.cs`,
`System/Device/Gpio/Drivers/LibGpiodV2EventObserver.cs` (`HandleEdgeEventsOfRequestInLoop`).
Tests: `src/System.Device.Gpio.Tests/LibGpiodV2DriverTests.cs`.
libgpiod: `include/gpiod.h`, `lib/edge-event.c` (the `assert`).

**Fork:** not cloned yet (see `../CLAUDE.md` §6).

## Status

- 2026-09-29: Workspace created. Issue, thread, PR #2601 and source researched. Briefing written as conversation
  entry #1, including a table of every `unsigned long`/`size_t` mismatch in the V2 binding. All findings
  **unverified** (from reading source, not from runs).
- 2026-09-29: **Scope correction:** the safety-net part is already covered by PR #2601; the open slice is the
  root-cause `nuint` fix plus a signature test.
- Nothing posted upstream.

## Next steps

1. Timothy reads conversation #1 and asks questions (entries #2+).
2. Confirm what 32-bit ARM target is available (Pi on a 32-bit OS, or Docker arm/v7 emulation).
3. Build E1 (walking skeleton on the Mac), then E2 (the same binaries on ARM32). Capture evidence.
4. Decide the route (conversation #1 §10); if A, comment on #2600 with the evidence.
