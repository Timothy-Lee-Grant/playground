2026-10-05 — dotnet/iot new binding (BMP3xx): New binding: Bosch BMP390/BMP388 pressure sensor

> **The living plan for mode P (plan-driven, Timothy observes).** One document, read top to bottom.
>
> **Who writes what:** the plan body (Stages 1–3) is owned by **desktop** and revised in place with a version
> number (v1, v2, …; old versions summarized, not deleted). **Entries** are append-only and tagged
> `[Author — YYYY_MM_DD_HH_MM]`: `[Timothy]` (recorded by whichever Claude he said it to), `[Desktop]`, `[CLI]`.
> Nobody edits someone else's entry. The CLI writes Stage 5 entries and change requests; desktop writes the rest.
>
> **Gates** (only Timothy opens them): **G1** plan approved → implementation may start · **G2** before anything is
> posted upstream · **G3** after the teach-back → PR may be opened. Between gates the CLI works autonomously.
> Deviation rules: `00-start-here.md` §6. The standard sequence is `exercises/ai-workflow/default-workflow.md`.
>
> **Plan version:** v1 (2026-10-05, desktop). Status: **waiting for G1.**

---

# Stage 1 (Direction)

## 1.1 Goal

Add a new device binding, **`Iot.Device.Bmp3xx`**, for the Bosch **BMP390** and **BMP388** barometric pressure and
temperature sensors to dotnet/iot, in a new folder `src/devices/Bmp3xx/`, following the repo's device conventions
and mirroring the public API of its sibling `Bmp280` so users can switch sensors with a one-line change.

## 1.2 The task in two sentences

There's no upstream issue: this is a new contribution, announced by a short proposal issue once the sensor works on
Timothy's Pi. The interesting work is not the I2C (Timothy's strength) but the **public API design**, the
**calibration math** and **testing it all without hardware**.

## 1.3 What success looks like (definition of done for the PR branch)

- [ ] `src/devices/Bmp3xx/` exists with the layout in Step 5, and **nothing outside that folder changes**.
- [ ] `dotnet build src/devices/Bmp3xx/tests/ --no-incremental` and `dotnet build src/devices/Bmp3xx/samples/`: **0 warnings, 0 errors**.
- [ ] `dotnet test src/devices/Bmp3xx/tests/`: all pass, 5 runs in a row (no flakiness), no real hardware or sleeps longer than needed.
- [ ] Compensated temperature and pressure match the **reference oracle** (Step 4) within the tolerances recorded in Step 7.
- [ ] The sample runs on Timothy's Pi with a real BMP390 and prints plausible values (pressure within a few hPa of
      a nearby weather station's station pressure; temperature within a few °C of the room). Evidence saved.
- [ ] README: title line, `## Documentation` with the datasheet first, `## Usage`, what is and isn't implemented.
- [ ] Every public member has XML docs with units and ranges; every timing/delay cites a datasheet section.
- [ ] Commits are small and reviewable; Timothy approved each.
- [ ] The implementation summary (Step 15) explains every file and every decision, for lecture 1.

## 1.4 In scope (v1)

| Area | Included |
|---|---|
| Chips | BMP390 (`0x60`), BMP388 (`0x50`) |
| Transport | I2C only (addresses `0x77` default, `0x76` secondary) |
| Startup | chip-ID check, soft reset, read calibration once, write known defaults |
| Configuration | pressure oversampling, temperature oversampling, IIR filter coefficient, output data rate, power mode (sleep / forced / normal) |
| Reading | `Read()` / `ReadAsync()` (forced measurement when not in normal mode), `TryReadTemperature`, `TryReadPressure`, `TryReadAltitude` (×2) |
| Diagnostics | `ReadStatus()` (data-ready / command-ready flags), `ReadErrors()` (fatal / command / configuration error flags) |
| Quality | unit tests on a simulated chip, sample, README, `category.txt`, `System.Device.Model` attributes |

## 1.5 Not in scope (possible follow-up PRs; list them in the README as "not implemented")

SPI · FIFO · interrupt pin (data-ready event) · I2C watchdog settings · sensor time · self-test · integer
(fixed-point) compensation · changes to `Bmxx80` or any shared code · editing `Device-Index.md` or the
categorized list in `src/devices/README.md` (generated).

## 1.6 Constraints the CLI must respect throughout

1. **Clean room.** Bosch's `BMP3_SensorAPI` (BSD-3) may be cloned, built and run **outside `develop/`** as an
   oracle (Step 4). Nothing from it (code, comments, names of internal functions, structure) is copied into the
   fork. The binding is written from the **datasheet**; XML remarks cite datasheet sections.
2. **Only `src/devices/Bmp3xx/**` changes in the fork.** If anything else seems to need changing (Common, a shared
   helper, a repo-level file), stop: **CHANGE REQUEST**.
3. **No upstream activity.** Never push, never use `gh`, never comment. Timothy owns G2/G3 and all public actions.
4. **Hardware steps need Timothy's hands** (wiring, power, and possibly running commands on the Pi). Prepare exact
   commands; run over `ssh`/`scp` only if Timothy has set that up and approves each command.
5. **One active PR:** #2611 is still open. This branch stays local until Timothy decides otherwise.

---

# Stage 2 (Discussion)

Tags: **ours** = Timothy decides at G1. **upstream** = maintainers may decide differently; we build the proposal so
switching later is cheap, and raise it in the proposal issue (Stage 4). The CLI never changes any of these on its
own; if one turns out wrong, it's a **CHANGE REQUEST**.

