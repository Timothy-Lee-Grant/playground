# Lecture 003: Walking through the BMP390 binding on video

> **Prompted by:** Timothy, 2026-10-17: a YouTube video that walks through the sample, explains the components,
> shows it working on the Pi, and explains how it was tested with the simulated device; the link can go in the PR.
> **Code shown:** your fork, `feature/bmp3xx-binding` (currently `8d23d3d4`). **Hardware:** Raspberry Pi 4 (32-bit
> Raspberry Pi OS, over Wi-Fi/ssh) + Adafruit BMP390 on the STEMMA QT cable.
> **What this is:** a run sheet, not a word-for-word script. Each segment says **what to show**, **what to say**
> (talking points in plain language), and **what to point at** on screen. It deliberately stays above the wire level
> (lecture 002 has that, if anyone asks).
> **Background, if you want it while preparing:** lecture 001 (§2 cast, §3 one `Read()`, §6 testing) and the evidence
> files `sample/evidence/000–002`.

---

## 0. Before you hit record

**Rule: the video is your first public announcement of this binding, so time it with the proposal issue and record the code you'll actually submit.**

| Check | Why |
|---|---|
| **Record after the Phase A changes are in** (the pressure/out-of-range change, any default-rate change, the merge with upstream `main`) | The PR will link the video. If the code on screen differs from the PR, a reviewer will notice. Say the commit on screen (`git log --oneline -1`) |
| **Upload as *unlisted*** until you post the proposal issue | A public video titled "new dotnet/iot binding" *is* an announcement. Unlisted means only people with the link (the issue, the PR) see it. Make it public whenever you like after that |
| **Change the Pi's password first** (`passwd`) | Your ssh login prints "the default password for the 'pi' user has not been changed". On video, that tells every viewer how to log into your Pi |
| Terminal and editor font size up (≥ 18 pt), a dark or light theme you like, clean prompt | Readable on a phone |
| In VS Code, dismiss the "System.Device.Model is not part of this solution" popup *before* recording (Continue with Degraded Editing) | It's harmless (lecture/chat of 2026-10-13), but it looks like an error on screen |
| Pre-build everything once (`dotnet build` the tests, `dotnet publish` the sample, copy it to the Pi) | So nothing downloads for 30 seconds on camera. You can still *show* the commands |
| Have your numbers ready: today's sea-level pressure for Newberg and the elevation (194 ft = 59 m) | For the "is it right?" segment (§6) |
| Decide how you'll mention AI help | Same as the PR: one honest sentence (§8) |

**Suggested length:** 10–13 minutes for the full video (the segment times below add up to about 12), with **chapters** (§9). Maintainers will watch 2–3 minutes at
most, so the chapters let them jump straight to the demo and the tests.

---

## 1. The outline at a glance

| # | Segment | Screen | Time |
|---|---|---|---|
| 1 | Hook: it working | Pi terminal, sensor in hand | 0:30 |
| 2 | What it is and why | README / one slide | 1:00 |
| 3 | The hardware | camera on the Pi + wiring, `i2cdetect` | 1:00 |
| 4 | The sample, line by line | VS Code: `samples/Program.cs` | 3:00 |
| 5 | What's behind the sample (the components) | VS Code: file tree, `Bmp3xxBase.cs` | 2:00 |
| 6 | Live demo + "is it right?" | Pi terminal | 2:00 |
| 7 | How it's tested without hardware | VS Code + terminal: tests, `SimulatedBmp3xx` | 2:30 |
| 8 | Wrap-up: status, what's next, AI note | README "Binding Notes" | 0:45 |

---

## 2. Segment 1: the hook (0:30)

**Show:** the Pi terminal already running the sample in normal mode, or one run of the forced-mode loop; then breathe
on the sensor and let the temperature climb.

**Say:**
- "This is a Bosch BMP390 pressure and temperature sensor on a Raspberry Pi, read from C# with a new driver I wrote
  for the .NET IoT library."
- "Watch the temperature when I breathe on it." (pause) "And it's sensitive enough to feel a one-metre change in height."

