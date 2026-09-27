# dotnet/iot#2403: `GpioPin` event handlers receive the wrong `sender`

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

[dotnet/iot#2403](https://github.com/dotnet/iot/issues/2403): when you subscribe with `pin.ValueChanged += handler`
(or `controller.RegisterCallbackForPinValueChangedEvent(...)`), the handler's `sender` is an internal object chosen
by the driver instead of the `GpioPin` (or controller) you subscribed on. It's item **G** in
[`../../scouting/001-issue_shortlist_sept_2026.md`](../../scouting/001-issue_shortlist_sept_2026.md), picked as a
small, testable bug in the core GPIO library that teaches .NET event design and backward compatibility.

## Upstream facts (checked 2026-09-27)

- Open. Labels `bug`, `Priority:2`. No assignee, milestone or PR. Last activity 2025-07-10.
- Reported by RoySalisbury from a review comment on commit `dd8e964` (PR #2368, virtual GPIO controller).
- **Maintainer triage (krwq):** sender should be the object you registered on (pin → pin, controller →
  controller). The fix is correct in principle but **breaking**, needs **a new major release** and testing across
  all drivers; current behavior "doesn't seem blocking".
- Latest release v4.2.0 (2026-03-14); next planned 4.3.0; no v5 milestone.
- Full briefing, including a per-driver table of what `sender` is today: conversation entry **#1**.

## Upstream rules that apply

- Breaking change → needs maintainer direction first (`.github/copilot-instructions.md`, and the triage comment).
  **Comment on the issue before opening a PR.**
- PR description starts with `Fixes #2403`; avoid force-pushing during review.
- Unit tests for anything testable without hardware (`src/System.Device.Gpio.Tests/`, `src/devices/Gpio/tests/`).
- No explicit AI-disclosure rule upstream; disclose briefly anyway.

## Where things are

| Path | Contents |
|---|---|
| `README.md` | Public landing page for maintainers (claim, status, where to look). |
| `conversation/001-conversation-log.md` | The linear log: briefing, questions and answers, progress, decisions. |
| `sample/` | Experiments E1–E3 (planned in conversation #1 §11). See `sample/README.md`. |
| `sample/evidence/` | Raw run output (none yet). |
| `report/` | The lab report, once there are results. See `report/README.md`. |
| `../iot_concepts/` | Lectures prompted by this issue (none yet; candidates in its README). |

**Upstream files that matter** (paths in dotnet/iot):
`src/System.Device.Gpio/System/Device/Gpio/GpioPin.cs` (the `ValueChanged` accessors),
`.../GpioController.cs` (`RegisterCallbackForPinValueChangedEvent`, `UnregisterCallbackForPinValueChangedEvent`),
`.../Drivers/LibGpiodV2EventObserver.cs`, `.../Drivers/LibGpiodDriverEventHandler.cs`,
`.../Drivers/UnixDriverDevicePin.cs` (where `Invoke(this, ...)` happens),
`src/devices/Board/VirtualGpioController.cs`, `src/devices/Board/VirtualGpioPin.cs`,
tests: `src/System.Device.Gpio.Tests/GpioControllerSoftwareTests.cs`, `src/devices/Gpio/tests/VirtualGpioTests.cs`.

**Fork:** not cloned yet (see `../CLAUDE.md` §6).

## Status

- 2026-09-27: Workspace created. Issue researched (thread, origin commit, source on `main` @ `1eb0b2f`, release
  history, precedent #2341/#2421). Briefing written as conversation entry #1. Findings, all **unverified** (from
  reading source, not from runs): every driver passes its own internal object as `sender`; the virtual controller
  path is the only one matching the triage rule; four possible separate bugs in `VirtualGpioController` /
  `VirtualGpioPin` (L1–L4).
- 2026-09-27: **Fit concern:** triage says the fix needs a major release, which the scouting entry didn't account
  for. Route decision pending (conversation #1 §10).
- Nothing posted upstream.

## Next steps

1. Timothy reads conversation #1 and asks questions (entries #2+).
2. Decide the route (A: ask first / B: build the fix anyway / C: docs-only slice / D: virtual-controller leads).
3. Write the first `iot_concepts/` lecture (probably .NET events and delegate identity).
4. Build E1 (sender pass-through) and E2 (virtual-controller leads) in `sample/`, capture evidence.
