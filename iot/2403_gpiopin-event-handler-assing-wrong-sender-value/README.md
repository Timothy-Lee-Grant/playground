# dotnet/iot#2403: `GpioPin` event handlers receive the wrong `sender`

**Upstream issue:** https://github.com/dotnet/iot/issues/2403 · **Related PR:** not yet ·
**Status:** investigating (started 2026-09-27); nothing verified by a run yet

## Claim

None yet. This folder will show, with runnable code and captured output, which object each `GpioDriver` passes as
`sender` to `ValueChanged` / `RegisterCallbackForPinValueChangedEvent` handlers, compared with the expected
behavior stated in the issue's triage (the object the handler was registered on).

## Where to look

| | |
|---|---|
| Full write-up | `report/` (written once there are results) |
| Run it yourself | [`sample/README.md`](sample/README.md) |
| Raw output | `sample/evidence/` |

## Environment

To be recorded with the first evidence run (OS, `dotnet --version`, `System.Device.Gpio` /
`Iot.Device.Bindings` versions, dotnet/iot commit).
