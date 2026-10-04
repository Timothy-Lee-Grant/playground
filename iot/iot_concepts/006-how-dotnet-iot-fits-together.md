# Lecture 006: How dotnet/iot fits together, from `new Bme280(...)` down to Linux and back

> **Prompted by:** Timothy, 2026-10-04: "I don't understand how `GpioController` connects to the things called
> devices, what the I2C classes are, how `System.Device.Gpio` connects to `devices`, or how the pieces inside
> `System.Device.Gpio` connect to each other. There's inheritance, composition and factory creation everywhere."
> · **Date:** 2026-10-04 · **Source:** dotnet/iot `main` @ `95384e7` (2026-10-01). Every class name, line of code
> and file path below was read at that commit; nothing here was *run*.
> · **Reading time:** about 40 minutes. You can read it with the code open: every section names the files.

**The whole thing in seven lines**

1. There are **two layers of code**: `System.Device.Gpio` (the *platform layer*: talks to Linux) and
   `Iot.Device.Bindings` (the *device layer*: one class per chip or gadget, e.g. `Bme280`, `GpioButton`).
2. The device layer **never talks to Linux**. It only talks to four **abstract classes** from the platform layer:
   `GpioController`/`GpioDriver` (pins), `I2cDevice`, `SpiDevice`, `PwmChannel`.
3. Each abstract class has **concrete subclasses that do the real work** (`LibGpiodDriver`, `UnixI2cDevice`, …).
   Your code almost never names them.
4. **Factories pick the subclass for you**: `new GpioController()`, `I2cDevice.Create(...)`. They're plain
   `if`/`switch` on "which OS? which board?", with try-this-then-that fallbacks. No reflection, no magic.
5. A binding **holds a reference** (composition) to the abstract object you hand it. It doesn't know or care which
   subclass it got; that's why the same `Bme280` code works on a Pi, over USB on a Mac, or against a fake in a test.
6. **`Board`** is an optional manager (it lives in the bindings package) that creates controllers and buses for you
   and refuses to let two things use the same pin.
7. Some bindings **plug back in underneath**: `Mcp23017` (an I2C chip) *is* a `GpioDriver`; `Ft232HDevice` (a USB
   adapter) *is* a `Board`. The layering is about *roles*, not about which folder a file is in.

---

## 0. The map

Here's the whole library on one page. Read it top to bottom as "who calls whom". Arrows mean "holds a reference to
and calls".

```
 YOUR PROGRAM            var bme = new Bme280(i2cDevice);      var button = new GpioButton(17);
 ───────────────────────────────│────────────────────────────────────────│───────────────────────────
 DEVICE LAYER                   ▼                                        ▼
 Iot.Device.Bindings      Bme280 ─┐ (is-a Bmx280Base                GpioButton (is-a ButtonBase)
 (src/devices/*)                  │  is-a Bmxx80Base)                     │
                                  │ holds                                 │ holds
 ─────────────────────────────────│───────────────────────────────────────│───────────────────────────
 PLATFORM LAYER                   ▼                                        ▼
 System.Device.Gpio         I2cDevice  (abstract)                   GpioController  (concrete class)
 (src/System.Device.Gpio)        ▲ is-a                                    │ holds
   "the contracts"               │                                         ▼
                                 │                                  GpioDriver  (abstract)
                                 │                                         ▲ is-a
   "the implementations"   UnixI2cDevice ──holds──► UnixI2cBus       LibGpiodDriver / LibGpiodV2Driver /
                                                          │          SysFsDriver / RaspberryPi3Driver
 ─────────────────────────────────────────────────────────│─────────────────│─────────────────────────
 LINUX                                                     ▼                 ▼
                                               /dev/i2c-1 (open + ioctl)   libgpiod.so → /dev/gpiochip0,
                                                                           or /sys/class/gpio,
                                                                           or /dev/gpiomem (Pi registers)
 ───────────────────────────────────────────────────────────────────────────────────────────────────────
 HARDWARE                                   the BME280 chip on the I2C wires      the button on pin 17
```

**The one rule that explains the shape:** every arrow from the device layer lands on an *abstract* class (or on
`GpioController`, which holds an abstract `GpioDriver`). The concrete classes at the bottom are chosen at runtime by
a factory. That's the whole architecture; the rest of this lecture is three journeys through it and the exceptions.

---

## 1. Two packages, one dependency direction

