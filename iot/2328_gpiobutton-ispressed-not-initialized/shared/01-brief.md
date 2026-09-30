# 01 — Brief: dotnet/iot#2328

> Owner: desktop Claude. CLI: read-only. Facts checked 2026-09-29/30 against `main` @ `1eb0b2f` (2026-09-11).
> Full desktop briefing: `exercises/scouting/conversation/001-conversation-log.md` entry #1 §1.

## 1. The issue in one paragraph

[dotnet/iot#2328](https://github.com/dotnet/iot/issues/2328) (open, `bug`, `up-for-grabs`, Priority 3; assigned to
raffaeler as area owner; no PR). After constructing a `GpioButton`, `IsPressed` is `false` even if the button is
already held down. Cause: `ButtonBase.IsPressed` starts `false` and changes only inside
`HandleButtonPressed/Released`, which run on pin **edges**; a level that exists at startup produces no edge.

## 2. The cast

| Character | Job | File |
|---|---|---|
| `GpioController` | The pin switchboard: open, read, edge callbacks | `src/System.Device.Gpio/` |
| `GpioDriver` | Talks to hardware under the controller. **The seam tests replace** | `src/System.Device.Gpio/` |
| `GpioButton` | Wiring translator: pull-up/down → pressed/released; subscribes to edges in its **constructor** | `src/devices/Button/GpioButton.cs` |
| `ButtonBase` | Hardware-independent brain: `IsPressed`, debounce, holding timer, raises `ButtonDown/ButtonUp/Press/DoublePress/Holding` | `src/devices/Button/ButtonBase.cs` |
| `TestButton` | Test subclass that calls the handlers directly (bypasses `GpioButton`) | `src/devices/Button/tests/TestButton.cs` |

## 3. What's actually broken (unverified: from reading the code; WO-2 proves it)

| Held at startup, then released… | Result |
|---|---|
| debounce off (default) | `ButtonUp` + `Press` fire with **no `ButtonDown` before them** |
| debounce on | `HandleButtonReleased` begins `if (_debounceTime.Ticks > 0 && !IsPressed) return;` → **release swallowed**: no `ButtonUp`, no `Press` |

## 4. Fix sketch (not decided)

In the `GpioButton` constructor, after `OpenPin`, read the pin once and set `IsPressed` from the active level:
pull-up → pressed = `Low`; pull-down → pressed = `High` (`_eventPinMode` records the wiring even when
`hasExternalResistor` makes the pin mode plain `Input`). **Set state only; raise no events** (raising `ButtonDown`
here would recreate #1715).

## 5. Open design question (needs maintainer input; don't decide it)

Issue [#1715](https://github.com/dotnet/iot/issues/1715) says the pin voltage hasn't settled right after the pull-up
is enabled (RC time), causing a false startup `Press`. So a `Read()` immediately after `OpenPin` may see the
unsettled level. Options: (a) read immediately; (b) short settle delay then read; (c) read lazily on first event
subscription, which is #1715's agreed fix (move the native subscription into the events' `add` accessors).

## 6. Coordination: PR #2608

pgrawehr (maintainer) opened [PR #2608](https://github.com/dotnet/iot/pull/2608) on 2026-09-27: "The button tests are
still flaky". It changes `ButtonBase.cs`, `GpioButton.cs`, `ButtonTests.cs`, `TestButton.cs` (adds an injectable
time source replacing `DateTime.UtcNow`). It does not touch `IsPressed` init. **Expect conflicts; plan to base the
branch on #2608 or rebase after it merges** (decision pending, see `02`).

## 7. How to test without hardware

- Button tests today: 8 `[Fact]`s in `ButtonTests.cs`, all through `TestButton`. Nothing tests `GpioButton`.
- Pattern to copy: `src/devices/Ili934x/tests/MockableGpioDriver.cs` (abstract `GpioDriver` exposing
  `FireEventHandler`, `OpenPinEx`, ...) + `new Mock<MockableGpioDriver>(MockBehavior.Loose)` +
  `new GpioController(mock.Object)`. Moq is available to all device tests via `eng/Versions.external.props`.
- Tests to write: pull-up + Low → pressed; pull-up + High → not pressed; pull-down mirror; external resistor;
  held-at-startup + debounce → release raises `ButtonUp`.

## 8. Repo rules and facts

- Target `net8.0`. `global.json`: SDK 9.0.306, `rollForward: major` (SDK 10 works).
- One test project: `dotnet test src/devices/Button/tests/` (full `./build.sh` takes 30–45 min; avoid).
- PR: first line `Fixes #2328`; describe the problem; **avoid force-pushing during review**.
- `.github/copilot-instructions.md`: no breaking API changes without maintainer direction; add unit tests for
  logic isolatable from hardware. `IsPressed` is public with a public setter; changing its initial value is a
  behavior change worth one sentence in the PR.
- Upstream comment draft (Timothy posts it): see `exercises/scouting/002-code_pr_shortlist_week_of_2026-09-29.md`,
  plus the §3 swallowed-release finding and the §5 question.
