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
> **Plan version:** v2 (2026-10-05, desktop). Status: **waiting for G1.**
> v2 = v1 adapted to the **project layout** (Timothy, 2026-10-05): one shared clone at
> `~/Desktop/projects/open_source/iot_project/iot/`, this mailbox at `iot_project/new-device-binding/`, scratch at
> `iot_project/scratch/new-device-binding/`; and **pushing the branch to Timothy's fork is now allowed** (with his
> OK, never upstream, never force). No step's content changed otherwise.
>
> **Paths below:** `iot/` = the shared clone (run commands from the project root); `shared/` = this mailbox;
> `$SAMPLE` = `~/Desktop/projects/exercises/iot/new-device-binding/sample`; `$SCRATCH` = `~/Desktop/projects/open_source/iot_project/scratch/new-device-binding`.

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

1. **Clean room.** Bosch's `BMP3_SensorAPI` (BSD-3) may be cloned, built and run **outside the clone `iot/`** (in `$SCRATCH/tools/`) as an
   oracle (Step 4). Nothing from it (code, comments, names of internal functions, structure) is copied into the
   fork. The binding is written from the **datasheet**; XML remarks cite datasheet sections.
2. **Only `src/devices/Bmp3xx/**` changes in the fork.** If anything else seems to need changing (Common, a shared
   helper, a repo-level file), stop: **CHANGE REQUEST**.
