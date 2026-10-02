# dotnet/iot: project orientation

Everything in `iot/` is work on **[dotnet/iot](https://github.com/dotnet/iot)**. This file is the shared context
for every issue in this folder: what the upstream project is, how its code is laid out, how to build and test it,
and how it takes contributions. The repo-wide workflow and conventions are in the root [`../CLAUDE.md`](../CLAUDE.md);
read that first.

> Facts about upstream were checked on **2026-09-27** against `main` @ `1eb0b2f` (2026-09-24). Re-check anything
> that matters before relying on it.

---

## 1. What's in this folder

| Path | What it is |
|---|---|
| `CLAUDE.md` | This file. |
| [`iot_concepts/`](iot_concepts/) | Lecture notes on IoT and dotnet/iot concepts (GPIO, drivers, .NET events, Linux GPIO APIs, ...), reusable across issues. Index in its `README.md`. |
| [`2403_gpiopin-event-handler-assing-wrong-sender-value/`](2403_gpiopin-event-handler-assing-wrong-sender-value/) | [#2403](https://github.com/dotnet/iot/issues/2403): `GpioPin` event handlers receive the wrong `sender`. |
| [`2600_libgpiodv2-edge-event-abort-on-arm32/`](2600_libgpiodv2-edge-event-abort-on-arm32/) | [#2600](https://github.com/dotnet/iot/issues/2600): `LibGpiodV2Driver` aborts the process on 32-bit ARM (`ulong` vs C `unsigned long`). |
| [`2328_gpiobutton-ispressed-not-initialized/`](2328_gpiobutton-ispressed-not-initialized/) | [#2328](https://github.com/dotnet/iot/issues/2328): `GpioButton.IsPressed` wrong when held at startup. First issue on the desktop + CLI workflow ([`../ai-workflow/`](../ai-workflow/)). |

### Issues in this project

| Issue | Folder | Scouting item | Status |
|---|---|---|---|
| [#2403](https://github.com/dotnet/iot/issues/2403) `GpioPin` handlers get the wrong `sender` | `2403_gpiopin-event-handler-assing-wrong-sender-value/` | G | Exploring since 2026-09-27 |
| [#2600](https://github.com/dotnet/iot/issues/2600) LibGpiodV2 abort on 32-bit ARM | `2600_libgpiodv2-edge-event-abort-on-arm32/` | C | Exploring since 2026-09-29. Safety net already in PR #2601 (others); open slice is the `nuint` root-cause fix |
| [#2328](https://github.com/dotnet/iot/issues/2328) `GpioButton.IsPressed` not initialized | `2328_gpiobutton-ispressed-not-initialized/` | scouting 002 #1 | **Active: first code PR, target 2026-10-04.** Fix done locally; commented 2026-10-01; awaiting reply |

Other dotnet/iot candidates in [`../scouting/001-issue_shortlist_sept_2026.md`](../scouting/001-issue_shortlist_sept_2026.md):
B (#2297, Pi samples and `config.txt`), D (#2602, `GpiodException`
escapes `TryCreate`), F (#2356, MPU-6050 calibration, needs hardware), J (#2352, FT4232H, stretch).

---

## 2. What dotnet/iot is

The .NET libraries for talking to hardware from C#: GPIO pins, I2C, SPI, PWM, and 130+ drivers for sensors,
displays and motors. It runs mostly on Linux single-board computers (Raspberry Pi and friends), plus USB adapters
(FT232H, FT4222) and Arduino over Firmata. Maintained by Microsoft employees and community members under the .NET
Foundation, MIT-licensed.

Two NuGet packages come out of it:

| Package | Source | What it is |
|---|---|---|
| `System.Device.Gpio` | `src/System.Device.Gpio/` | The core: `GpioController`, `GpioPin`, `GpioDriver` and the Linux drivers, plus the I2C/SPI/PWM base types. |
| `Iot.Device.Bindings` | `src/devices/*` (139 folders), built via `src/Iot.Device.Bindings/` | Device drivers ("bindings") for specific chips, and extras like `VirtualGpioController` and board support. |

## 3. Source map (the parts touched so far)

```
dotnet/iot
├── src/System.Device.Gpio/System/Device/
│   ├── Gpio/
│   │   ├── GpioController.cs          The Switchboard: owns open pins, routes calls to the driver
│   │   ├── GpioPin.cs                 The Front Desk: one pin; forwards everything to the controller
│   │   ├── GpioDriver.cs              abstract base every driver implements (protected internal abstract members)
│   │   ├── PinChangeEventHandler.cs   delegate void (object sender, PinValueChangedEventArgs e)
│   │   ├── PinValueChangedEventArgs.cs  ChangeType (Rising/Falling) + PinNumber
│   │   └── Drivers/
│   │       ├── LibGpiodV2Driver.cs + LibGpiodV2EventObserver.cs   modern Linux (libgpiod 2.x, Pi 5)
│   │       ├── LibGpiodDriver.cs + LibGpiodDriverEventHandler.cs  libgpiod 1.x
│   │       ├── SysFsDriver.cs + UnixDriverDevicePin.cs             legacy /sys/class/gpio
│   │       └── RaspberryPi3Driver.cs, RaspberryPi3LinuxDriver.cs   direct register access, falls back to an internal driver
│   ├── I2c/, Spi/, Pwm/
├── src/System.Device.Gpio.Tests/      unit tests; MockableGpioDriver.cs lets Moq fake a driver (no hardware)
├── src/devices/
│   ├── Board/VirtualGpioController.cs, VirtualGpioPin.cs   remap pins from other controllers
│   ├── Gpio/tests/VirtualGpioTests.cs                       tests for the virtual controller
│   ├── Arduino/, Mcp23xxx/, Tca955x/, Ft232H/, ...          bindings, several of which are GpioDrivers too
├── Documentation/                     CONTRIBUTING.md, Coding-guidelines.md, Devices-conventions.md, release process
├── samples/                           standalone samples that build against public NuGet
└── .github/                           PR template, copilot-instructions.md (guidance for AI agents)
```

**How a GPIO call flows:** app → `GpioPin` → `GpioController` → `GpioDriver` → kernel / chip. Events flow back up
from a driver's background watcher straight to your handler; `GpioPin` and `GpioController` aren't on the return
path (see the #2403 conversation, entry #1 §4).

## 4. Building and testing

| | |
|---|---|
| Target framework | `net8.0` (library and tests) |
| `global.json` | SDK `9.0.306`, `rollForward: major` (a newer SDK works) |
| Full build | `./build.sh --restore --build` (30–45 min; uses Arcade, needs Azure DevOps feeds) |
| All tests | `./build.sh --test` (15–30 min) |
| One test project | `./dotnet.sh test src/System.Device.Gpio.Tests/` or `dotnet test src/devices/Gpio/tests/` |
| Tests without hardware | Most unit tests mock the driver (`MockableGpioDriver` + Moq); hardware tests are separate. These run on macOS. |
| Fastest experiments | A standalone console app referencing the **public NuGet packages** (`System.Device.Gpio`, `Iot.Device.Bindings` 4.2.0). No repo build needed. |

Timothy's machine is a MacBook Air, so anything needing real GPIO needs a Raspberry Pi (or similar Linux board).

## 5. How dotnet/iot takes contributions

- **Process:** fork, branch, PR against `main`. The PR template asks for `Fixes #NNNN` as the **first line** of the
  description, a description of the problem, follow-up issues filed separately, and asks you to **avoid force-pushing** during review.
- **Guidelines:** `Documentation/CONTRIBUTING.md` (build setup), `Coding-guidelines.md`, `Devices-conventions.md`
  (for bindings).
- **AI:** no explicit AI-disclosure rule found in `CONTRIBUTING.md` or the PR template (checked 2026-09-27).
  `.github/copilot-instructions.md` tells AI agents: treat public APIs as "experimental but stable-in-spirit",
  prefer additive changes, **don't introduce breaking API changes without maintainer direction**, and add unit
  tests for logic that can be isolated from hardware. Our own rule: disclose AI assistance briefly anyway.
- **Breaking changes:** the maintainers batch these into major releases. #2341 tracked the planned breaking changes
  for v4.0. Pre-announced breaking removals have also landed in a minor (#2421 removed `PinNumberingScheme`
  between v4.0.0 and v4.1.0, after it was marked obsolete in #2358).
- **Labels seen:** `bug`, `Priority:1/2`, `untriaged` (added by bot), `up-for-grabs` (good starter issues).

### Releases

| Version | Tag date |
|---|---|
| v4.0.0 | 2025-05-09 |
| v4.1.0 | 2026-01-16 |
| v4.2.0 | 2026-03-14 (latest as of 2026-09-27; next planned: 4.3.0) |

### People seen so far

| Handle | Role as observed |
|---|---|
| krwq | Maintainer; does triage (labels, priority, "needs major release" calls) |
| pgrawehr (Patrick Grawehr) | Very active contributor/maintainer; author of the virtual GPIO controller and many driver changes |
| Ellerbach (Laurent Ellerbach) | Maintainer; co-author of the virtual GPIO controller |
| RoySalisbury | Community user; reported #2403 |
| raffaeler | Reviewer/maintainer; approved PR #2601 (#2600) |
| kai-melchior | Community user; reported #2600 |
| wolfgang-knobloch | Community user; proposed the ARM32 ABI root cause on #2600 (2026-09-23) |

## 6. Where the fork lives

Forks are cloned **outside this repo**, one CLI workspace per issue (see [`../ai-workflow/README.md`](../ai-workflow/README.md)):

| Issue | Workspace | Fork clone |
|---|---|---|
| #2328 | `~/Desktop/projects/oss-work/iot-2328/` | `develop/iot` (origin `Timothy-Lee-Grant/iot`, upstream `dotnet/iot`); created by the issue's `workspace/setup.sh` |