**The rule: bindings depend on `System.Device.Gpio`; `System.Device.Gpio` knows nothing about bindings.**

| | `System.Device.Gpio` | `Iot.Device.Bindings` |
|---|---|---|
| What it is | The platform layer: pins, I2C, SPI, PWM, and how to reach them on Linux/Windows | ~139 device folders: sensors, displays, motors, port expanders, adapters, plus `Board` |
| Source | `src/System.Device.Gpio/` | `src/devices/<DeviceName>/` (one folder and one `.csproj` per device) |
| Namespaces | `System.Device.Gpio`, `System.Device.I2c`, `System.Device.Spi`, `System.Device.Pwm` | `Iot.Device.<Name>` (e.g. `Iot.Device.Bmxx80`, `Iot.Device.Button`, `Iot.Device.Board`) |
| NuGet package | `System.Device.Gpio` | `Iot.Device.Bindings` (depends on `System.Device.Gpio`) |

One detail that confuses people when browsing the repo: each device folder has its **own** `.csproj` (so you can
build and test one device alone), but the NuGet package is **one** DLL. `src/Iot.Device.Bindings/Iot.Device.Bindings.csproj`
has a build step, `GetCompileItemsFromProjects`, that asks every device project for its `.cs` files and compiles them
all into `Iot.Device.Bindings.dll`. So "the `Button` project" exists for development; users get it inside the big
package.

**Where `Board` lives matters:** it's in `src/devices/Board/`, so it's part of the *bindings* package, even though it
manages platform-layer objects. It's a convenience on top, not part of the core.

---

## 2. The cast

| Character | Kind | Job | Lives in |
|---|---|---|---|
| **`GpioController`** | concrete class | The front desk for pins: `OpenPin`, `Write`, `Read`, `RegisterCallback…`. Keeps track of which pins are open, checks arguments, then forwards to its driver | `System.Device.Gpio/.../Gpio/GpioController.cs` |
| **`GpioDriver`** | **abstract** class | The contract every "way of reaching pins" must fulfil: `OpenPin`, `Read`, `Write`, `SetPinMode`, callbacks | `.../Gpio/GpioDriver.cs` |
| `LibGpiodDriver`, `LibGpiodV2Driver`, `SysFsDriver` | concrete drivers (via abstract `UnixDriver`) | Reach pins through Linux: the libgpiod C library (two versions), or the older `/sys/class/gpio` files | `.../Gpio/Drivers/` |
| `RaspberryPi3Driver` | concrete driver that **wraps another driver** | Pi-specific: on Linux, writes the Pi's GPIO registers directly through `/dev/gpiomem` (fast) | `.../Gpio/Drivers/` |
| **`GpioPin`** | concrete class | A convenience handle for one pin; every method forwards to its controller | `.../Gpio/GpioPin.cs` |
| **`I2cDevice`** | **abstract** class | "One chip at one address on one I2C bus": `Read`, `Write`, `WriteRead`, plus the factory `Create` | `.../I2c/I2cDevice.cs` |
| **`I2cBus`** | **abstract** class | One I2C bus; hands out `I2cDevice`s for addresses on it | `.../I2c/I2cBus.cs` |
| `UnixI2cDevice`, `UnixI2cBus` | concrete, `internal` | The Linux implementation: open `/dev/i2c-N`, `ioctl` | `.../I2c/` |
| **`SpiDevice`**, **`PwmChannel`** | **abstract** classes | Same pattern as `I2cDevice` (factory `Create`, Unix subclasses) | `.../Spi/`, `.../Pwm/` |
| **Binding** (e.g. `Bme280`, `GpioButton`) | concrete class, often at the end of an inheritance chain | Speaks one chip's or gadget's language (registers, timing, units) using an `I2cDevice` or a `GpioController` it holds | `src/devices/<Name>/` |
| **`Board`** | **abstract** class (+ `RaspberryPiBoard`, `GenericBoard`) | Optional manager: creates controllers/buses, reserves pins so two users can't clash | `src/devices/Board/` |

`internal` on `UnixI2cDevice` and `UnixI2cBus` means your program **can't** name them, even if it wanted to. You can
only get one through the factory, as the abstract type. That's a deliberate design choice: it forces everyone onto
the contract.

---

## 3. The three kinds of connection

You named all three in your question. Here's each one, with what it looks like in the code, so you can recognize it
on sight.

