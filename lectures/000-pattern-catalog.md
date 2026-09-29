# 000: Architecture and Pattern Catalog

> **What this is:** a running, cross-repo catalog of the design patterns, communication styles and engineering
> practices met while working on open-source issues. The goal is to **see the same idea in different codebases**
> and learn to recognize it quickly. Each row points to real code.
>
> **How to add:** when an issue shows you a pattern (or a new variant of one you've seen), add a row under the
> right heading. Link the concept lecture if one exists. Keep the "Note" to one or two sentences: what's
> interesting about *this* use. Mark anything you haven't checked in the code yourself as **unverified**.
>
> **Started:** 2026-09-27

---

## Structural patterns

| Pattern | Repo | Where in the code | Note | Lecture |
|---|---|---|---|---|
| **Strategy** (abstract back end, swappable implementations) | dotnet/iot | `GpioController` holds a `GpioDriver`; `LibGpiodV2Driver`, `SysFsDriver`, `RaspberryPi3Driver`, `Mcp23xxx`, `Ftx232HDevice`... | One front API, many hardware back ends. Chip bindings can *be* drivers, so an I2C expander becomes a GPIO controller. | [iot 001 §3, §10.3](../iot/iot_concepts/001-the-big-picture-dotnet-iot.md) |
| **Factory with runtime detection** | dotnet/iot | `GpioController.GetBestDriverForBoard()`, `UnixDriver.Create()`, `I2cDevice.Create()` | Tries drivers in order and falls back; the source even notes it "feels like it needs a driver-based pattern" (plugin discovery). | iot 001 §7 |
| **Facade / thin wrapper object** | dotnet/iot | `GpioPin` forwards everything to `GpioController` with its pin number | Convenience object with no state of its own. That's exactly why it can't control `sender` (#2403). | iot 001 §6 |
| **Composite / adapter over several sources** | dotnet/iot | `VirtualGpioController` / `VirtualGpioPin` | Re-exposes pins from several controllers under new numbers. | iot 003 §4.4 |
| **Adapter (chip binding as a driver)** | dotnet/iot | `Mcp23xxx : GpioDriver` | An I2C/SPI chip fulfills the GPIO driver contract, so `new GpioController(mcp)` works. | iot 003 §4.4 |
| **Template Method** | dotnet/iot | `GpioController.OpenPin` → `OpenPinCore`; `TryReadTemperature` → `TryReadTemperatureCore`; `Dispose()` → `Dispose(bool)` | Naming smell: public `Xxx()` + `protected virtual XxxCore()`. | iot 003 §4.2 |
| **Try-factory (fail softly, try candidates in order)** | dotnet/iot | `GpioDriver.TryCreate<T>(Func<T>, out T?)`, `Board.CreateBoardInternal<T>()` | Takes a delegate as the "recipe"; swallows only platform/DLL-not-found errors. | iot 003 §4.3 |
| **Re-raise with your own `sender`** | dotnet/iot | `GpioButton.PinStateChanged` → `ButtonBase.ButtonDown?.Invoke(this, …)` | Subscribe to a lower layer, ignore its `sender`, raise your own event correctly. The shape a #2403 fix would follow. | iot 002 §10, §12 |
| **Reverse proxy as a pipeline** | dotnet/yarp | ASP.NET Core middleware pipeline → YARP forwarder | Request handling as composable middleware; YARP is "just" the last middleware. | [yarp lecture](001-yarp-websocket-activity-timeout.md) |

## Communication and concurrency

| Pattern | Repo | Where in the code | Note | Lecture |
|---|---|---|---|---|
| **Observer via .NET events and delegates** | dotnet/iot | `PinChangeEventHandler`, `GpioPin.ValueChanged`, `RegisterCallbackForPinValueChangedEvent` | Custom `add`/`remove` accessors forward subscriptions; `sender` convention broken (#2403). | iot 001 §6.3 |
| **Background watcher thread → callbacks** | dotnet/iot | `LibGpiodV2EventObserver`, `LibGpiodDriverEventHandler` | Kernel events read on a dedicated thread; user handlers run on it, so they must be quick and thread-safe. | iot 002 §9 |
| **Invoke under lock vs. decide-under-lock, invoke outside** | dotnet/iot | `LibGpiodV2EventObserver.HandleEdgeEvent` vs. `ArduinoGpioControllerDriver.FirmataOnDigitalPortValueUpdated` | Two drivers make opposite choices about calling user code while holding a lock. | iot 002 §9.5 |
| **Extension methods on types you don't own** | dotnet/iot, ASP.NET Core | `I2cBusExtensions.PerformBusScan(this I2cBus)`; `builder.Services.AddSingleton` | Static helper that reads like an instance method; the compiler rewrites the call. | iot 002 §8 |
| **Bidirectional byte pumping (full-duplex streams)** | dotnet/yarp | WebSocket forwarding after the HTTP 101 upgrade | Proxy copies both directions independently; an idle connection is indistinguishable from a dead one without keep-alives. | yarp lecture |
| **Idle/activity timeouts and keep-alives** | dotnet/yarp | `ActivityTimeout`, `WebSocketOptions.KeepAliveInterval` | A liveness contract between hops; defaults on each side have to agree. | yarp lecture |

## Resource management, interop and compatibility

| Pattern | Repo | Where in the code | Note | Lecture |
|---|---|---|---|---|
| **Ownership-based disposal** | dotnet/iot | `Documentation/Devices-conventions.md` "Lifetime management"; `shouldDispose` flags (`GpioButton`, `Mcp23xxx`) | "Whoever can't share it, disposes it." "If I created it, I own it; if you gave it to me, you choose." | iot 003 §6 |
| **P/Invoke into native libraries, by version** | dotnet/iot | `src/System.Device.Gpio/Interop/Unix/libgpiod/V1`, `V2` | Two incompatible native APIs behind one managed abstraction. | iot 001 §8 |
| **API compatibility enforcement** | dotnet/iot | `CompatibilitySuppressions.xml` (package validation); breaking changes batched into majors (#2341) | Makes "breaking change" a build-time fact, not an opinion. | iot 001 §13 |

## Testing and verification

| Pattern | Repo | Where in the code | Note | Lecture |
|---|---|---|---|---|
| **Fake the hardware boundary** | dotnet/iot | `System.Device.Gpio.Tests/MockableGpioDriver.cs` + Moq | Exposes `protected internal` members so a mock can stand in for a driver. | iot 003 §8.5 |
| **Register-file fake + datasheet worked example as the test oracle** | dotnet/iot | `Bmxx80/tests/SimulatedI2cDevice.cs`, `Bmp280Tests.CalculationWithSampleValues` | Load the datasheet's example registers; assert the datasheet's answer (25.08 °C). | iot 003 §8.4 |
| **Test traits to split hardware and software tests** | dotnet/iot | `[Trait("feature", "gpio")]`, `[Trait("SkipOnTestRun", "Windows_NT")]` | CI runs what it can; hardware tests run on real boards. | iot 001 §13 |
| **Multi-process repro with a wire tap** | this repo (yarp#1764 sample) | `yarp/1764_*/sample/tools/wstap.py` | Observing the protocol between processes instead of trusting logs. | yarp lecture |

## Build, release and project process

| Practice | Repo | Where | Note |
|---|---|---|---|
| **Arcade SDK builds** | dotnet/iot (and most `dotnet/*`) | `eng/`, `build.sh`, `global.json` | Shared .NET build infrastructure; needs its feeds; per-project `dotnet test` is the fast path. |
| **Docs in a separate repo** | dotnet/yarp → `dotnet/AspNetCore.Docs` | — | Code and docs have different owners, reviewers and processes. |
| **`help wanted` / `up-for-grabs` as a deliberate on-ramp** | YARP, dotnet/iot | issue labels | Well-described small issues are left for newcomers on purpose. |
