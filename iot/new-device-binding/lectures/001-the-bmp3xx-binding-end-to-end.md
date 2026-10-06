# Lecture 001: The BMP3xx binding, end to end

> **Prompted by:** the new BMP390/BMP388 binding (plan Steps 0–11, CLI session 001) · **Date:** 2026-10-05
> **Code:** branch `feature/bmp3xx-binding` @ `8d23d3d4` on your fork (5 commits on dotnet/iot `main` @ `336e4696`)
> **Prerequisites:** none required. Helpful: [`../../iot_concepts/006-how-dotnet-iot-fits-together.md`](../../iot_concepts/006-how-dotnet-iot-fits-together.md) (the `Bme280` journey; this binding follows the same path)
> **Status of the claims:** everything marked ✅ comes from a saved run (`../shared/evidence/`). Everything about the
> real chip is ⚠️ **unverified until you plug one in** (§9).

---

## How to use this lecture

| If you have… | Read |
|---|---|
| **15 minutes** | §0 (run it), §1 (the one-paragraph story), §2 (the cast) |
| **45 minutes (the core path)** | add §3 (one `Read()`, step by step), §6 (testing without hardware) and §9 (what hardware will tell us) |
| **The full session** | everything, then the teach-back checklist at the end |

Each section starts with its **one-line rule** in bold. If you only read the bold lines, you have the skeleton.

---

## 0. Watch it work first (10 minutes, Mac only)

**Rule: run the tests before reading anything; they are the binding working without a chip.**

```bash
cd ~/Desktop/projects/open_source/iot_project/iot
git switch feature/bmp3xx-binding          # if you're not on it already
git log --oneline -6                        # the five commits, newest first

dotnet build src/devices/Bmp3xx/tests/ --no-incremental
dotnet test  src/devices/Bmp3xx/tests/ --no-build
```

Expected (✅ `evidence/012-device-read-green.txt`): **65 passed, 0 failed**, in under a second, 0 warnings.

Now run just the most important test and watch its name:

```bash
dotnet test src/devices/Bmp3xx/tests/ --no-build \
  --filter "FullyQualifiedName~Read_InSleep_TriggersForcedMeasurement" \
  --logger "console;verbosity=detailed"
```

That one test does everything a user does: create the sensor, call `Read()`, get 25.000 °C and 1013.25 hPa back.
§3 follows exactly what happens inside it.

---

## 1. What a binding is for, and what this one does

**Rule: a binding turns one chip's registers into a .NET API that speaks in units, so users never see a register.**

Without the binding, a user who wants the air pressure has to: write `0x13` to register `0x1B`, wait about 5 ms,
read register `0x03` until two bits are set, read six bytes from `0x04`, assemble two 24-bit numbers, read 21
calibration bytes from `0x31`, turn them into 14 coefficients with the right signs and powers of two, and run two
polynomials. With the binding they write:

```csharp
using var sensor = new Bmp390(i2cDevice);
Bmp3xxReadResult result = sensor.Read();
Console.WriteLine(result.Pressure?.Hectopascals);   // 1013.25
```

What was built (all in `src/devices/Bmp3xx/`, nothing outside it ✅ `evidence/006`, session report §2):

| Piece | Count | Job |
|---|---|---|
| Library (`*.cs` in the folder root) | 13 files, ~1,200 lines | The binding itself: the public API, the math, the register names |
| Tests (`tests/`) | 4 test files + 1 fake chip, **65 tests** | Prove it works with no hardware |
| Sample (`samples/Program.cs`) | 1 program | What a user would run on a Pi |
| `README.md`, `category.txt`, `.csproj`, `.sln` | | Docs, the device-index category, build wiring |

**Where it sits** (the same layers as `Bme280` in lecture 006):

```
 your app / samples/Program.cs
        │  sensor.Read()
        ▼
 Bmp390 ──is a──► Bmp3xxBase            ◄── THE BINDING (we wrote this)
        │            ├─ uses ─► Bmp3xxCalibrationData   (the math, no I/O)
        │            └─ talks through ─► I2cDevice      (already in System.Device.Gpio)
        ▼
 I2cDevice ──► Linux /dev/i2c-1 ──► the wires ──► BMP390 chip
```

**What the binding does *not* do** (F7): it doesn't know which I2C bus or address you use (you give it an
`I2cDevice`), it doesn't open Linux files itself, it doesn't run on its own thread, and it doesn't retry failed
reads. It's a translator, nothing more.

---

## 2. The cast of characters

**Rule: each class has exactly one job; the Translator decides *what* to ask, the Cookbook *how to convert*, the Courier *how bytes move*.**

