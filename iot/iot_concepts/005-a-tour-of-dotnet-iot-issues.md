# Lecture 005: A tour of dotnet/iot issues — fourteen bugs, what they teach, and which to take next

> **Prompted by:** Timothy, 2026-10-03, while PR #2611 waits for review: "look at a wide variety of different issues…
> what those issues are, why they are occurring, how I would go about fixing them, how I would go about testing them".
> **Checked against:** dotnet/iot `main` @ `95384e7` (2026-10-01) and each issue's thread on 2026-10-03.
> **Prerequisites:** lecture 001 (the big picture) helps; lectures 001/002 in `../2328_*/lectures/` show the full
> process on one issue. **Reading time:** dip in, one case at a time (~10 minutes each).

**What this is.** A casebook. Each case is a real dotnet/iot issue, explained the same way: what's wrong, why it
happens (traced into the code where possible), how you'd fix it, how you'd prove the fix, and what it teaches. They're
chosen to cover as many *different kinds* of problem as possible: logic, registers, timing, kernel drivers, ABI,
exception design, API design, tests, docs.

**Honesty labels.** *Verified* = I read the code at `95384e7` and it says so. *Hypothesis* = my best explanation,
not proven by a run; each one comes with how you'd prove or disprove it. Issue state can change any day: **re-check
the thread before acting on any case.**

