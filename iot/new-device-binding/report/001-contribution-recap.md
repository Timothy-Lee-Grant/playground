# Contribution recap: Bmp3xx binding for .NET IoT (BMP390 / BMP388)

> Source document for resume lines, interview stories and LinkedIn. Facts as of **2026-10-18**.
> Status: **PR open, in review** (not merged yet). Update §9 when that changes.

| | |
|---|---|
| Repository | [dotnet/iot](https://github.com/dotnet/iot): Microsoft's .NET library for GPIO, I2C, SPI and 100+ sensor drivers |
| Pull request | [dotnet/iot#2627](https://github.com/dotnet/iot/pull/2627): *New binding: Bmp3xx (Bosch BMP390 / BMP388 pressure and temperature sensor)* |
| Size | 24 files, **+2,593 lines**, 11 commits, all in a new folder `src/devices/Bmp3xx` (nothing outside it changed) |
| Language / stack | C# (.NET 8), xUnit, UnitsNet, System.Device.I2c; Linux I2C on Raspberry Pi |
| Hardware | Raspberry Pi 4 (Raspberry Pi OS, 32-bit), Adafruit BMP390 breakout (#4816), I2C bus 1, address 0x77 |
| Demo video | [YouTube walkthrough](https://www.youtube.com/watch?v=p0bjJdOGpkM) (~42 min): sample code, live hardware run, testing approach |
| Dates | Investigation 2026-10-04 → PR opened 2026-10-18 |
| Earlier contribution | [dotnet/iot#2611](https://github.com/dotnet/iot/pull/2611) (GpioButton bug fix + tests), merged 2026-10-08. This is the second code PR to the same repo, and the first **new feature** |

---

## 1. One-paragraph summary

I added a new device driver ("binding") to Microsoft's .NET IoT library for the **Bosch BMP390 and BMP388**, the
current-generation barometric pressure and temperature sensors and successors to the BMP280 that the library already
supported. The binding talks to the chip over I2C, reads its factory calibration, converts raw readings into
temperature, pressure and altitude using the datasheet's floating-point compensation formulas, and exposes every
measurement setting (oversampling, IIR filter, output data rate, power modes). It ships with a sample app, a
README and **79 unit tests that run against a simulated chip**, so CI needs no hardware. I verified it on a real
sensor on a Raspberry Pi, where it **found a bug that the simulated tests couldn't**: the chip silently ignores a mode change
while a measurement is in progress. The binding now handles that.

---

## 2. Why this contribution

- **No existing issue behind it.** Unlike a bug fix, a new binding is self-directed. I investigated whether the
  maintainers accept new bindings (they do, steadily: surveyed recent merged binding PRs first), then chose a device.
- **Why the BMP390:** popular, cheap (~$10 breakouts), a natural successor to an existing binding (`Bmp280`), and
  well documented (Bosch datasheets plus Bosch's open-source reference driver to check against).
- **Why it matters for me:** it combines my embedded background (register maps, I2C, datasheets, bench testing)
  with professional .NET library design (public API conventions, testability, packaging, CI), in a
  Microsoft-maintained repository.

---

## 3. What the binding does (user's view)

```csharp
using I2cDevice i2c = I2cDevice.Create(new I2cConnectionSettings(1, Bmp390.DefaultI2cAddress));
using var sensor = new Bmp390(i2c);

sensor.SetPowerMode(Bmp3xxPowerMode.Forced);       // take one measurement
if (sensor.TryReadPressure(out Pressure p)) { ... } // UnitsNet types: Pressure, Temperature, Length
```

- **Two chips, one API:** `Bmp390` and `Bmp388` share a base class `Bmp3xxBase`. Same registers; they differ in the
  chip ID (0x60 vs 0x50) and the measurement-timing constants.
- **Readings:** `Read()` / `ReadAsync()` (both values at once), `TryReadTemperature`, `TryReadPressure`,
  `TryReadAltitude` (with or without a sea-level reference pressure).
- **Configuration:** pressure/temperature oversampling (×1…×32), IIR filter coefficient, output data rate,
  power mode (sleep / forced / normal), status and error flags, soft reset, measurement-duration calculation.
- **API mirrors the existing `Bmp280`**, so switching from a BMP280 is a one-line change.
- **Not included** (listed as follow-ups): SPI, FIFO, interrupt pin, I2C watchdog, sensor time, self-test.

---

## 4. Technical details

### 4.1 Architecture

| Type | Role |
|---|---|
| `Bmp3xxBase` (abstract, ~630 lines) | All device logic: register I/O, calibration load, configuration, power modes, reads, reset, dispose |
| `Bmp390`, `Bmp388` (sealed) | Pass the expected chip ID and timing constants to the base |
| `Bmp3xxCalibrationData` (internal) | Parses the 21 calibration bytes into 14 scaled coefficients; compensation math |
| `Bmp3xxReadResult` | Nullable `Temperature?` / `Pressure?` result of `Read()` |
| Enums | `Bmp3xxOversampling`, `Bmp3xxFilterCoefficient`, `Bmp3xxOutputDataRate`, `Bmp3xxPowerMode`, `Bmp3xxStatus` and `Bmp3xxErrors` (flags), internal `Bmp3xxRegister : byte` |

Design decisions (all called out in the PR for reviewers):

1. **Separate folder, not an extension of `Bmxx80`.** The existing BMP280/BME280 base class hard-codes a different chip-ID
   register (0xD0), reset register (0xE0) and calibration layout; forcing the BMP3 family into it would have bent
   that class.
2. **Out-of-range values are reported as unavailable, not clamped.** Outside −40…85 °C or 300…1250 hPa, `TryRead*`
   return `false` and `Read()` returns `null`. Bosch's own driver clamps instead (and reports pressure 0 when too
   cold); I documented the difference.
3. **`TryRead*` never start a measurement** (same contract as `Bmp280`). They return `false` if nothing has been
   measured since reset (both data registers still at the reset value `0x800000`).
4. **Per-chip timing formulas.** The BMP390 datasheet revised the conversion-time constants; the BMP388 datasheet
   still has the old ones. Each class uses its own datasheet's formula, e.g. BMP390:
   `234 + (392 + 2^osr_p·2020) + (163 + 2^osr_t·2020)` µs.
5. **`SetPowerMode` waits for a running measurement before changing modes.** This was found on hardware (§6.2).
6. **Invalid configurations fail loudly.** `SetPowerMode(Normal)` throws if the output data rate is too fast for
   the oversampling settings, and leaves the sensor safely in sleep.
7. Follows the repo's `Devices-conventions.md`: UnitsNet on the public API, `Try*` pattern instead of sentinel
   values, `Async` + sync pairs, register enum, the binding disposes its `I2cDevice`, advanced calibration kept
   `internal`. One documented deviation: `GetMeasurementDuration()` returns `int` milliseconds to match `Bmxx80`.

### 4.2 The chip, at register level

- **Identity:** chip ID at `0x00` (0x60 = BMP390, 0x50 = BMP388), checked in the constructor.
- **Reset:** write `0xB6` to `CMD` (`0x7E`), wait the 2 ms start-up time, then poll the `EVENT` register's
  power-on-reset flag (bounded), then check `ERR.cmd_err`. This uses the chip's own completion signal instead of a guessed delay.
- **Data:** 6 bytes from `0x04` in one burst read (pressure then temperature, 24-bit little-endian each). A single
  burst keeps the pair consistent.
- **Calibration:** 21 bytes from `0x31`, parsed into 14 coefficients with per-coefficient power-of-two scale factors.
- **Compensation:** temperature first, then pressure computed from the **compensated** temperature (so an
  out-of-range temperature also makes pressure unavailable).
- **Bus path on Linux:** `I2cDevice.Create` opens `/dev/i2c-1`. Each register read is one `WriteRead`
  (write the register address, then read, combined in a single `ioctl(I2C_RDWR)` transaction).

---

## 5. Testing without hardware

- **79 xUnit tests** (53 test methods, several data-driven) in `tests/`, run on any OS.
- **A simulated chip** (`SimulatedBmp3xx`, ~340 lines) built on the repo's `I2cSimulatedDeviceBase`. It models the
  datasheet's behavior, not our implementation:
  - register writes as address/value pairs and auto-incrementing burst reads;
  - data-ready bits, soft reset, configuration errors;
  - the "mode write ignored during a measurement" window found on hardware.
- **Independent reference values for the math.** The compensation formulas are checked against two independent
  oracles, neither of which is the C# code under test:
  1. Bosch's open-source C driver (BMP3_SensorAPI), run in a separate harness (none of its code is in the PR);
  2. my own Python implementation of the datasheet formulas.

  The two oracles agree to within 5×10⁻¹⁰ °C and 8×10⁻⁵ Pa. The test vectors include calibration bytes and raw
  readings captured from the real BMP390.
- Builds with **0 warnings** under the repo's strict settings (warnings as errors, StyleCop, XML docs required).

---

## 6. Verified on real hardware

### 6.1 Bench results (Raspberry Pi 4 + BMP390)

| Check | Result |
|---|---|
| Bus scan, chip ID, reset path | Device at 0x77, ID 0x60, reset completes with `cmd_rdy` = 1 |
| Forced and normal mode via the sample | Both work. Pressure noise ≈ ±1 Pa at ×8 oversampling, filter coefficient 3 |
| Absolute accuracy | Station pressure **1005.5 hPa** vs **≈1005.1 hPa** derived from the local weather report (29.89 inHg at sea level, 59 m elevation): within the sensor's ±0.5 hPa spec |
| Physical sanity tests | Breath on the sensor → temperature rises; lifting the board ~1 m → pressure drops ≈12 Pa (as expected) |
| Mode-transition checks | Normal at default rate; too-fast settings throw and leave the sensor asleep; normal → forced at defaults and at ×32/×32; normal → sleep → normal: **all pass** |

Raw logs are kept as evidence in my workspace repo and linked from the PR.

### 6.2 The bug only hardware could find

Switching from normal to forced mode sometimes did nothing. The real chip **ignores a power-mode write that
arrives while a measurement is running**, and gives no error. The simulated chip didn't model this, because the
datasheet doesn't spell it out clearly. Diagnosed on the bench with targeted register probes. Fix: `SetPowerMode`
waits for the current measurement when leaving normal or forced mode (≈6 ms at defaults, up to ≈145 ms at
×32/×32). The behavior was then added to the simulated chip so the tests guard it from now on.

**Lesson worth telling in interviews:** fakes test your *reading* of the spec; hardware tests the spec itself.

### 6.3 Cross-compiling for the Pi

Found that the Pi 4 runs a 64-bit kernel with a **32-bit userland** (`uname -m` says aarch64, `getconf LONG_BIT`
says 32), so the sample must be published with `-r linux-arm`, not `linux-arm64`. Self-contained publish from
macOS, copied over `scp`.

---

## 7. Engineering process

- **Plan first.** A written implementation plan (steps, "done when" criteria, risks, decisions log) was approved
  before any code was written; deviations were recorded with reasons.
- **Review gates.** Work moved through explicit gates: plan approved → every change reviewed and understood (checked
  by explaining it back) → hardware verified → only then anything public. I directed the work, ran all hardware
  verification myself, and presented it on video.
- **Guardrails on a public repo:** git hooks (`commit-msg`, `pre-push`) that block commit messages from
  notifying upstream issues/people and only allow pushes to my fork, with protected branches. That kept
  experimentation private until I chose to announce.
- **Self-review against the maintainers' conventions** (`Devices-conventions.md`) before posting; the one deviation
  was disclosed in the PR.
- **Public explanation as verification:** recorded a 42-minute walkthrough. The transcript was reviewed
  for technical accuracy before linking it, and one misstatement got a correction note.

### 7.1 CI investigation (after opening the PR)

The first CI run failed on one of six legs (Windows Release) with `Build FAILED` and **0 errors**. Rather than
guess:

1. walked the Azure Pipelines job tree and identified the two visible errors as side effects (copy/publish
   steps with no packages to copy);
2. compared legs: Windows Debug runs the same packaging and passed; Linux and macOS passed;
3. checked pipeline history: an unrelated PR had failed identically three days earlier;
4. downloaded the job's MSBuild binary log, replayed it to a 2 GB diagnostic log, and searched for failed tasks.
   The only failure was the **test run of a different binding (`Tca955x.Tests`)**:
   `The "VSTestTask" task returned false but did not log an error`. Nothing in the failure chain mentioned Bmp3xx.

Posted the evidence on the PR and asked maintainers for a re-run.

---

## 8. Skills demonstrated

| Area | Evidence in this contribution |
|---|---|
| Embedded / hardware | Datasheet-driven register-level driver, I2C transactions, calibration and fixed→float compensation, bench verification, bug found on real silicon |
| .NET library design | Public API mirroring an existing family, UnitsNet types, `Try*` pattern, async/sync pairs, `IDisposable` ownership, internal vs public surface |
| Testing | Datasheet-modelled test double, differential testing against two independent oracles, regression test for a hardware-found bug |
| Cross-platform / Linux | Cross-compiling for ARM32 from macOS, Linux I2C (`/dev/i2c-N`, `ioctl`), Pi setup |
| CI / build systems | Azure Pipelines job tree, MSBuild binlogs, root-causing a silent build failure to another project |
| Open source practice | Self-directed proposal, convention review, clear PR description with design trade-offs for reviewers, demo video, evidence logs |
| Engineering process | Written plan with "done when" criteria, decisions log, review gates, git hooks as guardrails |

---

## 9. Resume line drafts (pick and edit; keep them true)

Status-safe while the PR is open:

- *Contributed a new device driver for the Bosch BMP390/BMP388 pressure sensors to Microsoft's open-source
  .NET IoT library (dotnet/iot#2627): I2C register-level C# driver, 79 unit tests against a simulated chip, and
  datasheet math cross-checked against Bosch's reference driver.*
- *Validated the driver on Raspberry Pi hardware (pressure within the sensor's ±0.5 hPa spec) and found and fixed a
  silent mode-change race that unit tests with a simulated device couldn't catch.*
- *Root-caused a silent Windows CI failure (`Build FAILED`, 0 errors) to an unrelated test project by replaying
  the MSBuild binary log from Azure Pipelines.*

After merge, change "Contributed" to "Contributed a merged ..." and add the outcome.

Interview story outline (STAR): **S** a library missing support for current-generation sensors → **T** add a
driver that maintainers will accept and that works on real hardware → **A** plan, convention review,
simulated-chip tests with independent oracles, bench verification, hardware-found bug fixed, video, CI root cause →
**R** PR #2627 (update with the outcome).

---

## 10. Links

- PR: https://github.com/dotnet/iot/pull/2627
- Code (my fork): https://github.com/Timothy-Lee-Grant/iot/tree/feature/bmp3xx-binding/src/devices/Bmp3xx
- Hardware evidence logs: https://github.com/Timothy-Lee-Grant/playground/tree/main/iot/new-device-binding/sample/evidence
- Video: https://www.youtube.com/watch?v=p0bjJdOGpkM
- Previous merged PR: https://github.com/dotnet/iot/pull/2611