| Character | Type (file) | Job in one line | Public? |
|---|---|---|---|
| **The Translator** | `Bmp3xxBase` (`Bmp3xxBase.cs`, 607 lines) | Turns API calls into register reads/writes and raw counts into `Temperature`/`Pressure`. Everything below is called from here | public, `abstract` |
| **The Two Name Tags** | `Bmp390`, `Bmp388` (`Bmp390.cs`, `Bmp388.cs`, ~10 lines of code each) | Only say "my chip ID is `0x60`" / "`0x50`" and pass it to the Translator | public, `sealed` |
| **The Cookbook** | `Bmp3xxCalibrationData` | Holds the 14 calibration coefficients and the two formulas. Pure math: never touches the bus | `internal` |
| **The Register Map** | `Bmp3xxRegister` (enum) | Names for the 11 register addresses used (`ChipId = 0x00`, `Data = 0x04`, …) | `internal` |
| **The Settings Menu** | `Bmp3xxOversampling`, `Bmp3xxFilterCoefficient`, `Bmp3xxOutputDataRate`, `Bmp3xxPowerMode` (enums) | The choices a user can make, each value = the bit pattern the chip wants | public |
| **The Flag Readers** | `Bmp3xxStatus`, `Bmp3xxErrors` (`[Flags]` enums) | Name the status bits (data ready, command ready) and error bits | public |
| **The Result Envelope** | `Bmp3xxReadResult` | Carries one measurement: `Temperature?` and `Pressure?`, either may be missing | public |
| **The Courier** | `I2cDevice` (not ours) | Moves bytes to one I2C address. Knows nothing about BMP390 | — |
| **The Stunt Double** | `SimulatedBmp3xx` (`tests/SimulatedBmp3xx.cs`) | A fake chip for tests: a table of registers that reacts to writes the way the datasheet says | test-only |

Two words that come up a lot:

- **`abstract` class:** a class you can't create with `new` directly; you create one of its subclasses
  (`new Bmp390(...)`). Here it holds all the shared code so `Bmp390` and `Bmp388` stay tiny.
- **`sealed` class:** nobody can inherit from it. It says "this is the end of the family tree".

Why this split matters: the Cookbook has **no I/O**, so its tests feed it numbers and check numbers, with no fake
chip at all (§6.2). The Translator has the I/O, so its tests need the Stunt Double (§6.3).

---

## 3. One `Read()`, step by step

**Rule: `Read()` = start one measurement, wait the computed time, check the two "ready" bits, read six bytes in one go, convert, range-check.**

This is the path of the test from §0, on a chip that was just created (sleep mode, settings ×1/×1). The right-hand
column shows **the chip's state after each step** (F8), using the register names from §2.

### 3.1 First, what the constructor already did

`new Bmp390(chip)` runs the base constructor, `Bmp3xxBase(0x60, chip)`:

| # | Code | Bus traffic | Chip state afterwards |
|---|---|---|---|
| 1 | `_i2cDevice = i2cDevice ?? throw new ArgumentNullException(...)` | — | — |
| 2 | `ReadRegister(ChipId)` | write `0x00`, read 1 byte → `0x60` | unchanged |
| 3 | `if (foundChipId != chipId) throw new IOException(...)` | — | — (a BMP280 would answer `0x58` and stop here) |
| 4 | pick the timing constants: BMP388 → 2000/313 µs, BMP390 → 2020/163 µs | — | — |
| 5 | `Reset()`: wait for STATUS bit `CommandReady` | read `0x03` → `0x10` | unchanged |
| 6 | read EVENT once (clears an old "reset happened" flag) | read `0x10` | EVENT = 0 |
| 7 | write the soft-reset command | write (`0x7E`, `0xB6`) | **everything back to factory defaults**; DATA = `0x800000`/`0x800000`; EVENT = 1 |
| 8 | `Thread.Sleep(2)`, then check ERR's command-error bit | read `0x02` → 0 | unchanged |
| 9 | wait for EVENT's "reset done" bit | read `0x10` → 1 | EVENT = 0 (cleared by the read) |
| 10 | `ApplySettings()`: write our defaults | 4 writes: OSR=`0x00`, CONFIG=`0x00`, ODR=`0x00`, PWR_CTRL=`0x03` | ×1/×1, filter off, 200 Hz, both sensors on, **sleep** |
| 11 | read the 21 calibration bytes, `Bmp3xxCalibrationData.Parse(...)` | write `0x31`, read 21 bytes | unchanged |

Two pieces of syntax from that table, expanded (F9):

```csharp
_i2cDevice = i2cDevice ?? throw new ArgumentNullException(nameof(i2cDevice));
// means exactly:
if (i2cDevice == null) { throw new ArgumentNullException("i2cDevice"); }
_i2cDevice = i2cDevice;
```

```csharp
Span<byte> calibration = stackalloc byte[21];
// "stackalloc" = a 21-byte buffer on the stack, like `uint8_t calibration[21];` inside a C function.
// Difference from C: Span<byte> knows its own length and C# checks every index, so calibration[21] throws
// instead of silently corrupting memory.
```

### 3.2 Now `Read()`

