# Lecture 003: Bindings, OOP Architecture, Interfaces, `IDisposable` and Ownership, UnitsNet, and Testing Without Hardware

| | |
|---|---|
| **Prompted by** | Timothy's questions after [lecture 001](001-the-big-picture-dotnet-iot.md): *what is a binding?*, *abstraction, strategy and factory selection*, *how should I read all those interfaces on a class?*, *`IDisposable` and ownership*, *what does "every sensor binding returns UnitsNet types" mean?*, *how do fakes, mocks and test traits work?* |
| **Companion** | [Lecture 002](002-delegates-events-callbacks-and-threads.md) (delegates, events, threads). This lecture covers everything else; both use working dotnet/iot code as examples. |
| **Date** | 2026-09-28 |
| **Upstream code quoted** | dotnet/iot `main` @ `1eb0b2f` (2026-09-24), trimmed (XML doc comments removed). |
| **Evidence status** | Code is quoted from the source. Nothing has been run yet; "Try it" programs are in §10. |

---

## 0. The thread that ties this lecture together

These topics look unrelated, but they're one story:

```
 A BINDING (§1) is a class that turns bytes on a bus into a physical value.
   │ it's built with OOP tools: abstract base classes, overrides, interfaces (§2, §3)
   │ it's plugged into swappable back ends: Strategy + Factory (§4)
   │ it holds OS resources, so it must be cleaned up: IDisposable (§5) … by exactly one owner (§6)
   │ it hands you values with units attached, not bare doubles: UnitsNet (§7)
   └ and because the back end is swappable, you can swap in a FAKE and test it on your Mac (§8)
```

The abstraction that makes the architecture flexible is **the same abstraction** that makes testing without
hardware possible. Keep that in mind; it's the "aha" of §8.

### What you'll be able to do after this lecture

- [ ] Say what a binding is, trace a BME280 temperature read from I2C bytes to `Temperature`, and relate it to your firmware work.
- [ ] Tell apart `class`, `abstract class`, `interface`, `sealed`, `virtual`/`override`, `protected internal`, and `struct`, with a dotnet/iot example of each.
- [ ] Read a class declaration line (`public class GpioPin : MarshalByRefObject, IEquatable<GpioPin>, IDisposable`) and say what each part tells you about the object.
- [ ] Recognize Strategy, Template Method, Factory (four flavors) and Adapter in real code, and say why each is there.
- [ ] Explain what `IDisposable` is for, what `using` compiles into, and the `Dispose(bool)` pattern.
- [ ] Decide who should dispose an object in a given design, using the `shouldDispose` examples.
- [ ] Explain what UnitsNet types are and why bindings return them.
- [ ] Explain unit tests, test doubles (dummy, stub, fake, mock), Moq, and test traits, with dotnet/iot examples; and sketch the failing test for #2403.

---

## 1. What a "binding" is

### 1.1 The word

In software, a **binding** is a piece of code that makes something foreign usable from your language in that
language's natural style. "Python bindings for OpenCV" are Python classes wrapping a C++ library. In dotnet/iot the
"foreign thing" is a **hardware chip**: its register map, its command sequences, its datasheet math.

**A dotnet/iot binding is a C# class that knows how to talk to one specific chip or module, and gives you an API in
terms of what the chip *means*** (temperature, a pressed button, an LCD line of text) instead of what it *does on
the wire* (write `0xF4`, read three bytes from `0xFA`).

You've written this kind of code in firmware: a C module that owns a sensor's registers and returns a converted
value. A binding is exactly that, in C#, reusable by anyone, and written against an abstract bus so it runs over
any back end.

### 1.2 A binding, layer by layer: the BME280

```
 WHAT YOU CALL                  bme.TryReadTemperature(out Temperature t)      → 25.08 °C
 ────────────────────────────── ──────────────────────────────────────────────────────────────
 L4  typed physical value       Temperature.FromDegreesCelsius(temp)           (UnitsNet, §7)
 L3  datasheet compensation     CompensateTemperature(adc): var1/var2 math using calibration data
 L2  register reads             Read24BitsFromRegister(TEMPDATA_MSB, BigEndian) → raw 20-bit count
 L1  bus transactions           _i2cDevice.WriteByte(register); _i2cDevice.Read(bytes)
 L0  the wire                   I2C bus 1, address 0x76/0x77 (/dev/i2c-1 on a Pi)
```

The real code for each layer (`src/devices/Bmxx80/`):

```csharp
// L1 + identity check: the constructor (Bmxx80Base.cs)
protected Bmxx80Base(byte deviceId, I2cDevice i2cDevice)
{
    _i2cDevice = i2cDevice ?? throw new ArgumentNullException(nameof(i2cDevice));
    _i2cDevice.WriteByte((byte)Bmxx80Register.CHIPID);         // point at the chip-ID register (0xD0)
    byte readSignature = _i2cDevice.ReadByte();
    if (readSignature != deviceId)                              // BME280 = 0x60, BMP280 = 0x58
    {
        throw new IOException($"Unable to find a chip with id {deviceId}. Found one with id {readSignature}");
    }
    ReadCalibrationData();                                      // the dig_T1..dig_P9 trimming values
    Reset();
}

// L2: read the raw temperature (Bmx280Base.cs)
protected bool TryReadTemperatureCore(out Temperature temperature)
{
    if (TemperatureSampling == Sampling.Skipped) { temperature = default; return false; }
    var temp = (int)Read24BitsFromRegister((byte)Bmx280Register.TEMPDATA_MSB, Endianness.BigEndian);
    temperature = CompensateTemperature(temp >> 4);             // 20-bit value in the top bits
    return true;
}

// L3 + L4: datasheet math, then wrap in a unit type (Bmxx80Base.cs)
protected Temperature CompensateTemperature(int adcTemperature)
{
    // The temperature is calculated using the compensation formula in the BMP280 datasheet.
    double var1 = ((adcTemperature / 16384.0) - (_calibrationData.DigT1 / 1024.0)) * _calibrationData.DigT2;
    double var2 = (adcTemperature / 131072.0) - (_calibrationData.DigT1 / 8192.0);
    var2 *= var2 * _calibrationData.DigT3 * TempCalibrationFactor;
    TemperatureFine = var1 + var2;
    double temp = (var1 + var2) / 5120.0;
    return Temperature.FromDegreesCelsius(temp);
}
```

Everything here is familiar from firmware: check the chip ID, load calibration, read a big-endian multi-byte
register, apply the datasheet formula. What's new is **around** it: the class hierarchy it sits in, the fact that it
takes an abstract `I2cDevice`, the `Temperature` type it returns, and the tests that run it without a chip.

### 1.3 Kinds of bindings