| Connection | Question it answers | Looks like | Example |
|---|---|---|---|
| **Inheritance** ("is-a") | "What contract does this class fulfil?" | `class X : Y` | `class LibGpiodDriver : UnixDriver`, `abstract class UnixDriver : GpioDriver` · `class Bme280 : Bmx280Base`, `abstract class Bmx280Base : Bmxx80Base` |
| **Composition** ("has-a") | "What does this object hold and call?" | a private field of another class's type, set in the constructor | `GpioController` holds `private GpioDriver _driver;` · `Bmxx80Base` holds `protected I2cDevice _i2cDevice;` · `UnixI2cDevice` holds `private UnixI2cBus _bus;` |
| **Factory** ("who builds it?") | "Which concrete class will I actually get?" | a `static` method (or a constructor) that returns the abstract type and contains `if`/`switch`/try-fallback | `I2cDevice.Create(settings)` · `GpioController()`'s `GetBestDriverForBoard()` · `UnixDriver.Create()` · `Board.Create()` |

**How they combine:** inheritance defines the *slots* (an `I2cDevice`-shaped slot), composition *fills* them (a field
of that type), and factories *choose what goes in* (which subclass). When you're lost in a class, ask those three
questions in that order.

**About "dynamic factory creation":** it's less dynamic than it looks. There's no scanning of assemblies or
reflection. The factories check `Environment.OSVersion.Platform`, read the board model from `/proc/cpuinfo`, and try
constructors in order, catching failures. The source even has a comment (in `GpioController.GetBestDriverForBoardOnWindows`)
saying a reflection-based discovery "really feels like it needs" to exist; it doesn't.

---

## 4. Journey 1: a GPIO pin, from `new GpioController()` to Linux

Your program:

```csharp
using GpioController controller = new GpioController();
controller.OpenPin(17, PinMode.Output);
controller.Write(17, PinValue.High);
```

### 4.1 `new GpioController()`: the factory runs

The parameterless constructor doesn't build anything itself. It calls a static factory and passes the result to the
*other* constructor:

```csharp
public GpioController()
    : this(GetBestDriverForBoard())        // 1. pick a driver
{ }

public GpioController(GpioDriver driver)   // 2. store it
{
    _driver = driver;
    _openPins = new ConcurrentDictionary<int, PinValue?>();
    ...
}
```

`GetBestDriverForBoard()` decides, step by step (`GpioController.cs`, around lines 423–490):