| ID | Decision | Owner | Proposal | Why |
|---|---|---|---|---|
| **O1** | Which device | ours | **BMP390 + BMP388** | Popular, missing upstream, cheap, highly testable (`../002-choosing-the-device.md`) |
| **U1** | Folder, project and namespace | upstream | New folder `src/devices/Bmp3xx/`, project `Bmp3xx.csproj`, namespace `Iot.Device.Bmp3xx` | `Bmxx80Base` reads chip ID at `0xD0` and resets via `0xE0`; BMP3xx uses `0x00` and `0x7E` and a different calibration layout. Same API *shape*, different implementation |
| **U2** | Public API shape | upstream | **Mirror `Bmx280Base`/`Bmp280`** member names and semantics: `Read()`, `ReadAsync()`, `TryReadTemperature`, `TryReadPressure`, `TryReadAltitude(seaLevelPressure)`, `TryReadAltitude()`, `ReadPowerMode()`, `SetPowerMode()`, `Reset()`, `GetMeasurementDuration()` (int ms), `DefaultI2cAddress = 0x77`, `SecondaryI2cAddress = 0x76` | Users switching from BMP280 change one line; reviewers see a familiar API |
| **O2** | Class structure | ours | `public abstract class Bmp3xxBase` + `public sealed class Bmp390` + `public sealed class Bmp388` (each only supplies its chip ID); `public class Bmp3xxReadResult` | Supports both chips for almost no code; mirrors `Bmx280Base` → `Bmp280` |
| **O3** | Calibration and compensation code | ours | `internal sealed class Bmp3xxCalibrationData` (parse 21 bytes → 14 `double` coefficients; `CompensateTemperature`, `CompensatePressure`), unit-tested directly via `InternalsVisibleTo` (precedent: `Vcnl4040.csproj`) | Pure functions = the most testable part; keeps them off the public API |
| **O4** | Floating point | ours | `double` throughout (conventions: "Use `double` when you need to return any floating point value"). Don't imitate Bosch's single-precision `pow` | Bosch's oracle uses `float` in places, so tests compare with a tolerance (decided in Step 7 from measured differences) |
| **U3** | Transport | upstream | I2C only; all bus access goes through **three private methods** (`ReadRegister`, `ReadRegisters(Span<byte>)`, `WriteRegister`) so SPI can be added later without touching the rest | Keeps v1 small; makes the SPI follow-up a local change |
| **U4** | Out-of-range compensated values (below −40 °C / above 85 °C, below 30 000 Pa / above 125 000 Pa) | upstream | `TryRead*` returns **`false`** (value `default`); `Read()` returns `null` for that quantity | Conventions: never return bogus data silently; a reading outside the sensor's range usually means bad data. Alternative: clamp like Bosch (return true). Ask in the proposal issue |
| **U5** | What `TryReadTemperature` / `TryReadPressure` do in sleep mode | upstream | Same as `Bmx280Base`: they **read the latest data registers without triggering a measurement**; `Read()`/`ReadAsync()` trigger a forced measurement when not in normal mode. If the data registers still hold their reset value (no measurement since reset; Step 0 verifies the value), return `false` | Consistency with the sibling; the reset-value check removes the sibling's "garbage after reset" gotcha |
| **U6** | Waiting for a forced measurement | upstream | Sleep `GetMeasurementDuration()`, then poll `STATUS` for both data-ready bits every 1 ms, **timeout** = 2 × measurement time + 10 ms → `Read()` returns nulls (no exception) | LPS22HB review (#2309) explicitly asked for a timeout on polling loops |
| **U7** | Disposal | upstream | The device **disposes the `I2cDevice`** it was given (no `shouldDispose` parameter) | `Devices-conventions.md`: I2C/SPI devices are 1:1 with hardware and "should be disposed by the device"; `Bmp280` does the same. (The unmerged Copilot skill draft suggests `shouldDispose`; conventions doc wins) |
| **U8** | Wrong chip ID in the constructor | upstream | Throw `IOException` "Unable to find a chip with id 0x60. Found 0x.." | Same exception type and message style as `Bmxx80Base` |
| **O5** | Defaults written by the constructor | ours | Pressure ×1, temperature ×1, filter off, ODR 200 Hz (valid: ×1/×1 takes ~4.9 ms < 5 ms), **sleep** mode, both sensors enabled | Deterministic state regardless of chip reset values; low power until asked; mirrors Bmp280's "UltraLowPower" defaults |
| **O6** | Invalid normal-mode settings (ODR faster than the measurement time) | ours | After `SetPowerMode(Normal)`, read `ERR`; if conf_err is set, put the chip back to sleep and throw `InvalidOperationException` explaining "output data rate too fast for the oversampling settings" | Fail loudly at the moment of the mistake, not with silent stale data |
| **O7** | Names of enums and values | ours (upstream may nitpick) | `Bmp3xxPowerMode {Sleep, Forced, Normal}`, `Bmp3xxOversampling {X1, X2, X4, X8, X16, X32}`, `Bmp3xxFilterCoefficient {Off, Coefficient1, Coefficient3, Coefficient7, Coefficient15, Coefficient31, Coefficient63, Coefficient127}`, `Bmp3xxOutputDataRate {Hz200, Hz100, Hz50, Hz25, Hz12_5, … }` (CLI proposes final names for the slow rates in Step 6, no abbreviations per `src/devices/README.md`), `[Flags] Bmp3xxStatus`, `[Flags] Bmp3xxErrors` | Full names, prefixed to avoid clashes inside the single `Iot.Device.Bindings` assembly |
| **O8** | Commit structure | ours | 4 commits: (1) skeleton + registers/enums; (2) calibration + its tests; (3) device class + simulated chip + tests; (4) sample + README + category | Each is reviewable alone; upstream will likely squash anyway |
| **O9** | Branch name | ours | `feature/bmp3xx-binding` from `upstream/main` | |

### Stage 2 Discussion Subsection

*(Questions and answers about the decisions go here.)*

---

# Stage 3 (Implementation Planning)

## 3.1 Map of the work

```
Phase A  Setup & audit         Step 0  plan vs. code + datasheet audit (no edits)
                               Step 1  branch + toolchain baseline on the Mac
Phase B  Ground truth          Step 2  Pi + sensor ready (Timothy's hands)          ┐ can run in parallel with
         (outside the fork)    Step 3  E1 hardware probe: raw bytes from the chip   │ Phase C if hardware is late;
                               Step 4  reference oracle → test vectors              ┘ Step 7 needs Step 4
Phase C  Build the binding     Step 5  scaffold folder, projects, solution
         (in the fork)         Step 6  register map + enums
                               Step 7  calibration + compensation (tests first)
                               Step 8  simulated chip (test double)
                               Step 9  device class: constructor, config, reset   (tests first)
                               Step 10 device class: reading path                 (tests first)
                               Step 11 sample + README + category.txt
Phase D  Prove it              Step 12 hygiene: warnings, 5× tests, diff scope, clean-room check
                               Step 13 the binding on real hardware (Timothy's hands)
Phase E  Package for Timothy   Step 14 commits (Timothy approves each)
                               Step 15 implementation summary + full diff  →  lecture 1 raw material
Phase F  Timothy's part        Steps 16–20: lecture, proposal issue (G2), teach-back (G3), PR, review
```

## 3.2 Step table

| Step | What | Who | Proof | Gate |
|---|---|---|---|---|
| **0** | Plan review against the code and the datasheet (no edits) | CLI | Stage 5 entry + `evidence/001-step0-audit.txt` | G1 |
| **1** | Branch `feature/bmp3xx-binding`; build + test `Ina236` as a toolchain baseline | CLI | `evidence/002-baseline.txt` | G1 |
| **2** | Pi ready: model/OS, I2C on, wiring, `i2cdetect` shows the sensor; decide ssh vs manual | **Timothy** + CLI | `evidence/003-pi-i2cdetect.txt` | — |
| **3** | E1 probe (outside fork): chip ID, calibration bytes, raw data from the real chip | CLI writes, Timothy/ssh runs | `../sample/evidence/001-E1-probe.txt` + copy in `evidence/004-E1-probe.txt` | — |
| **4** | Reference oracle (outside fork) → test vectors | CLI | `evidence/005-oracle-vectors.txt` | — |
| **5** | Scaffold `src/devices/Bmp3xx/` (projects, sln, empty types); builds clean | CLI | `evidence/006-scaffold-build.txt` | — |
| **6** | Register map + enums, with XML docs | CLI | build clean (in `007`) | — |
| **7** | Calibration + compensation: red tests → implementation → green | CLI | `evidence/007-calibration-red.txt`, `008-calibration-green.txt` | — |
| **8** | `SimulatedBmp3xx` test double + its own sanity tests | CLI | in `009` | — |
| **9** | Device class part 1 (ctor, chip ID, reset, calibration read, defaults, settings, power mode, status/errors): red → green | CLI | `evidence/009-device-config-red.txt`, `010-…-green.txt` | — |
| **10** | Device class part 2 (reading path, timeouts, out-of-range, altitude, async): red → green | CLI | `evidence/011-device-read-red.txt`, `012-…-green.txt` | — |
| **11** | Sample, README, `category.txt` | CLI | `evidence/013-sample-build.txt` | — |
| **12** | Hygiene | CLI | `evidence/014-hygiene.txt` | — |
| **13** | Binding on real hardware: publish sample, run on the Pi, compare with a weather station | **Timothy** + CLI | `evidence/015-hardware-run.txt` | — |
| **14** | Commits per O8 | CLI proposes, **Timothy approves** | `git log --oneline` in the entry | — |
| **15** | Implementation summary + full diff | CLI | Stage 5 entry + `evidence/016-diff.patch` | — |
| **16** | Lecture 1 "The binding, end to end" (Stage 6 spec) | Desktop writes; Timothy reads | lecture link | — |
| **17** | Timothy searches upstream issues, then posts the proposal issue in his own words (Stage 4 draft) | **Timothy** | link in a `[Timothy]` entry | **G2** (+ #2611 settled, or his explicit go) |
| **18** | Teach-back (voice on a walk is fine), analyzed by desktop | **Timothy** talks; desktop analyzes | `private/teachbacks/` | **G3** |
| **19** | Final test run by Timothy, push, PR (CLI drafts description; desktop reviews) | **Timothy** acts | PR link | after G3 |
| **20** | Review: each comment becomes a step; AI drafts, Timothy posts | all | Stage 8 entries | G2 per reply |

**If the hardware isn't there yet:** do Steps 0, 1, 4 (with synthetic and datasheet vectors only), 5–12, and
mark every hardware-dependent claim **unverified**. Steps 3 and 13 run when the sensor arrives; if E1's real
calibration bytes reveal a bug, that's a normal Stage 5 finding, not a failure.

## 3.3 Step cards

Each card: **Goal · Do · Output · Done when · Stop if.** "Stop if" means: write a Stage 5 entry and, where it says
so, a **CHANGE REQUEST**, then continue with unaffected steps.

---

### Step 0 — Plan review against the code and the datasheet (no edits)

**Goal:** prove this plan matches reality before writing anything.

**Do:**

1. **Workspace and disk:** `df -h ~` (the Mac has little free space; record it). `git -C develop/iot remote -v`
   (origin = `Timothy-Lee-Grant/iot`, upstream = `dotnet/iot`). `git -C develop/iot fetch upstream`.
   Record `upstream/main`'s hash.
2. **Nobody else is doing it:** `git -C develop/iot ls-tree -d upstream/main src/devices/ | grep -i bmp` (only
   `Bmp180`, `Bmxx80` expected). Then list PR heads newer than #2600 and check whether any adds a BMP3 folder:
   `git -C develop/iot ls-remote upstream 'refs/pull/*/head'`, fetch heads ≥ 2600 into `refs/remotes/pr/*`, and
   `git diff --name-only --diff-filter=A <merge-base> pr/N | grep -i bmp3`. Delete those refs afterwards.
3. **Conventions are as described:** confirm each and quote the line in the audit file:
   - `src/devices/Ina236/Ina236.csproj`, `Ina236.sln`, `category.txt`, `tests/Ina236.Tests.csproj`, `samples/*.csproj`
   - `src/devices/Directory.Build.props`: `DefaultBindingTfms`, automatic references to `Common` and `System.Device.Model`
   - `build.proj`: `UnitTestProjects Include="…src\devices\**\*.Tests.csproj"`
   - `eng/Compilers.props`: CS1591 enforced for non-test projects; `TreatWarningsAsErrors`
   - `eng/stylecop.json`: the copyright header text
   - `eng/Versions.external.props`: xunit, Moq, Shouldly auto-referenced in `*Tests` projects
   - `src/Iot.Device.Bindings/Iot.Device.Bindings.csproj`: picks up `src/devices/**/*.csproj` except samples/tests
   - `src/devices/Common/System/Device/I2c/I2cSimulatedDeviceBase.cs`: constructor, `RegisterMap`, `CurrentRegister`,
     abstract `WriteRead(byte[], byte[])`, `Register<T>` with update/read handlers
   - `src/devices/Bmxx80/Bmx280Base.cs` + `Bmp280.cs`: the exact public member list and semantics (U2, U5)
   - `src/devices/Vcnl4040/Vcnl4040.csproj`: `InternalsVisibleTo` pattern (O3)
4. **Datasheet audit:** download BST-BMP390-DS002 (URL in `01-brief.md` §3) and the BMP388 datasheet
   (BST-BMP388-DS001; find the current URL). Verify **every row of brief §3** and these extra facts, citing
   section/page for each (these citations go into XML remarks later):
   - reset (power-on) values of `OSR`, `ODR`, `CONFIG`, `PWR_CTRL`, and **the data registers** (needed for U5)
   - whether a burst read of `0x04..0x09` is required for consistent data (data shadowing), and register auto-increment
   - power-on/startup time; soft-reset duration; whether `cmd_rdy` must be checked before writing `CMD`
   - the calibration layout and scaling (table in Step 7) and the compensation formulas (the datasheet's appendix)
   - the measurement-time formula and the ODR ≥ measurement-time rule for normal mode (O6)
   - whether forced mode returns to sleep automatically after one measurement
   - any BMP388-vs-BMP390 differences beyond the chip ID (ranges, noise, registers)
5. **Toolchain:** `dotnet --version`; `cc --version` (needed for Step 4's oracle; if missing, note it).

**Output:** `evidence/001-step0-audit.txt` (commands + results + the datasheet fact table with citations), and one
Stage 5 entry: **"plan holds"** or a list of **CHANGE REQUEST**s.

**Done when:** every brief §3 row is marked ✔ (matches, with citation) or ✘ (differs, with the correct value).

**Stop if:** someone else has a BMP3xx PR open (stop everything, tell Timothy) · a datasheet fact contradicts a
Stage 2 decision · the conventions differ from what Steps 5–11 assume.

---

### Step 1 — Branch and toolchain baseline

**Do:**
```bash
cd develop/iot
git switch -c feature/bmp3xx-binding upstream/main
dotnet build src/devices/Ina236/tests/ --no-incremental
dotnet test  src/devices/Ina236/tests/ --no-build
```
**Output:** `evidence/002-baseline.txt` (header per `evidence/README.md`).
**Done when:** Ina236 tests pass with 0 warnings: proves the Mac can build and test a binding the way we will.
**Stop if:** the build fails for environmental reasons (SDK, feeds). Record the error; don't work around it by editing repo files.

---

### Step 2 — Pi and sensor ready (Timothy's hands)

**Goal:** a known-good bus with the sensor on it, and an agreed way to run code on the Pi.

**Timothy does (CLI gives him this checklist, one item at a time):**

| # | Action | Expected |
|---|---|---|
| 1 | On the Pi: `cat /proc/device-tree/model; uname -m; cat /etc/os-release \| head -3` | Record model and architecture: `aarch64` → publish `linux-arm64`; `armv7l` → `linux-arm` |
| 2 | Enable I2C: `sudo raspi-config` → Interface Options → I2C → Yes; reboot | `ls /dev/i2c-1` exists |
| 3 | **Power off**, then wire: Pi pin 1 (3V3) → VIN/VCC · pin 6 (GND) → GND · pin 3 (GPIO2/SDA1) → SDA/SDI · pin 5 (GPIO3/SCL1) → SCL/SCK. (STEMMA QT cable: red 3V3, black GND, blue SDA, yellow SCL.) Leave CS unconnected/high (I2C mode); SDO decides the address | — |
| 4 | `sudo apt install -y i2c-tools && i2cdetect -y 1` | `77` (or `76`) appears in the grid |
| 5 | Decide how code reaches the Pi: **(a)** `ssh pi@<host>` from the Mac works → the CLI may `scp`/`ssh` (each command approved), or **(b)** manual: CLI publishes to a folder, Timothy copies it (scp/USB) and runs it, then pastes the output | Recorded as a `[Timothy]` entry |
| 6 | .NET on the Pi isn't needed: we publish **self-contained** | — |

**Output:** `evidence/003-pi-i2cdetect.txt` (the model/arch output + `i2cdetect` grid).
**Done when:** the address shows up.
**Stop if:** nothing on the bus → check wiring and power before anything else; don't change code.

---

### Step 3 — E1: hardware probe, outside the fork

**Goal:** read the real chip's bytes before writing the binding. These become test vectors and catch datasheet misreadings early.

**Where:** `../sample/E1-probe/` (the issue folder's `sample/`, reachable from the workspace via the settings'
`additionalDirectories`). **Not** in `develop/`.

**Do:**
1. `dotnet new console -o ../sample/E1-probe -f net8.0`, then pin the package version (no `global.json` needed):
   `dotnet add ../sample/E1-probe package System.Device.Gpio --version 4.2.0` (the public NuGet; no repo build needed).
2. `Program.cs` uses only `I2cDevice.Create(new I2cConnectionSettings(1, address))` with the address as a
   command-line argument (default `0x77`), and prints, as hex:
   - chip ID (`WriteRead([0x00], 1 byte)`), `ERR` (`0x02`), `STATUS` (`0x03`)
   - the 21 calibration bytes (`WriteRead([0x31], 21 bytes)`)
   - then 5 times: write `OSR` = `0x00` (×1/×1), write `PWR_CTRL` = `0x13` (press_en | temp_en | forced), wait 10 ms,
     read `STATUS`, burst-read 6 bytes from `0x04`, print raw pressure and raw temperature as 24-bit integers
   - finally write `PWR_CTRL` = `0x00` (sleep)
   Keep it ~60 lines, no abstractions: it's a probe, not product code.
3. Publish: `dotnet publish ../sample/E1-probe -c Release -r <rid from Step 2> --self-contained -o ../sample/E1-probe/out`
4. Run on the Pi (route from Step 2.5): `./E1-probe 0x77`. Also note the room temperature and the time, and
   look up the nearest weather station's **station pressure** (not sea-level) at that time.
5. Write `../sample/README.md` (what E1 is, how to run it) if it doesn't exist.

**Output:** `../sample/evidence/001-E1-probe.txt` (header: date, Pi model, OS, rid, package version, command) and the
same content as `evidence/004-E1-probe.txt` for the CLI's own record.
**Done when:** chip ID is `0x60` (BMP390) or `0x50` (BMP388), 21 calibration bytes are printed, and the 5 raw
readings are stable (small variation).
**Stop if:** chip ID differs (wrong address, wrong part: some cheap boards are BMP280 = `0x58`) → tell Timothy.

---

### Step 4 — Reference oracle → test vectors (outside the fork)

**Goal:** independent expected values for the compensation, so the tests check the math against something other than our own code.

**Where:** `~/Desktop/projects/oss-work/iot-bmp3xx/tools/bmp3-oracle/` (the workspace root, not `develop/`, not `shared/`).

**Do:**
1. `git clone --depth 1 https://github.com/boschsensortec/BMP3_SensorAPI tools/bmp3-oracle/BMP3_SensorAPI` and
   record its commit (expected `db4cf8e`).
2. Write `tools/bmp3-oracle/oracle.c`: a fake bus whose read callback serves a 128-byte register image (chip ID,
   `STATUS` with `cmd_rdy` set, `ERR` = 0, calibration at `0x31`, data at `0x04`), and whose write/delay callbacks do
   nothing. Call the driver's init and get-sensor-data functions with `BMP3_FLOAT_COMPENSATION` defined. Input on
   the command line: 21 calibration bytes (hex) + raw pressure + raw temperature. Output: `T=%.9f P=%.9f`.
3. Build: `cc -O0 -DBMP3_FLOAT_COMPENSATION -I BMP3_SensorAPI oracle.c BMP3_SensorAPI/bmp3.c -lm -o oracle`.
4. Produce vectors:
   - **V1–V5:** E1's real calibration bytes with each of E1's 5 raw readings (if Step 3 is done)
   - **V6–V8:** the same calibration with raw values chosen to land near 0 °C, 25 °C, 60 °C
   - **V9–V10:** raw values that land **outside** the valid range (one too cold, one too high pressure), to test U4;
     record that Bosch clamps and returns a warning
   - **V11–V12:** a second, synthetic calibration set (e.g. from Bosch's or Adafruit's published examples, or E1's
     bytes with sign bits flipped on the `int8`/`int16` fields) so **negative coefficients** are exercised
5. **Second, independent check:** a tiny Python script (`tools/bmp3-oracle/formulas.py`) that implements the
   datasheet formulas in `float64` from the datasheet text. Print both oracles side by side; the difference shows
   how much Bosch's single-precision `pow` costs (feeds O4's tolerance).
6. **Fallback if there's no C compiler:** use only the Python oracle and say so in the entry (weaker: it's our own
   reading of the datasheet, so it can share our mistakes).

**Output:** `evidence/005-oracle-vectors.txt`: the commit, build command, and a table
`vector | calibration bytes | rawP | rawT | T(Bosch) | P(Bosch) | T(py) | P(py) | ΔT | ΔP`.
**Done when:** at least V6–V12 exist (V1–V5 too if hardware is ready).
**Stop if:** the two oracles disagree by more than ~0.01 °C or ~1 Pa on in-range vectors → one of the two
readings of the formulas is wrong; investigate before Step 7.

**Rule (repeat of Stage 1.6.1):** only *numbers* cross from `tools/` into the fork's tests. Never code or comments.

---

### Step 5 — Scaffold `src/devices/Bmp3xx/`

**Do:** create exactly these files (every `.cs` starts with the two-line copyright header from `eng/stylecop.json`):

```
src/devices/Bmp3xx/
├── Bmp3xx.csproj                 like Ina236.csproj + RootNamespace + InternalsVisibleTo (Vcnl4040 pattern)
├── Bmp3xx.sln                    dotnet new sln; add binding, samples, tests, ../Common/Common.csproj
├── Bmp3xxBase.cs                 abstract class, members throw NotImplementedException for now
├── Bmp390.cs, Bmp388.cs          sealed, ctor passes chip ID
├── Bmp3xxReadResult.cs
├── Bmp3xxCalibrationData.cs      internal sealed
├── Bmp3xxRegister.cs             internal enum : byte (filled in Step 6)
├── Bmp3xxPowerMode.cs, Bmp3xxOversampling.cs, Bmp3xxFilterCoefficient.cs, Bmp3xxOutputDataRate.cs,
│   Bmp3xxStatus.cs, Bmp3xxErrors.cs
├── README.md                     title line only for now
├── category.txt                  barometer / altimeter / thermometer (one per line, as in Bmp180)
├── samples/
│   ├── Bmp3xx.Samples.csproj     like Ina236.Samples.csproj (OutputType Exe, $(DefaultSampleTfms))
│   └── Program.cs                "Hello Bmp3xx" placeholder
└── tests/
    ├── Bmp3xx.Tests.csproj       name must end in .Tests (build.proj glob); like Ina236.Tests.csproj
    └── (test files from Step 7 on)
```

`Bmp3xx.csproj` (adapt from `Ina236.csproj` and `Vcnl4040.csproj`):
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>$(DefaultBindingTfms)</TargetFrameworks>
    <!--Disabling default items so samples source won't get build by the main library-->
    <EnableDefaultItems>false</EnableDefaultItems>
    <RootNamespace>Iot.Device.Bmp3xx</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="*.cs" />
    <None Include="README.md" />
  </ItemGroup>
  <!-- Make the internal classes visible to the unit test assembly -->
  <ItemGroup>
    <AssemblyAttribute Include="System.Runtime.CompilerServices.InternalsVisibleToAttribute">
      <_Parameter1>$(AssemblyName).Tests</_Parameter1>
    </AssemblyAttribute>
  </ItemGroup>
</Project>
```

**Output:** `evidence/006-scaffold-build.txt`: `dotnet build src/devices/Bmp3xx/tests/ --no-incremental` and
`dotnet build src/devices/Bmp3xx/samples/`, 0 warnings.
**Done when:** both build clean, and `git status` shows only new files under `src/devices/Bmp3xx/`.
**Stop if:** a check from Step 0 point 3 behaves differently (e.g. the test project isn't discovered).

---

### Step 6 — Register map and enums

**Do:**
- `internal enum Bmp3xxRegister : byte`: `ChipId = 0x00, Error = 0x02, Status = 0x03, Data = 0x04, PowerControl = 0x1B,
  Oversampling = 0x1C, OutputDataRate = 0x1D, Configuration = 0x1F, CalibrationData = 0x31, Command = 0x7E`
  (only what v1 uses; names spelled out).
- Public enums (values = the register bit patterns **before shifting**; XML docs on every value with its meaning,
  and for oversampling the resolution/noise trade-off from the datasheet):
  - `Bmp3xxPowerMode : byte { Sleep = 0, Forced = 1, Normal = 3 }`
  - `Bmp3xxOversampling : byte { X1 = 0, X2 = 1, X4 = 2, X8 = 3, X16 = 4, X32 = 5 }`
  - `Bmp3xxFilterCoefficient : byte { Off = 0, Coefficient1 = 1, Coefficient3 = 2, … Coefficient127 = 7 }`
  - `Bmp3xxOutputDataRate : byte` with 18 values `0x00 … 0x11` (propose readable names; document each period)
  - `[Flags] Bmp3xxStatus : byte { None = 0, CommandReady = 0x10, PressureDataReady = 0x20, TemperatureDataReady = 0x40 }`
  - `[Flags] Bmp3xxErrors : byte { None = 0, Fatal = 0x01, Command = 0x02, Configuration = 0x04 }`
- Bit positions/masks as `private const` in `Bmp3xxBase` (e.g. `PowerModeShift = 4`, `TemperatureOversamplingShift = 3`,
  `FilterShift = 1`), each with a datasheet citation comment.

**Output:** build clean (captured at the start of `evidence/007`).
**Done when:** no magic numbers remain outside these definitions (Steps 9–10 must use them).

---

### Step 7 — Calibration and compensation (tests first)

**Goal:** the math, proven against the oracle, before any I/O code exists.

**The calibration table** (verify in Step 0; byte index = offset from `0x31`, little-endian):

| Bytes | Name | Raw type | Scaled value (as `double`) |
|---|---|---|---|
| 0–1 | T1 | `ushort` | raw × 2^8 |
| 2–3 | T2 | `ushort` | raw ÷ 2^30 |
| 4 | T3 | `sbyte` | raw ÷ 2^48 |
| 5–6 | P1 | `short` | (raw − 2^14) ÷ 2^20 |
| 7–8 | P2 | `short` | (raw − 2^14) ÷ 2^29 |
| 9 | P3 | `sbyte` | raw ÷ 2^32 |
| 10 | P4 | `sbyte` | raw ÷ 2^37 |
| 11–12 | P5 | `ushort` | raw × 2^3 |
| 13–14 | P6 | `ushort` | raw ÷ 2^6 |
| 15 | P7 | `sbyte` | raw ÷ 2^8 |
| 16 | P8 | `sbyte` | raw ÷ 2^15 |
| 17–18 | P9 | `short` | raw ÷ 2^48 |
| 19 | P10 | `sbyte` | raw ÷ 2^48 |
| 20 | P11 | `sbyte` | raw ÷ 2^65 |

**The compensation, in words** (implement from the datasheet's appendix, not from this summary):
temperature: `d = rawT − T1`; `t = d·T2 + d²·T3` (°C). Pressure uses that `t`:
`offset = P5 + P6·t + P7·t² + P8·t³`; `sensitivity = rawP · (P1 + P2·t + P3·t² + P4·t³)`;
`nonlinear = rawP²·(P9 + P10·t) + rawP³·P11`; `p = offset + sensitivity + nonlinear` (Pa).

**Shape (internal):**
```csharp
internal sealed class Bmp3xxCalibrationData
{
    public const int Length = 21;
    public static Bmp3xxCalibrationData Parse(ReadOnlySpan<byte> bytes);   // throws ArgumentException if Length != 21
    public double T1 { get; } … public double P11 { get; }                 // scaled values, for tests and debugging
    public double CompensateTemperature(uint rawTemperature);              // °C, not clamped (range check is the caller's, U4)
    public double CompensatePressure(uint rawPressure, double temperatureCelsius); // Pa, not clamped
}
```

**Do, in order:**
1. **Red:** `tests/Bmp3xxCalibrationDataTests.cs` with:
   - `Parse_ScalesEachCoefficient`: hand-built 21 bytes where each field has a known raw value (including negative
     `sbyte`/`short`), asserting each scaled property (exact `double` equality is fine: powers of two are exact)
   - `Parse_RejectsWrongLength` (20 and 22 bytes)
   - `CompensateTemperature_MatchesOracle` / `CompensatePressure_MatchesOracle`: `[Theory]` rows V1–V12 from
     `evidence/005` (in-range ones), with a comment naming the vector and its source
   Run → **every test fails for the stated reason** (`NotImplementedException`), not a compile error.
   Save `evidence/007-calibration-red.txt`.
2. **Green:** implement `Parse` with `BinaryPrimitives.ReadUInt16LittleEndian` / `ReadInt16LittleEndian`, `(sbyte)`
   casts, and `Math.ScaleB(value, n)` or literal powers of two (choose one, say why). Implement the formulas
   exactly as the datasheet orders them. XML remarks cite the datasheet section.
3. **Tolerance:** compute the maximum |ours − Bosch| over the in-range vectors; set the test tolerance to a round
   number comfortably above it **and** far below the sensor's resolution (e.g. 0.001 °C and 0.01 Pa; the sensor's
   RMS noise is ~0.02 Pa). Record the measured differences and the chosen tolerance **and why** in the Stage 5 entry.
4. Save `evidence/008-calibration-green.txt`.

**Done when:** all calibration tests pass; tolerances justified in writing.
**Stop if:** an in-range vector misses by more than the tolerance and the cause isn't a typo → record the finding,
re-check the datasheet formula; if the datasheet and Bosch's code disagree, **CHANGE REQUEST** with both readings.

---

### Step 8 — The simulated chip (test double)

**Goal:** a fake BMP3xx good enough to drive the real `Bmp390` class through every path.

**Do:** `tests/SimulatedBmp3xx.cs : I2cSimulatedDeviceBase`
- Register map: one `Register<byte>` per address used (`0x00`, `0x02`, `0x03`, `0x04…0x09`, `0x1B`, `0x1C`, `0x1D`,
  `0x1F`, `0x31…0x45`, `0x7E`). Constructor takes `chipId` and the 21 calibration bytes.
- `WriteRead(byte[] input, byte[] output)`: first input byte sets `CurrentRegister`; following input bytes are
  written to consecutive registers (auto-increment); `output` is filled from consecutive registers starting at
  `CurrentRegister` (burst read with auto-increment; verify the real chip's behavior in Step 0).
- Behaviors (each switchable from a test):
  - writing `0xB6` to `0x7E` restores reset values and counts resets (`ResetCount`)
  - writing a forced/normal mode to `PWR_CTRL` "measures": copies the scripted raw pressure/temperature into
    `0x04…0x09` and sets the data-ready bits — or, if `NeverBecomesReady = true`, leaves them clear (timeout test)
  - forced mode returns `PWR_CTRL` mode bits to sleep after "measuring" (if Step 0 confirms the real chip does)
  - `ConfigurationErrorOnNormalMode = true` sets `ERR` conf_err when normal mode is written (O6 test)
  - a write log (`List<(byte register, byte value)>`) so tests can assert exactly what was written
- 2–3 small tests of the double itself (burst read crosses registers correctly; reset restores values), so a failing
  binding test can't be blamed on the fake.

**Output:** part of `evidence/009`.
**Done when:** the double's own tests pass.
**Rule:** the double models the **datasheet**, not our implementation. If writing it requires a guess about chip
behavior, record the guess as **unverified** in the Stage 5 entry and in a code comment.

---

### Step 9 — Device class, part 1: construction and configuration (tests first)

**Constructor sequence** (`protected Bmp3xxBase(byte chipId, I2cDevice i2cDevice)`):
1. `ArgumentNullException` if `i2cDevice` is null.
2. Read chip ID; if ≠ expected → `IOException` (U8).
3. Soft reset (the `Reset()` method below).
4. Read 21 calibration bytes in one burst → `Bmp3xxCalibrationData.Parse`.
5. Write defaults (O5): OSR ×1/×1, filter off, ODR 200 Hz, `PWR_CTRL` = press_en | temp_en | sleep.

**Public members in this step** (XML docs on all; `System.Device.Model` attributes as in `Bmx280Base`):
- `[Interface("…")]` on `Bmp3xxBase`; `public const byte DefaultI2cAddress = 0x77; SecondaryI2cAddress = 0x76;`
- `[Property] PressureSampling`, `[Property] TemperatureSampling` (`Bmp3xxOversampling`): setter does
  read-modify-write of `OSR`, preserving the other field
- `[Property] FilterCoefficient` (`CONFIG` bits 1–3), `[Property] OutputDataRate` (`ODR` bits 0–4)
- `[Property("PowerMode")] Bmp3xxPowerMode ReadPowerMode()` and `[Command] void SetPowerMode(Bmp3xxPowerMode)`:
  read-modify-write keeping press_en/temp_en; after `Normal`, check `ERR` (O6)
- `int GetMeasurementDuration()`: from the formula, in ms, **rounded up** (U2)
- `[Telemetry("Status")] Bmp3xxStatus ReadStatus()`, `Bmp3xxErrors ReadErrors()`
- `[Command] void Reset()`: wait for `cmd_rdy` (bounded), write `0xB6` to `CMD`, wait the datasheet time, check
  `ERR` cmd_err → `IOException` if set; afterwards re-apply the current settings (so properties stay truthful)
- `Dispose()` / `protected virtual Dispose(bool)`: dispose the `I2cDevice` (U7); later calls throw
  `ObjectDisposedException` (`ObjectDisposedException.ThrowIf`, as `Ina236` does)
- Private bus seam (U3): `ReadRegister`, `ReadRegisters(Bmp3xxRegister start, Span<byte>)`, `WriteRegister`, all
  using `WriteRead` for reads (write the register address, then read; one transaction)

**Tests first** (`tests/Bmp3xxBaseTests.cs`, all via `SimulatedBmp3xx`):
`Constructor_Bmp390_AcceptsChipId60` · `Constructor_Bmp388_AcceptsChipId50` · `Constructor_WrongChipId_ThrowsIOException`
· `Constructor_NullDevice_Throws` · `Constructor_ResetsOnce_AndWritesDefaults` (assert the write log) ·
`PressureSampling_SetX8_WritesOsrBitsAndKeepsTemperatureBits` · same for temperature · `FilterCoefficient_RoundTrips`
· `OutputDataRate_RoundTrips` · `SetPowerMode_Forced_KeepsSensorsEnabled` · `SetPowerMode_Normal_WithConfigurationError_Throws_AndReturnsToSleep`
· `GetMeasurementDuration_X1X1_Is5ms` · `GetMeasurementDuration_X32X2_MatchesFormula` · `Reset_WritesB6ToCommandRegister`
· `ReadErrors_ReportsFlags` · `Dispose_DisposesI2cDevice_AndBlocksFurtherUse`.

Red (`evidence/009-device-config-red.txt`, each failing for the stated reason) → implement → green (`010`).
**Done when:** all pass; no magic numbers; every delay cites the datasheet.

---

### Step 10 — Device class, part 2: the reading path (tests first)

**Members:**
- `Bmp3xxReadResult Read()`: if not `Normal`: `SetPowerMode(Forced)`, `Thread.Sleep(GetMeasurementDuration())`,
  then poll `ReadStatus()` until both data-ready flags are set, every 1 ms, up to the U6 timeout → on timeout return
  a result with both values `null`. Then one 6-byte burst read of `0x04…0x09`, compensate temperature, then
  pressure; range-check each (U4) → `null` when out of range.
- `Task<Bmp3xxReadResult> ReadAsync()`: same logic with `await Task.Delay(...)`. Share the non-waiting parts in
  private methods so the two can't drift apart.
- `[Telemetry("Temperature")] bool TryReadTemperature(out Temperature)` and `[Telemetry("Pressure")] bool TryReadPressure(out Pressure)`:
  read the latest data (one burst), no trigger (U5); `false` when the registers still hold the reset value, or the
  result is out of range (U4). Pressure needs temperature: compute it from the same burst.
- `bool TryReadAltitude(Pressure seaLevelPressure, out Length altitude)` and `TryReadAltitude(out Length)` (uses
  `WeatherHelper.MeanSeaLevelPressure`): read pressure + temperature, then `WeatherHelper.CalculateAltitude(pressure, seaLevelPressure, temperature)`, exactly as `Bmx280Base` does.
- Units: `Temperature.FromDegreesCelsius`, `Pressure.FromPascals`, `Length` from `WeatherHelper`.

**Tests first** (`tests/Bmp3xxReadTests.cs`):
`Read_InSleep_TriggersForcedMeasurement_AndReturnsOracleValues` (V-vector through the full stack) ·
`Read_InNormalMode_DoesNotWritePowerControl` · `Read_WhenNeverReady_TimesOutAndReturnsNulls` (assert it returns
within the expected time) · `Read_OutOfRangeTemperature_ReturnsNullTemperature` (V9) · same for pressure (V10) ·
`ReadAsync_MatchesRead` · `TryReadTemperature_AfterReset_ReturnsFalse` · `TryReadPressure_AfterMeasurement_ReturnsTrue` ·
`TryReadAltitude_AtSeaLevelPressure_IsNearZero` · `TryReadAltitude_UsesGivenSeaLevelPressure`.

Red (`011`) → implement → green (`012`).
**Done when:** all pass; the timeout test is fast (no multi-second sleeps in the suite).
**Stop if:** the reset-value check (U5) can't be made reliable from the datasheet → **CHANGE REQUEST** (options:
drop it and document the sibling's behavior, or track "has measured since reset" in the class).

---

### Step 11 — Sample, README, category

**Sample** (`samples/Program.cs`, top-level statements like the Bmp280 sample):
create `I2cDevice` on bus 1 at `Bmp390.DefaultI2cAddress` (comment: use `SecondaryI2cAddress` if SDO is low) →
`using var sensor = new Bmp390(i2cDevice)` → set ×8 pressure / ×1 temperature / filter 3 → loop: `Read()`, print °C,
hPa, altitude from `WeatherHelper.MeanSeaLevelPressure`, then once switch to `Normal` mode at 25 Hz and print 5
readings via `TryReadPressure`; handle `null`s. No hard-coded paths. Keep it short and readable.

**README.md** (structure per `src/devices/README.md` "Contributing a binding" and the newer bindings):
```
# BMP390/BMP388 - barometric pressure, altitude and temperature sensor
<two-line description; supported chips; common breakout names (Adafruit 4816, SparkFun, generic)>
## Documentation
- BMP390 [datasheet](https://www.bosch-sensortec.com/media/boschsensortec/downloads/datasheets/bst-bmp390-ds002.pdf)
- BMP388 [datasheet](<URL found in Step 0>)
## Usage
<code from the sample, shortened>
## Wiring
<pin table from Step 2>
## Binding Notes
Implemented: … · Not implemented: SPI, FIFO, interrupt pin, I2C watchdog, sensor time, self-test
Out-of-range behavior (U4); TryRead* in sleep mode (U5)
```
**category.txt:** `barometer`, `altimeter`, `thermometer` (one per line; existing categories, no new descriptions needed).

**Output:** `evidence/013-sample-build.txt` (`dotnet build src/devices/Bmp3xx/samples/`, 0 warnings).
**Done when:** sample builds; README renders (no broken relative links).
**Don't:** touch `src/devices/Device-Index.md` or `src/devices/README.md`.

---

### Step 12 — Hygiene

**Do** (one script, real exit codes captured as on iot#2328):
1. `dotnet build src/devices/Bmp3xx/tests/ --no-incremental` and `samples/`: 0 warnings, 0 errors.
2. `dotnet test src/devices/Bmp3xx/tests/ --no-build` **5 times**: identical pass counts; note total duration.
3. `git diff --stat upstream/main...HEAD` (or `git status` before commits): **only** `src/devices/Bmp3xx/**`.
4. Clean-room check: `grep -rniE "bosch sensortec|bmp3_|BSD|partial_data|quantized" src/devices/Bmp3xx` returns
   nothing (the datasheet URL and "Bosch" in README prose are fine; note them).
5. `grep -rn "TODO\|NotImplementedException" src/devices/Bmp3xx` returns nothing.
6. Every public member has XML docs (the build enforces it; confirm no `#pragma warning disable`).
7. Re-read the conventions list in `src/devices/README.md` and `Devices-conventions.md`; tick each in the entry.

**Output:** `evidence/014-hygiene.txt`.
**Done when:** all seven pass.

---

### Step 13 — The binding on real hardware

**Do:**
1. `dotnet publish src/devices/Bmp3xx/samples/ -c Release -r <rid> --self-contained -o ~/Desktop/projects/oss-work/iot-bmp3xx/publish/sample`
2. Run on the Pi via the Step 2.5 route for ~1 minute.
3. Record alongside: room temperature (any thermometer), nearest weather station's **station pressure** (or
   sea-level pressure + known elevation → convert with `WeatherHelper.CalculateBarometricPressure`), time.
4. Run E1 once more right after the sample, and put its raw bytes through the oracle (Step 4). The oracle's
   result and the sample's output should agree to within normal reading-to-reading noise. That ties the hardware
   run to the numbers the unit tests use.

**Output:** `evidence/015-hardware-run.txt` (Pi model, OS, rid, commit, sample output, reference values, deltas).
**Done when:** pressure within ~±3 hPa of the reference after altitude correction and temperature within a few °C
(the chip self-heats slightly; note it). This is a **sanity check, not a calibration**: say so.
**Stop if:** values are far off → compare with E1/oracle on the same bytes: if the oracle agrees with the
binding, the problem is the reference or the environment; if not, it's a bug (back to Step 7/10).

---

### Step 14 — Commits (Timothy approves each)

Per O8, on `feature/bmp3xx-binding`:
1. `Add Bmp3xx binding skeleton, registers and settings enums`
2. `Add BMP3xx calibration parsing and compensation with tests`
3. `Add Bmp390/Bmp388 device classes with simulated-device tests`
4. `Add Bmp3xx sample, README and category`

No AI trailers (rule 7 in `00-start-here.md`). Show Timothy `git diff --stat` for each before asking.
**Output:** `git log --oneline upstream/main..HEAD` in the Stage 5 entry.

---

### Step 15 — Implementation summary (raw material for lecture 1)

One Stage 5 entry, written for a reader who wasn't watching:
1. **The file tour:** every file, its job in one line, and who calls it (a table) + an ASCII diagram of the call
   path for `Read()` from the sample down to `WriteRead`.
2. **Every decision** from Stage 2 as actually built, plus every implementation-level choice made along the way,
   each with its **why** and the **alternatives rejected**.
3. **The tests:** for each test, one line on what it proves and one on what it **doesn't**; the tolerance story;
   which behaviors of the simulated chip are datasheet-verified vs. guessed.
4. **The hardware story:** E1, oracle, final run; what's verified on hardware and what's only unit-tested.
5. **Open questions** for the proposal issue and the PR.
6. Save `git diff upstream/main...HEAD > shared/evidence/016-diff.patch`.

---

### Steps 16–20 — Timothy's part (desktop + Timothy; the CLI only drafts when asked)

- **16 Lecture 1** — desktop writes it from Step 15 (spec in Stage 6). Timothy reads it and asks questions.
- **17 Proposal issue (G2)** — Timothy first searches upstream issues for "BMP390", "BMP388", "BMP3"; then posts
  Stage 4's draft in his own words. Wait a few days for maintainer answers to U1–U7; each answer that differs from
  our proposal becomes a plan revision (v2) and a CLI step.
- **18 Teach-back (G3)** — on lecture 1's checklist; desktop analyzes per root `CLAUDE.md` §10.4.
- **19 PR** — CLI drafts the description (Stage 7 skeleton); desktop reviews; Timothy reruns Step 12 himself,
  pushes `feature/bmp3xx-binding` to his fork, opens the PR **with "Fixes #<proposal issue>"** as the first line.
- **20 Review** — each comment becomes a numbered step in Stage 8; AI drafts code and replies; Timothy posts.

## 3.4 Stage 3 Discussion Subsection

*(Questions about the plan, and the G1 grant, go here.)*

[Desktop — 2026_10_05] Things Timothy may want to change before granting G1: the device (O1), the class structure
(O2), out-of-range behavior (U4: return false vs. clamp), defaults (O5), and whether Steps 3/13 wait for hardware
or the CLI builds everything first. Also: **when to start**. Steps 0–15 are local and can start now; Step 17 (the
first public action) waits for #2611 to settle unless Timothy decides otherwise.

---

# Stage 4 (Upstream Communication)

**Draft v1 of the proposal issue** (Timothy rewrites it in his own words before posting at G2; fill the `<…>`):

> **Title:** Proposal: new binding for Bosch BMP390 / BMP388 pressure sensor
>
> Hi! I'd like to contribute a binding for the Bosch BMP390 (and the BMP388, which is register-compatible and
> differs only in its chip ID). It's a common successor to the BMP280 and isn't in the repo yet.
>
> **Scope (v1):** I2C; temperature, pressure and altitude through `Read()`/`ReadAsync()` and `TryRead*` with UnitsNet
> types; oversampling, IIR filter, output data rate and power-mode settings; a sample; unit tests against a
> simulated device (`I2cSimulatedDeviceBase`). SPI, FIFO and the interrupt pin would be follow-ups.
>
> **Design questions:**
> 1. A new `src/devices/Bmp3xx/` folder rather than extending `Bmxx80` (the chip-ID/reset registers and the
>    calibration layout differ). OK?
> 2. I've mirrored `Bmp280`'s public API (`Read`, `TryReadTemperature`, `TryReadPressure`, `TryReadAltitude`,
>    `SetPowerMode`, `Reset`) so users can switch sensors easily. Any preference otherwise?
> 3. When a compensated value falls outside the sensor's range (−40…85 °C, 300…1250 hPa), I return `false` from
>    `TryRead*` rather than clamping. Is that the behavior you'd want?
>
> **Status:** working on a Raspberry Pi <model> with a BMP390 breakout; unit tests pass locally. Happy to maintain it.
> (AI-assisted; I've reviewed and can explain all of it.)

---

# Stage 5 (Implementation)

*(CLI entries go here, one per step or per meaningful finding, each with: what changed, deviations, evidence,
and **why**. Change requests are marked **CHANGE REQUEST** and stop work on the affected steps until Timothy
grants or declines them.)*

---

# Stage 6 (Understanding)

**Lecture 1 spec: "The binding, end to end"** (built from Step 15, `evidence/` and the diff; apply the learner
model's lessons for future lectures):
0. **Watch it work** (10 min): run the tests, then the sample on the Pi; commands to copy.
1. **What a binding is for**, and where this one sits (app → `Bmp390` → `I2cDevice` → kernel → chip).
2. **The cast** (the characters from `../003-…` §1) and the call path of one `Read()`, with the chip's state after
   every step (a state-per-step table, not just actions).
3. **The file tour** and the public API, member by member: why each exists and why it's shaped like `Bmp280`'s.
4. **The calibration math:** from 21 bytes to °C and Pa; signed vs unsigned fields; why `double`; the tolerance story.
5. **The tests:** the object graph (real vs fake), arrange/act/assert for three representative tests, what each
   group proves and what none of them prove.
6. **Every decision** (Stage 2) as built, with the alternatives, and which ones maintainers might push back on.
7. **The proposal issue, sentence by sentence:** what it commits him to, and what each likely answer would mean.
8. **Teach-back checklist** (5–10 ideas, starting with what the binding is *for*).

---

# Stage 7 (Contribution)

**PR description skeleton** (CLI fills at Step 19; desktop reviews):

```
Fixes #<proposal issue>

Adds a binding for the Bosch BMP390 / BMP388 barometric pressure and temperature sensors (`src/devices/Bmp3xx`).

### What's included
- `Bmp390`, `Bmp388` (shared `Bmp3xxBase`): I2C; Read/ReadAsync; TryReadTemperature/Pressure/Altitude; oversampling,
  IIR filter, output data rate, power mode; status and error flags
- Unit tests against a simulated device (`I2cSimulatedDeviceBase`), compensation checked against reference values
- Sample and README (implemented / not implemented)

### Not included (possible follow-ups)
SPI, FIFO, interrupt pin, I2C watchdog, self-test

### Verified on
Raspberry Pi <model>, <OS>, <BMP390 breakout>; .NET SDK <version>; tests <n>/<n>

AI-assisted; I've reviewed all of it and can explain every part.
```

---

# Stage 8 (Review)

*(One entry per review comment and its resolution.)*