| # | Code (in `Bmp3xxBase`) | Bus traffic | Chip state afterwards |
|---|---|---|---|
| 1 | `StartForcedMeasurementIfNeeded` → `ReadPowerMode()` | read `0x1B` → `0x03` (mode bits 00) | sleep |
| 2 | → `SetPowerMode(Forced)`: read PWR_CTRL, keep the two enable bits, set mode = 01 | read `0x1B`, write (`0x1B`, `0x13`) | **measuring**; when done: DATA = new values, STATUS data-ready bits = 1, mode back to sleep by itself |
| 3 | → `GetMeasurementDuration()` = 234 + (392 + 1×2020) + (163 + 1×2020) = 4829 µs → **5 ms** (rounded up) | — | — |
| 4 | `Thread.Sleep(5)` | — | measurement finished |
| 5 | `IsDataReady()`: are **both** ready bits set? If not: sleep 1 ms and ask again, give up after 5 + 10 ms | read `0x03` → `0x70` | unchanged |
| 6 | `ReadResult(...)`: **one** 6-byte burst read from `0x04` | write `0x04`, read 6 bytes | data-ready bits cleared (reading the data clears them) |
| 7 | assemble: `rawPressure = data[0] | data[1] << 8 | data[2] << 16`, same for temperature from `data[3..5]` | — | — |
| 8 | Cookbook: `CompensateTemperature(raw)` → 25.000 °C, then `CompensatePressure(raw, 25.000)` → 101 325 Pa | — | — |
| 9 | range check: −40…85 °C and 30 000…125 000 Pa, else `null` | — | — |
| 10 | `return new Bmp3xxReadResult(temperature, pressure)` | — | — |

**Why one burst read in step 6, not two separate reads?** The chip copies a finished measurement into the data
registers in one go, and "shadows" them while you're reading (datasheet §3.10.1). Read all six in one transaction
and pressure and temperature are guaranteed to come from the same measurement. Two separate reads could straddle a
new measurement in normal mode.

**Why temperature first in step 8?** The pressure formula *uses* the temperature (§5). That's also why the
reading is a pair, not two independent values.

**If the chip never sets the ready bits** (wiring fault, chip stuck): after 15 ms `Read()` returns a result with
**both values `null`**. It does not throw. (Test: `Read_WhenNeverReady_TimesOutAndReturnsNulls`.)

### 3.3 The other ways to read

| Method | Starts a measurement? | Waits? | Returns when there's no data |
|---|---|---|---|
| `Read()` | yes, unless the chip is in normal mode | yes, `Thread.Sleep` | nulls |
| `ReadAsync()` | same | yes, `await Task.Delay` (doesn't block the thread) | nulls |
| `TryReadTemperature(out t)` / `TryReadPressure(out p)` | **no** | no | `false` |
| `TryReadAltitude(out h)` | **no** | no | `false` |

The `TryRead*` methods just read the latest six data bytes. That's what you want in **normal mode** (the chip keeps
measuring by itself) or right after a `Read()`. In sleep mode with no measurement since the last reset, the data
registers still hold their reset value `0x800000`/`0x800000`, so they return `false` (§7, decision U5).

`out` parameters, expanded: `sensor.TryReadPressure(out Pressure p)` is the C pattern
`bool try_read_pressure(sensor_t *s, double *out)`: the return value says *whether* it worked, the `out` argument
receives the value. Difference from C: the compiler forces the method to assign `p` on every path, even when it
returns `false` (there it assigns `default`, i.e. 0 Pa, which you must not use).

`Read()` and `ReadAsync()` share every step except the waiting (`StartForcedMeasurementIfNeeded`, `IsDataReady`,
`ReadResult` are the same private methods), so the two can't drift apart.

---

## 4. The settings, and why they're cached

**Rule: every setting is a read-modify-write of one register field, and the binding remembers what it wrote so it can put it back after a reset.**

| Property / method | Register | Bits | Values |
|---|---|---|---|
| `PressureSampling` | OSR `0x1C` | 2..0 | `X1, X2, X4, X8, X16, X32` (more samples = less noise, longer measurement) |
| `TemperatureSampling` | OSR `0x1C` | 5..3 | same |
| `FilterCoefficient` | CONFIG `0x1F` | 3..1 | `Off, Coefficient1, Coefficient3, … Coefficient127` (smooths fast changes, e.g. a door slamming) |
| `OutputDataRate` | ODR `0x1D` | 4..0 | `Period5Milliseconds` (200 Hz) … `Period655360Milliseconds` (named by period: your decision D6) |
| `SetPowerMode` / `ReadPowerMode` | PWR_CTRL `0x1B` | 5..4 | `Sleep`, `Forced`, `Normal` |

**Read-modify-write** (`UpdateRegister`): pressure and temperature oversampling share one register. Setting
pressure ×8 must not wipe the temperature bits:

```
read OSR            = 0b00_011_000   (temperature ×8, pressure ×1)
clear pressure bits = 0b00_011_000 & ~0b0000_0111
set pressure ×8     = ...          | 0b0000_0011
write OSR           = 0b00_011_011
```

Same idea as `REG = (REG & ~MASK) | (value << SHIFT)` in C. The test
`PressureSampling_SetX8_WritesOsrBitsAndKeepsTemperatureBits` checks exactly that.

