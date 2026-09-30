# dotnet/iot#2328: `GpioButton.IsPressed` is wrong when the button is already held at startup

**Upstream issue:** https://github.com/dotnet/iot/issues/2328 · **Status:** set up 2026-09-30; nothing posted
upstream or verified by a run yet

## Claim

None yet. From reading the code (unverified): `IsPressed` starts as `false` and only changes on pin edges, so a
button held down at startup is reported as released, and with debounce enabled its first release is swallowed
(no `ButtonUp`, no `Press`). This folder will prove that with unit tests against a mocked GPIO driver.

## Where to look

| | |
|---|---|
| Development log (CLI sessions) | [`shared/sessions/`](shared/sessions/) and [`shared/STATUS.md`](shared/STATUS.md) |
| Test output | [`shared/evidence/`](shared/evidence/) |
| Full write-up | `report/` (once there are results) |

## Environment

To be recorded with the first evidence run.
