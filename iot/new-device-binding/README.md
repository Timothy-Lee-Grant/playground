# dotnet/iot: contributing a new sensor binding (investigation)

**Upstream issue:** none yet (this is a proposal-stage investigation) · **Related PR:** not yet ·
**Status:** investigated 2026-10-04; leading candidate is a Bosch **BMP390 / BMP388** binding

## Claim

Nothing verified on hardware yet. This folder records whether a new device binding would be a welcome
contribution to [dotnet/iot](https://github.com/dotnet/iot), which device to pick, and what building it involves.

## Where to look

| File | Question it answers |
|---|---|
| [`001-should-i-add-a-sensor-binding.md`](001-should-i-add-a-sensor-binding.md) | Would maintainers welcome it? What would I learn? How does it fit a software-engineering career path? |
| [`002-choosing-the-device.md`](002-choosing-the-device.md) | Which sensor, and why (candidates scored) |
| [`003-what-youd-build-and-the-steps.md`](003-what-youd-build-and-the-steps.md) | The binding's parts, the test strategy, step-by-step plan, draft proposal issue |
| [`evidence/`](evidence/) | Raw research: upstream PR survey, sensor coverage check, reference-driver facts |

## Environment

Research only: dotnet/iot `main` @ `95384e7` (2026-10-01); Bosch `BMP3_SensorAPI` @ `db4cf8e`. No code run yet.