**Why cache the values in fields** (`_pressureSampling`, …)? Two reasons. A soft reset wipes the chip's settings,
and `Reset()` ends with `ApplySettings()` to write them back, so the properties never lie. And the getter returns
the cached value without a bus transaction.

**Two safety behaviors in `SetPowerMode`:**

1. **Forced ↔ Normal goes through Sleep.** The datasheet says the chip *ignores* mode changes it considers
   illegal (§3.3.4), and its diagram of legal transitions couldn't be read reliably. Going through sleep is always
   legal, so the binding takes the detour: one extra register write. ⚠️ Which direct transitions really work is
   checked on hardware (§9).
2. **Normal mode with impossible settings throws.** In normal mode, if the output data rate is faster than one
   measurement takes (e.g. ×32 at 200 Hz), the chip raises a configuration error. The binding checks right after
   switching, puts the chip back to sleep, and throws `InvalidOperationException` with a message telling you the
   minimum period. Compare the two guards: **`SetPowerMode` checks the chip's error flag after the fact**;
   `ThrowIfUndefined` (in every setter) **checks the argument before touching the chip** (F11).

`GetMeasurementDuration()` uses **each chip's own datasheet formula**: the BMP390 datasheet (rev 1.1+) changed
the constants to 2020 µs and 163 µs; the BMP388 datasheet still says 2000 and 313. The CLI found this in Step 0 ✅
(`evidence/001`): the brief I wrote had the old constants for both.

---

## 5. The calibration math (the Cookbook)

**Rule: every chip is trimmed at the factory; its 21 calibration bytes turn the raw counts into °C and Pa through two polynomials.**

### 5.1 From 21 bytes to 14 numbers

| Bytes | Name | Read as | Then | Why it matters |
|---|---|---|---|---|
| 0–1 | T1 | `ushort` | × 2⁸ | unsigned: `0x8001` must be 32769, not −32767 |
| 2–3 | T2 | `ushort` | ÷ 2³⁰ | |
| 4 | T3 | `sbyte` | ÷ 2⁴⁸ | signed 8-bit: `0x80` must be −128, not 128 |
| 5–6 | P1 | `short` | (− 2¹⁴) ÷ 2²⁰ | the only two with an offset |
| 7–8 | P2 | `short` | (− 2¹⁴) ÷ 2²⁹ | |
| 9, 10 | P3, P4 | `sbyte` | ÷ 2³², ÷ 2³⁷ | |
| 11–12, 13–14 | P5, P6 | `ushort` | × 2³, ÷ 2⁶ | |
| 15, 16 | P7, P8 | `sbyte` | ÷ 2⁸, ÷ 2¹⁵ | |
| 17–18 | P9 | `short` | ÷ 2⁴⁸ | |
| 19, 20 | P10, P11 | `sbyte` | ÷ 2⁴⁸, ÷ 2⁶⁵ | |

In C# (one line of `Bmp3xxCalibrationData`):

```csharp
T3 = Math.ScaleB((sbyte)bytes[4], -48);
//   (sbyte)bytes[4]  : reinterpret the byte as signed 8-bit, same as (int8_t) in C
//   Math.ScaleB(x, n): x × 2^n, exact (it only changes the exponent of the double), like ldexp() in C
```

`BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(0, 2))` = "take two bytes starting at 0, low byte first".
The **signedness bugs** this table warns about are the classic way bindings go wrong, so the test
`Parse_ScalesEachCoefficient` feeds deliberately nasty raw values (e.g. T1 = `0x8001`, P5 = `0xFFFF`) that give
wrong answers if any field is read with the wrong sign or width.

### 5.2 The two formulas

Temperature (°C), with `d = rawTemperature − T1`:
```
t = d·T2 + d²·T3
```
Pressure (Pa), using that `t`:
```
offset       = P5 + P6·t + P7·t² + P8·t³
sensitivity  = rawP · (P1 + P2·t + P3·t² + P4·t³)
nonlinearity = rawP²·(P9 + P10·t) + rawP³·P11
p = offset + sensitivity + nonlinearity
```

**Why `double`?** The repo's conventions say floating-point results are `double`. Bosch's own driver uses `float`
in one helper, so our results differ from Bosch's by up to 0.00008 Pa ✅ (`evidence/005`, `008`). That's 200 times
smaller than the sensor's resolution (0.016 Pa), so it's irrelevant to users, but it's why the tests compare with
a **tolerance** (1e-6 °C, 1e-3 Pa) instead of exact equality.

### 5.3 Where the expected numbers came from (two independent "oracles")

An **oracle** in testing is anything that tells you the right answer without using the code under test.

```
  datasheet ──read by──► formulas.py (our float64 reading)      ┐
                                                                ├─ agree to 5e-10 °C and 8e-5 Pa ✅ evidence/005
  Bosch's C driver (BSD-3) ──built in scratch/, run on──────────┘
                                │
                                └── numbers only ──► the [InlineData(...)] rows in the C# tests
```