**Candidate marker.** ⭐ **Candidate** = I think it's a good next contribution for you, with the reason. Every other
case is 📖 learn-only (already claimed, needs hardware you don't have, or mainly instructive).

---

## The map

| # | Issue | Kind of problem | Hardware to work on it | Verdict |
|---|---|---|---|---|
| 1 | [#1887](https://github.com/dotnet/iot/issues/1887) Pca9685/MotorHat throws on unexpected register values | Register decoding, robustness, state that survives restarts | None (mockable) | ⭐ **Top candidate** |
| 2 | [#1328](https://github.com/dotnet/iot/issues/1328) WS2812 LEDs wrong colors when fewer than 8 | Timing through a kernel SPI driver (DMA threshold) | Pi + a short WS2812 strip; logic analyzer ideal | ⭐ Candidate (hardware) |
| 3 | [#2356](https://github.com/dotnet/iot/issues/2356) MPU-6050 calibration throws on bandwidth readback | Datasheet mismatch in a class hierarchy | MPU-6050 breakout (~$5–10) | ⭐ Candidate (hardware) |
| 4 | [#1715](https://github.com/dotnet/iot/issues/1715) Button fires `Press` at startup | Event subscription timing; pull-up settle | None | ⭐ Later (after #2611 and #2608) |
| 5 | [#1469](https://github.com/dotnet/iot/issues/1469) Better error when libgpiod is missing | Error-message design | None (Linux helpful) | ⭐ Small candidate |
| 6 | [#1663](https://github.com/dotnet/iot/issues/1663) Mcp23xxx constructor resets the pins | Constructor side effects; non-breaking API change | None | Your practice hunt (lecture 002 §5.4) |
| 7 | [#2403](https://github.com/dotnet/iot/issues/2403) `GpioPin` events pass the wrong `sender` | .NET event conventions; behavior compatibility | None | 📖 |
| 8 | [#2600](https://github.com/dotnet/iot/issues/2600) LibGpiodV2 abort on 32-bit ARM | ABI: C `unsigned long` vs C# `ulong` | 32-bit ARM board | 📖 |
| 9 | [#2604](https://github.com/dotnet/iot/issues/2604) / PR #2605 libgpiod V1 `time_t` on 32-bit | ABI: struct layout; **a fix that merged this week** | 32-bit ARM | 📖 (study the merged PR) |
| 10 | [#2602](https://github.com/dotnet/iot/issues/2602) `GpiodException` escapes the driver fallback | Exception filters; fallback chains | None | 📖 (claimed) |
| 11 | [#2312](https://github.com/dotnet/iot/issues/2312) TM1637 display garbled on Pi 5 | Bit-banged protocol timing; faster CPU breaks it | Pi 5 + TM1637 | 📖 (claimed) |
| 12 | [#1519](https://github.com/dotnet/iot/issues/1519) I2C ack polling for EEPROMs | Public API design and review | EEPROM chip | 📖 |
| 13 | [#2088](https://github.com/dotnet/iot/issues/2088) Sense HAT v2 color sensor | Composing an existing binding into another | Sense HAT v2 | 📖 (possible later) |
| 14 | [#1581](https://github.com/dotnet/iot/issues/1581) A test behaves differently on different hardware | Hardware tests and CI | Several boards | 📖 |

---

## Case 1 · #1887 · Pca9685 throws when the chip already holds values the library didn't write ⭐ Top candidate

**State (2026-10-03):** open since 2022, `bug`, `up-for-grabs`, `area-device-bindings`, Priority 2, assigned to
pgrawehr (area owner), one comment, no PR. **Ask first** (it's assigned).

### What's wrong
**Rule: the driver can only *read back* duty-cycle values that it *wrote* itself.** Create a `MotorHat` on a PCA9685
whose PWM registers hold anything else, and the first `CreateDCMotor(1)` throws
`InvalidOperationException: Unexpected value of duty cycle (on, off)`.

### Why it happens (verified in code)
The PCA9685 has, per channel, two 13-bit counters' worth of registers: `ON` (when in the 4096-tick cycle the output
turns on) and `OFF` (when it turns off), plus a "full on" bit (bit 12 of `ON`) and a "full off" bit (bit 12 of
`OFF`). Any pair of values is legal to the chip. The decoder in `src/devices/Pca9685/Pca9685.cs` accepts only the
three shapes the library itself writes:

```csharp
if (onCycles == 0)
    return (offCycles == Max) ? (ushort)0 : offCycles;      // (0, 4096) = off; (0, N) = N/4096
else if (onCycles == Max && offCycles == 0)
    return 4096;                                            // (4096, 0) = fully on
// we didn't set this value anywhere in the code
throw new InvalidOperationException($"Unexpected value of duty cycle ({onCycles}, {offCycles})");
```

Registers live in the chip, not in your program. They survive a program restart (the chip stays powered), so
anything written earlier by another library, a tool like `i2cset`, or code using **phase offsets** (`ON ≠ 0`, used
to stagger channels and reduce current spikes) leaves values this decoder rejects. And `MotorHat`'s constructor calls
`new Pca9685(device)` without `dutyCycleAllChannels`, so nothing resets the channels first.

Firmware analogy, bounded: a driver that assumes registers hold its own power-on values, but runs after a bootloader
that already configured the peripheral. Where it stops: here the "previous owner" is just a previous *process*.

### How you'd fix it (two options, for the maintainer to choose)
| Option | Change | Trade-off |
|---|---|---|
| A. **Decode every legal combination** (the root fix) | Implement the datasheet's rules: full-OFF bit wins; else full-ON bit → 1.0; else duty = `((off − on) & 0xFFF) / 4096` | Correct for all inputs; needs care with the bit-12 cases and wraparound. Small, contained |
| B. **Initialize in `MotorHat`** (the issue's suggestion) | Pass `dutyCycleAllChannels: 0` to `new Pca9685(...)` | One line, but changes behavior: motors are stopped on construction. And plain `Pca9685` users still crash |

A is the better engineering answer; B may also be wanted. That's exactly a question for the first comment.

### How you'd test it
No tests exist for `Pca9685` today (verified: no `tests/` folder). You'd add a test project that mocks `I2cDevice`
with Moq, scripts the register bytes the chip "holds", and asserts what `GetDutyCycle` returns:
`(0, 4096)` → 0.0; `(4096, 0)` → 1.0; `(0, 2048)` → 0.5; **`(1024, 3072)` → 0.5** (phase offset: throws today);
**`(4096, 4096)`** → 0.0 (full-off wins: throws today); `(3072, 1024)` → wraparound → 0.5. The bold rows are red on
`main`. Same red → green method as #2328.

### What it teaches
Register maps and datasheet rules (your strength), chip state that outlives the process, defensive decoding,
writing a brand-new test project in an unfamiliar repo, and a design question to put to a maintainer.

### Why it's a good next one for you
Datasheet-level work with no hardware needed, a clear red → green story, small diff, and a new test project
(more testing exposure). It also uses everything from #2328: ask first, mode P, evidence, PR. **Risk:** it's
assigned to pgrawehr; ask whether he minds you taking it.

---

## Case 2 · #1328 · WS2812 LEDs show wrong colors when the strip has fewer than 8 LEDs ⭐ Candidate (hardware)

**State:** open since 2020, `bug`, `up-for-grabs`, Priority 2, unassigned, 2 comments, no PR. Reporter's workaround:
"if you increase the number of pixels to 8 (without actually having 8 pixels on the line) it works just fine."

### What's wrong
**Rule: WS2812 LEDs read a single-wire signal with tight timing; any pause in the middle of the bitstream corrupts
the colors.** With fewer than 8 LEDs, colors are wrong on Pi 2/4; with 8 or more, they're fine.

### Why it happens (hypothesis, with a striking number)
dotnet/iot drives WS2812 through **SPI**, abusing it as a precise bit generator: each LED bit becomes 3 SPI bits
(`110` or `100`), so each LED needs 9 bytes. The buffer size, verified in `Ws28xx/BitmapImageNeo3.cs`:

```csharp
private const int ResetDelayInBytes = 30;
protected const int BytesPerPixel = BytesPerComponent * 3;   // 3 × 3 = 9
… new byte[width * height * BytesPerPixel + ResetDelayInBytes] …
```

| LEDs | Buffer | |
|---|---|---|
| 7 | 7 × 9 + 30 = **93 bytes** | broken |
| 8 | 8 × 9 + 30 = **102 bytes** | works |

The Raspberry Pi's Linux SPI driver (`spi-bcm2835`) only uses **DMA** for transfers of at least **96 bytes**; shorter
ones are fed to the hardware FIFO by the CPU, which can leave gaps between bytes. A gap longer than the WS2812's
tolerance is read as a reset or a wrong bit. The threshold sits exactly between 7 and 8 LEDs. **Not proven**: it's a
strong hypothesis from the numbers plus how the kernel driver works.

### How you'd prove it, then fix it
1. **Prove:** a Pi, a strip with ≤7 LEDs, and a logic analyzer on MOSI. Capture 7 LEDs vs 8 LEDs and look for gaps
   between bytes. That capture *is* the evidence (your strongest skill; maintainers rarely have it).
2. **Fix:** pad the transfer to at least 96 bytes with extra trailing zeros (zeros just hold the line low, i.e.
   extend the reset period, which the LEDs ignore). A tiny change in buffer sizing.
3. **Test:** a unit test that the buffer for 1–7 LEDs is ≥ 96 bytes and the color bytes are unchanged; plus the
   hardware capture after the fix.

### What it teaches
SPI as a waveform generator, kernel-driver behavior (PIO vs DMA), why "works with 8" is a clue, and evidence with a
logic analyzer. **Caveat for the comment:** the fix is Raspberry Pi-specific in motivation; maintainers may prefer
padding always (cheap) or only on Pi.

### Why it's a candidate
It's pure home turf (SPI, timing, scope captures) and the hypothesis is testable in an evening. **Needs** a WS2812
strip (a few dollars) and ideally a logic analyzer.

---

## Case 3 · #2356 · MPU-6050 `CalibrateGyroscopeAccelerometer()` throws on bandwidth readback ⭐ Candidate (hardware)

**State:** open, `bug`, Priority 3, unassigned, 9 comments, no PR. A maintainer asked if the reporter wanted to PR it.
Error: `Can set GyroscopeBandwidth, desired value Bandwidth0184Hz, stored value Bandwidth0250Hz`, and afterwards
all readings were zero.

### Why it happens (two hypotheses from the code; neither proven)
**Rule: the `Mpu6050` class is the base of `Mpu6500` and `Mpu9250`, but parts of it were written from the MPU-9250
datasheet.** Verified facts: `Mpu6500 : Mpu6050` and `Mpu9250 : Mpu6500`; `Reset()` writes `PWR_MGMT_1 = 0x80` and
cites the *MPU-9250* datasheet; the calibration routine calls `Reset()` and then configures registers, without
writing `PWR_MGMT_1` again; `AccelerometerBandwidth` writes register `0x1D` (`ACCEL_CONFIG_2`).

- **H1 (sleep after reset):** after a reset, the MPU-6050's `PWR_MGMT_1` defaults to `0x40` (SLEEP set), whereas the
  MPU-9250's defaults to `0x01` (awake). If the 6050 stays asleep, sensor outputs read as zero, which matches "all
  readings were zero afterwards". Whether the config writes are also lost while asleep is part of what to check.
- **H2 (a register that doesn't exist):** `ACCEL_CONFIG_2` (0x1D) is an MPU-6500/9250 register; the MPU-6050 doesn't
  have it. Even if H1 is fixed, the very next line (`AccelerometerBandwidth = …`) may fail its readback on a real 6050.

Firmware analogy, bounded: one driver for a chip family written from the newest part's datasheet. Where it stops:
here the *oldest* chip is the *base class*, so newer-chip assumptions leak down into it through inheritance.

### How you'd prove it, then fix it
1. On a Pi with an MPU-6050: run calibration, then dump registers with `i2cget -y 1 0x68 0x6B` (PWR_MGMT_1), `0x1A`
   (CONFIG), `0x1D`. Compare with the MPU-6050 register map. That decides H1/H2 in ten minutes.
2. Fix per the findings: wake the device after reset on the 6050 path; make the accelerometer-bandwidth setting
   6500/9250-only (override or capability flag).
3. Test: unit tests with a mocked `I2cDevice` that behaves like a 6050 (asleep after reset, no 0x1D), plus a hardware
   run as evidence.

### What it teaches
Datasheet differences across a chip family, class hierarchies that encode hardware assumptions, and register dumps
as evidence. **Needs** a ~$5–10 MPU-6050 breakout. A great interview story if H1/H2 hold.

---

## Case 4 · #1715 · The Button raises `Press` at application startup ⭐ Later

**State:** open since 2021, assigned to raffaeler, fix agreed in triage (2021-11-11): move the native pin subscription
from `GpioButton`'s constructor into the events' `add` accessors. raffaeler said in 2024 he'd do it.

### Why it happens
Right after the pull-up is enabled, the line is still rising (RC settle, lecture 001 §5.1). The constructor subscribes
to edges immediately, so the rising voltage can look like an edge → a false press/release pair. Lazy subscription
gives the line time to settle before anyone listens, and puts the timing in the user's hands.

### Fix and test
Subscribe on the first `add` of any of the five events (thread-safely), unsubscribe in `Dispose`. Tests with the
`MockableGpioDriver` harness you already have: no `AddCallback…Ex` call until the first `+=`; exactly one call when
several events are subscribed.

### Why "later"
It's your #2611's neighbor (same files) and overlaps pgrawehr's #2608. After both land, it's a natural follow-up,
and the maintainers would already know you. Ask raffaeler then.

---

## Case 5 · #1469 · Better error message when libgpiod is missing ⭐ Small candidate

**State:** open since 2021, `good first issue`, `up-for-grabs`, `documentation`, unassigned.

### What's wrong
**Rule: an error message is user interface.** A user installed the `gpiod` command-line tools instead of the library,
and got a low-level loading error that didn't say what to do. joperezr asked for messages that point to repo docs
via an `aka.ms` short link, and scoped the issue to messages + docs.

### Fix, test, and the catch
Catch the library-load failure where the driver first loads libgpiod and rethrow (or wrap) with a message naming the
package to install and the docs link. Test: a unit test where loading fails (a fake loader) and the message contains
the guidance. **The catch:** the `aka.ms` link is created by Microsoft staff, so the comment must ask for it; and it
touches the same area as #2602 (Case 10), so coordinate. Small, but a good way to learn the driver-loading path.

---

## Case 6 · #1663 · Mcp23xxx constructor resets every pin

This is your practice hunt in lecture 002 §5.4, so the analysis isn't repeated here (no spoilers). One hint for the
fix side once you've done the hunt: the maintainer's "don't break existing APIs" means a new **optional** constructor
parameter whose default keeps today's behavior. Bring your notes and we'll compare.

---

## Case 7 · #2403 · `GpioPin` events pass the controller as `sender`, not the pin 📖

**Rule: in .NET's event pattern, `sender` is the object that raised the event.** `GpioPin` forwards events but passes
`this` from the wrong level (the issue's reporter expected the pin). The fix is one argument, but it's a **behavior
change** in a public API: code that cast `sender` to the old type would break. That's why maintainers discussed
batching it into a major release (#2341 precedent). You studied it in `../2403_*`.
**Teaches:** event conventions, delegate identity, and why "one-line fixes" can still need a release plan.

---

## Case 8 · #2600 · LibGpiodV2 aborts the process on 32-bit ARM 📖

**Rule: P/Invoke signatures must match the C ABI on every architecture.** The V2 binding declares a C `unsigned
long` as C# `ulong` (always 64-bit). On 32-bit ARM, C `unsigned long` is 32-bit, so the argument lands in the wrong
register and libgpiod reads a garbage index, returns NULL, and a native `assert` kills the process (no managed
`catch` can stop it). The proposed root fix is `nuint` (native-sized). A null-check safety net is in PR #2601.
**Teaches:** ABI, calling conventions, why native crashes bypass exceptions. Your `../2600_*` folder has the details.

---

## Case 9 · #2604 / PR #2605 · libgpiod V1 `time_t` on 32-bit: a fix that merged this week 📖

**Rule: a struct's layout is part of the ABI too.** Newer 32-bit Linux distributions build with 64-bit `time_t` (the
"year 2038" fix), so `struct timespec` grew from 8 to 16 bytes, but the V1 binding modeled `time_t` as C `long`
(32-bit there). Every field after it was read from the wrong offset, breaking edge events. PR #2605 (by
wietsejorissen) fixed it and was **merged on 2026-10-01** (it's the commit your branch was rebased onto).
**Study it:** read the PR's diff, tests and review comments. It's the sibling of Case 8 and shows what a finished,
accepted native-interop fix looks like. **Teaches:** struct layout across ABIs, Y2038, how a community contributor
got an interop fix merged.

---

## Case 10 · #2602 · `GpiodException` escapes the driver-fallback chain 📖 (claimed)

**Rule: an exception filter is a list of the failures you've decided to survive; anything not on it escapes.**
When the controller picks a driver automatically it tries libgpiod v1, then v2, then sysfs. `TryCreate`'s filter
catches `PlatformNotSupportedException` and `DllNotFoundException`, but the v2 proxy wraps errors into
`GpiodException` (an `IOException`), so a missing `libgpiod.so.3` or `/dev/gpiochip0` escapes instead of falling
back to sysfs. Options in the issue: widen the filter, catch around the v2 attempt, or stop wrapping load failures.
Assigned to krwq/Copilot. **Teaches:** exception hierarchies, wrapping vs. preserving exception types, fallback design,
and why "catch more" isn't automatically right (it can hide real failures).

---

## Case 11 · #2312 · TM1637 display shows garbage on a Raspberry Pi 5 📖 (claimed)

**Rule: a bit-banged protocol is only as correct as its slowest timing margin; a faster CPU removes accidental
delays.** TM1637 uses an I2C-like two-wire protocol driven by toggling GPIOs in software. Code that worked on a Pi
3/4 because each GPIO call was slow enough breaks on the faster Pi 5 (and the chip's ack window is narrow). A
contributor (varadero) found clock-width and sequence changes that work and was testing them. **Teaches:** why
explicit timing beats incidental timing, and comparing against a known-good implementation (the Python library).

---

## Case 12 · #1519 · I2C ack polling for EEPROMs 📖

**Rule: adding public API is a design process, not a code change.** After an EEPROM write, the chip ignores its
address (NACK) for a few milliseconds; "ack polling" asks "are you ready?" repeatedly. The thread shows the real
API-review path: a first proposal (`AckPoll()`), alternatives, an approved shape (`IsDeviceReady()` on `I2cBus` and
`I2cDevice`), cross-platform concerns (Windows, the Linux `I2C_M_NOSTART` flag), how to unit-test "no device at this
address", and finally a maintainer suggesting it might belong in the EEPROM binding instead of the core library.
**Teaches:** .NET API design and review; why core APIs are expensive to add.

---

## Case 13 · #2088 · Sense HAT v2 color sensor (TCS34725) 📖 (possible later)

**Rule: before writing a driver, look for one that already exists.** The Sense HAT v2 added a TCS34725 color sensor.
The repo already has a `Tcs3472x` binding (verified: it lists TCS34721/34725), so the work is **composition**: expose
it from `SenseHat` like the other sensors (`SenseHatTemperatureAndHumidity`, etc.), handle v1 boards that lack it,
and document. **Teaches:** composing bindings, optional hardware, API consistency. **Needs** a Sense HAT v2 (~$50).

---

## Case 14 · #1581 · A test that behaves differently on different hardware 📖

**Rule: a hardware test that's skipped instead of explained is a known unknown parked in code.** The test
`OpenPinDefaultsModeToLastMode` (verified, `GpioControllerTestBase.cs`) checks that reopening a pin restores its
last mode; on sysfs it gives different results on different boards, so it now returns early for `SysFsDriver` with a
comment pointing at #1581. **Teaches:** the difference between unit tests (fakes, run anywhere) and hardware tests
(real boards, results depend on kernel and chip), and how projects live with flaky hardware behavior.

---

## Patterns across the cases

| Pattern | Cases | The question to ask on any new issue |
|---|---|---|
| **State that exists before your code runs** | 1 (registers), 4 (line voltage), 6 (pins), and #2328 | "What was already true before the constructor ran, and does the code read it or assume it?" |
| **Timing that was correct by accident** | 2 (SPI gaps), 4 (settle), 11 (faster CPU) | "What delay is this code relying on that nobody wrote down?" |
| **Two sides of an interface disagreeing** | 3 (datasheet vs. class), 8 and 9 (C vs. C#) | "Whose definition is this code written against, and is it the right one for this platform?" |
| **Public behavior is a contract** | 5, 7, 10, 12 | "Who could depend on what this does today?" |
| **Evidence needs the real thing** | 2, 3, 11, 14 | "Can a fake reproduce this, or do I need the board and a logic analyzer?" |

## Recommendation

1. **Next PR: Case 1 (#1887).** No hardware, datasheet-level, a clear red → green story, and a test project to build.
   Start with a comment to pgrawehr asking whether he minds you taking it, and whether he'd prefer option A, B or both.
2. **First hardware case: Case 2 (#1328).** One evening with a strip and a logic analyzer could confirm or kill the
   96-byte hypothesis. That capture would be a strong contribution even before any code.
3. **Case 3 (#2356)** if you'd like an IMU story; **Case 4** after #2611 and #2608 settle.

One active PR at a time (your rule): #2611 first.

## Check yourself

1. For Case 1, why can't the library assume the chip starts at power-on defaults?
2. For Case 2, what single number explains "works with 8 LEDs"? How would you prove it?
3. For Case 3, how could a base class end up with assumptions from a newer chip?
4. Cases 8 and 9 are both ABI bugs. What's different about them?
5. Pick any case and say which row of "Patterns across the cases" it fits, and why.