**Why open this way:** it proves it works before you ask anyone to look at code.

---

## 3. Segment 2: what it is and why (1:00)

**Show:** the README's first lines (`src/devices/Bmp3xx/README.md`), or a simple slide with the layer picture below.

```
 your app (the sample)
      │  sensor.Read()
 Bmp390            ← the new binding: turns the chip's registers into °C and hPa
      │
 I2cDevice         ← already in .NET IoT: talks to Linux's I2C bus
      │
 BMP390 chip
```

**Say:**
- **dotnet/iot** is Microsoft's open-source .NET library for hardware: GPIO, I2C, SPI, and 130+ "bindings", which are
  drivers for specific chips.
- The BMP390 (and its older sibling, the BMP388) is the modern successor to the **BMP280**, which already has a
  binding. The BMP3 family had none.
- A binding's job: **the user asks for "pressure in hectopascals"; the binding handles registers, timing and
  calibration math.**
- The API deliberately **mirrors the existing BMP280 binding**, so someone using a BMP280 can switch with a one-line change.

**Point at:** `## Binding Notes` in the README: "Implemented / Not implemented" (it shows you scoped it on purpose).

---

## 4. Segment 3: the hardware (1:00)

**Show:** camera on the board and the four wires; then the Pi terminal.

**Say:**
- Four wires: 3.3 V, ground, SDA (data), SCL (clock). The STEMMA QT cable makes it solderless.
- "I2C devices have an address. Let's ask the bus who's there:"

```bash
i2cdetect -y 1          # shows 77
i2cget -y 1 0x77 0x00   # 0x60: the BMP390's chip ID
```

- "0x77 is the sensor's address, and register 0 says 0x60, which is how the chip says 'I'm a BMP390'. The driver
  checks exactly this when it starts."

