# 003: What you'd build, and the steps to get there (BMP390)

> Date: 2026-10-04 · Builds on: [001](001-should-i-add-a-sensor-binding.md), [002](002-choosing-the-device.md)
> Register facts below are from Bosch's reference driver (`BMP3_SensorAPI` @ `db4cf8e`) and must be checked
> against the BMP390 datasheet before coding. Nothing here has been run yet.

---

## 1. The cast of characters

| Character | Type | Job, in one line |
|---|---|---|
| **The App** | user code / `samples/Program.cs` | Asks for "pressure, please" in pascals and doesn't want to know about registers |
| **The Translator** | `Bmp390` (via a `Bmp3xxBase`) — **you write this** | Turns API calls into register reads/writes, and raw counts into `Pressure` / `Temperature` |
| **The Cookbook** | `Bmp3xxCalibration` (internal) — **you write this** | Reads the chip's 21 factory calibration bytes once and holds the 14 scaled coefficients plus the compensation formulas. Pure math, no I/O |
| **The Register Map** | `enum Bmp3xxRegister : byte`, plus enums for oversampling, IIR filter, output rate, power mode — **you write these** | Names for every address and bit field, so nothing in the code is a magic number |
| **The Courier** | `I2cDevice` (already exists) | Carries bytes to and from address 0x76/0x77. Knows nothing about BMP390 |
| **The Chip** | the real BMP390 on your Pi | Measures, stores raw 24-bit counts, answers on the bus |
| **The Stunt Double** | `SimulatedBmp390 : I2cSimulatedDeviceBase` (in `tests/`) — **you write this** | Pretends to be the chip in unit tests: a dictionary of registers with scripted values |

**Rule:** the Translator owns *what to ask*, the Cookbook owns *how to convert*, the Courier owns *how bytes move*.
Keep them separate and each one is testable on its own.

---

## 2. One reading, step by step (forced mode)

```
App                      Bmp390 (Translator)               Cookbook                I2cDevice ─► Chip
 │ TryReadPressure(out p)   │                                 │                        │
 │─────────────────────────►│ 1. write PWR_CTRL 0x1B:          │                        │
 │                          │    press_en | temp_en | forced ─────────────────────────►│ starts one measurement
 │                          │ 2. wait the computed time         │                        │ (depends on oversampling)
 │                          │    (from OSR settings)            │                        │
 │                          │ 3. read STATUS 0x03 ───────────────────────────────────►│
 │                          │    drdy_press 0x20 && drdy_temp 0x40 set?                  │
 │                          │    no → retry until timeout → return false                 │
 │                          │ 4. read DATA 0x04..0x09 (6 bytes) ──────────────────────►│
 │                          │    pressure = 24-bit LE, temp = 24-bit LE                  │
 │                          │ 5. Compensate(rawT) ───────────►│ temperature first      │
 │                          │    Compensate(rawP, t) ────────►│ pressure needs temp    │
 │◄─── true, p (Pressure) ──│◄────────────────────────────────│                        │
```

The calibration bytes (`0x31`, 21 bytes) are read **once**, in the constructor, after checking the chip ID
(`0x00` = `0x60` for BMP390, `0x50` for BMP388). If the ID is wrong, the constructor throws, like `Ina236` does.

---

## 3. The files

```
src/devices/Bmp3xx/
├── Bmp3xx.csproj              copy Ina236.csproj's shape: $(DefaultBindingTfms), EnableDefaultItems=false
├── Bmp3xx.sln                 binding + samples + tests
├── Bmp3xxBase.cs              constructor, chip-ID check, settings, TryRead*, Reset, Dispose
├── Bmp388.cs, Bmp390.cs       thin: just the expected chip ID
├── Bmp3xxCalibration.cs       internal: parse 21 bytes → 14 doubles; CompensateTemperature/Pressure
├── Bmp3xxRegister.cs          enum Bmp3xxRegister : byte
├── Bmp3xxPowerMode.cs, Bmp3xxOversampling.cs, Bmp3xxIirFilter.cs, Bmp3xxOutputDataRate.cs
├── README.md                  "# BMP390/BMP388 - …", "## Documentation" (datasheet link first), "## Usage",
│                              what's implemented and what isn't (FIFO, interrupts, SPI)
├── category.txt               barometer / altimeter / thermometer   (Device-Index.md is generated: don't edit it)
├── samples/
│   ├── Bmp3xx.Samples.csproj
│   └── Program.cs             reads and prints every second; shows settings; uses `using`
└── tests/
    ├── Bmp3xx.Tests.csproj
    ├── SimulatedBmp390.cs     register map with known calibration + raw data
    ├── CalibrationTests.cs    bytes → coefficients → compensated values vs reference numbers
    └── Bmp390Tests.cs         constructor, wrong chip ID, settings round-trip, not-ready → false, dispose
```