| Kind | Examples (`src/devices/`) | What it gives you |
|---|---|---|
| **Sensors** | `Bmxx80`, `Dhtxx`, `Mpu6050`, `Ccs811` | Readings as UnitsNet values |
| **Displays** | `CharacterLcd`, `Ssd13xx`, `Ili934x` | Text or pixels |
| **Actuators / motor drivers** | `ServoMotor`, `DCMotor`, `Pca9685` | Speed, angle, PWM duty |
| **Inputs** | `Button`, `RotaryEncoder`, `KeyMatrix` | **Events** (`Press`, `ValueChanged`): see lecture 002 |
| **GPIO expanders** | `Mcp23xxx`, `Pcx857x`, `Tca955x` | More pins. These **are** `GpioDriver`s (§4.4) |
| **Bus adapters ("embassies")** | `Ft232H`, `Ft4222`, `Arduino` | A PC grows real GPIO/I2C/SPI |
| **Board support** | `Board` (`RaspberryPiBoard`, `VirtualGpioController`) | Pin ownership, bus management |

What a binding **is not**: it's not a Linux kernel driver, and it's not in `System.Device.Gpio`. Bindings live in
the `Iot.Device.Bindings` package and sit **on top of** the protocol abstractions (`I2cDevice`, `SpiDevice`,
`GpioController`, `PwmChannel`).

---

## 2. The OOP building blocks, with dotnet/iot examples

You came to C# from C, so here are the building blocks side by side, each with a real example you can open.

| Keyword / construct | What it means | C analogy (rough) | dotnet/iot example |
|---|---|---|---|
| `class` | A type with data + behavior; instances live on the heap and are passed by reference | a struct + functions taking a pointer to it | `GpioController` |
| `struct` | A **value type**: copied on assignment, usually small, no identity | a plain C struct passed by value | `PinValue` (`public readonly struct PinValue : IEquatable<PinValue>`) |
| `abstract class` | A base class that **can't be instantiated**; may contain real code plus `abstract` members subclasses **must** implement | a struct of function pointers with some defaults filled in | `GpioDriver`, `I2cDevice`, `Bmxx80Base` |
| `interface` | A pure **contract**: a list of members, no state; a class can implement many | a header listing required functions | `IDisposable`, `IEquatable<T>`, `IDeviceManager` (in `Board`) |
| `virtual` / `override` | "Subclasses *may* replace this" / "I'm replacing it" | swapping one function pointer in the table | `GpioController.OpenPinCore` (virtual) ← `VirtualGpioController.OpenPinCore` (override) |
| `abstract` member | "Subclasses *must* provide this" | a required function pointer with no default | `protected internal abstract void OpenPin(int pinNumber);` in `GpioDriver` |
| `sealed` | "Nobody may inherit from this" | — | `internal sealed class LibGpiodV2EventObserver`, `public sealed class LibGpiodV2Driver` |
| `protected internal` | Visible to subclasses (anywhere) **and** to code in the same assembly | — | `GpioDriver`'s members: `GpioController` (same assembly) calls them; your subclass can override them; your *app* can't call them |
| `internal` | Visible only inside the same assembly (DLL) | `static` functions in a `.c` file | `VirtualGpioPin`, `IDeviceManager` |

### 2.1 An inheritance chain you can walk

The BMxx80 family shares most of its code through a chain of base classes:

```
 Bmxx80Base            abstract   chip-ID check, calibration, CompensateTemperature, Dispose
   │                              declares: public abstract bool TryReadTemperature(out Temperature t);
   ▼
 Bmx280Base            abstract   BMP280/BME280 shared register layout
   │                              overrides: TryReadTemperature(...) => TryReadTemperatureCore(...)
   ├──► Bmp280                    DeviceId = 0x58
   └──► Bme280                    DeviceId = 0x60, adds humidity: TryReadHumidity, Read()
 (Bme680 derives from Bmxx80Base directly: a different register layout.)
```

The rule of thumb this shows: **put shared behavior as high as it's truly shared, and push differences down.** The
datasheet formula lives once, in the base; each chip only states what's different (its ID, its extra sensor).

### 2.2 Abstract class or interface?