**Keep it to this.** No bus timing, no wire diagrams (that's lecture 002 material).

---

## 5. Segment 4: the sample, line by line (3:00)

**Show:** `src/devices/Bmp3xx/samples/Program.cs` in VS Code. Walk top to bottom; highlight each block as you talk.

### 5.1 Connecting to the sensor

```csharp
const int busId = 1;
I2cConnectionSettings settings = new(busId, Bmp3xxBase.DefaultI2cAddress);
I2cDevice i2cDevice = I2cDevice.Create(settings);
using Bmp3xxBase sensor = new Bmp390(i2cDevice);
```

| Line | Say |
|---|---|
| `busId = 1` | "Bus 1 is the I2C bus on the Pi's header pins 3 and 5." |
| `I2cConnectionSettings` | "Which bus, which address. `DefaultI2cAddress` is 0x77; the binding also has `SecondaryI2cAddress`, 0x76, for boards wired the other way." |
| `I2cDevice.Create` | "This comes from .NET IoT itself, not my code. It opens the Linux I2C device for us." |
| `new Bmp390(i2cDevice)` | "This is where the new binding starts. In the constructor it checks the chip ID (the 0x60 we just saw), resets the chip, and reads the chip's **factory calibration**: 21 bytes that are different on every sensor." |
| `using` | "When the program ends, `using` disposes the sensor, and that closes the I2C device too. The sensor *owns* the device we handed it, which is the library's convention." |

**Point out:** "For a BMP388, you'd write `new Bmp388(...)` instead. That's the only difference."

### 5.2 The settings

```csharp
sensor.PressureSampling = Bmp3xxOversampling.X8;
sensor.TemperatureSampling = Bmp3xxOversampling.X1;
sensor.FilterCoefficient = Bmp3xxFilterCoefficient.Coefficient3;
Console.WriteLine($"One measurement takes about {sensor.GetMeasurementDuration()} ms.");
```

**Say:**
- **Oversampling:** "the chip takes 8 pressure samples and averages them: less noise, longer measurement."
- **Filter:** "smooths out quick changes, like a door slamming."
- **`GetMeasurementDuration()`:** "computed from the datasheet's formula: 19 ms with these settings. That's the line you'll see first in the output."
- "These are **enums**, not magic numbers, so you can't set an invalid value by accident."

### 5.3 Forced mode: one measurement on request

```csharp
Bmp3xxReadResult result = sensor.Read();
Console.WriteLine($"  Temperature: {Format(result.Temperature?.DegreesCelsius, "0.00 °C")}, " + ...);
if (sensor.TryReadAltitude(out Length altitude)) { ... }
```

**Say:**
- "`Read()` wakes the chip for **one** measurement, waits for it, and returns temperature and pressure together.
  Between readings the chip sleeps: this is the low-power way to use it."
- "The values are **typed units** (`Temperature`, `Pressure`, `Length` from the UnitsNet library), so you ask for
  `.DegreesCelsius` or `.Hectopascals`, never a raw number you have to guess the unit of."
- "Each value can be **missing**: that's the `?`. If a reading isn't valid, the binding gives you *no value* instead
  of a wrong one."
- "`TryReadAltitude` turns pressure into height. The sample assumes the standard sea-level pressure; for a real
  altitude you pass today's sea-level pressure in." (Foreshadow §6.)

### 5.4 Normal mode: continuous measuring

```csharp
sensor.OutputDataRate = Bmp3xxOutputDataRate.Period40Milliseconds;
sensor.SetPowerMode(Bmp3xxPowerMode.Normal);
... sensor.TryReadPressure(out Pressure pressure) ...
sensor.SetPowerMode(Bmp3xxPowerMode.Sleep);
```

**Say:**
- "In **normal mode** the chip measures by itself, here every 40 ms, 25 times a second. `TryReadPressure` just picks up
  the latest value without waiting."
- "Two styles for two jobs: **`Read()`** when you want one fresh measurement, **`TryRead…`** when the chip is
  streaming."
- "If you pick a rate faster than the chip can measure, `SetPowerMode` throws an exception that tells you the
  minimum period, instead of silently giving you stale data."

**Careful (honesty):** the "throws if too fast" path is unit-tested but **not yet confirmed on the real chip**
(lecture 001 §9 #3). Either say "it's designed to throw…" or test it before recording.

---

## 6. Segment 5: what's behind the sample (2:00)

**Show:** the file tree of `src/devices/Bmp3xx/`, then briefly `Bmp3xxBase.cs` and `Bmp3xxCalibrationData.cs`.

**The four components to name (lecture 001 §2, simplified):**

| Component | File | Say in one sentence |
|---|---|---|
| **The sensor classes** | `Bmp390.cs`, `Bmp388.cs` | "Tiny: each just says 'my chip ID is 0x60' or '0x50'." |
| **The base class** | `Bmp3xxBase.cs` | "All the real work: settings, power modes, reading, reset. Shared by both chips." |
| **The calibration math** | `Bmp3xxCalibrationData.cs` | "Turns the chip's raw counts into °C and pascals using the 21 factory bytes and two formulas from the datasheet. No hardware access at all, which matters for testing." |
| **The settings enums** | `Bmp3xxOversampling.cs`, `Bmp3xxFilterCoefficient.cs`, … | "Every choice the chip offers, with names instead of bit patterns." |

**One code moment worth showing** (in `Bmp3xxBase.cs`, the `Read()` method): scroll to it and summarise:
"start a measurement, wait the computed time, check the chip says 'data ready', read all six data bytes in one go so
temperature and pressure belong together, convert, check the range." Don't read the code aloud, just narrate the
steps while the code is on screen.

**One datasheet moment** (optional, 15 seconds): "every delay and register in the code cites the Bosch datasheet
section it came from", and show one comment like `// PWR_CTRL (0x1B), section 4.3.17`.

---

## 7. Segment 6: live demo, and "is it right?" (2:00)

**Show:** the Pi terminal over Wi-Fi ssh.

```bash
ssh pi@raspberrypi.local
cd ~/bmp3xx-sample
./Bmp3xx.Samples
```

**While it runs, say:**
- Point at "One measurement takes about 19 ms" (from §5.2).
- Point at the forced-mode lines: "temperature about 22 °C, pressure about 1005.5 hPa, and look how stable: it moves
  by about 0.01 hPa between readings."
- Point at the normal-mode lines: "continuous mode works too."

**Then the three physical checks (pick two; the first is the strongest):**

| Check | Do | Say |
|---|---|---|
| **Weather station** | Show today's sea-level pressure for Newberg | "Sea-level pressure today is X inches = Y hPa. Newberg is at 59 m; correcting for that gives about Z hPa. The sensor says 1005.5. That's within the sensor's ±0.5 hPa accuracy spec." (On 2026-10-11: 29.89 inHg = 1012.2 hPa → about 1005.1 hPa expected vs 1005.5 measured, `sample/evidence/002`.) |
| **Breath** | Breathe on it during a run | "Temperature climbs, then settles back." |
| **Lift** | Run once on the desk, once held about 1 m higher | "Pressure drops by about 0.12 hPa per metre, and the altitude line moves by about a metre." |

**How you got it on the Pi** (15 seconds, optional): "I build it on my Mac with `dotnet publish -r linux-arm
--self-contained`, which bundles .NET into the folder, and copy it over with scp. One gotcha: my Pi reports a 64-bit
kernel but runs a 32-bit OS, so `linux-arm`, not `linux-arm64`." Viewers love a real gotcha.

---

## 8. Segment 7: how it's tested without hardware (2:30)

**Rule for this segment: the point is "the library's CI has no sensors, so every test runs against a fake chip, and the math is checked against numbers that didn't come from my code."**

### 8.1 Run the tests (show it, 20 seconds)

```bash
cd ~/Desktop/projects/open_source/iot_project/iot
dotnet test src/devices/Bmp3xx/tests/
```

**Say:** "65 tests, under a second, no hardware. These run in the library's CI on every pull request."

### 8.2 The simulated chip (show `tests/SimulatedBmp3xx.cs`, 1:00)

**Say:**
- "The binding talks to an `I2cDevice`. In the tests I hand it a **fake** `I2cDevice`: `SimulatedBmp3xx`. The binding
  can't tell the difference."
- "The fake is a table of the chip's registers that reacts the way the **datasheet** says the chip reacts: writing
  the reset command restores the defaults, starting a measurement fills the data registers and sets the 'ready' bits,
  reading the data clears them."
- "It can also **misbehave on purpose**." Point at `NeverBecomesReady` and `ConfigurationErrorOnNormalMode`: "so I can
  test what the binding does when the chip never finishes, or rejects a setting."
- "And it keeps a **log of every register write**." Point at `WriteLog`: "so a test can check, for example, that
  `Read()` really sent the 'start a measurement' command."

**How it relates to the real hardware (the key message):**

```
 Unit tests:      binding ──► SimulatedBmp3xx (fake chip, behaves like the datasheet)      fast, every PR, no hardware
 Today's demo:    binding ──► I2cDevice ──► Linux ──► real BMP390                         proves the fake was right
```

"The fake and the real chip are interchangeable from the binding's point of view. The tests prove the logic; the Pi
proves the fake matches reality. Things like 'is the command-ready bit set when the chip is idle' I checked on the
real chip with `i2cget`, because a fake can only be as right as my reading of the datasheet."

### 8.3 Where the expected numbers come from (show one test row, 40 seconds)

**Show:** `tests/Bmp3xxCalibrationDataTests.cs`, one `[InlineData("V7", SetA, 7033326u, 25.000007690, 101324.996390588)]` row.

**Say:**
- "The hard part of this chip is the calibration math. How do I know my formulas are right?"
- "I ran **Bosch's own reference driver**, separately from this project, and a second independent implementation of the
  datasheet formulas, fed them the same inputs, and they agreed. Those outputs are the expected values in these tests.
  None of Bosch's code is in the binding; only the numbers."
- "The tests compare with a tiny tolerance, a thousandth of a pascal, because Bosch's driver uses single precision
  and mine uses double. That difference is about 200 times smaller than the sensor can even resolve."

### 8.4 Optional: break it on purpose (30 seconds, a great video moment)

In `Bmp3xxCalibrationData.cs`, change `(sbyte)bytes[4]` to `bytes[4]`, rerun the tests, show the red failure, undo.

**Say:** "That byte is a *signed* number in the datasheet. Read it as unsigned and the math is wrong, and the test that
feeds it a negative value catches it immediately. Sign mistakes like this are the classic bug in sensor drivers."

---

## 9. Segment 8: wrap-up (0:45)

**Show:** README `## Binding Notes`.

**Say:**
- **What's in:** I2C, BMP390 and BMP388, temperature/pressure/altitude, single-shot and continuous modes, all the settings.
- **What's not (yet):** SPI, the FIFO buffer, the interrupt pin. "Those can be follow-up contributions."
- **Status:** "I'm proposing this to the dotnet/iot maintainers; the link to the issue/PR is in the description."
  (Don't say "it's been accepted" until it has.)
- **AI, honestly, in one sentence:** e.g. "I built this with an AI coding assistant as a pair programmer; I designed
  the tests with it, reviewed every line, and verified it on the hardware myself." (Match what you'll write in the PR.)
- Call to action that fits *your* channel.

**Suggested chapters for the description:**
```
0:00 Demo
0:30 What is this?
1:30 Hardware and wiring
2:30 The sample, line by line
5:30 What's inside the binding
7:30 Live demo: is it accurate?
9:30 Testing without hardware
12:00 What's next
```

---

## 10. What *not* to say (keeping it accurate)

| Avoid | Say instead | Why |
|---|---|---|
| "This is part of .NET IoT" / "officially supported" | "a binding I'm proposing to .NET IoT" | It isn't merged |
| "It works on every Pi / every board" | "verified on a Raspberry Pi 4 with Raspberry Pi OS 32-bit and an Adafruit BMP390" | That's what's tested |
| "The BMP388 works too" (as a fact) | "the BMP388 uses the same code; I've tested the BMP390" | No BMP388 on hand (unless you buy one) |
| "Accurate to 0.4 hPa" | "within the sensor's ±0.5 hPa spec compared with the local weather report" | One comparison, not a calibration |
| "It throws if the rate is too fast" (as verified) | "it's designed to throw…", or test it first | Not yet checked on the real chip |
| Deep wire-level detail | "if you want the bus level, I have a write-up" | Keeps the video focused; lecture 002 has it |

---

## 11. Using the video in the PR

- Put it near the top of the PR description, one line: `Demo (9 min, chapters): <link>: sample walkthrough, hardware
  run on a Raspberry Pi 4, and how the tests use a simulated device.`
- **It complements, not replaces,** the written evidence: keep the "Verified on…" line and the link to your evidence
  folder. Reviewers decide from the code and tests; the video shows care and makes the hardware claim tangible.
- Also link it in the **proposal issue** (that's when you'd switch it from unlisted to public).

---

## Check yourself (before recording)

1. In one sentence, what does a binding do, and what does `I2cDevice` do?
2. What's the difference between `Read()` and `TryReadPressure()`, and when would you use each?
3. Why does every value come back as "maybe missing" (`Temperature?`)?
4. What does `SimulatedBmp3xx` do that the real chip can't, and what can only the real chip prove?
5. Where did the expected numbers in the calibration tests come from, and why isn't Bosch's code in the project?
6. Which claims in your video are verified on hardware, and which are only unit-tested?

## Teach-back checklist (if you want one before recording)

1. A binding turns a chip's registers into typed .NET values; this one mirrors the BMP280 API.
2. The sample: connect (settings → `I2cDevice` → `Bmp390`), configure (oversampling, filter), read (forced `Read()`, then normal mode + `TryRead…`), clean up (`using`).
3. The constructor checks the chip ID, resets, and reads the 21 calibration bytes.
4. Values are UnitsNet types and nullable: no value instead of a wrong value.
5. Four components: the two tiny sensor classes, the base class, the calibration math, the settings enums.
6. Tests use a fake `I2cDevice` that behaves like the datasheet, can misbehave on purpose, and logs every write.
7. Expected math values come from Bosch's reference driver and an independent implementation, run outside the project.
8. The hardware run proves the fake matches reality; the weather-station check and the lift/breath tests prove the numbers make sense.
