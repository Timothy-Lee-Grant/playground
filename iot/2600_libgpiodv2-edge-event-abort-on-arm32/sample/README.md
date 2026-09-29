# Sample: experiments for dotnet/iot#2600

Runnable code that shows how a C `unsigned long` argument arrives in native code when C# declares it as `ulong`
versus `nuint`, on 64-bit and 32-bit ARM, plus the captured output of every run. Rules for this folder: root
[`../../../CLAUDE.md`](../../../CLAUDE.md) §4.2 (walking skeleton first, pinned toolchain, evidence captured with a
header, never edited after capture).

## Experiments

| ID | Question | Hardware | Status |
|---|---|---|---|
| E1 | Walking skeleton: a tiny C library `probe(void* p, unsigned long index)` that prints what it receives, called from C# with `ulong` and with `nuint`. Does everything build and run, and do both declarations work on 64-bit? | The Mac (arm64) | Planned |
| E2 | The same binaries on 32-bit ARM: does the `ulong` declaration deliver a junk index while `nuint` delivers the right one? | 32-bit ARM (Pi on a 32-bit OS, or Docker `linux/arm/v7` emulation) | Planned |
| E3 | Real GPIO on 32-bit ARM with libgpiod 2.x: 4.2.0 aborts; a build with #2601 stops raising events after the first edge; a build with `nuint` works. | Pi on a 32-bit OS, jumper wire | Optional |
| E4 | A reflection test that checks every V2 `[DllImport]` signature against the C types in `gpiod.h`. | None | Planned |

Design notes for each experiment are in the conversation log (entry #1 §11 and later entries).

## Planned setup (not built yet)

- `abi_probe/`: one C file plus a build script for the host and for `arm-linux-gnueabihf`.
- A console app pinned with `global.json` (.NET 10 SDK), published for `osx-arm64` and `linux-arm`.
- E3 only: `System.Device.Gpio` 4.2.0 from NuGet, and local builds of dotnet/iot from the fork.

## Evidence

`evidence/NNN-<experiment>-<what>.txt`, each starting with a header: date, OS and architecture, `dotnet --version`,
compiler version, package versions, dotnet/iot commit if relevant, and the exact command.

| File | Experiment | What it shows |
|---|---|---|
| *(none yet)* | | |

## How to run

To be written with E1.