**Rough size (estimate):** 600–900 lines including tests. `Ina236` is ~450 with tests; this chip has more settings.

### The public API sketch (to propose, not final)

Same shape as `Bmp280` / `Bmx280Base`, so users can switch sensors easily:

```csharp
namespace Iot.Device.Bmp3xx;

public abstract class Bmp3xxBase : IDisposable
{
    public const byte DefaultI2cAddress = 0x77;     // SDO high
    public const byte SecondaryI2cAddress = 0x76;   // SDO low

    protected Bmp3xxBase(byte expectedChipId, I2cDevice i2cDevice, bool shouldDispose = true);

    public Bmp3xxOversampling PressureSampling { get; set; }
    public Bmp3xxOversampling TemperatureSampling { get; set; }
    public Bmp3xxIirFilter FilterCoefficient { get; set; }

    public bool TryReadTemperature(out Temperature temperature);
    public bool TryReadPressure(out Pressure pressure);
    public bool TryReadAltitude(Pressure seaLevelPressure, out Length altitude);   // via WeatherHelper
    public bool TryReadAltitude(out Length altitude);                             // mean sea-level pressure

    public void SetPowerMode(Bmp3xxPowerMode mode);
    public void Reset();                     // CMD 0x7E ← 0xB6
    public void Dispose();
}

public sealed class Bmp390 : Bmp3xxBase { public Bmp390(I2cDevice d, bool shouldDispose = true) : base(0x60, d, shouldDispose) { } }
public sealed class Bmp388 : Bmp3xxBase { public Bmp388(I2cDevice d, bool shouldDispose = true) : base(0x50, d, shouldDispose) { } }
```

---

## 4. Testing without hardware

**Rule:** tests replace only the Chip. Everything above the Courier is the real code.

```
   ┌──────────── test (xUnit) ────────────┐
   │  arrange: script the Stunt Double     │
   │  act:     call the real Bmp390        │
   │  assert:  check the returned value,   │
   │           or check what was written   │
   └───────────────┬───────────────────────┘
                   ▼
   Bmp390 (REAL) ──► Bmp3xxCalibration (REAL)
        │
        ▼
   SimulatedBmp390 (FAKE, : I2cSimulatedDeviceBase)
        register map: 0x00 → 0x60, 0x31..0x45 → known calibration bytes, 0x04..0x09 → known raw counts,
                      0x03 → data-ready bits (scriptable: set "not ready" to test the timeout path)
```

| Test group | What it proves | Where the expected numbers come from |
|---|---|---|
| **Calibration parsing** | 21 bytes become the right 14 coefficients (signedness, byte order, offsets of 16384 on P1/P2, power-of-two scales) | Hand-computed from the datasheet's formulas; cross-check with Bosch's reference implementation |
| **Compensation** | Raw counts + coefficients → °C and Pa | Run Bosch's C code (or the float formulas by hand) once on chosen inputs; freeze the outputs as test vectors. Also capture a few real `(calibration bytes, raw counts, result)` triples from **your** chip |
| **Behavior** | Wrong chip ID throws; not-ready returns `false` after the timeout; settings write the right bits; `Reset` writes 0xB6 to 0x7E; `Dispose` respects `shouldDispose` | The register map in the datasheet |

**Real-hardware evidence** (for the PR description, not CI): a run on your Pi with the sample, compared with a
nearby weather station's sea-level pressure. That's a sanity check, not a calibration.