| Step | Check | Result |
|---|---|---|
| 1 | Is the OS Windows? | Windows path: read the registry for the board name; a Pi 2/3 → `new RaspberryPi3Driver()`; anything else → `PlatformNotSupportedException` |
| 2 | Linux: read the board model (`RaspberryBoardInfo.LoadBoardInfo()`, which parses `/proc/cpuinfo`) | |
| 3 | Pi 3 / Pi 4 / Zero 2 W / CM3 / CM4 / 400 | Try `RaspberryPi3Driver.CreateInternalRaspberryPi3LinuxDriver()` (needs `/dev/gpiomem`). Worked → `new RaspberryPi3Driver(internalDriver)`. Didn't → `UnixDriver.Create()` |
| 4 | Pi 5 | Find the GPIO chip with 54 lines (the Pi 5's RP1 chip), try `LibGpiodDriver` on it, then `LibGpiodV2Driver`. Neither → exception |
| 5 | Anything else (another Linux board, your Linux desktop) | `UnixDriver.Create()` |

And `UnixDriver.Create()` is itself a small factory with fallbacks:

```csharp
if (TryCreate(() => new LibGpiodDriver(0), out driver))   return driver;   // libgpiod v1 (libgpiod.so.2)
if (TryCreate(() => new LibGpiodV2Driver(0), out driver)) return driver;   // libgpiod v2 (libgpiod.so.3)
if (TryCreate(() => new SysFsDriver(), out driver))       return driver;   // /sys/class/gpio files
throw new PlatformNotSupportedException("No unix driver appears to be runnable");
```

**After construction, the object graph is:**

```
controller : GpioController
   _driver ──────────► e.g. LibGpiodDriver      (field type: GpioDriver; actual object: LibGpiodDriver)
   _openPins: {}                               (which pins are open, and the last value written)
```

That's composition with a factory-chosen part. The pattern's name is **Strategy**: the controller's behavior
("how do I reach a pin?") is a swappable object.

### 4.2 `controller.OpenPin(17, PinMode.Output)`: bookkeeping, then forward

```csharp
public GpioPin OpenPin(int pinNumber)
{
    if (IsPinOpen(pinNumber)) return _gpioPins[pinNumber];
    OpenPinCore(pinNumber);                          // ──► _driver.OpenPin(pinNumber)
    _openPins.TryAdd(pinNumber, null);
    _gpioPins[pinNumber] = new GpioPin(pinNumber, this);
    return _gpioPins[pinNumber];
}

protected virtual void OpenPinCore(int pinNumber) => _driver.OpenPin(pinNumber);
```

| | What the controller does itself | What it hands to the driver |
|---|---|---|
| `OpenPin` | remembers the pin is open; creates a `GpioPin` handle | `_driver.OpenPin(17)`: the driver asks Linux for line 17 |
| `SetPinMode` (inside the `OpenPin(pin, mode)` overload) | if a value was remembered, passes it along | `_driver.SetPinMode(17, Output)` |
| `Write(17, High)` | remembers the value; checks the pin is an output | `_driver.Write(17, High)` |

Notice `OpenPinCore` is `protected virtual`. That's a hook: a subclass of `GpioController` can override *just* that
step. §7 shows who does.

### 4.3 Where it reaches Linux

What `_driver.Write(17, High)` does depends on which driver the factory picked:

| Driver | Reaches the hardware through |
|---|---|
| `LibGpiodDriver` / `LibGpiodV2Driver` | P/Invoke calls into the C library `libgpiod.so.2` / `libgpiod.so.3`, which talks to the kernel's GPIO character device (`/dev/gpiochip0`) |
| `SysFsDriver` | Writes text files under `/sys/class/gpio` (the old, deprecated kernel interface) |
| `RaspberryPi3Driver` (on Linux) | Holds a `RaspberryPi3LinuxDriver`, which memory-maps `/dev/gpiomem` and writes the Pi's GPIO registers directly |

`RaspberryPi3Driver` is worth a second look: it **is-a** `GpioDriver` and **has-a** `GpioDriver` (`private GpioDriver
_internalDriver;`), which is either the Linux register driver or a Windows driver. It's one driver that delegates to
another: composition inside the driver layer too.

### 4.4 Why can't my program call `driver.Write` directly?

Look at how `GpioDriver` declares its methods:

```csharp
public abstract class GpioDriver : IDisposable
{
    protected internal abstract void Write(int pinNumber, PinValue value);
    protected internal abstract PinValue Read(int pinNumber);
    ...
}
```

`protected internal` means: callable from **inside the `System.Device.Gpio` assembly** (so `GpioController` can call
it) **or from a subclass** (so a driver in the bindings package can override it). Your program is neither, so it
can't call `driver.Write(...)`. You *can* create a driver and pass it in (`new GpioController(new SysFsDriver())`),
but every pin operation has to go through the controller. That's how the controller's bookkeeping can't be skipped.

### 4.5 `GpioPin`: the same thing, shaped differently

`OpenPin` returns a `GpioPin`. Every one of its methods is a one-line forward to the controller:

```csharp
public virtual void Write(PinValue value) => _controller.Write(_pinNumber, value);
public virtual PinValue Read()            => _controller.Read(_pinNumber);
```

So `controller.Write(17, High)` and `pin.Write(High)` are the same call. `GpioPin` exists for convenience (pass one
pin around instead of a controller plus a number). It owns nothing.

---

## 5. Journey 2: an I2C sensor, from `Bme280` down to `ioctl`

Your program:

```csharp
var settings = new I2cConnectionSettings(busId: 1, deviceAddress: 0x76);
using I2cDevice i2c = I2cDevice.Create(settings);
using var bme = new Bme280(i2c);
Bme280ReadResult result = bme.Read();
```

### 5.1 `I2cDevice.Create(settings)`: the factory

```csharp
public static I2cDevice Create(I2cConnectionSettings settings)
{
    if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        throw new PlatformNotSupportedException("There's no default I2C driver on Windows available");
    else
        return new UnixI2cDevice(UnixI2cBus.Create(settings.BusId), settings.DeviceAddress, shouldDisposeBus: true);
}
```

Two objects get built, and the abstract class's own `static` method builds its own subclass:

| Object | What its constructor/factory does |
|---|---|
| `UnixI2cBus` (via `UnixI2cBus.Create(1)`) | Builds the path `/dev/i2c-1`, `open`s it, asks the kernel (`ioctl I2C_FUNCS`) what the bus supports, keeps the file descriptor |
| `UnixI2cDevice(bus, 0x76, shouldDisposeBus: true)` | Stores the bus and the address. `shouldDisposeBus: true` means "this device owns the bus; close it when I'm disposed" |

**What `Create` doesn't do:** it doesn't check that a chip is actually at 0x76. Opening the bus file succeeds whether
or not anything is wired to it. The first real check happens in the binding (next step).

### 5.2 `new Bme280(i2c)`: three constructors run, top of the chain first

`Bme280`'s inheritance chain is `Bme280 → Bmx280Base → Bmxx80Base`. C# runs base constructors first, so:

```
new Bme280(i2c)
  └─ Bmx280Base(deviceId, i2c)
       └─ Bmxx80Base(deviceId, i2c)
            _i2cDevice = i2cDevice ?? throw new ArgumentNullException(...);   ← composition: store the reference
            _i2cDevice.WriteByte((byte)Bmxx80Register.CHIPID);                 ← first real I2C traffic
            byte readSignature = _i2cDevice.ReadByte();
            (throws if the signature isn't the expected chip ID)               ← "is a BME280 really there?"
            ... read calibration data from the chip ...
       Bmx280Base: BMP280/BME280-specific setup
  Bme280: humidity-specific setup
```

Why a three-level chain? The `Bmxx80` folder covers four related Bosch chips (BMP280, BME280, BME680, BMP680-ish
variants). `Bmxx80Base` holds what they all share (the chip-ID check, reading registers over I2C); `Bmx280Base` what
the 280 family shares (temperature/pressure); `Bme280` adds humidity. Each level is a real difference in the
hardware, not decoration.

**The object graph after construction:**

```
bme : Bme280                       (is-a Bmx280Base, is-a Bmxx80Base)
  _i2cDevice ─────► UnixI2cDevice  (field type: I2cDevice; Bme280 never sees "Unix")
                       _bus ─────► UnixI2cBus  (file descriptor for /dev/i2c-1)
                       address 0x76
```

### 5.3 `bme.Read()`: the call going down

| Layer | Call |
|---|---|
| `Bme280` / bases | "to read the temperature register: write its address, then read 3 bytes" → `_i2cDevice.WriteByte(register); _i2cDevice.Read(bytes);` |
| `UnixI2cDevice` | `_bus.Write(_deviceAddress, buffer)` / `_bus.Read(_deviceAddress, buffer)`: adds the address it remembered |
| `UnixI2cBus` | builds an `i2c_rdwr_ioctl_data` message and calls `Interop.ioctl(BusFileDescriptor, I2C_RDWR, …)` |
| Linux | the kernel's I2C driver clocks the bytes out on the wires |

Then the bytes come back up, and `Bme280` turns raw register values into `Temperature`, `Pressure` and
`RelativeHumidity` (UnitsNet types) using the calibration data.

### 5.4 The point of the whole design, in one sentence

**`Bme280` only knows "I have an `I2cDevice`".** Hand it any subclass and the same code runs:

| You pass | Where the bytes actually go |
|---|---|
| `I2cDevice.Create(...)` on a Pi | `/dev/i2c-1` on the Pi |
| `ft232h.CreateI2cDevice(...)` on your Mac | over USB to an FT232H adapter, then onto the I2C wires (§8.2) |
| A fake/mock `I2cDevice` in a unit test | nowhere: the test scripts the bytes (the same seam your #2328 tests used for GPIO) |

That's what "program against abstractions" buys, concretely.

---

## 6. Journey 3: `GpioButton`, a binding built on a controller

```csharp
using var button = new GpioButton(17);       // or: new GpioButton(17, gpio: myController, shouldDispose: false)
button.Press += (s, e) => Console.WriteLine("pressed");
```

The constructor (`src/devices/Button/GpioButton.cs`):

```csharp
_gpioController = gpio ?? new GpioController();          // use yours, or build one (factory runs, §4.1)
_shouldDispose  = gpio == null ? true : shouldDispose;   // built it myself → I own it → I dispose it
...
_gpioController.OpenPin(_buttonPin, _gpioPinMode);
_gpioController.RegisterCallbackForPinValueChangedEvent(_buttonPin, PinEventTypes.Falling | PinEventTypes.Rising, PinStateChanged);
```

Three things in four lines:

1. **Composition with an optional default:** the button *has-a* `GpioController`. If you don't give it one, it calls
   the factory itself.
2. **Ownership:** `_shouldDispose` records who must clean the controller up. If the button built it, the button
   disposes it. If you passed yours in, you choose. (This is the convention across bindings.)
3. **The callback path is a round trip:** the button hands a method (`PinStateChanged`) *down* through the
   controller to the driver; later, when the pin changes, the driver's event thread calls it back *up*, and
   `ButtonBase` turns edges into `ButtonDown`, `ButtonUp`, `Press`, `Holding`.

```
  DOWN at construction                                UP when the pin changes
  GpioButton ─RegisterCallback(17, PinStateChanged)─► GpioController ─AddCallback…─► driver
                                                                                     │ (driver's thread
  ButtonBase ◄── PinStateChanged(sender, args) ◄──────────────────────────────────────┘  waits on Linux)
     └─ raises Press / ButtonUp / … to your handler
```

`GpioButton` is-a `ButtonBase` (inheritance: the hardware-independent logic), and has-a `GpioController`
(composition: the hardware access). The split is why the #2328 fix went in `GpioButton` (the pin reading) and not in
`ButtonBase`.

---

## 7. `Board`: the optional manager

So far your program created the platform objects itself. `Board` (in the bindings package) is an alternative front
door:

```csharp
using Board board = Board.Create();                         // factory: which board am I on?
using GpioController gpio = board.CreateGpioController();
I2cDevice i2c = board.CreateI2cDevice(new I2cConnectionSettings(1, 0x76));
```

| Step | What happens |
|---|---|
| `Board.Create()` | Try `new RaspberryPiBoard()` and `Initialize()`; if that throws `NotSupportedException`/`IOException`, try `GenericBoard`; else throw. (Try-and-fall-back again) |
| `board.CreateGpioController()` | Asks the board subclass for its best driver (`TryCreateBestGpioDriver()`), then returns **`new ManagedGpioController(this, driver)`** |
| `board.CreateI2cDevice(...)` | Finds or creates the bus (`CreateOrGetI2cBus`), reserving the bus's pins; on a Pi it can switch those pins into I2C mode |

**What `ManagedGpioController` adds** is one override of the hook from §4.2:

```csharp
internal class ManagedGpioController : GpioController, IDeviceManager
{
    protected override void OpenPinCore(int pinNumber)
    {
        _board.ReservePin(pinNumber, PinUsage.Gpio, this);   // throws if pin is already used for I2C, SPI, PWM or by someone else
        base.OpenPinCore(pinNumber);                         // then the normal path: _driver.OpenPin
    }
}
```

That's the **Template Method** pattern: `GpioController.OpenPin` fixes the outline (check, open, remember) and lets a
subclass change one step. It's also the answer to "what does `Board` give me that `new GpioController()`
doesn't?": **conflict detection.** A plain `GpioController` will happily let you open pin 2 as GPIO while the I2C bus
is using it. The board throws `InvalidOperationException("Pin 2 has already been reserved for I2c by class …")`.

---

## 8. Bindings that plug in *underneath*: the layers are roles, not folders

Here's what usually breaks people's mental model: some classes in `src/devices/` don't sit on top of the platform
layer; they **implement** its abstract classes.

### 8.1 `Mcp23017`: an I2C chip that is a GPIO driver

The MCP23017 is a chip with 16 extra GPIO pins, controlled over I2C. In the code:

```csharp
public abstract partial class Mcp23xxx : GpioDriver      // is-a GpioDriver!
{
    protected BusAdapter _bus;                           // has-a bus (I2C or SPI, via an adapter)
    ...
}
public class Mcp23017 : Mcp23x1x                         // → Mcp23xxx → GpioDriver
{
    public Mcp23017(I2cDevice i2cDevice, ...)            // built on an I2cDevice
}
```

So you can do this, and get a `GpioController` whose "pins" are on an expander chip:

```csharp
I2cDevice i2c = I2cDevice.Create(new I2cConnectionSettings(1, 0x20));
using var expanderPins = new GpioController(new Mcp23017(i2c));
expanderPins.OpenPin(0, PinMode.Output);
expanderPins.Write(0, PinValue.High);
```

Follow the call: the stack goes down, then *loops*:

```
expanderPins.Write(0, High)
  GpioController ──► _driver : Mcp23017 (a GpioDriver)       ← a binding acting as a driver
                         └─► _bus : I2cAdapter ──► I2cDevice (UnixI2cDevice) ──► UnixI2cBus ──► /dev/i2c-1
                                                                                        ▼
                                                                    MCP23017 chip sets its pin 0 high
```

And because it's a `GpioDriver`, **any binding that takes a `GpioController` works on expander pins too**: a
`GpioButton(0, gpio: expanderPins)` would read a button wired to the expander. That's the Adapter pattern (an I2C
chip adapted into the GPIO contract), and it only works because `GpioButton` holds the abstract type.

### 8.2 `Ft232HDevice`: a USB adapter that is a whole board

The FT232H is a USB chip that gives a laptop GPIO, I2C and SPI pins. In the code:

```
Ft232HDevice : Ftx232HDevice : FtDevice : Board          ← is-a Board
   CreateGpioController()  → board machinery + Ft232HGpio  (Ft232HGpio : GpioDriver)
   CreateI2cDevice(...)    → Ft232HI2cBus / Ft232HI2cDevice  (Ft232HI2cDevice : I2cDevice)
   CreateSpiDevice(...)    → Ft232HSpi  (Ft232HSpi : SpiDevice)
```

From the sample (`src/devices/Ft232H/samples/Program.cs`):

```csharp
Ftx232HDevice ft232h = Ftx232HDevice.GetFtx232H()[0];
var gpioController = ft232h.CreateGpioController();
```

Now the §5.4 claim is literal: `new Bme280(ft232h.CreateI2cDevice(...))` runs the **same** `Bme280` code on your Mac,
with bytes going over USB instead of `/dev/i2c-1`.

### 8.3 So what *is* a "binding"?

Bounded definition: a binding is a class in `src/devices/` that speaks a specific chip's or gadget's language. **Most**
bindings are *consumers* of the platform contracts (they hold an `I2cDevice` or a `GpioController`). **Some** are also
*providers* of those contracts (they inherit `GpioDriver`, `I2cDevice` or `Board`). Same folder, different role. When
you open a binding, its class declaration line tells you which.

---

## 9. The whole library as one table

When you open any class, fill in this row and you'll know where it sits:

| Class | Is-a (inherits) | Has-a (holds) | Created by | Role |
|---|---|---|---|---|
| `GpioController` | — (concrete) | `GpioDriver _driver`, open-pin table | you (`new`), `GpioButton`, `Board` | front desk for pins |
| `ManagedGpioController` | `GpioController` | `Board _board` | `Board.CreateGpioController()` | front desk + pin reservations |
| `GpioDriver` | — (abstract) | — | — | the pin contract |
| `LibGpiodDriver`, `LibGpiodV2Driver`, `SysFsDriver` | `UnixDriver` → `GpioDriver` | native handles / files | `UnixDriver.Create()` | Linux pin access |
| `RaspberryPi3Driver` | `GpioDriver` | `GpioDriver _internalDriver` | `GpioController` factory, `RaspberryPiBoard` | Pi register access |
| `GpioPin` | — | `GpioController _controller` | `GpioController.OpenPin` | convenience handle |
| `I2cDevice` | — (abstract) | — | — | the I2C-chip contract (+ factory `Create`) |
| `UnixI2cDevice` (internal) | `I2cDevice` | `UnixI2cBus _bus`, address | `I2cDevice.Create`, `UnixI2cBus.CreateDevice` | Linux I2C access |
| `Bme280` | `Bmx280Base` → `Bmxx80Base` | `I2cDevice _i2cDevice` | you | sensor (consumer) |
| `GpioButton` | `ButtonBase` | `GpioController _gpioController` | you | input gadget (consumer) |
| `Mcp23017` | … → `Mcp23xxx` → `GpioDriver` | `BusAdapter _bus` → `I2cDevice` | you | consumer of I2C **and** provider of GPIO |
| `Board` / `RaspberryPiBoard` | — / `GenericBoard` → `Board` | managers, pin reservations | `Board.Create()` | optional manager |
| `Ft232HDevice` | … → `FtDevice` → `Board` | USB handle | `GetFtx232H()` | provider of GPIO, I2C, SPI over USB |

---

## 10. How to find these connections yourself

| Question | How to answer it in the repo |
|---|---|
| "What implements this abstract class?" | Search for the base after a colon: `grep -rn ": GpioDriver" src/` · `grep -rn ": I2cDevice" src/` · in an IDE, **Go to Implementations** on the class name |
| "Which concrete class will I get at runtime?" | Open the factory (`Create`, or the parameterless constructor) and read its `if`/`switch` |
| "What does this binding need from me?" | Read its **constructor parameters**: `I2cDevice`, `SpiDevice`, `GpioController`, pin numbers, `shouldDispose` |
| "Is this binding a consumer or a provider?" | Read its **class declaration line**: `: GpioDriver`, `: I2cDevice`, `: Board` means provider |
| "Who cleans what up?" | Look for `shouldDispose` / `shouldDisposeBus` fields and the `Dispose(bool)` method |

---

## 11. What these abstractions do *not* do

| Abstraction | Doesn't |
|---|---|
| `GpioController` (plain) | Detect that a pin is already used by I2C/SPI or by another controller. Only `Board`'s `ManagedGpioController` does |
| `GpioController` | Translate pin numbers. It uses the driver's line numbers (on a Pi: the BCM/GPIO numbers, not the physical header positions). The old "numbering scheme" option no longer exists at this commit |
| `I2cDevice.Create` | Check a chip is present at that address (bindings like `Bme280` check the chip ID in their constructor) |
| `I2cDevice.Create` / `I2cBus.Create` | Work on Windows: both throw `PlatformNotSupportedException` there |
| The factories | Use reflection or configuration files. They're hard-coded `if`/`switch`/try-fallback chains |
| A binding | Know which concrete platform class it got |

---

## 12. Common misconceptions

| Misconception | Actually |
|---|---|
| "`GpioController` talks to the hardware" | It does bookkeeping and argument checks, then forwards every operation to its `GpioDriver`. The driver talks to Linux |
| "Bindings call Linux / libgpiod directly" | Bindings only call the abstract contracts. Only the concrete classes in `System.Device.Gpio` (and provider bindings like FT232H) touch native APIs |
| "Everything in `src/devices/` sits on top of `System.Device.Gpio`" | Most do; some implement its contracts (`Mcp23xxx : GpioDriver`, `FtDevice : Board`) |
| "`Board` is part of `System.Device.Gpio`" | It's in the bindings package (`src/devices/Board/`) |
| "The factory discovers drivers dynamically" | It's an explicit chain: OS check → board model from `/proc/cpuinfo` → try constructors in order |
| "`GpioPin` is a different way of reaching the hardware" | Every `GpioPin` method forwards to its controller; it's a handle, not a path |
| "Each device is its own NuGet package" | Each has its own `.csproj` for development; all are compiled into one `Iot.Device.Bindings.dll` |

---

## 13. Teach-back checklist

1. Two layers: `System.Device.Gpio` (platform: talks to Linux) and `Iot.Device.Bindings` (devices). Bindings depend on
   the platform layer, never the reverse.
2. Bindings only hold **abstract** types (`I2cDevice`, `SpiDevice`, `PwmChannel`) or a `GpioController` (which holds
   an abstract `GpioDriver`).
3. Three connections: inheritance = what contract a class fulfils; composition = what it holds and calls; factory =
   which concrete class fills the slot.
4. `new GpioController()` runs a factory: OS → board model → try drivers in order (Pi register driver, libgpiod v1,
   v2, sysfs). The controller then forwards every pin operation to that driver.
5. `GpioDriver` methods are `protected internal`: only the controller (same assembly) or a subclass can call them, so
   the controller's checks can't be skipped.
6. `I2cDevice.Create` builds an internal `UnixI2cBus` (`/dev/i2c-N`) and a `UnixI2cDevice` that owns it; `Bme280`
   checks the chip ID in its constructor and reads registers through the `I2cDevice` it holds.
7. Because bindings hold abstract types, the same binding works on a Pi, over USB (FT232H), or against a fake in a
   test.
8. `Board` is an optional manager in the bindings package: it creates controllers and buses and reserves pins
   (`ManagedGpioController` overrides `OpenPinCore`: Template Method).
9. Some bindings are providers, not just consumers: `Mcp23017` is a `GpioDriver` built on an `I2cDevice`;
   `Ft232HDevice` is a `Board`.
