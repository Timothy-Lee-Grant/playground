# Sample: experiments for dotnet/iot#2403

Runnable code that shows which object arrives as `sender` in GPIO event handlers, plus the captured output of
every run. Rules for this folder: root [`../../../CLAUDE.md`](../../../CLAUDE.md) §4.2 (walking skeleton first,
pinned toolchain, evidence captured with a header, never edited after capture).

## Experiments

| ID | Question | Hardware | Status |
|---|---|---|---|
| E1 | Do `GpioPin` and `GpioController` pass the driver's `sender` straight through (never substituting themselves)? Subscribe via pin, controller, `VirtualGpioController`, `VirtualGpioPin` against a fake driver; print `sender` type and identity. | None (runs on macOS) | Planned |
| E2 | Are the virtual-controller leads L1–L4 real (second pin never fires, second handler dropped, unregister removes all, double firing)? | None | Planned |
| E3 | What does `LibGpiodV2Driver` pass as `sender` on real hardware? Jumper an output pin to an input pin and toggle. | Raspberry Pi | Optional |

Design notes for each experiment are in the conversation log (entry #1 §11 and later entries).

## Planned setup (not built yet)

- Console app(s) referencing the public NuGet packages `System.Device.Gpio` and `Iot.Device.Bindings` (pin exact
  versions; 4.2.0 is the latest as of 2026-09-27), plus a `global.json` pinning the SDK.
- A minimal fake `GpioDriver` subclass, the same trick as upstream's `src/System.Device.Gpio.Tests/MockableGpioDriver.cs`,
  so events can be fired without hardware.

## Evidence

`evidence/NNN-<experiment>-<what>.txt`, each starting with a header: date, OS, `dotnet --version`, package
versions, dotnet/iot commit if relevant, and the exact command.

| File | Experiment | What it shows |
|---|---|---|
| *(none yet)* | | |

## How to run

To be written with E1.
