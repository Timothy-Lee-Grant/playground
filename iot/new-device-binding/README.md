# dotnet/iot: new binding for the Bosch BMP390 / BMP388 pressure sensor

**Upstream issue:** not posted yet (proposal-stage) · **Related PR:** not yet ·
**Status:** 2026-10-05: implementation plan written; nothing built, run or posted yet

## Claim

None yet. This folder plans and will record a new `Iot.Device.Bmp3xx` binding for
[dotnet/iot](https://github.com/dotnet/iot): why it's worth adding, which device, how it's built and tested.

## Where to look

| File | What it is |
|---|---|
| [`001-should-i-add-a-sensor-binding.md`](001-should-i-add-a-sensor-binding.md) | Would maintainers welcome a new binding? (evidence from past PRs) |
| [`002-choosing-the-device.md`](002-choosing-the-device.md) | Candidate sensors compared; why BMP390/BMP388 |
| [`003-what-youd-build-and-the-steps.md`](003-what-youd-build-and-the-steps.md) | Overview of the binding and the steps |
| [`shared/plan.md`](shared/plan.md) | The detailed implementation plan (decisions, step-by-step) |
| [`shared/01-brief.md`](shared/01-brief.md) | The chip's register facts and the repo's constraints |
| [`shared/evidence/`](shared/evidence/), [`sample/`](sample/) | Build/test output and the hardware probe (once they exist) |
| [`evidence/`](evidence/) | Raw research behind 001–003 |

## Environment

To be recorded with the first evidence run (macOS, .NET SDK, Raspberry Pi model, dotnet/iot commit).