| Use an **abstract class** when... | Use an **interface** when... |
|---|---|
| Subclasses share real code or state (`Bmxx80Base` holds `_i2cDevice` and the math) | You only need a contract, not shared code |
| There's one natural "is-a" family (every driver *is a* `GpioDriver`) | Unrelated types need the same capability (a pin, a sensor and a file can all be `IDisposable`) |
| You want to control construction (`protected` constructors) | A type needs several capabilities (C# allows many interfaces but only one base class) |

dotnet/iot mostly uses **abstract classes** for its big abstractions (`GpioDriver`, `I2cDevice`, `SpiDevice`,
`PwmChannel`, `Board`) and **interfaces** for capabilities (`IDisposable`, `IEquatable<T>`).

---

## 3. Reading a class declaration: interfaces as capability labels

### 3.1 The technique

When you open an unfamiliar type, **read the declaration line before anything else.** It tells you what the
object *is* and what it *can do* before you read a single method, which is the "read the interface, not the
implementation" lesson you learned at work, applied at the smallest scale.

```csharp
public class GpioPin : MarshalByRefObject, IEquatable<GpioPin>, IDisposable
//     │      │          │                  │                    │
//     │      │          │                  │                    └─ holds a resource: must be disposed (§5)
//     │      │          │                  └─ defines its own equality: two GpioPin objects for the same
//     │      │          │                     pin on the same controller count as "equal"
//     │      │          └─ base class (the one thing before the interfaces). A legacy .NET Framework
//     │      │             marker for cross-AppDomain remoting; in .NET 8 it has no practical effect
//     │      └─ the type's name
//     └─ a class: a reference type, not abstract, not sealed, so it can be subclassed (VirtualGpioPin does)
```

More examples, and what each declaration tells you at a glance:

| Declaration | What you learn before reading any code |
|---|---|
| `public readonly struct PinValue : IEquatable<PinValue>` | Small immutable value, copied around freely, compared by value. No cleanup needed. |
| `public abstract class GpioDriver : IDisposable` | A base for back ends; you'll never `new` it directly; every driver holds resources. |
| `public abstract partial class I2cDevice : IDisposable` | Abstract (platform picks the real one), split across files (`partial`), holds a resource. |
| `public abstract class Board : MarshalByRefObject, IDisposable` | An abstract family of boards that own resources. |
| `public abstract partial class Mcp23xxx : GpioDriver` | A chip binding that **is a** GPIO driver (§4.4). Inherits `IDisposable` from `GpioDriver`. |
| `internal sealed class LibGpiodV2EventObserver : IDisposable` | Implementation detail: not for you to use or extend; owns threads or handles. |
| `public class ButtonBase : IDisposable` | Owns something that needs cleanup (a `Timer`); subclassable (`GpioButton`). |

### 3.2 The interfaces you'll meet most often

| Interface | What it promises | What it tells you to do |
|---|---|---|
| `IDisposable` | `void Dispose()` releases resources deterministically | Use `using`, or know who owns it (§5–§6) |
| `IAsyncDisposable` | `ValueTask DisposeAsync()` | `await using` |
| `IEnumerable<T>` | "You can loop over me with `foreach`" (and use LINQ on me) | It's a *sequence*; may be lazy (computed as you iterate) |
| `ICollection<T>` / `IList<T>` / `IReadOnlyCollection<T>` | Counted / indexable / read-only collections | What you're allowed to do with the collection |
| `IEquatable<T>` | Custom equality (`Equals(T)`) | `==`/`Equals`/dictionary keys behave by *value*, not reference |
| `IComparable<T>` | Has a natural sort order | Can be sorted |
| `IProgress<T>` | Receives progress reports | Seen in `PerformBusScan(this I2cBus bus, IProgress<float>? progress, ...)` |
| `IObservable<T>` / `IObserver<T>` | Push-based event streams (Rx.NET) | The interface-based cousin of events (your `reactive/` lectures) |
| `IHostedService` | Start/stop hooks for the Generic Host | It's a long-running service (`BackgroundService` implements it) |

### 3.3 Generic constraints: extra labels on type parameters

```csharp
public static bool TryCreate<T>(Func<T> creationAction, [NotNullWhen(true)] out T? driver)
    where T : class, IDisposable                       // GpioDriver.cs
private static T? CreateBoardInternal<T>() where T : Board, new()   // Board.cs
```

`where T : Board, new()` reads as *"T must be some kind of `Board` and must have a public parameterless constructor"*.
That's what lets the method write `new T()`. Constraints are declarations, just like the class line: read them
first.

---

## 4. Design patterns in dotnet/iot

### 4.1 Strategy: "the controller has a driver"

**Problem:** GPIO works completely differently on a Pi 4 (registers), a Pi 5 (libgpiod), a Mac with an FT232H (USB),
and an Arduino (serial messages), but application code shouldn't care.

**Solution:** separate the *stable front* from the *swappable algorithm*. The front **holds** a reference to an
abstract strategy and delegates to it:

```csharp
public class GpioController : IDisposable
{
    private GpioDriver _driver;                              // the strategy (abstract type)

    public GpioController() : this(GetBestDriverForBoard()) { }   // pick one automatically (§4.3)
    public GpioController(GpioDriver driver) { _driver = driver; ... }  // or be given one

    public virtual PinValue Read(int pinNumber) { ... return _driver.Read(pinNumber); }  // delegate the work
}
```

```
                       ┌──────────────── GpioController ────────────────┐
  app ─── Read(17) ──► │ checks pin is open, keeps bookkeeping           │
                       │ _driver.Read(17)  ─────────────┐                │
                       └────────────────────────────────┼────────────────┘
                                                        ▼  (abstract GpioDriver)
         ┌────────────────┬───────────────────┬─────────┴────────┬─────────────────────┐
   LibGpiodV2Driver  RaspberryPi3Driver   Ftx232H GPIO      ArduinoGpioControllerDriver   (any subclass)
```

This is **composition over inheritance**: `GpioController` doesn't inherit from a driver; it *has* one. You change
behavior by passing a different object, not by writing a new subclass of the controller. The payoff:

| Benefit | Where it shows |
|---|---|
| New hardware without touching the controller (open/closed principle) | Every driver in the list above |
| Bindings written once work on any back end | `Bme280` takes an abstract `I2cDevice`: Pi, FT232H or Arduino |
| **Testability** | Pass a fake or mock driver (§8) |

### 4.2 Template Method: a fixed outline with overridable steps

**Problem:** some steps must always happen in the same order (bookkeeping, validation), but one step varies.

**Solution:** a public, **non-virtual** method fixes the outline and calls a `protected virtual` step:

```csharp
// GpioController.cs (trimmed)
public GpioPin OpenPin(int pinNumber)                 // the template: always the same outline
{
    if (IsPinOpen(pinNumber)) { return _gpioPins[pinNumber]; }
    OpenPinCore(pinNumber);                           // ← the variable step
    _openPins.TryAdd(pinNumber, null);
    _gpioPins[pinNumber] = new GpioPin(pinNumber, this);
    return _gpioPins[pinNumber];
}

protected virtual void OpenPinCore(int pinNumber) => _driver.OpenPin(pinNumber);   // default step

// VirtualGpioController.cs overrides only the step:
protected override void OpenPinCore(int pinNumber) { ... }
```

Spot it by the naming: **`Xxx()` public + `XxxCore()` protected virtual**. The same shape appears as
`TryReadTemperature` → `TryReadTemperatureCore` in `Bmx280Base`, and as `Dispose()` → `Dispose(bool)` everywhere
(§5.3).

### 4.3 Factory: "give me the right one; I don't want to know how"

A **factory** is any code whose job is to *create* objects, so callers don't hard-code which concrete class to use.
dotnet/iot has four flavors:

| Flavor | Real code | What it decides |
|---|---|---|
| **Static factory method** | `I2cDevice.Create(settings)`, `SpiDevice.Create(...)`, `PwmChannel.Create(...)` | Which platform class (`UnixI2cDevice`, BeagleBone PWM, ...). You get the **abstract** type back. |
| **Selection factory** (runtime detection) | `GpioController.GetBestDriverForBoard()` → reads `/proc/cpuinfo`, picks a driver; `UnixDriver.Create()` tries libgpiod v1 → v2 → sysfs | Which strategy to plug into the controller (lecture 001 §7) |
| **Try-factory** (fail softly) | `GpioDriver.TryCreate(() => new LibGpiodDriver(0), out var driver)` returns `false` instead of throwing on `PlatformNotSupportedException` or `DllNotFoundException` | Lets the selection factory try candidates in order |
| **Generic factory** | `Board.Create()` → `CreateBoardInternal<RaspberryPiBoard>()`, then `CreateBoardInternal<GenericBoard>()`, each `new T(); board.Initialize();` | Same "try in order" idea, with the type as a parameter |

```csharp
// GpioDriver.cs: the Try-factory
public static bool TryCreate<T>(Func<T> creationAction, [NotNullWhen(true)] out T? driver)
    where T : class, IDisposable
{
    try { driver = creationAction(); }
    catch (Exception x) when (x is PlatformNotSupportedException || x is DllNotFoundException)
    {
        driver = null;
        return false;
    }
    return true;
}
```

Note how it takes a **delegate** (`Func<T>`, lecture 002): "here's *how* to make one; you decide *whether* it worked."
That's why the selection code reads like a list of recipes to try.

**Why factories matter:** the decision "which concrete class?" lives in **one place**. Callers depend only on the
abstract type. That's the same reason `builder.Services.AddSingleton<IFoo, Foo>()` exists in ASP.NET Core: the DI
container is a big, configurable factory, and your classes ask for `IFoo` without knowing it's `Foo`.

Even the maintainers note where this could go next. The comment on `GetBestDriverForBoardOnWindows()` says it
*"feels like it needs a driver-based pattern"* where each driver reports whether it fits the current machine (a
**plugin discovery** design). A hard-coded `switch` is simple; plugin discovery scales better. Recognizing that
trade-off is exactly the architectural reading you want to practice.

### 4.4 Adapter: an I2C chip that becomes a GPIO driver

The MCP23017 is a chip on the I2C bus that provides 16 GPIO pins. Its binding **inherits from `GpioDriver`**, so it
*adapts* "I2C register writes" into "the GPIO driver contract":

```csharp
public abstract partial class Mcp23xxx : GpioDriver { ... }     // an I2C/SPI binding that IS a GpioDriver

using var mcp = new Mcp23017(i2cDevice);
using var pins = new GpioController(mcp);        // the Strategy slot accepts it like any other driver
pins.OpenPin(0, PinMode.Output);
pins.Write(0, PinValue.High);                    // → Mcp23xxx writes the OLAT register over I2C
```

Strategy (§4.1) is what makes this possible: because the controller only knows the abstract `GpioDriver`, anything
that fulfills that contract can plug in. `VirtualGpioController` is the other direction: a **composite** that makes
pins from several controllers look like one controller.

### 4.5 The patterns in one table

| Pattern | Question it answers | dotnet/iot example | Smell that tells you it's there |
|---|---|---|---|
| Strategy | "How do I swap *how* something is done?" | `GpioController` + `GpioDriver` | A field of an abstract/interface type that methods delegate to |
| Template Method | "How do I fix the outline but vary one step?" | `OpenPin` → `OpenPinCore` | `Xxx()` + `protected virtual XxxCore()` |
| Factory | "Who decides which concrete class?" | `I2cDevice.Create`, `GetBestDriverForBoard`, `TryCreate` | `static ... Create(...)` returning an abstract type |
| Adapter | "How do I make X fit contract Y?" | `Mcp23xxx : GpioDriver` | A class inheriting a contract from a different domain |
| Composite | "How do I treat many as one?" | `VirtualGpioController` | Holds a collection of the same abstraction it implements |
| Observer | "How do I tell others when something happens?" | events (lecture 002) | `event`, callbacks, `IObservable<T>` |

These all go into the cross-repo catalog: [`../../lectures/000-pattern-catalog.md`](../../lectures/000-pattern-catalog.md).

---
## 5. `IDisposable`: deterministic cleanup

### 5.1 The problem

.NET's garbage collector frees **memory**, eventually, whenever it decides to. It knows nothing about the other
things your objects hold:

| Resource | In dotnet/iot | What happens if nobody releases it |
|---|---|---|
| A file descriptor | `/dev/i2c-1`, `/dev/gpiochip0`, `/dev/spidev0.0` | fd leak; the device stays open |
| A claimed GPIO line | a libgpiod line request | Anyone else who tries to claim it while this process lives gets **"Device or resource busy"** (the kernel only frees it when the process exits) |
| A thread | `LibGpiodV2EventObserver`'s observer threads | the process can't shut down cleanly; lines stay reserved |
| A timer | `ButtonBase._holdingTimer` | callbacks firing on a "dead" object |
| A native handle | libgpiod chip / request pointers | native memory leak |

These need to be released **at a known moment**, not "sometime". That's what `IDisposable` is for:

```csharp
public interface IDisposable
{
    void Dispose();     // "release what you hold, now"
}
```

It's the C#-shaped version of the `close()`/`free()`/`gpiod_chip_close()` calls you'd write in C. The interface on
the declaration line (§3) is the label that says *this one needs it*.

### 5.2 `using`: Dispose that can't be forgotten

```csharp
using GpioController controller = new();      // "using declaration": disposed at the end of the enclosing block
controller.OpenPin(18, PinMode.Output);
...
// ← controller.Dispose() runs here automatically, even if an exception was thrown above
```

The compiler turns it into a `try/finally`:

```csharp
GpioController controller = new();
try
{
    controller.OpenPin(18, PinMode.Output);
    ...
}
finally
{
    controller?.Dispose();
}
```

That's the C `goto cleanup;` pattern, done by the compiler. There's also the older block form,
`using (var x = ...) { ... }`, which you saw in `PerformBusScan` (lecture 002 §8.2): each probe device is disposed as
soon as its block ends, before the next address is tried.

### 5.3 The `Dispose(bool)` pattern

Almost every disposable class in dotnet/iot has **two** dispose methods. `Bmxx80Base`:

```csharp
public void Dispose()                       // what callers use (the IDisposable contract)
{
    Dispose(true);
    GC.SuppressFinalize(this);              // "no need for the finalizer to run later"
}

protected virtual void Dispose(bool disposing)   // what subclasses override (Template Method, §4.2)
{
    _i2cDevice?.Dispose();
    _i2cDevice = null!;
}
```

- `Dispose()` is the fixed outline; `Dispose(bool)` is the overridable step. A subclass adds its own cleanup and
  then calls `base.Dispose(disposing)`, so the whole chain runs.
- `disposing == true` means "called from `Dispose()`, so it's safe to touch other managed objects".
  `disposing == false` would mean "called from a **finalizer**" (a GC-time last resort), when other objects may
  already be gone. Few classes in dotnet/iot have finalizers, but the shape is kept for consistency.

### 5.4 Disposing twice must be harmless

Guideline: `Dispose` should be safe to call more than once. dotnet/iot does it in two ways:

```csharp
// (a) a "disposed" flag: GpioButton.cs
protected override void Dispose(bool disposing)
{
    if (_disposed) { return; }
    ...
    _disposed = true;
}

// (b) null-out after releasing: UnixI2cDevice.cs
protected override void Dispose(bool disposing)
{
    if (_bus != null)
    {
        if (_shouldDisposeBus) { _bus.Dispose(); } else { _bus.RemoveDeviceNoCheck(_deviceAddress); }
        _bus = null!;
    }
    base.Dispose(disposing);
}
```

That matters because of the BME280 sample in lecture 001:

```csharp
using I2cDevice i2cDevice = I2cDevice.Create(i2cSettings);
using Bme280 bme80 = new Bme280(i2cDevice);
```

At the end of the block, `bme80` is disposed first (reverse order), which disposes `i2cDevice`. Then the `using` on
`i2cDevice` disposes it **again**. That's harmless only because of (b). It's also a hint that **two parties think
they own the same object**, which is the subject of the next section.

### 5.5 Disposal cascades down the ownership tree

```
 using GpioController controller = new();          // you own the controller
 └─ controller.Dispose()
     ├─ ClosePinCore(pin) for every open pin
     └─ _driver.Dispose()                          // the controller owns its driver
         └─ LibGpiodV2Driver disposes its line requests and its event observer
             └─ LibGpiodV2EventObserver.Dispose(): _shouldExit = true; t.Join() on every observer thread
                  // "this should only return after all requests have been released" (comment in the source)
```

One `Dispose()` at the top releases everything below, **if** every level knows what it owns.

---

## 6. Ownership: who is responsible for cleaning up?

### 6.1 The idea

**The owner of a resource is the one object responsible for disposing it.** Everyone else only *borrows* it. You
already do this in C without the name: a comment like "caller must free the returned buffer" or "the driver keeps
this pointer; do not free" is an ownership rule. C# doesn't enforce ownership (Rust does); it's a **convention** the
code has to state and follow.

Two failure modes, both familiar from C:

| Mistake | C version | C# version |
|---|---|---|
| **Nobody** disposes | memory/fd leak | GPIO line stays claimed until the process exits ("busy" for any other user of it) |
| **Two** parties dispose, or one disposes while the other still uses it | double `free`, use-after-free | `ObjectDisposedException`, or a device closed out from under another binding |

### 6.2 dotnet/iot's rule (from `Documentation/Devices-conventions.md`)

> *If object cannot be concurrently used by multiple devices then it should be disposed* [by the device].

In practice:

| Given to a binding | Shared by others? | Who disposes | Example |
|---|---|---|---|
| `I2cDevice`, `SpiDevice`, `PwmChannel` | No (one chip, one address) | **The binding** (it takes ownership) | `Bmxx80Base.Dispose` → `_i2cDevice?.Dispose()` |
| `GpioController` | **Often** (many devices on one controller) | The binding **by default**, with an opt-out `shouldDispose` flag | `GpioButton`, `Mcp23xxx` |
| A driver passed to `new GpioController(driver)` | No | **The controller** | `GpioController.Dispose` → `_driver?.Dispose()` |

### 6.3 The `shouldDispose` pattern, read closely

`GpioButton`'s constructor:

```csharp
public GpioButton(int buttonPin, ..., GpioController? gpio = null, bool shouldDispose = true, ...)
{
    _gpioController = gpio ?? new GpioController();          // use yours, or make my own
    _shouldDispose = gpio == null ? true : shouldDispose;     // ← the ownership decision
    ...
}
```

Read the second line as two rules:

1. **"If I created it, I own it."** No controller passed in → the button made its own → `_shouldDispose` is forced
   to `true`.
2. **"If you gave it to me, you choose."** A controller passed in → honor the caller's `shouldDispose`.

And `Dispose` acts on that decision:

```csharp
_gpioController.UnregisterCallbackForPinValueChangedEvent(_buttonPin, PinStateChanged);   // stop listening first
if (_shouldDispose) { _gpioController?.Dispose(); }       // I own it: release it entirely
else { _gpioController.ClosePin(_buttonPin); }            // I borrowed it: clean up only MY pin, leave the rest
```

`Mcp23xxx` states the same rule in one expression: `_shouldDispose = shouldDispose || gpioController is null;`.

### 6.4 An example where getting it wrong bites

Three buttons sharing one controller:

```csharp
using var controller = new GpioController();                 // I (the app) own the controller
var start = new GpioButton(5, gpio: controller, shouldDispose: false);   // borrowers
var stop  = new GpioButton(6, gpio: controller, shouldDispose: false);
var estop = new GpioButton(13, gpio: controller, shouldDispose: false);
...
start.Dispose();     // closes only pin 5; the controller keeps working for stop and estop ✅
```

With the default `shouldDispose: true`, `start.Dispose()` would dispose the **shared** controller, and `stop` and
`estop` would silently stop working. That's the C# version of one module freeing a buffer another module still
uses.

```
 OWNERSHIP TREE (who disposes whom)
 app ──owns──► controller ──owns──► driver ──owns──► observer threads
  │
  ├──owns──► start (GpioButton) ─borrows─┐
  ├──owns──► stop  (GpioButton) ─borrows─┼──► controller   (borrowers: close only their own pin)
  └──owns──► estop (GpioButton) ─borrows─┘
```

### 6.5 Ownership questions to ask of any API

1. If I pass this object in, **does the callee keep it** after the call returns? (Look for a field assignment.)
2. If it keeps it, **will it dispose it**? (Look at its `Dispose`, and for a `shouldDispose`/`leaveOpen` flag.
   `leaveOpen` is the name the .NET BCL uses on streams: `new StreamReader(stream, leaveOpen: true)`.)
3. If a method **returns** a disposable, am I now the owner? (For factories like `I2cDevice.Create`: yes.)

---

## 7. UnitsNet: numbers that know their units

### 7.1 What "every sensor binding returns these types" means

Look at the BME280's API again:

```csharp
public abstract bool TryReadTemperature(out Temperature temperature);   // Bmxx80Base.cs
public abstract bool TryReadPressure(out Pressure pressure);
public bool TryReadHumidity(out RelativeHumidity humidity);             // Bme280.cs
public bool TryReadAltitude(Pressure seaLevelPressure, out Length altitude);   // Bmx280Base.cs
```

`Temperature`, `Pressure`, `RelativeHumidity` and `Length` are **not** `double`s. They're `struct`s from the
[UnitsNet](https://github.com/angularsen/UnitsNet) NuGet package (dotnet/iot pins `UnitsNet` 5.75.1 in
`eng/Versions.external.props`). Each stores a value **together with its unit**, and converts on request:

```csharp
if (bme.TryReadTemperature(out Temperature t))
{
    double c = t.DegreesCelsius;       // 25.08
    double f = t.DegreesFahrenheit;    // 77.14
    double k = t.Kelvins;              // 298.23
}

Pressure p = Pressure.FromHectopascals(1013.25);
double inHg = p.InchesOfMercury;
```

So *"every sensor binding returns these types"* means: **dotnet/iot's convention is that a binding's public API
hands you physical quantities as UnitsNet types, never as bare numbers.** It's rule #1 under "Units" in
`Documentation/Devices-conventions.md`.

### 7.2 Why bother?

| With `double` | With UnitsNet |
|---|---|
| `double temp = sensor.Read();` Is that °C, °F, or raw counts? Check the docs and hope. | `Temperature t` can't be mistaken; you ask for the unit you want. |
| `var total = pressureInPa + pressureInHpa;` compiles and is silently wrong by 100× | Arithmetic between `Pressure` values converts correctly |
| Unit mistakes surface in the field | Many surface at compile time, because the types don't match |

(The classic cautionary tale: the Mars Climate Orbiter was lost in 1999 because one team's software produced
pound-force-seconds where another expected newton-seconds.)

The cost is a dependency and a little ceremony. For a library used by thousands of people with different unit
habits, it's worth it.

### 7.3 Where the unit gets attached

In the binding, at the very last step: `return Temperature.FromDegreesCelsius(temp);` (§1.2). The math inside is
plain `double`; the **public boundary** is typed. That's a good general rule: be strict at API boundaries, pragmatic
inside.

---

## 8. Testing hardware code without hardware

### 8.1 What a unit test is

A **unit test** is a small method that runs a piece of your code with controlled inputs and **asserts** the output
is what you expect. It runs in milliseconds, on any machine, with no hardware. dotnet/iot uses **xUnit**:

```csharp
public class GpioControllerSoftwareTests : IDisposable      // one test class
{
    [Fact]                                                   // "this method is a test"
    public void PinCountReportedCorrectly()
    {
        var ctrl = new GpioController(_mockedGpioDriver.Object);   // arrange
        Assert.Equal(28, ctrl.PinCount);                           // act + assert
    }
}
```

| xUnit piece | Meaning |
|---|---|
| `[Fact]` | A test with no parameters |
| `[Theory]` + `[InlineData(1, 2)]` | The same test run with several parameter sets |
| `Assert.Equal`, `Assert.True`, `Assert.Throws<T>` | Checks: fail the test if not satisfied |
| The test class **constructor** | Runs before **each** test (xUnit makes a new instance per test): setup |
| The test class's `Dispose()` | Runs after each test: teardown. `IDisposable` again, used as a test lifecycle hook. |

Run them with `dotnet test`.

### 8.2 The seam: why abstraction makes this possible

Here's the payoff promised in §0. A unit test needs to replace the hardware with something controllable. That's only
possible where the code depends on an **abstract type** you can substitute: a **seam**.

```
 production                                  test
 ──────────                                  ────
 Bme280 ──► I2cDevice (abstract)             Bme280 ──► I2cDevice (abstract)
              │                                           │
              ▼                                           ▼
        UnixI2cDevice → /dev/i2c-1 → chip          SimulatedI2cDevice: a byte[256] register file
```

`Bme280` can't tell the difference: it only knows `I2cDevice`. **The Strategy pattern from §4.1 and the testability
you get here are the same design decision.** Code that does `new UnixI2cDevice(...)` internally would have no seam
and couldn't be tested this way.

### 8.3 Test doubles: dummy, stub, fake, mock

"Test double" is the umbrella term (like a stunt double) for anything standing in for a real dependency. The kinds
differ in how smart they are:

| Kind | What it is | dotnet/iot example |
|---|---|---|
| **Dummy** | Exists only to fill a parameter; never really used | `DummyGpioDriver` (`src/devices/Board/`): zero pins, every method throws "No such pin". `VirtualGpioController` passes one to its base constructor because it needs *a* driver. |
| **Stub** | Returns canned answers; no logic | Parts of `Mcp23xxxTest.I2cDeviceMock`: `WriteByte`, `ReadByte`, `WriteRead` just `throw new NotImplementedException()` ("Don't need these.") |
| **Fake** | A **working, simplified implementation** | `SimulatedI2cDevice` (`src/devices/Bmxx80/tests/`): a real little register file. Writes set a register pointer and store bytes; reads return them. It behaves like a chip. |
| **Mock** | A double you **program with expectations** and later **verify** it was called as expected | `Mock<MockableGpioDriver>` with Moq in `GpioControllerSoftwareTests` |
| **Spy** | Records how it was used so the test can inspect it | `MockableGpioDriver` keeps the registered callback (`_event = callback;`) so the test can fire it later |

(Names are used loosely in practice: `Mcp23xxxTest` calls its hand-written fakes "Mock". Judge by behavior, not by
name.)

### 8.4 A fake in action: testing the datasheet math

`Bmp280Tests.cs` loads the **datasheet's own worked example** into the fake chip's registers, then checks the binding
gets the datasheet's answer:

```csharp
public Bmp280SensorTests()
{
    _i2cDevice = new SimulatedI2cDevice();
    _i2cDevice.SetRegister(0xD0, 0x58);                                    // chip ID = BMP280
    _i2cDevice.SetRegister((int)Bmx280Register.DIG_T1, 27504);             // calibration from the datasheet example
    _i2cDevice.SetRegister((int)Bmx280Register.DIG_T2, 26435);
    _i2cDevice.SetRegister((int)Bmx280Register.DIG_T3, -1000);
    ...
    _i2cDevice.SetRegister(0xFA, 0x7E);                                    // raw temperature 0x7EED00 >> 4 = 519888 (the datasheet's adc_T)
    _i2cDevice.SetRegister(0xFB, 0xED);
    _i2cDevice.SetRegister(0xFC, 0x00);
}

// This runs the calculation with the sample values defined in [the BMP280 datasheet]
[Fact]
public void CalculationWithSampleValues()
{
    Bmp280 sensor = new Bmp280(_i2cDevice);
    sensor.TemperatureSampling = Sampling.HighResolution;
    sensor.TryReadTemperature(out Temperature temperature);
    Assert.Equal(25.08, temperature.DegreesCelsius, 2);                    // the datasheet's answer
    sensor.TryReadPressure(out Pressure pressure);
    Assert.Equal(100653.27, pressure.Pascals, 2);
    sensor.Dispose();
}
```

This is what you'd do on the bench (feed known inputs, compare with the datasheet), turned into code that runs on
every commit. It tests layers L1–L4 of §1.2 without a chip.

### 8.5 A mock in action: Moq

A **fake** implements real behavior. A **mock** is generated for you by a library (**Moq**), and you tell it how to
behave per test:

```csharp
// GpioControllerSoftwareTests.cs (trimmed)
_mockedGpioDriver = new Mock<MockableGpioDriver>(MockBehavior.Default);   // Moq builds a subclass at run time
_mockedGpioDriver.CallBase = true;                                          // unset members call the real base code

[Fact]
public void WriteInputPinDoesNotThrow()
{
    _mockedGpioDriver.Setup(x => x.OpenPinEx(1));                                            // expect OpenPin(1)
    _mockedGpioDriver.Setup(x => x.IsPinModeSupportedEx(1, It.IsAny<PinMode>())).Returns(true); // answer "yes"
    _mockedGpioDriver.Setup(x => x.SetPinModeEx(1, It.IsAny<PinMode>()));
    _mockedGpioDriver.Setup(x => x.GetPinModeEx(1)).Returns(PinMode.Input);
    var ctrl = new GpioController(_mockedGpioDriver.Object);                                 // the mock IS a GpioDriver

    ctrl.OpenPin(1, PinMode.Input);
    ctrl.Write(1, PinValue.High);                                                            // must not throw
}

public void Dispose() => _mockedGpioDriver.VerifyAll();   // after each test: was every Setup actually called?
```

| Moq piece | Meaning |
|---|---|
| `new Mock<T>()` | Generate a stand-in subclass of `T` at run time |
| `.Object` | The stand-in instance to hand to the code under test |
| `.Setup(x => x.Method(args))` | "Expect this call" (and optionally `.Returns(value)`) |
| `It.IsAny<T>()` | "Any argument of this type matches" |
| `.VerifyAll()` | Fail if any `Setup` wasn't called: checks the code **did** talk to the driver as expected |

**Why `MockableGpioDriver` exists.** `GpioDriver`'s members are `protected internal` (§2): a test in another
assembly can't call them, so Moq's `Setup(x => x.OpenPin(1))` wouldn't compile. The workaround is a small abstract
subclass that forwards each protected member to a **public** abstract twin with an `Ex` suffix, which Moq *can* set
up:

```csharp
// System.Device.Gpio.Tests/MockableGpioDriver.cs (trimmed)
public abstract class MockableGpioDriver : GpioDriver
{
    private PinChangeEventHandler? _event;

    public abstract void OpenPinEx(int pinNumber);
    protected override void OpenPin(int pinNumber) => OpenPinEx(pinNumber);     // protected → public twin

    public abstract void AddCallbackForPinValueChangedEventEx(int pinNumber, PinEventTypes eventTypes, PinChangeEventHandler callback);
    protected override void AddCallbackForPinValueChangedEvent(int pinNumber, PinEventTypes eventTypes, PinChangeEventHandler callback)
    {
        _event = callback;                                                       // spy: remember the handler
        AddCallbackForPinValueChangedEventEx(pinNumber, eventTypes, callback);
    }

    public void FireEventHandler(int forPin, PinEventTypes eventTypes)          // lets a test simulate an edge
        => _event?.Invoke(this, new PinValueChangedEventArgs(eventTypes, forPin));
    ...
}
```

That's an **Adapter** (§4.4) built purely to create a seam for testing. Test infrastructure uses the same patterns
as production code.

### 8.6 Fake or mock?

| Prefer a **fake** when... | Prefer a **mock** when... |
|---|---|
| The dependency has real behavior you want exercised (a register file, an in-memory database) | You care about **interactions**: *was* `OpenPin(1)` called, with what, how often? |
| Many tests share it | Each test needs different canned answers |
| You want tests to read like the real system | Writing a full fake would be a lot of work |

### 8.7 Test traits: hardware tests vs. software tests

Some tests in `System.Device.Gpio.Tests` really do need a Raspberry Pi with jumper wires. xUnit **traits** tag them
so CI and developers can choose what to run:

```csharp
[Trait("feature", "gpio")]
[Trait("feature", "gpio-libgpiod")]
[Trait("SkipOnTestRun", "Windows_NT")]
public class LibGpiodV1DriverTests : GpioControllerTestBase { ... }
```

Tags seen in that project: `feature` = `gpio`, `gpio-libgpiod`, `gpio-libgpiod2`, `gpio-rpi3`, `gpio-sysfs`, `i2c`,
`pwm`, plus `SkipOnTestRun` = `Windows_NT`. A test run can filter on them, e.g.
`dotnet test --filter "feature=i2c"` to run only the I2C tests, or `--filter "feature!=gpio"` to skip GPIO hardware
tests. (⚠ The exact filters the upstream CI uses are in its build scripts; not checked here.)

### 8.8 Sketch: the failing test for #2403

Everything above combines into the first step of any bug fix, **a test that fails because of the bug**:

```csharp
// SKETCH: modeled on GpioControllerSoftwareTests; not compiled or run yet
[Fact]
public void ValueChanged_SenderIsThePin()
{
    _mockedGpioDriver.Setup(x => x.OpenPinEx(1));
    _mockedGpioDriver.Setup(x => x.AddCallbackForPinValueChangedEventEx(1, It.IsAny<PinEventTypes>(), It.IsAny<PinChangeEventHandler>()));
    var ctrl = new GpioController(_mockedGpioDriver.Object);
    GpioPin pin = ctrl.OpenPin(1);

    object? seenSender = null;
    pin.ValueChanged += (sender, e) => seenSender = sender;            // subscribe ON the pin

    _mockedGpioDriver.Object.FireEventHandler(1, PinEventTypes.Rising); // simulate an edge (spy, §8.3)

    Assert.Same(pin, seenSender);   // today: FAILS, since seenSender is the mock driver (it invokes with `this`)
}
```

That's experiment **E1** from the #2403 briefing, written as a unit test. It runs on your Mac.

---

## 9. Everything together

```
                         ┌──────────────── your app ─────────────────┐
                         │ using var bus = I2cDevice.Create(...)      │  ← Factory (§4.3); you own it (§6)
                         │ using var bme = new Bme280(bus)            │  ← binding takes ownership (§6.2)
                         │ bme.TryReadTemperature(out Temperature t)  │  ← UnitsNet return type (§7)
                         └───────────────────┬───────────────────────┘
                                             │ Template Method: TryReadTemperature → …Core (§4.2)
                         ┌───────────────────▼───────────────────────┐
                         │ Bme280 : Bmx280Base : Bmxx80Base           │  ← inheritance chain (§2.1)
                         │   : IDisposable                            │  ← capability label (§3)
                         └───────────────────┬───────────────────────┘
                                             │ depends on the ABSTRACT I2cDevice: the seam
                        ┌────────────────────┴─────────────────────┐
                        ▼ production (Strategy §4.1)                ▼ test (§8)
                 UnixI2cDevice → /dev/i2c-1                 SimulatedI2cDevice (fake) or a Moq mock
```

---

## 10. Try it

On your Mac, no hardware. `dotnet new console`, then `dotnet add package UnitsNet` for #2 and #3. Expected outputs
are predicted; run them to confirm.

**Try-it 1: `using` really runs Dispose, even on an exception**
```csharp
try
{
    using var r = new Resource("A");
    throw new Exception("boom");
}
catch (Exception ex) { Console.WriteLine($"caught {ex.Message}"); }
// Expected: "A disposed" BEFORE "caught boom"

class Resource(string name) : IDisposable
{
    public void Dispose() => Console.WriteLine($"{name} disposed");
}
```

**Try-it 2: UnitsNet conversions** (`dotnet add package UnitsNet`)
```csharp
using UnitsNet;
var t = Temperature.FromDegreesCelsius(25.08);
Console.WriteLine($"{t.DegreesFahrenheit:F2} °F, {t.Kelvins:F2} K");   // Expected: 77.14 °F, 298.23 K
var p = Pressure.FromHectopascals(1006.53);
Console.WriteLine(p.Pascals);                                            // Expected: 100653
```

**Try-it 3: a fake I2C device and a tiny "binding"**
Write `interface IRegisterBus { byte ReadRegister(byte reg); }`, a `FakeBus` backed by a `byte[256]`, and a
`MySensor(IRegisterBus bus)` whose `ReadId()` returns `bus.ReadRegister(0xD0)`. Set `0xD0 = 0x60` in the fake and
check `ReadId()` returns `0x60`. You've just built a seam, a fake, and the chip-ID check from `Bmxx80Base`.

**Try-it 4: ownership bug on purpose**
Make a `Shared : IDisposable` that throws `ObjectDisposedException` when used after `Dispose`. Give it to two
"borrowers", dispose it through one, use it through the other, and watch it fail. Then add a `shouldDispose` flag
and fix it the `GpioButton` way.

**Try-it 5 (stretch): run the real tests**
In a clone of dotnet/iot: `dotnet test src/devices/Bmxx80/tests/` and find `CalculationWithSampleValues` in the
output. (It needs the repo's build infrastructure; if restore fails, note the error in the conversation log. That's
useful information about the repo too.)

---

## 11. Common mistakes

1. **Forgetting `using`** on disposable objects, or disposing a *borrowed* object. Ask the three ownership questions
   (§6.5).
2. **Reading implementation before declarations.** Read the class line, the base class and the interfaces first.
3. **Inheriting when you should compose.** If you only need to *use* something's behavior, hold it in a field (Strategy);
   inherit only for a real "is-a" relationship.
4. **Creating dependencies inside a class** (`new UnixI2cDevice(...)` in a binding). It kills the seam, and the class
   can't be tested without hardware. Take the abstraction in the constructor instead.
5. **Returning bare `double`s** from a sensor API. Use UnitsNet at the boundary.
6. **Mocking everything.** Mocks tied to exact calls break on harmless refactors. Use fakes for behavior, mocks for
   interactions.
7. **Tests that need hardware without a trait**, so they fail on every CI agent.

## 12. Interview relevance

- **"Abstract class vs. interface?"** Answer with §2.2's table and a real example (`GpioDriver` vs. `IDisposable`).
- **"Explain the Strategy and Factory patterns"**: `GpioController` + `GpioDriver` + `GetBestDriverForBoard()` is a
  real, memorable example, and you can connect it to DI containers.
- **"What is `IDisposable` / what does `using` do?"**: deterministic cleanup; `try/finally`; the `Dispose(bool)`
  pattern; idempotency.
- **"How do you unit test code that talks to hardware / a database / an HTTP API?"**: seams via abstractions, fakes
  vs. mocks, with the BMP280 datasheet test as your example. Few candidates can give a hardware example; you can.
- **SOLID in one breath:** Single responsibility (a binding does one chip), Open/closed (new drivers without changing
  the controller), Liskov (any `GpioDriver` works in the controller), Interface segregation (small capability
  interfaces), Dependency inversion (bindings depend on abstract `I2cDevice`, not `UnixI2cDevice`).

## 13. Check yourself

1. What's the difference between a binding and a driver in dotnet/iot? Name one class that is both.
2. Read `public abstract partial class I2cDevice : IDisposable` aloud and list four facts it tells you.
3. Where is the Template Method pattern in `Bmx280Base`? In `GpioController`?
4. Why does `GpioDriver.TryCreate` take a `Func<T>` instead of an already-created driver?
5. In `GpioButton`, when is `_shouldDispose` forced to `true`, and why?
6. What goes wrong if three `GpioButton`s share a controller and all use the default `shouldDispose`?
7. Why is `SimulatedI2cDevice` a fake and not a mock? Why is `Mock<MockableGpioDriver>` a mock?
8. Why couldn't Moq set up `GpioDriver.OpenPin` directly, and what did the repo do about it?
9. Write, in words, the assertion that makes the #2403 test fail today.

## 14. Glossary

| Term | Meaning |
|---|---|
| **Binding** | A C# class that drives one specific chip/module and exposes a domain-level API. |
| **Abstract class** | A base class that can't be instantiated; may hold shared code and require overrides. |
| **Interface** | A contract (members only); a class can implement many. |
| **Composition** | Building behavior by *holding* other objects (has-a) rather than inheriting (is-a). |
| **Strategy** | A swappable algorithm object held behind an abstract type. |
| **Template Method** | A fixed outline method that calls overridable steps. |
| **Factory** | Code whose job is choosing and creating the concrete object. |
| **Adapter** | A class that makes one thing fit another's contract. |
| **Seam** | A place where a dependency can be substituted (usually an abstract parameter). |
| **`IDisposable` / `using`** | Deterministic release of non-memory resources / compiler-generated `try/finally`. |
| **Owner / borrower** | The one responsible for disposing / anyone else using it. |
| **UnitsNet** | A library of value types that carry physical units (`Temperature`, `Pressure`, ...). |
| **Unit test** | A small automated check of one piece of code with controlled inputs. |
| **Test double** | Dummy, stub, fake, mock or spy standing in for a real dependency. |
| **Moq** | A .NET library that generates mocks at run time. |
| **Trait** | An xUnit tag on a test, used to filter test runs. |

## Sources

dotnet/iot `main` @ `1eb0b2f` (2026-09-24):
`src/devices/Bmxx80/{Bmxx80Base,Bmx280Base,Bme280,Bmp280,Bme680}.cs`, `src/devices/Bmxx80/ReadResult/Bme280ReadResult.cs`,
`src/devices/Bmxx80/tests/{SimulatedI2cDevice,Bmp280Tests}.cs`;
`src/System.Device.Gpio/System/Device/Gpio/{GpioController,GpioPin,GpioDriver,PinValue}.cs`,
`src/System.Device.Gpio/System/Device/I2c/{I2cDevice,I2cBus}.cs`, `.../I2c/Devices/UnixI2cDevice.cs`;
`src/devices/Button/{ButtonBase,GpioButton}.cs`; `src/devices/Mcp23xxx/Mcp23xxx.cs`, `src/devices/Mcp23xxx/tests/Mcp23xxxTest.cs`;
`src/devices/Board/{Board,DummyGpioDriver,VirtualGpioController,IDeviceManager}.cs`;
`src/System.Device.Gpio.Tests/{GpioControllerSoftwareTests,MockableGpioDriver,LibGpiodV1DriverTests}.cs`;
`Documentation/Devices-conventions.md`; `eng/Versions.external.props` (UnitsNet 5.75.1).