The C# code was then written from the datasheet and checked against those numbers. Bosch's **code** never enters
the fork, only numbers (the clean-room rule, because Bosch's license is BSD-3 and dotnet/iot is MIT). If our
reading of the datasheet were wrong, Python and Bosch would disagree; they don't.

⚠️ One honest gap: two of the three calibration sets (A and B) came from a web-search summary of Bosch forum
posts that no longer load. That doesn't weaken the comparison (both oracles get identical bytes), but real bytes
from **your** chip (Step 3) will be added as vectors V1–V5.

---

## 6. Testing without hardware

**Rule: the tests replace only the chip; everything above the Courier is the real code.**

### 6.1 The object graph (real vs. fake)

```
   ┌────────────────── xUnit test method ──────────────────┐
   │  ARRANGE: build the fake chip, script its raw values  │
   │  ACT:     call the real Bmp390                        │
   │  ASSERT:  check the result, or check what was written │
   └──────────────┬────────────────────────────────────────┘
                  ▼
        Bmp390 / Bmp3xxBase      REAL  (the code we ship)
             │          └──► Bmp3xxCalibrationData   REAL
             ▼  I2cDevice.WriteRead / Write
        SimulatedBmp3xx          FAKE  (tests only)
          : I2cSimulatedDeviceBase    (repo-provided base, already used by Ina236's tests)
          register table: 0x00 → 0x60, 0x31..0x45 → calibration, 0x04..0x09 → scripted raw values, …
          + switches: NeverBecomesReady, ConfigurationErrorOnNormalMode, SoftResetFails
          + a WriteLog: every (register, value) the binding wrote
```

The trick: `Bmp3xxBase` takes an `I2cDevice`, and `SimulatedBmp3xx` **is** an `I2cDevice` (it inherits from one).
The binding can't tell the difference. That's why the constructor takes the `I2cDevice` as a parameter instead of
creating one itself: it makes the class testable (the same idea as dependency injection in ASP.NET Core).

**What the fake models** (each from a datasheet section, written in comments in the file): writes are
(register, value) pairs; reads auto-increment; reading the data clears the ready bits; reading ERR clears its
error bits; forced mode measures once and returns to sleep; soft reset restores defaults but keeps calibration.

**What the fake does *not* prove:** timing (it measures instantly), electrical problems, and anything the
datasheet is vague about. Two of its behaviors are **guesses**, marked "unverified" in the code: it says
`CommandReady` is 1 when idle, and it raises the configuration error instantly. §9 checks both on the real chip.

### 6.2 Three representative tests (arrange / act / assert)

**A. Pure math, no fake at all** (`Bmp3xxCalibrationDataTests.CompensatePressure_MatchesReference`, row V7):

```csharp
var calibration = Bmp3xxCalibrationData.Parse(Convert.FromHexString(SetA));   // arrange: 21 known bytes
double actual = calibration.CompensatePressure(7033326u, 25.000007690);        // act
Assert.True(Math.Abs(101324.996390588 - actual) <= 1e-3);                      // assert: matches the oracle
```
Proves: the formula and the scaling are right for these inputs. Doesn't prove: anything about I2C.

**B. Behavior through the fake** (`Bmp3xxReadTests.Read_InSleep_TriggersForcedMeasurement_AndReturnsReferenceValues`):

```csharp
using var chip = CreateChip(V7RawPressure, V7RawTemperature);   // arrange: fake chip with scripted raw values
using var sensor = new Bmp390(chip);                             //          real binding on top
var result = sensor.Read();                                      // act
Assert.Contains((PowerControl, (byte)0x13), chip.WriteLog);      // assert 1: it started a forced measurement
AssertTemperature(V7Celsius, result.Temperature);                // assert 2: 25.000 °C
AssertPressure(V7Pascals, result.Pressure);                      // assert 3: 101 325 Pa
```

`CreateChip` uses an **object initializer**, expanded:
```csharp
new SimulatedBmp3xx(0x60, Calibration) { RawPressure = p, RawTemperature = t };
// means:
var chip = new SimulatedBmp3xx(0x60, Calibration);
chip.RawPressure = p;
chip.RawTemperature = t;
```
and `using var chip = ...;` means "call `chip.Dispose()` automatically when this method ends", like a
`try { ... } finally { chip.Dispose(); }` around the rest of the method.

Proves: the whole stack (power mode → wait → ready bits → burst read → math) works on a datasheet-faithful chip.
Doesn't prove: that the real chip behaves like the fake.

**C. A failure path** (`Bmp3xxBaseTests.SetPowerMode_Normal_WithConfigurationError_Throws_AndReturnsToSleep`):

```csharp
using var chip = new SimulatedBmp3xx(0x60, Calibration) { ConfigurationErrorOnNormalMode = true };  // arrange
using var sensor = new Bmp390(chip);
var exception = Assert.Throws<InvalidOperationException>(                                           // act + assert
    () => sensor.SetPowerMode(Bmp3xxPowerMode.Normal));
Assert.Contains("output data rate", exception.Message);
Assert.Equal(Bmp3xxPowerMode.Sleep, sensor.ReadPowerMode());   // and the chip was put back to sleep
```
`() => sensor.SetPowerMode(...)` is a **lambda**: a small unnamed function handed to `Assert.Throws`, which calls
it and checks that it throws. Like passing a function pointer in C, except the function is written inline and can
use the local variable `sensor`.

### 6.3 The 65 tests, grouped

| File | Tests | Covers |
|---|---|---|
| `Bmp3xxCalibrationDataTests.cs` | 22 | parsing (signs, widths, wrong length) + both formulas vs. the oracle (V6–V15) |
| `SimulatedBmp3xxTests.cs` | 5 | **the fake itself** (burst reads, write pairs, reset, forced mode, error clearing), so a failing binding test can't be blamed on a broken fake |
| `Bmp3xxBaseTests.cs` | 23 | constructor (chip IDs, wrong ID, null, reset + defaults), every setting, power modes, measurement time, reset errors, flags, dispose |
| `Bmp3xxReadTests.cs` | 15 | `Read`/`ReadAsync`, timeout, out-of-range values, `TryRead*` after reset, altitude |

How they were written: **tests first**. For each step, the CLI wrote the tests, ran them and saved the failures
("red": 22, then 23, then 15 failing with `NotImplementedException`, ✅ `evidence/007`, `009`, `011`), then wrote
the code until they passed ("green"). It also **broke the fake on purpose** three times to check that exactly one
test caught each break (✅ `evidence/009`). That's called mutation checking: a test that never fails proves nothing.

### 6.4 Exercises you can do on the Mac (each 5–10 minutes)

These are the fastest way to make the code yours. Do them on a scratch branch so nothing leaks into the real one:

```bash
git switch -c play/bmp3xx feature/bmp3xx-binding     # throwaway branch
# ...edit, run tests, look at what fails...
git restore . && git switch feature/bmp3xx-binding && git branch -D play/bmp3xx   # throw it away
```

| # | Break this (in `src/devices/Bmp3xx/`) | Predict first: which test fails, and why? |
|---|---|---|
| 1 | In `Bmp3xxCalibrationData.cs`, change `(sbyte)bytes[4]` to `bytes[4]` | |
| 2 | In `ReadResult`, swap `data[0]` and `data[2]` in the pressure line | |
| 3 | In `UpdateRegister`, delete `& ~mask` | |
| 4 | In `Read()`, delete the `while (!IsDataReady())` loop (keep the `Thread.Sleep`) | (hint: maybe nothing fails; what does that tell you about the fake?) |
| 5 | In `ReadResult`, change `&&` to `||` in the reset-value check | |
| 6 | Change `Bmp390ConversionMicroseconds` from 2020 to 2000 | |

Run `dotnet test src/devices/Bmp3xx/tests/` after each, then restore. Exercise 4 is the interesting one: it shows
the limit of a fake that measures instantly.

**Stepping through in a debugger:** open `iot_project/iot` in VS Code with the C# Dev Kit, open
`tests/Bmp3xxReadTests.cs`, set a breakpoint on `var result = sensor.Read();`, and click "Debug Test" above
`Read_InSleep_...`. Step into `Read()` and watch `chip.WriteLog` grow in the Variables pane: that's §3.2's table
happening.

---

## 7. The decisions, as built

**Rule: every choice a reviewer might question is written down with its reason, so you can defend it in your own words.**

| ID | Decision | What was built | Why | Might maintainers push back? |
|---|---|---|---|---|
| U1 | New folder, not inside `Bmxx80` | `src/devices/Bmp3xx/`, namespace `Iot.Device.Bmp3xx` | `Bmxx80Base` reads the chip ID at `0xD0` and resets via `0xE0`; BMP3xx uses `0x00`/`0x7E` and a different calibration | possible; asked in the proposal issue |
| U2 | Mirror `Bmp280`'s API | `Read`, `ReadAsync`, `TryRead*`, `SetPowerMode`, `Reset`, `GetMeasurementDuration`, same address constants | A user switching from a BMP280 changes one line | unlikely |
| O2 | Abstract base + two tiny classes | `Bmp3xxBase`, `Bmp390`, `Bmp388` | Two chips for ~20 lines | unlikely |
| O3 | Math in an internal class, tested directly | `Bmp3xxCalibrationData` + `InternalsVisibleTo` (precedent: `Vcnl4040`) | The most testable part stays off the public API | unlikely |
| U3 | I2C only, bus access in 3 private methods | `ReadRegister`, `ReadRegisters`, `WriteRegister` | Adding SPI later = change those three | unlikely |
| U4 | Out-of-range → no value | `null` / `false` instead of clamping like Bosch | Never return bogus data silently | **asked in the proposal issue; one open gap, see below** |
| U5 | `TryRead*` don't trigger a measurement; reset value means "no data" | both raw fields = `0x800000` → `false` | Same as `Bmp280`; the "both" rule because raw temperature `0x800000` is a real 23.7 °C | possible |
| U6 | Timeout, not exception, when data never gets ready | nulls after measurement time + 10 ms | The LPS22HB review (#2309) asked for timeouts on polling loops | unlikely |
| U7 | The device disposes the `I2cDevice` | `Dispose` → `_i2cDevice.Dispose()` | The repo's conventions doc says I2C devices belong to the binding | unlikely |
| U8 | Wrong chip ID → `IOException` | message in hex: "id 0x60 … found 0x58" | Same type as the sibling; hex matches the datasheet | unlikely |
| O5 | Defaults | ×1/×1, filter off, 200 Hz, sleep | Lowest power, deterministic | see §9 (200 Hz is borderline) |
| O6 | Impossible normal-mode settings throw | `InvalidOperationException` + back to sleep | Fail loudly at the moment of the mistake | possible |
| D6 | Output data rate named by period | `Period5Milliseconds` … | Your choice in session 001 | unlikely |

**"Dispose" and "owns", in one paragraph** (F10): `Dispose()` is how a .NET object releases something outside
the managed heap (here, the open `/dev/i2c-1` file handle). "The binding owns the `I2cDevice`" means *the binding
is responsible for closing it*. In C terms: whoever owns a `FILE*` must `fclose` it exactly once; the sample creates
the `I2cDevice` but hands it to the sensor, and `using Bmp3xxBase sensor = ...` closes both when the program ends.

**The open gap (U4), for you to decide:** if the *temperature* is out of range (say −45 °C), the pressure is still
calculated from that temperature and reported if it lands in range. Options: (a) keep it; (b) report pressure as
`null` too, because a pressure computed from an out-of-range temperature is extrapolation (Bosch effectively does
this: it reports 0). My recommendation is (b): one line plus one test.

---

## 8. Testing with hardware

**Rule: first prove the wires (i2cdetect), then the raw bytes (probe E1), then the binding (the sample); never debug two layers at once.**

```
 Level 1: wires     i2cdetect shows 77            → if not: power, SDA/SCL swapped, I2C disabled
 Level 2: bytes     E1 probe prints chip ID 0x60  → if not: wrong address, wrong chip (BMP280 = 0x58)
 Level 3: binding   the sample prints sane hPa    → if not: it's our code (or §9's unverified behaviors)
```

### 8.1 Wire it (plan Step 2)

| Board pin | Raspberry Pi pin |
|---|---|
| VIN / VCC | 1 (3.3 V) |
| GND | 6 (GND) |
| SCK / SCL | 5 (GPIO 3, SCL1) |
| SDI / SDA | 3 (GPIO 2, SDA1) |
| SDO | leave as the board has it (Adafruit: 0x77); to GND for 0x76 |
| CS | not connected (I2C mode). **Never pull it low**: the chip switches to SPI until powered off |

On the Pi:
```bash
cat /proc/device-tree/model; uname -m      # aarch64 → linux-arm64 below; armv7l → linux-arm
sudo raspi-config                          # Interface Options → I2C → enable; reboot
sudo apt install -y i2c-tools
i2cdetect -y 1                             # expect 77 (or 76) in the grid
```

### 8.2 Read raw bytes first (plan Step 3, probe E1)

The CLI writes a ~60-line console app that uses only `I2cDevice` (no binding) and prints the chip ID, the 21
calibration bytes and five raw readings. Ask it to do Step 3 once the sensor is wired. Why bother when the binding
exists? Because if the binding misbehaves later, you'll know the chip and the wires were fine, and the bytes become
real test vectors (V1–V5) for the unit tests.

### 8.3 Run the binding's sample (plan Step 13)

On the Mac, from `iot_project/`:
```bash
dotnet publish iot/src/devices/Bmp3xx/samples/ -c Release -r linux-arm64 --self-contained \
  -o scratch/new-device-binding/publish/sample
scp -r scratch/new-device-binding/publish/sample pi@<pi-host>:~/bmp3xx-sample
```
On the Pi:
```bash
cd ~/bmp3xx-sample && ./Bmp3xx.Samples
```
`--self-contained` packs the .NET runtime into the folder, so the Pi doesn't need .NET installed. (If `scp` to the Pi
doesn't work from your Mac, copy the folder any way you like: USB stick, or your Linux desktop.)

### 8.4 Is it right? Four physical sanity checks

| Check | How | Expected |
|---|---|---|
| **Absolute pressure** | Look up the nearest weather station's *station* pressure (or sea-level pressure + your elevation, converted with `WeatherHelper.CalculateBarometricPressure`) | within ~±3 hPa |
| **Temperature** | Compare with any room thermometer | within a few °C (the board warms itself slightly) |
| **Height** | Read on the floor, then lift the board ~1 m onto a table | pressure drops by ~**12 Pa (0.12 hPa)** per metre near sea level. The BMP390's relative accuracy is about ±3 Pa (~25 cm), so this is clearly visible with ×8 oversampling |
| **Breath** | Breathe warm air on it | temperature rises within seconds, falls back after |

The height test is the nicest: it checks the whole chain (calibration, both formulas, the pressure–temperature
coupling) with a ruler.

---

## 9. What only hardware can tell us

**Rule: these behaviors are built from the datasheet but marked unverified; each has a predicted symptom if we got it wrong.**

| # | Assumption in the code | If it's wrong on the real chip, you'll see… | How to check |
|---|---|---|---|
| 1 | STATUS `CommandReady` reads **1** when the chip is idle (the datasheet's reset table says STATUS = `0x00`) | **The constructor throws** `IOException: The sensor is not ready to accept a command.` every time. This is the **biggest risk** | E1 prints STATUS right after power-up |
| 2 | Forced ↔ Normal directly isn't allowed, so the binding detours through Sleep | nothing breaks (the detour is safe either way); we'd just learn whether it's needed | E1 extra check: write normal, then forced, read back PWR_CTRL |
| 3 | The configuration error appears immediately after switching to normal mode | an impossible setting might **not** throw; you'd get stale or missing data instead | set ×32/×32 at 200 Hz, call `SetPowerMode(Normal)` |
| 4 | Normal mode at the default 200 Hz with ×1/×1 works (typical 4.82 ms < 5 ms period, but the *maximum* is 5.70 ms) | `SetPowerMode(Normal)` throws with **default** settings | sample with `OutputDataRate` left at default |

If #4 fails, the fix is to change the default to 100 Hz (`Period10Milliseconds`): my recommendation anyway. If #1
fails, `Reset()` needs a different readiness check (e.g. skip the `CommandReady` wait, which the datasheet doesn't
strictly require): a plan revision, then a CLI step.

---

## 10. What's left before a PR

| Step | What | Who | Needs hardware? |
|---|---|---|---|
| 12 | Hygiene: warnings, 5× test runs, diff scope, clean-room grep | CLI | no |
| 15 | Implementation summary + full diff | CLI | no (finalized after 13) |
| — | Decide U4 (§7) and the README diagram (recommend: wiring table, no Fritzing) | **you** | no |
| 2, 3, 13 | Wire, E1 probe, sample on the Pi (§8) | **you** + CLI | **yes** |
| 17 | Search upstream issues, then post the proposal issue (G2) | **you** | no |
| 18 | Teach-back on this lecture (G3) | **you** | no |
| 19 | Open the PR from your fork | **you** | no |

**About the proposal issue** (plan Stage 4): it commits you to three things only: that you have a working BMP390
binding, that you'd like to contribute it, and that you'll maintain it. Its three questions (folder, API shape,
out-of-range behavior) are genuine; each answer that differs from ours becomes a small plan revision, not a
rewrite, because the tests' structure stays the same and only expected values change.

---

## Check yourself

1. Why does `Read()` read all six data bytes in one transaction instead of reading pressure and temperature separately?
2. `TryReadPressure` returns `false` right after `new Bmp390(...)`. Why, and what has to happen first?
3. Which of the 65 tests would still pass if `SimulatedBmp3xx` were deleted? Why is that a good thing?
4. What's the difference between the guard in `ThrowIfUndefined` and the check after `SetPowerMode(Normal)`?
5. The tests compare pressure with a tolerance of 0.001 Pa. Where did that number come from, and why not 0?
6. If the real chip reads STATUS = `0x00` when idle, what happens when you run the sample, and where in the code?
7. You lift the board from the floor to a 1 m table. What should the pressure do, and why is that a better test than comparing with a weather station?

## Teach-back checklist

Say these back in your own words (a walk is fine). Start with the first one.

1. **What the binding is for:** it turns register reads and 21 calibration bytes into `Temperature` and `Pressure`, so users never touch a register.
2. **The cast:** Translator (`Bmp3xxBase`) decides what to ask; Cookbook (`Bmp3xxCalibrationData`) converts with no I/O; Courier (`I2cDevice`) moves bytes; `Bmp390`/`Bmp388` only supply the chip ID.
3. **One `Read()`:** forced mode → wait the computed time → both ready bits → one 6-byte burst → temperature first, then pressure from it → range check → nulls on timeout.
4. **`Read()` vs `TryRead*`:** `Read` starts a measurement (unless normal mode); `TryRead*` only read the latest data and return `false` after a reset.
5. **Settings:** read-modify-write of shared registers, cached so `Reset()` can restore them; normal mode with impossible settings throws.
6. **Calibration:** 14 coefficients with mixed signs and widths, scaled by powers of two; signedness is where bindings break.
7. **Testing without hardware:** real binding on top of a fake `I2cDevice` that models the datasheet; math tested directly against two independent oracles; tests written first and checked by breaking things on purpose.
8. **Clean room:** Bosch's code ran only outside the fork to produce numbers; the binding was written from the datasheet.
9. **Testing with hardware:** wires (i2cdetect) → raw bytes (E1) → binding (sample) → physical checks (station pressure, 1 m lift ≈ 12 Pa).
10. **What's unverified:** `CommandReady` when idle (biggest risk: constructor would throw), legal mode transitions, when the configuration error appears, 200 Hz default.