---

## 5. The steps

Same mode P you used for #2328 ([`../../ai-workflow/default-workflow.md`](../../ai-workflow/default-workflow.md)):
the AI plans and builds, you learn it through a lecture and a teach-back, then *you* post upstream. Each step
ends with something you can see.

| # | Step | Output | Gate before the next step | Estimate |
|---|---|---|---|---|
| 0 | **Buy the hardware**; wait for #2611 to settle before any upstream activity | Sensor on the desk | — | days (shipping) |
| 1 | **Walking skeleton, outside the fork:** a console app on the Pi using plain `I2cDevice` reads chip ID `0x60`, the 21 calibration bytes and 6 raw data bytes | `sample/E1` + `sample/evidence/001-…txt` | Chip answers; bytes look sane | 1 session |
| 2 | **Spike the math in the sample:** compensation in C#, printed next to a weather station's pressure | `sample/evidence/002-…txt` | Numbers within a few hPa / °C of reality | 1 session |
| 3 | **Check for duplicates**, then **post the proposal issue** (draft in §6). This is the first public step: by now you know it works on your hardware | Upstream issue | No "someone's already doing this" | 1 hour, then wait a few days (keep building) |
| 4 | **Scaffold the issue folder** with `ai-workflow/new-issue.sh` (it becomes `iot/<issue#>_bmp390-binding/`), plan, CLI builds the binding in the fork | Branch in the fork with binding + sample + tests | Builds; tests pass; sample runs on the Pi | 2–3 weekends |
| 5 | **Lecture + teach-back** on the built binding (API choices, calibration, tests) | `lectures/001-…` + teach-back | You can explain every design choice | 1–2 weeks of evenings |
| 6 | **You open the PR**: "Fixes #N" first line, scope, what's not implemented, hardware tested, AI disclosure | PR | — | 1 session |
| 7 | **Review rounds**: answer within 48 h; one polite ping after ~2 weeks of silence | Merged binding | — | weeks (their pace) |
| 8 | **Follow-ups** (separate PRs): SPI, data-ready interrupt as a .NET event, FIFO | More PRs | v1 merged | optional |

**Total, your side:** roughly 4–6 weekends plus evening study (estimate), then review time on the maintainers' side.

---

## 6. Draft of the proposal issue (for step 3)

> **Title:** Proposal: new binding for Bosch BMP390 / BMP388 pressure sensor
>
> Hi! I'd like to contribute a binding for the Bosch BMP390 (and BMP388, which differs only in chip ID).
> It's a common successor to the BMP280 and isn't in the repo yet.
>
> **Planned scope (v1):** I2C; temperature, pressure and altitude via `TryRead*` with UnitsNet types; oversampling,
> IIR filter and power-mode settings; forced and normal mode; a sample; unit tests against a simulated device
> (`I2cSimulatedDeviceBase`) using reference values. SPI, FIFO and the interrupt pin would be follow-ups.
>
> **Design questions:**
> 1. A new `src/devices/Bmp3xx/` folder rather than extending `Bmxx80` (the chip ID/reset registers and the
>    calibration layout differ). OK?
> 2. I plan to mirror `Bmp280`'s public API (`TryReadTemperature`, `TryReadPressure`, `TryReadAltitude`,
>    `SetPowerMode`, `Reset`) so users can switch sensors easily. Any preference otherwise?
>
> **Hardware:** I have a BMP390 on a Raspberry Pi <model> and have read the chip and calibration data with a
> small test app. Happy to maintain the binding afterwards.

Fill in the `<model>` and adjust once steps 1–2 are done.

---

## Check yourself

1. Why does the Translator read calibration once, in the constructor, and not on every reading?
2. Which part of this binding can be fully unit-tested with no fake device at all? Why?
3. Why would a reviewer prefer `TryReadPressure(out Pressure)` to a `Pressure` property?
4. What's the argument *for* a new `Bmp3xx` folder, and what's the argument for putting it in `Bmxx80`?
5. Why post the proposal issue **after** step 2 and not before step 1?
