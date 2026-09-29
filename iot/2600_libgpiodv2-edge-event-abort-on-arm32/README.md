# dotnet/iot#2600: `LibGpiodV2Driver` aborts the process on 32-bit ARM when an edge event arrives

**Upstream issue:** https://github.com/dotnet/iot/issues/2600 · **Related PR:** [#2601](https://github.com/dotnet/iot/pull/2601)
(null-check safety net, by others) · **Status:** investigating (started 2026-09-29); nothing verified by a run yet

## Claim

None yet. This folder will test, with runnable code and captured output, the root cause proposed in the issue
thread: the V2 P/Invoke binding declares C `unsigned long` parameters as `ulong`, which is 64-bit, while C
`unsigned long` is 32-bit on ARM32. On ARM32 the argument lands in the wrong register, so libgpiod reads a garbage
index.

## Where to look

| | |
|---|---|
| Full write-up | `report/` (written once there are results) |
| Run it yourself | [`sample/README.md`](sample/README.md) |
| Raw output | `sample/evidence/` |

## Environment

To be recorded with the first evidence run (OS and architecture, `dotnet --version`, `System.Device.Gpio` version,
libgpiod version, dotnet/iot commit).
