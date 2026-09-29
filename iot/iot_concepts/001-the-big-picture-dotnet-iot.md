# Lecture 001: The Big Picture. What dotnet/iot Is, How It's Built, and How to Use It

| | |
|---|---|
| **Prompted by** | Starting work on [dotnet/iot#2403](https://github.com/dotnet/iot/issues/2403). Before digging into one bug, get a map of the whole system. |
| **Date** | 2026-09-27 |
| **Upstream version described** | `main` @ `1eb0b2f` (2026-09-24); latest NuGet release **4.2.0** |
| **Prerequisites** | None. Assumes you know C, Linux on a Raspberry Pi, and what GPIO/I2C/SPI are (you do this at work). |
| **Evidence status** | Everything here comes from reading the upstream source and docs. Nothing has been run yet, so behavior claims are **unverified** until a sample in `../2403_*/sample/` backs them. Places where that matters most are marked ⚠. |

---

## 0. What you'll be able to do after this lecture

- [ ] Say in one sentence what dotnet/iot is, and where it fits next to Python's `gpiozero`, C's `libgpiod`, and .NET nanoFramework.
- [ ] Draw the layers from your `Main()` down to the Linux kernel, and name the class at each layer.
- [ ] Find your way around the upstream repo: which folder holds what, and which NuGet package it ends up in.
- [ ] Explain how `new GpioController()` picks a driver, and predict what happens on your Mac.
- [ ] Read and write pins, react to edges three different ways, and talk to an I2C/SPI device.
- [ ] Read any device "binding" (e.g. `Bme280`) and know what it's built from.
- [ ] Pick a way to run code: no hardware, a Raspberry Pi, a USB adapter on the Mac, or an Arduino.
- [ ] Name the .NET and Linux concepts you'll hit repeatedly when contributing, starting with the one #2403 is about.

---

## 1. What problem does dotnet/iot solve?

### 1.1 The one-sentence version

**dotnet/iot lets a C# program running on a Linux computer (usually a Raspberry Pi) drive hardware: GPIO pins,
I2C, SPI and PWM, plus 130+ ready-made drivers for real chips (sensors, displays, motor controllers).**

It's the same job you do in C at work with `libgpiod`, `/dev/i2c-1` and `ioctl()`, but with a typed, object-oriented
.NET API on top, and the whole .NET ecosystem (ASP.NET Core, dependency injection, `async`, NuGet) available in the
same process.

### 1.2 Why it exists

Imagine you're building a greenhouse controller. You need to read a temperature/humidity sensor over I2C, switch a
fan relay on a GPIO pin, show values on a small SPI display, and expose it all over a REST API to a phone app.

| Option | What it's like |
|---|---|
| **C + libgpiod + your own I2C code** | Full control and fast, but you write the REST server, JSON, threading and sensor math yourself. |
| **Python + gpiozero / smbus2** | Very quick to start and huge community, but slower, dynamically typed, and weaker for large long-running services. |
| **Node.js + onoff / i2c-bus** | Good for web-first projects, but the hardware libraries are thinner. |
| **C# + dotnet/iot** | Typed APIs for the hardware, 130+ chip drivers on NuGet, and ASP.NET Core, background services and DI in the same app. Costs: .NET runtime size, JIT startup, GC pauses. |
| **.NET nanoFramework** | C# on the *microcontroller itself* (ESP32, STM32), with no Linux. A **different project** with a similar-looking API. |

So dotnet/iot exists for people who want to **build the device's software the way they'd build a backend service**.
That's also why it suits your goal: it sits exactly where your firmware experience and your backend ambitions meet.

### 1.3 A short history (why some things look odd)

| When | What happened | Traces you'll still see |
|---|---|---|
| 2018 | Microsoft starts `System.Device.Gpio` with public API design reviews, targeting **Windows 10 IoT Core** and Linux. | Windows code paths in `GpioController.GetBestDriverForBoardOnWindows()` (only Pi 2/3 under Windows IoT Core); `Windows_NT` test skips. |
| 2019–2023 | Community adds hundreds of bindings; libgpiod support; Raspberry Pi register-level driver. | Many bindings of varying quality; sysfs driver still around. |
| 2021+ | Windows IoT Core fades out. Linux is effectively the platform. | `I2cDevice.Create()` and `PwmChannel.Create()` now throw on Windows. |
| 2024–2025 | **v4.0 (2025-05-09):** breaking cleanup, drops .NET Standard, targets `net8.0` only, obsoletes then removes `PinNumberingScheme`, adds `VirtualGpioController` (#2341 tracked the plan). | Some docs still say ".NET Standard 2.0" (upstream `README.md`, `Documentation/README.md`). The code says `net8.0`. **Trust the code.** |
| 2025–2026 | Raspberry Pi 5 support (new RP1 GPIO chip), libgpiod v2 driver, v4.1.0 (2026-01), v4.2.0 (2026-03). | `LibGpiodV2Driver`, Pi 5 special case in driver selection. |

The upstream README still says "experimental stage and all APIs are subject to changes." In practice the
maintainers treat the API as stable and batch breaking changes into major versions, which is exactly why #2403 is
stuck waiting for one.

---

## 2. From what you already know to dotnet/iot

You already know most of the hardware side. This table maps your C/Linux vocabulary onto the .NET classes.

| At work (C on a Pi) | dotnet/iot equivalent | Notes |
|---|---|---|
| `gpiod_chip_open("/dev/gpiochip0")` | `new GpioController()` (auto) or `new GpioController(new LibGpiodV2Driver(0))` | The controller owns a driver; the driver owns the chip. |
| `gpiod_line_request_output(line, ...)` | `controller.OpenPin(17, PinMode.Output)` | Returns a `GpioPin` object for that line. |
| `gpiod_line_set_value(line, 1)` | `controller.Write(17, PinValue.High)` or `pin.Write(PinValue.High)` | `PinValue` is a struct, `High`/`Low`. |
| bias flags (pull-up/down) | `PinMode.InputPullUp` / `PinMode.InputPullDown` | Only four modes: `Input`, `Output`, `InputPullUp`, `InputPullDown`. |
| `gpiod_line_event_wait` + `read` in a loop | `controller.WaitForEvent(...)` / `WaitForEventAsync(...)` | Blocking or async wait for an edge. |
| an interrupt thread calling your function pointer | `RegisterCallbackForPinValueChangedEvent` / `pin.ValueChanged +=` | A C# delegate instead of a function pointer. **#2403 lives here.** |
| `open("/dev/i2c-1")` + `ioctl(I2C_SLAVE, 0x76)` | `I2cDevice.Create(new I2cConnectionSettings(1, 0x76))` | Then `Read(Span<byte>)`, `Write(...)`, `WriteRead(...)`. |
| `open("/dev/spidev0.0")` + `SPI_IOC_MESSAGE` | `SpiDevice.Create(new SpiConnectionSettings(0, 0) { ClockFrequency = ..., Mode = SpiMode.Mode0 })` | `TransferFullDuplex(write, read)`. |
| writing to `/sys/class/pwm/pwmchip0/...` | `PwmChannel.Create(chip: 0, channel: 0, frequency: 400, dutyCyclePercentage: 0.5)` | Duty cycle is 0.0–1.0. |
| Your driver code for a BME280 (register map, compensation math) | `new Bme280(i2cDevice).Read()` from `Iot.Device.Bindings` | Someone already wrote the register-level code. That's what a **binding** is. |
| `close()` / `gpiod_chip_close()` | `Dispose()`, usually via `using` | Deterministic cleanup, like RAII in C++. |
| Returning `-1` or `NaN` for "no reading" | `bool TryReadX(out T value)` | A dotnet/iot convention for bindings. |

---

## 3. The layered architecture: the city map

```
 ┌──────────────────────────────────────────────────────────────────────────────┐
 │  YOUR APPLICATION           console app · ASP.NET Core service · worker        │
 └──────────────────────────────────────────────────────────────────────────────┘
                 │ uses                                  │ uses
                 ▼                                       ▼
 ┌─────────────────────────────────────┐   ┌────────────────────────────────────┐
 │ DEVICE BINDINGS                     │   │ BOARD LAYER (in Iot.Device.Bindings)│
 │ package: Iot.Device.Bindings        │   │ Board, RaspberryPiBoard,            │
 │ Bme280, Mcp23017, CharacterLcd,     │   │ GenericBoard, VirtualGpioController │
 │ Ssd1306, Dht22, ... (src/devices/*) │   │ (pin ownership, bus management)     │
 └─────────────────────────────────────┘   └────────────────────────────────────┘
                 │ built on                              │ built on
                 ▼                                       ▼
 ┌──────────────────────────────────────────────────────────────────────────────┐
 │ PROTOCOL ABSTRACTIONS       package: System.Device.Gpio                        │
 │  GpioController ─ GpioPin      I2cBus ─ I2cDevice      SpiDevice    PwmChannel │
 └──────────────────────────────────────────────────────────────────────────────┘
                 │ delegates to (Strategy pattern)
                 ▼
 ┌──────────────────────────────────────────────────────────────────────────────┐
 │ DRIVERS / PLATFORM IMPLEMENTATIONS                                            │
 │  GpioDriver subclasses: LibGpiodV2Driver · LibGpiodDriver · SysFsDriver ·     │
 │                         RaspberryPi3Driver · (bindings that are drivers too:  │
 │                         Mcp23xxx, Ftx232H, ArduinoGpioControllerDriver, ...)  │
 │  UnixI2cDevice · UnixSpiDevice · UnixPwmChannel                               │
 └──────────────────────────────────────────────────────────────────────────────┘
                 │ P/Invoke (DllImport) into native libs / syscalls
                 ▼
 ┌──────────────────────────────────────────────────────────────────────────────┐
 │ LINUX                                                                          │
 │  libgpiod.so.2/.so.3 → /dev/gpiochipN     /dev/i2c-N     /dev/spidevB.C        │
 │  /sys/class/gpio (deprecated)             /sys/class/pwm/pwmchipN              │
 │  /dev/gpiomem (Pi register access)                                             │
 └──────────────────────────────────────────────────────────────────────────────┘
                 │
                 ▼
          SoC GPIO block (BCM2711 on Pi 4, RP1 on Pi 5) · I2C/SPI controllers · wires
```

**The one design idea to hold onto:** every protocol has an **abstract front** (what you code against) and a
**swappable back end** (how it's done on this machine). `GpioController` doesn't know whether it's talking to libgpiod
on a Pi, an FT232H USB chip on your Mac, or an Arduino over a serial cable. That's why a sensor binding written once
works on all of them.

---

## 4. The cast of characters

| Character | Real type | Personality and job | Talks to |
|---|---|---|---|
| **The Switchboard** | `GpioController` | The operator you actually call. Keeps the list of open pins, checks you're not using a closed one, and routes every request to whichever driver it was given. Owns the driver and disposes it. | App ↔ Driver |
| **The Front Desk** | `GpioPin` | A friendly clerk for *one* pin. Has no power of its own: every method just calls the Switchboard with its pin number attached. Exists so you can pass "a pin" around as an object. | App ↔ Switchboard |
| **The Hardware Interpreter** | `GpioDriver` (abstract) and its subclasses | Speaks the machine's language: libgpiod calls, sysfs files, register writes, USB commands, Firmata messages. Its methods are `protected internal`, so apps never call it directly; only the Switchboard does. | Switchboard ↔ OS/hardware |
| **The Runners** | driver-internal watchers (`LibGpiodV2EventObserver`, `LibGpiodDriverEventHandler`, `UnixDriverDevicePin`) | Sit on background threads waiting for the kernel to report an edge, then call your handler directly. In #2403, they're the ones who hand you the wrong `sender`. | Kernel → your handler |
| **The Mail Carrier** | `I2cBus` / `I2cDevice` | Walks one shared street (the bus, `/dev/i2c-1`) and delivers to house numbers (7-bit addresses like `0x76`). One `I2cDevice` = one house. | Binding ↔ `/dev/i2c-N` |
| **The Private Phone Line** | `SpiDevice` | A dedicated line to one chip, selected by chip-select. Full duplex: you talk and listen at the same time. | Binding ↔ `/dev/spidevB.C` |
| **The Metronome** | `PwmChannel` | Keeps a steady beat (frequency) and decides how long each beat stays high (duty cycle). Dims LEDs, drives servos. | Binding ↔ `/sys/class/pwm` |
| **The Specialists** | device bindings (`Bme280`, `Mcp23017`, `Ssd1306`, ...) | Each knows one chip's register map and math by heart. You say "read the temperature"; they do the register dance and hand you a `Temperature` value. | App ↔ I2C/SPI/GPIO |
| **The Building Manager** | `Board` (`RaspberryPiBoard`, `GenericBoard`) | Knows which physical pins are wired to which function (a pin can't be GPIO *and* the I2C clock) and hands out controllers and buses without double-booking. | App ↔ protocol objects |
| **The Stand-in Switchboard** | `VirtualGpioController` / `VirtualGpioPin` | Collects pins from several real controllers (the Pi, an expander) and presents them as one controller with its own numbering. | App ↔ several Switchboards |
| **The Unit Clerk** | UnitsNet types (`Temperature`, `Pressure`, `Length`, ...) | Refuses to hand you a bare `double`. You get a `Temperature` and ask for `.DegreesCelsius` or `.DegreesFahrenheit`. | Bindings → App |
| **The Embassy** | `Ftx232HDevice` (FT232H), `Ft4222Device` | A USB chip that plugs into any PC (including a Mac) and gives it real GPIO/I2C/SPI pins. | Your Mac ↔ real wires |
| **The Remote-Controlled Puppet** | `ArduinoBoard` (Firmata) | An Arduino running the Firmata sketch. Your C# program runs on the PC and tells the Arduino, over serial, which pins to wiggle. | PC ↔ Arduino pins |

---

## 5. The two packages and the repo layout

### 5.1 What you install

| NuGet package | Built from | Contains |
|---|---|---|
| **`System.Device.Gpio`** | `src/System.Device.Gpio/` | The core: GPIO (controller, pin, drivers), I2C, SPI, PWM abstractions and their Linux implementations. Changes here need an **API proposal and review** (`Documentation/README.md`: expect "higher exigence for the code quality and longer discussions"). |
| **`Iot.Device.Bindings`** | every project under `src/devices/*`, merged by `src/Iot.Device.Bindings/` | ~139 device folders: sensors, displays, motor drivers, GPIO expanders, the `Board` layer, `VirtualGpioController`, FT232H, Arduino. Easier to contribute to. |
| `Iot.Device.Bindings.SkiaSharpAdapter` | `src/Iot.Device.Bindings.SkiaSharpAdapter/` | Optional graphics support for display bindings. |

Both target **`net8.0`** only (checked in the `.csproj` files). Latest stable: **4.2.0**. There's also a nightly
feed on Azure DevOps (see the upstream `README.md`).

### 5.2 The repo, folder by folder

```
dotnet/iot/
├── src/
│   ├── System.Device.Gpio/
│   │   ├── System/Device/Gpio/          GpioController, GpioPin, GpioDriver, PinValue, PinMode, events
│   │   │   └── Drivers/                 LibGpiodV2Driver, LibGpiodDriver, SysFsDriver, RaspberryPi3Driver, ...
│   │   ├── System/Device/I2c/           I2cBus, I2cDevice, UnixI2cBus (/dev/i2c-N)
│   │   ├── System/Device/Spi/           SpiDevice, SpiConnectionSettings, UnixSpiDevice (/dev/spidev)
│   │   ├── System/Device/Pwm/           PwmChannel, UnixPwmChannel (/sys/class/pwm)
│   │   ├── Interop/Unix/                P/Invoke declarations: libgpiod V1/V2, libc (ioctl, epoll), libbcm_host
│   │   └── CompatibilitySuppressions.xml  approved API breaks (package validation)
│   ├── System.Device.Gpio.Tests/        unit tests (Moq + MockableGpioDriver) and hardware tests
│   ├── devices/                         one folder per binding
│   │   ├── Bmxx80/                      Bme280.cs ..., samples/, tests/, README.md, category.txt
│   │   ├── Board/                       Board, RaspberryPiBoard, VirtualGpioController, KeyboardGpioDriver
│   │   ├── Ft232H/  Ft4222/  Arduino/   "embassies" that make a PC grow pins
│   │   ├── Mcp23xxx/  Pcx857x/ ...      GPIO expanders (bindings that are also GpioDrivers)
│   │   └── ... ~130 more
│   └── Iot.Device.Bindings/             packs all of devices/* into one NuGet package
├── samples/                             standalone starter apps (led-blink, led-matrix-weather, ...)
├── tools/                               DevicesApiTester CLI, binding template
├── Documentation/                       CONTRIBUTING, coding + device conventions, Pi how-tos, release process
├── eng/  build.sh  global.json          Arcade build infrastructure (SDK 9.0.306, rollForward major)
└── .github/                             PR template, copilot-instructions.md, workflows
```

**Tip from the upstream README:** `main` may be ahead of the package you installed. When copying sample code,
browse the **tag that matches your package version** (e.g. `v4.2.0`).

---

## 6. GPIO in depth

### 6.1 Pin numbers: which "17" is it?

On a Raspberry Pi there are three numbering systems:

| Scheme | Example for the same pin | Who uses it |
|---|---|---|
| **Physical header position** | pin 11 | The 40-pin header silkscreen; wiring diagrams |
| **BCM / GPIO number** | GPIO17 | Datasheets, Linux, **dotnet/iot** |
| **Chip line offset** | line 17 on `gpiochip0` (Pi 3/4); on the Pi 5, the RP1 chip | libgpiod, `gpioinfo` |

dotnet/iot uses the **logical number the driver understands**, which on a Pi is the BCM number (the same as the
libgpiod line offset). The old `PinNumberingScheme.Board` option (physical numbering) was **removed in v4**
(#2358 obsoleted it, #2421 removed it). If you want your own numbering, wrap pins in a `VirtualGpioController`.

### 6.2 The basic life cycle

```csharp
using System.Device.Gpio;

using GpioController controller = new();          // picks a driver for this machine (§7)

controller.OpenPin(18, PinMode.Output);           // claim the line
controller.OpenPin(23, PinMode.InputPullUp);      // button to GND, so pressed = Low

controller.Write(18, PinValue.High);              // LED on
PinValue v = controller.Read(23);                 // poll the button
controller.Toggle(18);                            // LED off

// Object style: the same operations through GpioPin
GpioPin led = controller.OpenPin(18);             // returns the already-open pin
led.Write(PinValue.Low);

// Batch: several pins in one call
controller.Write(stackalloc PinValuePair[] { new(18, PinValue.High), new(24, PinValue.Low) });
// (24 would have to be open as an output first; shown for shape only)

// `using` disposes the controller → closes all its pins → releases the lines
```

The public surface of `GpioController` (from `GpioController.cs`): `OpenPin` (3 overloads), `ClosePin`,
`IsPinOpen`, `SetPinMode`, `GetPinMode`, `IsPinModeSupported`, `Read`, `Write`, `Toggle`, batch `Read`/`Write` with
`PinValuePair`, `WaitForEvent`, `WaitForEventAsync`, `RegisterCallbackForPinValueChangedEvent`,
`UnregisterCallbackForPinValueChangedEvent`, `PinCount`, `QueryComponentInformation` (debugging), `Dispose`.

### 6.3 Three ways to react to an input

| Style | Code | Thread it runs on | When to use |
|---|---|---|---|
| **Polling** | `while (true) { if (controller.Read(23) == PinValue.Low) ...; Thread.Sleep(10); }` | Yours | Simple, slow-changing inputs. Wastes CPU; can miss short pulses. |
| **Wait for an edge** | `var r = await controller.WaitForEventAsync(23, PinEventTypes.Falling, token);` | Yours (awaits) | A worker loop that handles one edge at a time. Good fit for `BackgroundService`. |
| **Callback / event** | `controller.RegisterCallbackForPinValueChangedEvent(23, PinEventTypes.Falling, OnEdge);` or `pin.ValueChanged += OnEdge;` | **The driver's background thread** | Many pins, fire-and-forget reactions. |

How a callback travels (the path #2403 is about):

```
 kernel edge on line 23
        │
        ▼
 Runner thread inside the driver (e.g. LibGpiodV2EventObserver)
        │  handler.Invoke(this, new PinValueChangedEventArgs(Falling, 23))
        │                  ▲
        │                  └── this "this" is the Runner, not your GpioPin  (#2403)
        ▼
 your OnEdge(object sender, PinValueChangedEventArgs e)   ← runs on the Runner's thread
```

Consequences worth remembering:

- **Your handler runs on a driver thread**, not your main thread. Keep it short, don't block, and protect shared
  state (`lock`, `Interlocked`, or hand off to a `Channel<T>`).
- **`PinEventTypes`** is a flags enum (`Rising`, `Falling`, or both). Callbacks registered through `pin.ValueChanged`
  always get both edges.
- **`e.PinNumber`** tells you which pin fired, in that controller's numbering. `sender` should tell you *which
  object*, but today it doesn't (#2403).
- **Mechanical buttons bounce.** Expect several edges per press; debounce in software (the `Button` binding in
  `src/devices/Button/` does this).

---

## 7. How `new GpioController()` picks a driver

The parameterless constructor calls `GetBestDriverForBoard()`. From `GpioController.cs` and `UnixDriver.cs`:

```
 new GpioController()
        │
        ├─ OS is Windows? ──► read registry BaseBoardProduct
        │                      "Raspberry Pi 2/3" → RaspberryPi3Driver, anything else → PlatformNotSupportedException
        │
        └─ otherwise ("Unix", which includes Linux AND macOS)
               │
               ▼
          RaspberryBoardInfo.LoadBoardInfo()      (reads /proc/cpuinfo)
               │
       ┌───────┼──────────────────────────────┬──────────────────────────────┐
       ▼       ▼                              ▼                              ▼
   Pi 3/3+/4/400/Zero W/Zero 2 W/CM3/CM4   Pi 5                          anything else
       │                                     │                              │
   RaspberryPi3Driver (direct register    libgpiod on the chip with       UnixDriver.Create():
   access via /dev/gpiomem), or           54 lines (RP1): try V1,         try LibGpiodDriver(0)
   UnixDriver.Create() if that fails      then V2                          → LibGpiodV2Driver(0)
                                                                          → SysFsDriver
                                                                          → PlatformNotSupportedException
```

⚠ **Prediction for your MacBook (unverified):** .NET reports macOS as `PlatformID.Unix`, so it takes the right-hand
branch. There's no `/proc/cpuinfo`, no libgpiod and no `/sys/class/gpio`, so every attempt fails and you get
`PlatformNotSupportedException("No unix driver appears to be runnable")`. Running that is a two-minute first
experiment: it proves the decision tree above with your own eyes.

**When to pick the driver yourself:** in production, on unusual boards, or in tests. For example
`new GpioController(new LibGpiodV2Driver(chipNumber))`. (Note: `Documentation/gpio-linux-libgpiod.md` still shows
a `new GpioController(chipNumber)` constructor, but the current source only has `GpioController()` and
`GpioController(GpioDriver)`. Docs drift like this is common here, and fixing it makes a small, easy PR.)

---

## 8. Linux GPIO under the hood

You'll meet three generations of Linux GPIO interfaces in the driver code:

| Interface | How it works | Status | dotnet/iot driver |
|---|---|---|---|
| **sysfs** `/sys/class/gpio` | `echo 17 > export`, then read/write text files. Edges via `poll()` on the `value` file. | **Deprecated** since Linux 4.8; being removed from distributions. | `SysFsDriver` (+ `UnixDriverDevicePin` for events) |
| **GPIO character device** `/dev/gpiochipN` via **libgpiod 1.x** | ioctl-based: request lines, get file descriptors, read edge events. | Current kernel interface; the library is being replaced by v2. | `LibGpiodDriver` (+ `LibGpiodDriverEventHandler`) |
| **same device via libgpiod 2.x** | Redesigned C API (line requests, edge-event buffers). **Not source-compatible with 1.x.** | Current on newer distros (Raspberry Pi OS Bookworm and later). | `LibGpiodV2Driver` (+ `LibGpiodV2EventObserver`) |
| **Direct register access** `/dev/gpiomem` | mmap the SoC's GPIO registers; flip bits directly. Fastest, Pi-specific. | Pi 3/4 family. | `RaspberryPi3Driver` / `RaspberryPi3LinuxDriver` |

**The libgpiod version trap:** the *documented* version and the *shared-library* name don't match
(`Documentation/gpio-linux-libgpiod.md`): libgpiod 1.1–1.6 ships as `libgpiod.so.2`, and 2.x ships as
`libgpiod.so.3`. dotnet/iot loads them by those names via `DllImport` (`src/System.Device.Gpio/Interop/Unix/libgpiod/V1/`
and `V2/`). If the right `.so` isn't installed, driver creation fails. That's why the selection code *tries* drivers
in order instead of assuming.

**Handy command-line tools on the Pi** (from the `gpiod` package): `gpiodetect` (list chips), `gpioinfo` (list
lines and who owns them), `gpioget`/`gpioset`. Use them to check the hardware before blaming your C#.

**Permissions:** your user needs access to `/dev/gpiochip*` (the `gpio` group on Raspberry Pi OS), `/dev/i2c-*`
(`i2c` group) and `/dev/spidev*` (`spi` group). "Permission denied" usually means the group, not the code.

---

## 9. I2C, SPI and PWM

All three follow the same pattern: **settings object → static `Create()` → device object → `Dispose()`**.

```csharp
using System.Device.I2c;
using System.Device.Spi;
using System.Device.Pwm;

// I2C: bus 1 (/dev/i2c-1), device at address 0x76
using I2cDevice i2c = I2cDevice.Create(new I2cConnectionSettings(busId: 1, deviceAddress: 0x76));
Span<byte> id = stackalloc byte[1];
i2c.WriteRead(stackalloc byte[] { 0xD0 }, id);   // read the chip-ID register

// SPI: bus 0, chip select 0 (/dev/spidev0.0)
using SpiDevice spi = SpiDevice.Create(new SpiConnectionSettings(0, 0) { ClockFrequency = 1_000_000, Mode = SpiMode.Mode0 });
Span<byte> rx = stackalloc byte[3];
spi.TransferFullDuplex(stackalloc byte[] { 0x01, 0x80, 0x00 }, rx);

// PWM: chip 0, channel 0 (/sys/class/pwm/pwmchip0/pwm0), 400 Hz, 50 %
using PwmChannel pwm = PwmChannel.Create(0, 0, frequency: 400, dutyCyclePercentage: 0.5);
pwm.Start();
```

| Protocol | Linux path used | Enable on a Pi | On Windows |
|---|---|---|---|
| I2C | `/dev/i2c-N` (`UnixI2cBus`) | `raspi-config` → Interface Options, or `dtparam=i2c_arm=on` in `config.txt` | `Create()` throws |
| SPI | `/dev/spidevB.C` (`UnixSpiDevice`) | `dtparam=spi=on` | not supported by default |
| PWM | `/sys/class/pwm/pwmchipN` (`UnixPwmChannel`; BeagleBone special-cased) | `dtoverlay=pwm` or `pwm-2chan` | `Create()` throws |

Note: on current Raspberry Pi OS, `config.txt` moved to `/boot/firmware/config.txt`. Updating the samples for that
is scouting item B (#2297).

The APIs use **`Span<byte>`** for buffers: no allocations, and it works over stack memory (`stackalloc`), arrays, or
native memory. It's the .NET equivalent of passing `uint8_t *buf, size_t len`.

---

## 10. Device bindings: the Specialists

### 10.1 Anatomy of a binding

Take `src/devices/Bmxx80/` (BME280/BMP280/BME680 environmental sensors):

```
Bmxx80/
├── Bmxx80Base.cs, Bme280.cs, ...   the driver classes (register enums, compensation math)
├── samples/Bme280/                 a runnable console sample
├── tests/                          unit tests (where logic can be tested without hardware)
├── README.md                       wiring diagram, datasheet link, usage
└── category.txt                    tags used to generate the device index
```

Using it (trimmed from `samples/Bme280/Bme280.sample.cs`):

```csharp
using I2cDevice i2cDevice = I2cDevice.Create(new I2cConnectionSettings(1, Bme280.DefaultI2cAddress));
using Bme280 bme = new(i2cDevice);                 // the binding takes the protocol object
var r = bme.Read();                                 // one measurement
Console.WriteLine($"{r.Temperature?.DegreesCelsius:0.#} °C, {r.Pressure?.Hectopascals:0.##} hPa");
```

### 10.2 Conventions every binding follows (`Documentation/Devices-conventions.md`)

| Rule | Why |
|---|---|
| Take the protocol object (`I2cDevice`, `SpiDevice`, `GpioController`) in the constructor | The binding works on any back end: Pi, FT232H, Arduino. |
| **Dispose what you were given** if nobody else can use it (I2C/SPI/PWM always; `GpioController` usually, with an optional `shouldDispose` flag) | Clear ownership: one owner closes each file descriptor. |
| Use **UnitsNet** types for physical values; otherwise SI | No "is this °C or °F?" bugs. |
| Methods for values that change (`ReadTemperature`), properties for ones that don't | Reading a sensor is an action with a cost. |
| `bool TryReadX(out T)` instead of sentinel values | Explicit failure. |
| `-1` means "no pin assigned" | Consistency across 130+ bindings. |
| `enum Register : byte` for register maps | Readable, typo-proof register access. |

### 10.3 Bindings can be drivers too

A GPIO expander like the MCP23017 (16 extra pins over I2C) is a **binding** (it talks I2C) *and* a **`GpioDriver`**
(it provides pins). So you can write:

```csharp
using var i2c = I2cDevice.Create(new I2cConnectionSettings(1, 0x20));
using var mcp = new Mcp23017(i2c);
using var expanderPins = new GpioController(mcp);   // a Switchboard whose Interpreter is an I2C chip
expanderPins.OpenPin(0, PinMode.Output);
```

This is the Strategy pattern paying off. It's also why `sender` is so inconsistent in #2403: every one of these
drivers wrote its own event-firing code.

---

## 11. Where can it run?

| Platform | GPIO | I2C / SPI / PWM | Notes |
|---|---|---|---|
| **Raspberry Pi 3 / 3B+ / 4 / 400 / CM3 / CM4 / Zero 2 W** | ✅ `RaspberryPi3Driver` or libgpiod | ✅ | The main target. Use 64-bit Raspberry Pi OS. |
| **Raspberry Pi 5** | ✅ libgpiod on the RP1 chip | ✅ | Newer path; see the Pi 5 branch in §7. |
| Original Pi Zero / Pi 1 (ARMv6) | ❌ | ❌ | .NET doesn't run on ARMv6 at all. |
| Other ARM Linux boards (Rockchip, Allwinner/Sunxi, BeagleBone, ...) | ✅ generic libgpiod, plus board-specific drivers in `src/devices/Gpio/Drivers/` | mostly ✅ | Less tested. |
| x64 Linux PC | only if it has GPIO chips; otherwise via USB adapters | via adapters | |
| Windows | ❌ natively (legacy Windows IoT Core code only) | ❌ natively | Use FT232H/FT4222 or Arduino. |
| **macOS (your MacBook)** | ❌ natively (⚠ expected `PlatformNotSupportedException`, §7) | ❌ natively | **FT232H family works on macOS** (Ft232H README: Windows, Linux and macOS via FTDI D2XX drivers). Arduino/Firmata over USB serial is documented for Windows/Linux; macOS unverified. |
| Microcontrollers (ESP32, STM32, ...) | not this project | | Use **.NET nanoFramework**. |

Runtime requirement: **.NET 8 or later**, since the packages target `net8.0`.

---

## 12. How to run it: four paths for you

### Path A: no hardware, on the Mac (best for #2403)

A lot of dotnet/iot is plain C# logic that can run against a **fake driver**. The upstream tests do exactly this
(`src/System.Device.Gpio.Tests/MockableGpioDriver.cs`, plus Moq). `GpioDriver`'s members are
`protected internal abstract`, so your own assembly can subclass it and override them:

```csharp
// Sketch: a fake driver you control from the test code (not yet built or run)
class FakeDriver : GpioDriver
{
    private readonly Dictionary<int, PinChangeEventHandler> _callbacks = new();
    protected override int PinCount => 28;
    protected override void OpenPin(int pin) { }
    protected override void ClosePin(int pin) { }
    protected override void SetPinMode(int pin, PinMode mode) { }
    protected override PinMode GetPinMode(int pin) => PinMode.Input;
    protected override bool IsPinModeSupported(int pin, PinMode mode) => true;
    protected override PinValue Read(int pin) => PinValue.Low;
    protected override void Write(int pin, PinValue value) { }
    protected override WaitForEventResult WaitForEvent(int pin, PinEventTypes t, CancellationToken ct) => default;
    protected override void AddCallbackForPinValueChangedEvent(int pin, PinEventTypes t, PinChangeEventHandler cb)
        => _callbacks[pin] = _callbacks.TryGetValue(pin, out var e) ? e + cb : cb;
    protected override void RemoveCallbackForPinValueChangedEvent(int pin, PinChangeEventHandler cb)
        { if (_callbacks.TryGetValue(pin, out var e)) _callbacks[pin] = (e - cb)!; }

    public void Fire(int pin, PinEventTypes t) => _callbacks.GetValueOrDefault(pin)?.Invoke(this, new(t, pin));
}
```

⚠ The exact set of abstract members to override is from reading `GpioDriver.cs`; the compiler will tell you if one
is missing. This fake is the core of experiment **E1** in the #2403 sample. Other no-hardware tools:
`DummyGpioDriver` (zero pins, just satisfies the interface), `VirtualGpioController`, and the repo's own unit tests
(`dotnet test src/System.Device.Gpio.Tests/` in a clone).

### Path B: a Raspberry Pi (the real thing)

```
 MacBook (develop + build)                         Raspberry Pi (run)
 ─────────────────────────                         ──────────────────
 dotnet new console -o Blink                       64-bit Raspberry Pi OS
 dotnet add package System.Device.Gpio             (optional) .NET 8+ runtime, if framework-dependent
 write Program.cs                                  user in the gpio / i2c / spi groups
 dotnet publish -c Release -r linux-arm64 \
     --self-contained -o out            ──scp/rsync──►   ./out/Blink
```

1. **Wire it:** LED + ~330 Ω resistor from GPIO18 (physical pin 12) to GND. Remember the Pi is **3.3 V logic and
   not 5 V tolerant**.
2. **Build on the Mac:** `dotnet publish -r linux-arm64 --self-contained` cross-compiles, so the Pi doesn't need
   .NET installed. Framework-dependent is smaller but needs the runtime on the Pi (install with Microsoft's
   `dotnet-install.sh`).
3. **Copy and run:** `rsync -av out/ pi@raspberrypi.local:~/blink/ && ssh pi@raspberrypi.local ~/blink/Blink`.
4. **Debug:** VS Code Remote-SSH (edit and run on the Pi), or remote debugging with `vsdbg` on the Pi.
5. **Docker (optional):** pass the device in with `--device /dev/gpiochip0` (see `Documentation/raspi-Docker-GPIO.md`).

The upstream `samples/led-blink/` is exactly this program. (It references package **3.1.0**; bump it to 4.2.0.)

### Path C: an FT232H USB adapter on the Mac

A ~$15 breakout board. Install FTDI's D2XX driver, plug it in, and your Mac has GPIO, I2C and SPI:

```csharp
using Iot.Device.Ft232H;          // package: Iot.Device.Bindings
var ft = Ftx232HDevice.GetFtx232H()[0];
using GpioController gpio = ft.CreateGpioController();
using I2cBus bus = ft.CreateOrGetI2cBus(ft.GetDefaultI2cBusNumber());
```

Same bindings, same code; only where the controller comes from changes. Great for developing bindings without a Pi.

### Path D: an Arduino as a remote-controlled puppet

Flash the **ConfigurableFirmata** sketch, then `new ArduinoBoard("/dev/cu.usbmodem1101", 115200)` (port name
varies) and ask it for a `GpioController`, I2C or SPI. The C# runs on your computer; the Arduino just obeys
(`src/devices/Arduino/README.md`).

---

## 13. Working on the repo itself (the contributor's view)

| Task | How |
|---|---|
| Full build | `./build.sh --restore --build` (Arcade SDK; 30–45 min; needs the Azure DevOps feeds) |
| Test one project | `./dotnet.sh test src/System.Device.Gpio.Tests/` or plain `dotnet test <path>` (a newer SDK works: `rollForward: major`) |
| Hardware vs. software tests | xUnit traits: `[Trait("feature", "gpio")]`, `[Trait("SkipOnTestRun", "Windows_NT")]`. Mocked tests (e.g. `GpioControllerSoftwareTests`) run anywhere. |
| CI | Azure Pipelines (`azure-pipelines.yml`) |
| API compatibility | Package validation compares against the last release; approved breaks go in `CompatibilitySuppressions.xml`. **This is the mechanism that makes "breaking change" concrete.** |
| New binding | Start from `tools/templates/DeviceBindingTemplate/`; follow `Devices-conventions.md`. |
| PR | `Fixes #NNNN` first line; avoid force-pushing; unit tests for hardware-independent logic. |
| Core API change | API proposal issue, then review. Slower, stricter. |

---

## 14. The concepts you need to be competent in

Ranked by how soon you'll need them.

| # | Concept | Why it matters here | Where you'll see it |
|---|---|---|---|
| 1 | **.NET events and delegates:** `event` accessors (`add`/`remove`), multicast delegates, delegate identity, the `(sender, e)` convention | #2403 is entirely about this. | `GpioPin.ValueChanged`, `PinChangeEventHandler`, every driver's callback list |
| 2 | **Threads and callbacks:** handlers on driver threads, thread-safe collections, re-entrancy | Callbacks arrive on background threads; subscriptions can happen from any thread. | `LibGpiodV2EventObserver`, `ConcurrentDictionary` in `GpioController`, `VirtualGpioController` |
| 3 | **Abstraction and strategy:** abstract base plus swappable implementations, factory selection | The whole architecture. | `GpioDriver`, `GetBestDriverForBoard()`, `I2cDevice.Create()` |
| 4 | **`IDisposable` and ownership:** who closes what, `using`, dispose patterns | Leaked lines stay claimed; double-dispose bugs. | Devices-conventions "Lifetime management", `GpioPin.Dispose` |
| 5 | **Native interop (P/Invoke):** `DllImport`, marshalling structs, `SafeHandle`, library names | libgpiod and libc calls; scouting item C (#2600) is a null-pointer crash here. | `src/System.Device.Gpio/Interop/Unix/` |
| 6 | **Linux device model:** `/dev/gpiochipN`, `/dev/i2c-N`, sysfs, permissions, device-tree overlays | Every "doesn't work on my Pi" issue. | Drivers, `Documentation/raspi-*.md` |
| 7 | **`Span<T>` and `stackalloc`** | Zero-allocation buffer APIs for I2C/SPI. | `I2cDevice.Read/Write/WriteRead`, `SpiDevice.TransferFullDuplex` |
| 8 | **`async` / `ValueTask` / `CancellationToken`** | Edge waiting and long-running loops. | `WaitForEventAsync` |
| 9 | **Library compatibility:** binary vs. source vs. behavioral breaking changes, semver, package validation | Decides whether your fix can merge now or waits for a major release. | #2403 triage, #2341, `CompatibilitySuppressions.xml` |
| 10 | **Testing hardware code without hardware:** fakes, mocks (Moq), test traits | How you prove a fix on a Mac. | `MockableGpioDriver`, `VirtualGpioTests` |
| 11 | **UnitsNet** | Every sensor binding returns these types. | Any binding's public API |

Each of these is a candidate for its own lecture in this folder. #1, #3 and #9 are the ones #2403 needs first.

---

## 15. Edge cases and gotchas

| Gotcha | What happens | What to do |
|---|---|---|
| Floating input | Random reads and spurious edges | Use `InputPullUp`/`InputPullDown` or an external resistor. |
| Switch bounce | Many callbacks per press | Debounce (time filter) or use the `Button` binding. |
| Slow or blocking work in a callback | Missed edges; other pins' events delayed | Queue the work (`Channel<T>`) and return fast. |
| Pin already in use | `OpenPin` fails ("device or resource busy") | Check with `gpioinfo`; another process (or a kernel overlay) owns the line. |
| Wrong libgpiod version | Driver creation throws | Check `apt show libgpiod*`; choose the driver explicitly. |
| Pi 5 chip numbering | Code that hard-codes chip 0 breaks | Let auto-selection find the RP1 chip, or look it up with `gpiodetect`. |
| 5 V signals into a Pi | Damaged GPIO | Level shifters; the Pi is 3.3 V only. |
| Sample code from `main` against an older package | Missing APIs | Browse the tag matching your package version. |
| Upstream docs out of date | Instructions that don't compile (e.g. `GpioController(chipNumber)`) | Trust the source; consider a docs PR. |
| `sender` in GPIO event handlers | Not the pin or controller you subscribed on (#2403) | Use `e.PinNumber` for now. |

## 16. Common mistakes

1. **Assuming `new GpioController()` works everywhere.** It only knows Linux boards (and legacy Windows IoT). On a
   Mac you need a fake driver or a USB adapter.
2. **Mixing up numbering.** Physical pin 12 is GPIO18. dotnet/iot wants **18**.
3. **Forgetting to dispose.** Lines stay claimed until the process exits, so a second process (or a stale copy of yours still running) fails with "busy".
4. **Treating callbacks as if they run on the main thread.** Race conditions, especially with UI or shared lists.
5. **Casting `sender`** to `GpioPin` inside a handler (today it throws, #2403).
6. **Editing the core library for something a binding could do.** Core changes need API review; bindings don't.
7. **Believing the README over the code** on versions and target frameworks.

## 17. Interview relevance

- **Hardware abstraction layers = Strategy + Factory.** `GpioController` + `GpioDriver` + `GetBestDriverForBoard()`
  is a clean, real example. You can talk about it from both the firmware side and the .NET side, which is rare.
- **Observer pattern and its sharp edges:** event threading, unsubscription and delegate identity, memory leaks
  from forgotten `-=`.
- **Resource ownership:** "who disposes what" is the same question as ownership of connections in a backend
  service (DB connections, `HttpClient`).
- **API evolution:** why a correct bug fix can still be a breaking change, and how projects manage it (semver,
  obsoletion first, package validation). Senior-level interview material.
- **Testing I/O-bound code:** seams, fakes and mocks, the same skills as testing code that calls a database or
  message queue.

## 18. Real-world production usage

- **Edge gateways and kiosks:** a Pi running an ASP.NET Core app that reads sensors and serves a dashboard or pushes
  to the cloud (the upstream `samples/bmp280-sensor-azure-iot-hub/` shows the pattern).
- **Industrial retrofits:** reading PLC-adjacent signals, relays and displays on Linux boards, with .NET background
  services for reliability.
- **Test rigs and lab automation:** FT232H or Arduino attached to a PC, driven by a C# test harness. (Your embedded
  work probably has rigs like this.)
- **Hobby-to-product:** prototype on a Pi with bindings, then move to a custom board that has libgpiod and a Linux
  kernel; the code barely changes.

## 19. Check yourself

1. Which class decides what `sender` is when a GPIO callback fires, and why isn't it `GpioPin`?
2. What would `new GpioController()` do on your Mac, step by step? Which file would you read to confirm it?
3. You have an MCP23017 on I2C bus 1 at `0x20`. How do you get a `GpioController` for its 16 pins?
4. Why does a BME280 binding take an `I2cDevice` rather than a bus number?
5. What's the difference between `libgpiod.so.2` and `libgpiod.so.3`, and which driver uses each?
6. Why is changing which object gets passed as `sender` a breaking change, even though no method signature changes?
7. Your button handler sometimes fires three times per press. Name two causes and two fixes.
8. Which package would a new temperature-sensor driver go into, and what review would it need compared with a
   change to `GpioController`?

## 20. Glossary

| Term | Meaning |
|---|---|
| **GPIO** | General-purpose input/output: a pin you can read or drive high/low. |
| **Edge** | A transition: **rising** (low→high) or **falling** (high→low). |
| **Line** | libgpiod's word for one GPIO on a chip. |
| **Chip** (`gpiochipN`) | A GPIO controller as Linux sees it; a SoC can expose several. |
| **Binding** | A dotnet/iot class that drives one specific chip or module. |
| **Driver** (`GpioDriver`) | The back end that implements GPIO on a specific platform. |
| **Firmata** | A serial protocol for remote-controlling a microcontroller's pins. |
| **D2XX** | FTDI's USB driver used by the FT232H/FT4222 bindings. |
| **RP1** | The Raspberry Pi 5's I/O chip (54 GPIO lines). |
| **Package validation** | .NET SDK feature that flags API changes against the previous release. |
| **nanoFramework** | A separate project: .NET on microcontrollers without an OS. |

## Sources

Read on 2026-09-27 at `main` @ `1eb0b2f`:
`README.md`; `Documentation/README.md`, `CONTRIBUTING.md`, `Devices-conventions.md`, `gpio-linux-libgpiod.md`,
`How-to-Deploy-an-IoT-App.md`; `.github/copilot-instructions.md`;
`src/System.Device.Gpio/System/Device/Gpio/{GpioController,GpioPin,GpioDriver,PinMode}.cs`,
`.../Drivers/{UnixDriver,LibGpiodV2Driver,RaspberryPi3LinuxDriver}.cs`,
`src/System.Device.Gpio/System/Device/{I2c/I2cDevice,Pwm/PwmChannel}.cs`;
`src/devices/{Board,Ft232H,Arduino,Bmxx80}/`; `samples/led-blink/`;
NuGet version list for `System.Device.Gpio` (latest 4.2.0); git tags v4.0.0 / v4.1.0 / v4.2.0.