3. **No upstream activity.** Never push to `upstream`, never use `gh`, never comment, never open a PR. Pushing
   `feature/bmp3xx-binding` to **origin** (Timothy's fork) is allowed when Timothy says so: it notifies nobody
   upstream. Timothy owns G2/G3 and all public actions.
4. **Hardware steps need Timothy's hands** (wiring, power, and possibly running commands on the Pi). Prepare exact
   commands; run over `ssh`/`scp` only if Timothy has set that up and approves each command.
5. **One active PR:** #2611 is still open. This branch lives only locally and on Timothy's fork until he decides
   otherwise. **Never touch `fix/2328-gpiobutton-initial-state`** (protected in `ISSUES.md`), and never switch
   branches with uncommitted work.

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
| **3** | E1 probe (outside fork): chip ID, calibration bytes, raw data from the real chip | CLI writes, Timothy/ssh runs | `$SAMPLE/evidence/001-E1-probe.txt` + copy in `evidence/004-E1-probe.txt` | — |
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

1. **Workspace and disk:** `df -h ~` (the Mac has little free space; record it). `git -C iot remote -v`
   (origin = `git@github.com:Timothy-Lee-Grant/iot.git`; upstream fetch = `dotnet/iot`, upstream **push** = the
   disabled placeholder set by `setup.sh`: if it's a real URL, stop). `git -C iot fetch upstream`.
   Record `upstream/main`'s hash.
2. **Nobody else is doing it:** `git -C iot ls-tree -d upstream/main src/devices/ | grep -i bmp` (only
   `Bmp180`, `Bmxx80` expected). Then list PR heads newer than #2600 and check whether any adds a BMP3 folder:
   `git -C iot ls-remote upstream 'refs/pull/*/head'`, fetch heads ≥ 2600 into `refs/remotes/pr/*`, and
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
git -C iot status --porcelain          # must be empty before switching (project CLAUDE.md rule 2)
cd iot
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

**Where:** `$SAMPLE/E1-probe/` (the issue folder's `sample/` in `exercises`, reachable via the settings'
`additionalDirectories`). **Not** in the clone `iot/`.

**Do:**
1. `dotnet new console -o $SAMPLE/E1-probe -f net8.0`, then pin the package version (no `global.json` needed):
   `dotnet add $SAMPLE/E1-probe package System.Device.Gpio --version 4.2.0` (the public NuGet; no repo build needed).
2. `Program.cs` uses only `I2cDevice.Create(new I2cConnectionSettings(1, address))` with the address as a
   command-line argument (default `0x77`), and prints, as hex:
   - chip ID (`WriteRead([0x00], 1 byte)`), `ERR` (`0x02`), `STATUS` (`0x03`)
   - the 21 calibration bytes (`WriteRead([0x31], 21 bytes)`)
   - then 5 times: write `OSR` = `0x00` (×1/×1), write `PWR_CTRL` = `0x13` (press_en | temp_en | forced), wait 10 ms,
     read `STATUS`, burst-read 6 bytes from `0x04`, print raw pressure and raw temperature as 24-bit integers
   - finally write `PWR_CTRL` = `0x00` (sleep)
   Keep it ~60 lines, no abstractions: it's a probe, not product code.
3. Publish: `dotnet publish $SAMPLE/E1-probe -c Release -r <rid from Step 2> --self-contained -o $SCRATCH/publish/E1-probe`
   (binaries go to scratch, never into `exercises`)
4. Run on the Pi (route from Step 2.5): `./E1-probe 0x77`. Also note the room temperature and the time, and
   look up the nearest weather station's **station pressure** (not sea-level) at that time.
5. Write `$SAMPLE/README.md` (what E1 is, how to run it) if it doesn't exist.

**Output:** `$SAMPLE/evidence/001-E1-probe.txt` (header: date, Pi model, OS, rid, package version, command) and the
same content as `evidence/004-E1-probe.txt` for the CLI's own record.
**Done when:** chip ID is `0x60` (BMP390) or `0x50` (BMP388), 21 calibration bytes are printed, and the 5 raw
readings are stable (small variation).
**Stop if:** chip ID differs (wrong address, wrong part: some cheap boards are BMP280 = `0x58`) → tell Timothy.

---

### Step 4 — Reference oracle → test vectors (outside the fork)

**Goal:** independent expected values for the compensation, so the tests check the math against something other than our own code.

**Where:** `$SCRATCH/tools/bmp3-oracle/` (outside the clone, outside `exercises`). Below, `tools/` means `$SCRATCH/tools/`.

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
1. `dotnet publish src/devices/Bmp3xx/samples/ -c Release -r <rid> --self-contained -o $SCRATCH/publish/sample`
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

No AI trailers (rule 7 in `00-start-here.md`). **No issue/PR numbers, GitHub URLs or `@mentions` in any commit
message** (they can notify upstream once pushed; the clone's hooks reject them). Show Timothy `git diff --stat` for each before asking.

**Push to the fork (new in v2):** when Timothy says so, `git -C iot push -u origin feature/bmp3xx-binding`
(first time; afterwards `git -C iot push origin feature/bmp3xx-binding`). Settings ask every time. This is a
backup and lets him browse the code on GitHub; it is **not** a PR and notifies nobody. Never force-push: if a
pushed commit must change, add a new commit, or stop and let Timothy decide. Pushing earlier (e.g. after each
green step) is fine whenever he asks.
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
  makes sure `feature/bmp3xx-binding` is pushed to his fork, opens the PR **with "Fixes #<proposal issue>"** as the first line.
- **20 Review** — each comment becomes a numbered step in Stage 8; AI drafts code and replies; Timothy posts.

## 3.4 Stage 3 Discussion Subsection

*(Questions about the plan, and the G1 grant, go here.)*

[Desktop — 2026_10_05] Things Timothy may want to change before granting G1: the device (O1), the class structure
(O2), out-of-range behavior (U4: return false vs. clamp), defaults (O5), and whether Steps 3/13 wait for hardware
or the CLI builds everything first. Also: **when to start**. Steps 0–15 are local and can start now; Step 17 (the
first public action) waits for #2611 to settle unless Timothy decides otherwise.

[Timothy — 2026_10_05_21_40] (recorded by CLI) **G1 approved**, plan v2 as written, no changes to Stage 2. Implementation may start (Step 0 onward).

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

[CLI — 2026_10_05_21_46] **Step 0: plan review against code and datasheets. Verdict: plan holds; no CHANGE REQUEST.**

*Changed:* nothing in the fork. Datasheets + a Python venv (pypdf, for text extraction) in `scratch/new-device-binding/tools/` (outside the fork). Recorded G1 in §3.4 and D5 in `02-decisions.md`.

*Evidence:* `evidence/001-step0-audit.txt` (upstream/main @ `336e4696`; datasheets BST-BMP390-DS002-07 rev 1.7 and BST-BMP388-DS001-07 rev 1.7 with sha256).

*Results:*
- No competing work: no BMP3 folder upstream; PR heads #2601–#2616 add no BMP3 file.
- Every convention Steps 5–11 assume is as described (quotes in the audit file).
- Brief §3: every row ✔ except **measurement time ✘** (the brief has Bosch's driver constants; the BMP390 datasheet §3.9.2 says `234 + (392 + 2^osr_p·2020) + (163 + 2^osr_t·2020)` µs, revised in datasheet rev 1.1 "Changed the T_conv equation"; the **BMP388** datasheet still has `…·2000 … 313 …`). CMD row: soft-reset duration isn't stated; completion is signalled by EVENT `0x10` bit0 `por_detected`.
- Confirmed: forced mode returns to sleep by itself (§3.3.2); burst read needed for consistent data (§3.10.1); reads auto-increment (§5.2.2); calibration table and all 14 scale factors and both formulas match Step 7; BMP388 = BMP390 except chip ID, timing constants, REV_ID, `itf_act_pt`.

*Findings that shape later steps (decided at implementation level per §6; Timothy may veto):*
1. **Timing (Step 9):** each chip uses its own datasheet's formula, selected inside `Bmp3xxBase` from the chip ID. **Why:** the plan says implement from the datasheet, and the two datasheets disagree; picking one would make one chip's XML citation wrong. Rejected: one conservative max-of-both formula (cites neither datasheet exactly); BMP390 formula for both (≤ 0.11 ms off for ×1/×1 on BMP388, harmless because the read path polls, but uncitable). Both round up to 5 ms for ×1/×1.
2. **U5 reset-value check (Step 10):** the data registers reset to `0x800000` for both quantities (Table 25), so the check is datasheet-backed. But a real raw *temperature* near room temperature is also ≈ 8.3–8.4 M, so it can, rarely, be exactly `0x800000`. The check requires **both** raw values to equal `0x800000`. **Why:** that keeps U5 as written while making a false "no data" vanishingly rare, and its effect is a single `false` (next read works). Rejected now: tracking "measured since reset" in the class (the Step 10 fallback). It needs extra state and can't see measurements started by another process, and isn't needed unless E1's raw values show the collision is likely. Unverified until E1.
3. **Mode transitions (Step 9):** §3.3.4 says illegal mode changes are *ignored*; Figure 4's arrows are vector art I couldn't read. `SetPowerMode` will pass through Sleep when moving between Forced and Normal (safe whatever the diagram says). E1 (Step 3) gets one extra check: write normal, then forced directly, and read back `PWR_CTRL`. **Why:** a silently ignored write is the worst failure mode for a driver; the detour costs one register write.
4. **Fake chip writes (Step 8):** I²C multi-byte write is "pairs of register addresses and register data" (§5, §5.2.1), not auto-increment as the Step 8 card says. The fake models pairs. **Why:** "the double models the datasheet".
5. **Reset (Steps 6, 9):** wait `t_startup` 2 ms (Table 2, the only documented figure), then poll EVENT `por_detected` (bounded), then check `ERR.cmd_err`. Adds `Event = 0x10` to `Bmp3xxRegister`. **Why:** uses the chip's own completion signal instead of a guessed delay.
6. **Model attributes (Step 9):** `SetPowerMode` gets `[Property("PowerMode")]` and `GetMeasurementDuration` gets `[Property("MeasurementDuration")]`, as in `Bmx280Base`, not `[Command]` as the card says. **Why:** the card also says "attributes as in `Bmx280Base`"; the sibling is the tie-breaker.
7. **U8 message:** the sibling prints IDs in decimal; I'll keep the plan's hex (`0x60`), which matches how the datasheet writes them. Same exception type.
8. **O5 note (no action):** ×1/×1 at 200 Hz is within the *typical* time (4.82 ms) but not the *maximum* (5.70 ms, Table 23). Defaults are written in sleep, so no conf_err there; whether Normal at 200 Hz/×1/×1 raises conf_err on a real chip is unverified (check in Step 13).
9. **Tests project:** `Ina236.Tests` sets `LangVersion 10`; I won't copy it (collection expressions need C# 12).

*Toolchain:* SDK 10.0.302 inside the repo (global.json roll-forward), .NET 8.0.13 runtime present, Apple clang 17 (Step 4's oracle can be built), Python 3.14.

[CLI — 2026_10_05_21_47] **Step 1: branch + toolchain baseline. Done.**

*Changed:* created `feature/bmp3xx-binding` from `upstream/main` @ `336e4696` (clone was clean; was on `main`). No files changed.

*Deviations:* `git switch -c … upstream/main` made the branch track `upstream/main`; I ran `git branch --unset-upstream`. **Why:** a branch tracking upstream invites a bare `git push`/`git pull` to talk to dotnet/iot. The push URL is disabled anyway, so this is belt and braces. The first `git push -u origin feature/bmp3xx-binding` (Step 14, with Timothy's OK) sets the right tracking.

*Evidence:* `evidence/002-baseline.txt`: `dotnet build src/devices/Ina236/tests/ --no-incremental` → 0 warnings, 0 errors, exit 0 (8 s, packages already cached); `dotnet test … --no-build` → 3/3 passed on net8.0, exit 0.

**Why this step:** it separates "the Mac can't build a binding" from "our binding is broken". From here on, a red build is ours.

[CLI — 2026_10_05_21_51] **Step 4: reference oracle → test vectors. Done (without hardware: V1–V5 wait for E1).**

*Changed:* nothing in the fork. In `$SCRATCH/tools/bmp3-oracle/`: Bosch `BMP3_SensorAPI` @ `db4cf8e` (as expected), `oracle.c` (fake-bus harness), `formulas.py` (our float64 datasheet implementation + vector generator).

*Evidence:* `evidence/005-oracle-vectors.txt` (calibration sets as 21-byte hex + scaled coefficients, the vector table, provenance).

*Result:* vectors **V6–V15** (plan asked for V6–V12; V13–V15 added, see below). The two oracles agree on every in-range vector to **max |ΔT| = 5×10⁻¹⁰ °C, max |ΔP| = 8×10⁻⁵ Pa**, far inside the stop threshold (0.01 °C / 1 Pa). So the datasheet formulas as I read them and Bosch's code compute the same thing.

*Deviations:*
1. **Calibration sets.** No published raw-byte example in Bosch's repo. Sets A and B are NVM coefficients a web-search summary attributes to two real BMP388s on Bosch's forum. **The pages now 404, so provenance is unverified**, and I say so in the evidence. Set C is set A with the signed fields sign-flipped (synthetic). **Why:** a calibration set only has to be realistic in size and sign for an oracle; both oracles get identical bytes, so a wrong source can't make them agree falsely. Rejected: inventing coefficients from scratch (risk of non-physical combinations that hide scale-factor bugs). Real bytes come with E1 (V1–V5).
2. **Extra vectors:** V13–V14 (set C, so negative P3/P4/P9/P10 and nonzero P4 are exercised; set A/B have P4 = 0, which would hide a P4 bug) and **V15** = the reset value `0x800000`/`0x800000`. **Why:** cheap, and V15 feeds the U5 test.
3. **Harness fix:** `bmp3_init` returns `BMP3_E_NULL_PTR` when `intf_ptr` is NULL even though the bus ignores it; the harness passes a dummy pointer.

*Findings:*
- **U5, quantified (unverified on real hardware):** with set A, raw temperature `0x800000` = **23.68 °C**, a normal room value. So "raw temperature equals the reset value" alone would misfire. Raw pressure `0x800000` = 799 hPa (≈ 2 km altitude), so the "both equal" rule from my Step 0 entry needs both conditions at once. That supports the Step 0 decision.
- **Bosch's out-of-range behavior** (for the U4 discussion with maintainers): too cold → T clamped to −40 °C **and P reported as 0** (warning 3); too high → P clamped to 125000 Pa (warning 6). Our U4 returns `null`/`false` instead. Unclamped float64 values are recorded for our range tests.
- **O4 confirmed:** Bosch's float path uses a float-returning power helper; that's the ~10⁻⁴ Pa gap. Implication for Step 7's tolerance: our C# `double` should match **oracle 2** to ~1e-9 and **Bosch** to ~1e-4 Pa. I'll set the tolerance in Step 7 from the measured C# numbers, as the plan says.

**Why two oracles:** Bosch's code is the reference people trust, but it's float, has clamps, and could share a misreading with us only if we copied it (we don't). The Python script is our own reading of the datasheet. Their agreement means the *datasheet reading* is right, so Step 7's C# can be tested against numbers that didn't come from C#.

[CLI — 2026_10_05_22_07] **Step 5: scaffold `src/devices/Bmp3xx/`. Done.**

*Changed (uncommitted, branch `feature/bmp3xx-binding`):* 19 new files, all under `src/devices/Bmp3xx/`: `Bmp3xx.csproj`, `Bmp3xx.sln`, `Bmp3xxBase.cs` (protected ctor and `Dispose(bool)` throw `NotImplementedException`; `Dispose()` done), `Bmp390.cs`/`Bmp388.cs` (sealed; `private const byte DeviceId` = `0x60`/`0x50`, as `Bmp280` does), `Bmp3xxReadResult.cs` (complete: same shape as `Bmp280ReadResult`), `Bmp3xxCalibrationData.cs` (internal, empty), `Bmp3xxRegister.cs` + six public enums (empty bodies, Step 6 fills), `README.md` (title line), `category.txt`, `samples/Bmp3xx.Samples.csproj` + `Program.cs` ("Hello Bmp3xx!"), `tests/Bmp3xx.Tests.csproj`.

*Evidence:* `evidence/006-scaffold-build.txt`: tests build and samples build `--no-incremental`: **0 warnings, 0 errors**, exit 0 each; `dotnet test` finds the assembly ("No test is available": expected, none written yet); xunit/Moq/Shouldly present in the test output (auto-referenced by `eng/Versions.external.props`); `git status` = only new files under `src/devices/Bmp3xx/`.

*Deviations / implementation choices:*
1. **Block-scoped namespaces** (`namespace X { }`). **Why:** 1,638 `.cs` files in `src/devices` use them against 18 file-scoped (Ina236 mixes both); the sibling `Bmxx80` uses block. Rejected: file-scoped (shorter, but the minority style; a reviewer might ask to change it).
2. **UTF-8 with BOM** for `.cs`, csproj, sln, README (as Ina236's files are). **Why:** `.editorconfig` `[*.cs] charset = utf-8-bom` (StyleCop SA1412). `category.txt` is plain ASCII, as in Ina236.
3. **`.sln`:** created with `dotnet new sln --format sln` (SDK 10 would make `.slnx`). `dotnet sln add` auto-added `System.Device.Gpio` and `System.Device.Model`; I removed them. **Why:** only 7 of 129 binding solutions list Gpio and 1 lists Model; Ina236 lists binding + samples + tests + `Common`, as plan Step 5 says.
4. **Test csproj** uses `$(DefaultTestTfms)` (Ina236 uses `$(DefaultSampleTfms)`; both are net8.0) and **no `LangVersion 10`**. **Why:** the property meant for tests exists in `src/devices/Directory.Build.props`; LangVersion 10 would forbid C# 12 collection expressions that `I2cSimulatedDeviceBase` itself uses.
5. **Samples csproj** has no Arduino reference (Ina236's sample targets an Arduino; ours targets the Pi's I2C bus directly).
6. `Bmp3xxReadResult` written in full now (it's a 2-property data holder identical in shape to `Bmp280ReadResult`; nothing to defer).

**Why this step on its own:** it proves the build wiring (project discovery, analyzers, XML-doc enforcement, test packages) before any logic exists, so the first red build in Step 7 can only be about the code.

[CLI — 2026_10_05_22_19] **Step 6: register map + enums. Done.**

*Changed (uncommitted):* `Bmp3xxRegister.cs` (11 registers, incl. `Event = 0x10` per Step 0 finding 5), `Bmp3xxPowerMode.cs`, `Bmp3xxOversampling.cs` (docs give the typical pressure **and** temperature resolution per step, DS390 tables 6/7), `Bmp3xxFilterCoefficient.cs`, `Bmp3xxOutputDataRate.cs` (18 values), `Bmp3xxStatus.cs`, `Bmp3xxErrors.cs`, and 15 `private const` bit fields in `Bmp3xxBase.cs` (PWR_CTRL, OSR, ODR, CONFIG, EVENT `por_detected`, CMD soft reset), each group with a datasheet section/table comment.

*Evidence:* `evidence/007-calibration-red.txt` part 1: tests + samples build `--no-incremental`, 0 warnings, 0 errors; `git status` only `src/devices/Bmp3xx/`.

*Decision (Timothy, D6):* output data rate values are named by period in milliseconds, `Period5Milliseconds` … `Period655360Milliseconds`, replacing O7's proposed `Hz200 … Hz12_5`. Asked with four options (period ms / sibling `Ms…` / O7 frequency / ms-then-seconds). Values generated by a script from `5 ms × 2^n` and checked against DS390 table 45.

*Deviations / implementation choices:*
1. **Every public enum value has an XML doc with its meaning**, not just a name. **Why:** CS1591 requires docs; the README asks for units and ranges; the docs are where the datasheet tables live for users.
2. **Bit constants include the shifts the plan named plus masks** (`PowerModeMask`, `PressureOversamplingMask`, …) and `PressureOversamplingShift = 0`. **Why:** a read-modify-write needs mask + shift; an explicit zero shift keeps the pressure and temperature code symmetric. Unused `private const`s give no warning in this repo's build (checked).
3. **`Bmp3xxErrors.Configuration` doc says "only detected in Normal mode".** **Why:** DS390 table 28 says so (DS388 doesn't; noted in Step 0). Users need this to understand O6's check.

*Process note:* my file-writing tool drops the UTF-8 BOM, and StyleCop SA1412 failed the build with **6 errors** on the first try. I re-added BOMs with a script. **Why it matters:** the repo treats encoding as a build error, so it can't slip into a PR; the check works as intended.

[CLI — 2026_10_05_22_19] **Finding: Timothy committed and pushed the Step 5 scaffold himself.** `dd1c7694 Creating Bmp3xx scaffold` (2026-10-05 22:11, 19 files = exactly the Step 5 state), on `origin/feature/bmp3xx-binding`. Step 6's changes are uncommitted on top of it.

*Effect on O8 (commit structure):* O8 planned commit 1 = "skeleton + registers/enums". With the skeleton already pushed, that can't be one commit without rewriting pushed history (force-push is denied; rewrites are Timothy's). Step 6 needs its own commit, giving **5 commits instead of 4**, or Timothy rewrites later. Upstream will probably squash anyway, so this only affects review readability. Also: the pushed subject "Creating Bmp3xx scaffold" isn't imperative (00 §3 rule 7 asks for imperative); harmless on a squash-merge. Not changed by me.

[CLI — 2026_10_05_22_26] **Step 6 committed; Step 7: calibration + compensation (tests first). Done.**

*Changed:* commit `93af1acb Add Bmp3xx register map and settings enums` (Timothy's OK; local only, not pushed). Uncommitted: `Bmp3xxCalibrationData.cs` (implementation), `tests/Bmp3xxCalibrationDataTests.cs` (new, 22 test cases).

*Evidence:* `evidence/007-calibration-red.txt` part 2: build 0 warnings; **22/22 fail**, all by `NotImplementedException` (the 3 `Parse_RejectsWrongLength` cases fail because they got `NotImplementedException` instead of `ArgumentException`; also the right reason). `evidence/008-calibration-green.txt`: build 0 warnings; **22/22 pass**; tolerance measurement table.

*Tests (what each proves / doesn't):*
- `Parse_ScalesEachCoefficient`: every field's offset, width, signedness and scale factor, using raw values chosen to break the usual mistakes (`T1 = 0x8001` and `P5 = 0xFFFF` go negative if read signed; `P1 = −32768`; mixed signs). Hex built by hand and cross-checked against `formulas.py`'s encoder. Expected values are written as `raw / Math.Pow(2, n)` in the test, so the test doesn't share the implementation's `ScaleB` expression. Doesn't prove: the formulas.
- `Parse_RejectsWrongLength` (0, 20, 22 bytes).
- `CompensateTemperature_MatchesReference` / `CompensatePressure_MatchesReference`: 8 in-range vectors each (V6–V8, V11–V15) across the three calibration sets, against **Bosch's** values (the external reference). Pressure is fed Bosch's temperature, so the pressure test doesn't depend on our temperature code. Doesn't prove: real-chip calibration (V1–V5 wait for E1).
- `*_DoesNotClamp` (V9, V10): the formulas return the unclamped value (Bosch clamps), because the range check is the caller's job (U4, Step 10). Expected from `formulas.py`.

*Tolerance (Step 7.3):* measured with a throwaway reflection app in `$SCRATCH/tools/measure-tolerance` (outside the fork). |C# − Bosch| max **5×10⁻¹⁰ °C, 8.03×10⁻⁵ Pa** (Bosch's float arithmetic); |C# − python| ≤ 5×10⁻¹⁰ (that's the 9 printed decimals). Chosen: **10⁻⁶ °C** (~2000× above measured, 150× below the 0.00015 °C finest resolution) and **10⁻³ Pa** (~12× above measured, 16× below the 0.016 Pa resolution, 20× below 0.02 Pa RMS noise). **Why these:** any real formula bug (sign, scale, term order) misses by ≥ whole Pa; rounding never trips them. Rejected: 0.01 Pa (the plan's example; fine too, but 10⁻³ Pa still has 12× headroom and is stricter); exact equality (Bosch's float makes it impossible).

*Implementation choices:*
1. **`Math.ScaleB(raw, n)`** for all 14 scale factors. **Why:** documented as an exact power-of-two scaling (it only changes the exponent); `Math.Pow(2, n)` is exact in practice but not specified to be; and the tests use `Math.Pow`, so the two sides don't share an expression. Rejected: literal constants like `1073741824.0` (unreadable; easy to mistype).
2. **Private constructor + static `Parse`**, get-only properties. **Why:** `Parse` validates the length before anything is read; an instance can't exist half-filled.
3. **Own names** in the formulas (`difference`, `offset`, `sensitivity`, `nonLinearity`), matching the plan's description, not the appendix's temporaries (clean room). Clean-room grep over `src/devices/Bmp3xx` (incl. `partial_data`, `t_lin`, `uncomp_`) → no hits, after I reworded one test comment that named "Bosch Sensortec" as the source of the expected numbers ("the manufacturer's open-source reference driver"). The README will name Bosch, as the plan allows.
4. Namespace `Iot.Device.Bmp3xx.Tests` (112 test files follow `Iot.Device.<X>.Tests`).

[CLI — 2026_10_05_22_33] **Step 7 committed; Step 8: simulated chip. Done.**

*Changed:* commit `9e1ab226 Add BMP3xx calibration parsing and compensation with tests` (Timothy's OK; local only). Uncommitted: `tests/SimulatedBmp3xx.cs` (new), `tests/SimulatedBmp3xxTests.cs` (new, 5 tests). Per O8 these belong in commit 3 together with the device class (Steps 9–10), so I'm not proposing a commit yet.

*Evidence:* `evidence/009-device-config-red.txt` part 1: 27/27 pass (22 calibration + 5 simulation), 0 warnings; **mutation check**: three deliberate breaks of the fake (forced mode doesn't return to sleep; ERR not cleared on read; writes as auto-increment instead of pairs), each turning exactly its matching test red (1 failed / 26 passed), file restored (`cmp` identical) → 27/27 again.

*What the fake models (each with its datasheet citation in a code comment):* reset values (table 25); burst read with auto-increment (§5.2.2); writes as (register, value) pairs, throwing on an odd-length write so a binding bug can't hide (§5, §5.2.1); soft reset `0xB6` restores reset values, keeps calibration (NVM), sets `por_detected` (table 48, §3.2); forced mode measures once and returns to sleep (§3.3.2); normal mode measures, and again whenever a test changes the scripted raw values; disabled sensor → its data unchanged; ERR cmd_err/conf_err and EVENT por_detected clear on read (tables 28, 33); reading a pressure/temperature data register clears its data-ready bit (table 29); read-only registers ignore writes.
Test hooks: `RawPressure`, `RawTemperature`, `NeverBecomesReady`, `ConfigurationErrorOnNormalMode`, `SoftResetFails`, `ResetCount`, `WriteLog` (every write, including ignored ones), `IsDisposed`, `GetRegister` (peek without read side effects).

*Unverified assumptions (also marked in code comments; E1 can check the first two):*
1. **`cmd_rdy` reads 1 when idle**, although table 25 lists STATUS reset = `0x00`. **Why:** table 29 defines it as "ready to accept a new command", and Bosch's driver refuses to send a command unless it's set, yet works on real chips. If the real chip reads 0 at idle, our Reset's "wait for cmd_rdy" (Step 9) must not depend on it.
2. **Every mode transition is accepted** (Figure 4 unreadable). The binding's detour through Sleep (Step 0 finding 3) will be asserted from the write log, not from the fake rejecting anything.
3. A disabled sensor leaves its data registers unchanged (not needed in v1: both sensors always enabled, O5).

*Deviations:*
1. **5 self-tests instead of "2–3".** **Why:** each covers one modelled behavior a binding test will lean on (burst read, pairs, reset, forced mode, clear-on-read); cheap and fast (< 10 ms).
2. **Mutation check** (not in the plan). **Why:** the self-tests passed on the first run, so they had never been seen failing; a test that can't fail proves nothing. Three targeted breaks show each test catches the thing it names.
3. **The fake's I2C address is the literal `0x77`** for now; Step 9 adds `Bmp3xxBase.DefaultI2cAddress` and the fake will use it.
4. Removed one no-op line (`chip.ReadByte()`) from a self-test before capturing evidence.

[CLI — 2026_10_05_22_41] **Step 9: device class part 1 (construction + configuration), tests first. Done.**

*Changed (uncommitted; for O8 commit 3 with Steps 8 and 10):* `Bmp3xxBase.cs` (implementation), `tests/Bmp3xxBaseTests.cs` (new: 20 test methods, 23 cases), `tests/SimulatedBmp3xx.cs` (now uses `Bmp3xxBase.DefaultI2cAddress`).

*Evidence:* `evidence/009-device-config-red.txt` part 2: build 0 warnings; **23 new cases fail**, every one by `NotImplementedException` (the 3 expecting `ArgumentNullException`/`IOException` got it instead); the 27 earlier tests still pass. `evidence/010-device-config-green.txt`: **50/50 pass**, 0 warnings, every test name listed; plus a numeric-literal scan.

*What was built, with the why:*
- **Constructor** (plan sequence): null check (`ArgumentNullException`) → chip ID (`IOException`, U8, message in hex, e.g. "…id 0x60. Found one with id 0x58.") → `Reset()` (which writes the O5 defaults) → one 21-byte burst read → `Bmp3xxCalibrationData.Parse`. Calibration is exposed as an `internal` property for Step 10 and tests.
- **Settings** (`PressureSampling`, `TemperatureSampling`, `FilterCoefficient`, `OutputDataRate`): the setter validates the enum (`ArgumentOutOfRangeException`), does a read-modify-write of only its own bits, then updates a **cached** field; the getter returns the cache (no bus read). **Why cache:** `Reset()` must re-apply the settings so the properties don't lie after a reset (plan Step 9), and the sibling `Bmxx80Base` caches too. Rejected: reading the register in every getter (a bus transaction per property read, and after a reset it would show chip defaults that disagree with what the user set).
- **`SetPowerMode`**: read PWR_CTRL once; if switching between two different non-sleep modes, write Sleep first (Step 0 finding 3); write the mode keeping press_en/temp_en; after Normal, read ERR and on conf_err write Sleep and throw `InvalidOperationException` naming the minimum period (O6).
- **`Reset()`**: wait (≤ 10 ms) for `cmd_rdy` → read EVENT to clear a stale `por_detected` → write `0xB6` → sleep 2 ms (t_startup, table 2) → `cmd_err` → `IOException` → wait (≤ 10 ms) for `por_detected` (§3.2) → re-apply settings. **Why clear EVENT first:** `por_detected` is already 1 after power-on (never read), so without clearing it, the "reset finished" check would pass before the reset even started.
- **`GetMeasurementDuration()`**: each chip's own datasheet formula (Step 0 finding 1), `Math.Ceiling` to whole ms. BMP390 ×1/×1 = 4829 µs → 5; ×32/×2 = 69469 µs → 70 (table 23 says 69.46 typ); BMP388 ×32/×2 = 68939 µs → 69.
- **Bus seam** (U3): `ReadRegister`, `ReadRegisters`, `WriteRegister`, all through one `Device` property that does the disposed check. Reads are one `WriteRead` transaction.
- **Dispose**: disposes the `I2cDevice` (U7); later calls throw `ObjectDisposedException`; a second `Dispose()` is harmless.

*Deviations / implementation choices:*
1. **Four tests beyond the plan's 16:** `SetPowerMode_FromNormalToForced_GoesThroughSleep` (Step 0 finding 3, asserted on the write log), `Reset_CommandError_ThrowsIOException`, `Settings_InvalidValue_ThrowsArgumentOutOfRange`, `ReadStatus_ReportsFlags`; `Constructor_WrongChipId` also covers BMP280's `0x58` (common wrong part). `Constructor_ResetsOnce_AndWritesDefaults` asserts "first write is the reset command" + final register values, **not** the exact write order. **Why:** behavior, not my implementation's order, is what matters.
2. **Model attributes:** `[Interface]` only on `Bmp3xxBase` (the README of System.Device.Model: derived classes that add nothing get no extra `[Interface]`); `[Property]` on the four settings; `[Property("PowerMode")]` on read+set (merged into one model property); `[Property("MeasurementDuration")]`; `[Telemetry("Status")]`; `[Command]` on `Reset`. **`ReadErrors()` has no attribute.** **Why:** reading clears the flags (side effect), and model properties/telemetry should be safe to read.
3. **`ObjectDisposedException.ThrowIf(_i2cDevice is null, this)`** instead of Ina236's `typeof(I2cDevice)`. **Why:** the disposed object is the sensor, so the exception names the right type.
4. **Magic numbers:** my first green version had the four timing constants inline; the scan in `010` caught them, and they're now named consts next to the formula. Remaining literals: `0b00`/`0b11` (PWR_CTRL mode field values, under the table-42 citation) and `1000.0` (µs per ms).
5. **Refactor:** `SetPowerMode` decodes the mode from the PWR_CTRL value it already read (one bus read fewer); `ReadPowerMode` uses the same decoder.

*Unverified (hardware):* (a) Reset depends on `cmd_rdy` reading 1 when idle (Step 8 assumption 1); if a real chip reads 0, the constructor throws "not ready to accept a command", which E1/Step 13 would show immediately. (b) O6 reads ERR right after writing Normal; whether a real chip has already evaluated the configuration by then isn't stated in the datasheet. (c) On a wrong chip ID the constructor throws without disposing the `I2cDevice`, same as `Bmxx80Base`; the caller still owns it in that case.

[CLI — 2026_10_05_22_47] **Step 10: device class part 2 (reading path), tests first. Done. One open question on U4 (below).**

*Changed (uncommitted):* `Bmp3xxBase.cs` (+ `Read`, `ReadAsync`, `TryReadTemperature`, `TryReadPressure`, 2× `TryReadAltitude`, private helpers), `tests/Bmp3xxReadTests.cs` (new, 15 tests).

*Evidence:* `evidence/011-device-read-red.txt`: build 0 warnings; **15 new tests fail, all by `NotImplementedException`**; the 50 earlier ones pass. `evidence/012-device-read-green.txt`: **65/65 pass**, 0 warnings; whole suite 0.8 s; the two timeout tests take ~25 ms each (no multi-second sleeps); numeric-literal scan (remaining literals explained there).

*What was built, with the why:*
- **`Read()` / `ReadAsync()`**: if not in Normal mode, `SetPowerMode(Forced)`, sleep `GetMeasurementDuration()`, then poll both data-ready flags every 1 ms until `duration + 10 ms` more have passed, so the **total U6 timeout is 2 × duration + 10 ms**. On timeout → both values `null`, no exception (U6). In Normal mode: no write at all, just read the latest data. Sync and async share `StartForcedMeasurementIfNeeded`, `IsDataReady` and `ReadResult`; only the waiting differs (plan: "so the two can't drift apart"). `ConfigureAwait(false)` in the async version. **Why:** library code shouldn't capture a UI synchronization context.
- **`ReadResult`**: one 6-byte burst (DS390 §3.10.1, data shadowing) → compensate temperature, then pressure with that temperature → **each value range-checked on its own** (U4): −40…85 °C, 30 000…125 000 Pa (table 2) → `null` when outside.
- **U5 reset-value check**: both raw fields == `0x800000` → no data. Applied in `TryRead*`, `TryReadAltitude` and a Normal-mode `Read()`; **not** after our own forced measurement (the data is known fresh there, so a genuine `0x800000`/`0x800000` reading isn't thrown away). Test `TryReadTemperature_OnlyTemperatureAtResetValue_ReturnsTrue` pins the "both" rule (raw T `0x800000` = 23.68 °C with set A is a real reading).
- **`TryReadAltitude`**: pressure and temperature from **one** burst read, then `WeatherHelper.CalculateAltitude(pressure, seaLevel, temperature)`; the no-argument overload uses `WeatherHelper.MeanSeaLevelPressure`, as `Bmx280Base` does. **Why one read:** the sibling reads pressure and temperature in two transactions, which in Normal mode can mix two measurements. Same result otherwise.
- **Attributes:** `[Telemetry("Temperature")]`, `[Telemetry("Pressure")]` as in the sibling; `TryReadAltitude` has none, as in the sibling.

*Tests (15):* forced trigger + reference values (V7, full stack: fake chip → I2C → calibration → compensation), Normal mode writes nothing, timeout (sync and async; returns nulls, elapsed asserted 5–1000 ms), out-of-range temperature (V9) and pressure (V10), `ReadAsync` = `Read`, `TryRead*` after reset → false, single field at reset value → true, `TryReadTemperature` writes nothing, `TryReadPressure` after a measurement / out of range, altitude at mean sea-level pressure ≈ 0 m (±1 cm), altitude uses the given sea-level pressure (equals `WeatherHelper`'s result and > 600 m), altitude after reset → false.

**Open question for Timothy (U4, behavior: not decided by me):** U4 says out-of-range → `null` "for that quantity". The **pressure formula uses the temperature**. When only the temperature is out of range (V9: −45 °C), should the pressure still be reported?
- **(a) As built (literal U4):** temperature `null`; pressure range-checked on its own (V9 gives 999.99998 hPa → reported). For: the pressure is a valid number in range; the sensor may be just below −40 °C. Against: it was computed with a temperature we just called untrustworthy.
- **(b)** Pressure `null` too whenever the temperature is out of range. For: consistent with the "outside the range = bad data" reasoning; Bosch's driver also gives up on pressure then (reports 0). Against: throws away possibly-good data.
- Switching is a one-line change in `ReadResult` plus one test expectation (`Read_OutOfRangeTemperature_ReturnsNullTemperature`). This could also go to maintainers with question 3 of the proposal issue (Stage 4).

[CLI — 2026_10_05_22_52] **Commit 3 done; Step 11: sample, README, category. Done.**

*Changed:* commit `0c17f5b3 Add Bmp390/Bmp388 device classes with simulated-device tests` (Steps 8–10; Timothy's OK; local only). Uncommitted: `samples/Program.cs` (real sample), `README.md` (full). `category.txt` unchanged from Step 5 (barometer / altimeter / thermometer).

*Evidence:* `evidence/013-sample-build.txt`: samples build `--no-incremental` 0 warnings / 0 errors; tests still 65/65; README's only relative link (`samples/Program.cs`) exists; the three categories are listed in `tools/device-listing/Program.cs` (no new category description needed); only the two files changed. **Not run:** the sample itself (needs `/dev/i2c-1` and a sensor → Step 13).

*Sample (plan outline):* bus 1 at `DefaultI2cAddress` (comment for 0x76 and for `Bmp388`) → ×8 / ×1 / filter 3 → prints `GetMeasurementDuration()` → **10** forced `Read()`s 1 s apart (°C, hPa, standard-atmosphere altitude) → normal mode at 25 Hz (`Period40Milliseconds`; ×8/×1 takes ~19 ms) → 5 × `TryReadPressure` → back to sleep. Nulls print "not available".

*README:* title; two-line description (BMP390 vs BMP388; Adafruit BMP390 **product 4816**, verified via a Digi-Key listing; Adafruit BMP388 named without a number because I couldn't verify it); `## Documentation` with both datasheets first; `## Usage` (shortened sample); `## Wiring` (pin table from Step 2, plus the DS390 §5.1 warning that pulling CS low once locks the chip into SPI until power-off); `## Binding Notes` (implemented / not implemented / four behaviors a user can trip over: TryRead* never trigger, out-of-range → null/false (U4), timeout → nulls (U6), Normal-mode configuration error (O6)).

*Deviations:*
1. **The sample ends by itself** (10 forced + 5 normal readings), instead of the sibling's endless loop. **Why:** Step 13 runs it on the Pi for a recorded comparison; a finite run gives a complete, pasteable output. Rejected: endless loop (needs Ctrl+C; output cut at a random point).
2. **No fritzing diagram.** `src/devices/README.md` asks for one; I can't draw it, and Ina236 (newest binding) has none. Options for Timothy: (a) leave the pin table only; (b) add a photo of his own wiring after Step 13; (c) draw one in Fritzing. Not blocking.
3. `samples/Program.cs` keeps the Ina236-style file name (the sibling uses `Bmp280.sample.cs`). **Why:** the csproj already includes it by default; both styles exist in the repo.
4. The README's usage snippet uses `Bmp3xxBase.DefaultI2cAddress` (the constant lives on the base class; `Bmp390.DefaultI2cAddress` also compiles via inheritance, but StyleCop/IDE may flag access through a derived type).
