# 002: Choosing the device

> Date: 2026-10-04 · Upstream checked: dotnet/iot `main` @ `95384e7` · Builds on: [001](001-should-i-add-a-sensor-binding.md)
> Raw findings: [`evidence/001-…`](evidence/001-upstream-new-binding-survey-2026-10-04.txt) §D–F

---

## 0. The pick

**Recommended: Bosch BMP390** (and its older sibling BMP388, same driver), a barometric pressure and temperature
sensor. **Runner-up: INA226**, added by generalizing the existing `Ina236` binding. **Small fallback: VEML7700**
(ambient light).

---

## 1. What makes a good first binding

| Criterion | Why | Weight |
|---|---|---|
| **Not already in dotnet/iot, and nobody's PR is open for it** | Otherwise it's duplicate work | Gate |
| **People actually use it** (sold by Adafruit/SparkFun, common in Pi projects) | Maintainers carry it forever; popularity is what justifies that | Gate |
| **Cheap and easy to wire to your Pi** (I2C breakout, ≤ ~$15) | Real-hardware evidence is your edge over AI-only PRs | Gate |
| **Unit-testable without hardware**, ideally with known-good reference numbers | CI has no sensors; good tests are what make reviewers trust you | High |
| **Teaches software design, not just register poking** | Section 3 of [001](001-should-i-add-a-sensor-binding.md): API design and testing are the growth | High |
| **Scope can be cut** into a small v1 and later follow-ups | Small PRs merge; big ones stall | High |
| **Has a sibling binding to stay consistent with** | Gives reviewers and you a pattern to follow | Medium |

---

## 2. The candidates

All of these were checked against `src/devices` on `main` @ `95384e7` (none exist) and against every PR
#2300–#2616 (none add them). Prices are approximate retail; check current listings.

| Device | What it is | Why it's interesting | Watch out for | Fit |
|---|---|---|---|---|
| **BMP390 / BMP388** (Bosch) | Pressure + temperature + altitude | Very popular successor to BMP280. **21 bytes of factory calibration** turned into 14 scaled coefficients and a compensation formula: great unit-test material, and Bosch's open-source reference driver (BSD-3) gives you values to check against. I2C *and* SPI. Optional FIFO and data-ready interrupt for later PRs. Sibling: `Bmxx80` | Don't copy Bosch's C code; implement from the datasheet and use theirs only to cross-check numbers. Decide whether it joins `Bmxx80` or gets its own folder (§3) | ⭐ **Best** |
| **INA226** (TI) | Current / voltage / power monitor | Closest to your professional data path (power telemetry). INA226's register map is very close to INA236's, so the natural design is **generalizing `Ina236` to cover both**. That's an API design discussion with the maintainer who wrote Ina236 (pgrawehr considered a common interface on #2459) | Less a "new binding" than a refactor of a maintainer's fresh code: more negotiation, less of your own design. Someone (Kash0321) is fixing `Ina219` right now (#2612). Register similarity is **unverified**: compare datasheets first | Good alternative |
| **VEML7700** (Vishay) | Ambient light (lux) | Small and popular. Its interesting part is software: an **auto-ranging algorithm** (pick gain and integration time) and a non-linearity correction, both pure logic you can unit test | Thin on architecture; a smaller story | Good warm-up |
| **TMP117** (TI) | High-accuracy temperature | Tiny and clean | Almost no design content; ~a weekend | Too small on its own |
| **SCD30** (Sensirion) | CO₂ + temperature + humidity | Command-based protocol with CRC-8 (unlike register-based chips) | Uses long I2C clock stretching, which the Raspberry Pi's I2C hardware is known to handle badly; ~$60 | Not first |
| **SPS30 / PMSA003I** | Particulate matter | Interesting framing and CRCs | ~$45 (SPS30); niche-ish | Not first |
| **Port from nanoFramework** (e.g. HDC1080, MAX1704x) | Various | Lowest risk: the .NET nanoFramework project (also .NET Foundation, also Ellerbach) already has drivers, and a port has precedent (AM2320, #1998) | You'd learn the least: the design is already done | Fallback only |

**Avoid for now:** touch sensors (a CAP1208 PR opened 2026-10-03), anything that needs special hardware you don't own,
and big IMUs (ICM-20948, LSM6DSOX): too much surface for a first binding.

---

## 3. Why BMP390 wins

| Criterion | BMP390 |
|---|---|
| Missing upstream | ✅ No BMP3xx folder; no PR adds one |
| Popular | ✅ Adafruit #4816 (STEMMA QT), SparkFun and generic modules; the common upgrade from BMP280 |
| Cheap, easy to wire | ✅ ~$11 for the Adafruit board; generic BMP388 boards are cheaper. 4 wires to the Pi's I2C pins |
| Testable without hardware | ✅ **Best of the list.** The compensation math is pure functions over the calibration bytes, and Bosch's reference implementation gives expected outputs. A simulated chip on `I2cSimulatedDeviceBase` covers the register traffic |
| Teaches design | ✅ The design questions below are real ones a reviewer will ask |
| Scope can be cut | ✅ v1 = temperature, pressure, altitude, configuration. Later PRs: SPI, FIFO, data-ready interrupt |
| Sibling to follow | ✅ `Bmp280` / `Bme280` (`Bmx280Base`): same vendor, same kind of API |

### The design questions you'd have to answer (this is where the learning is)

| Question | Options | What I'd propose (to discuss in the issue) |
|---|---|---|
| **New folder or join `Bmxx80`?** | (a) new `Bmp3xx/` folder; (b) add `Bmp390` under `Bmxx80` | **(a) a new folder.** `Bmxx80Base`'s constructor reads the chip ID from register `0xD0` and resets through `0xE0`; on BMP3xx those are `0x00` and `0x7E`, and the calibration layout differs too. It also holds an `I2cDevice` field directly. Same *API shape*, different *implementation* |
| **Match `Bmp280`'s public API?** | Same method names (`TryReadTemperature`, `TryReadPressure`, `TryReadAltitude(seaLevelPressure)`, `SetPowerMode`, `Reset`) or a fresh design | **Match it.** A user switching from BMP280 to BMP390 should change one line. Consistency is a strong argument in review |
| **One class or two?** | `Bmp390` only; or `Bmp3xx` base + `Bmp388` + `Bmp390` | Base + two thin classes that differ only in chip ID (0x50 vs 0x60). Supports both chips for almost no extra code |
| **I2C only, or SPI too?** | I2C only (like `Bmxx80`); or a transport adapter (like `Mcp23xxx`'s `I2cAdapter`/`SpiAdapter`) | **I2C in v1**, structured so SPI can be added later without breaking the API. Mention SPI as a follow-up |
| **Floating-point or integer compensation?** | Bosch provides both | `double`, per the conventions ("Use `double` when you need to return any floating point value") |
| **Blocking read in forced mode** | Poll the status register until data-ready, or wait the computed measurement time | Wait the computed time, then check the status bit, **with a timeout** (the LPS22HB review asked for exactly this) |

### The runner-up, in one line

If, after reading both, you're more excited by power monitoring, pick **INA226**, but **ask first** on an issue
whether pgrawehr would rather extend `Ina236` or have a separate class, because it changes his code.

---

## 4. Hardware you'd need

| Item | Have it? | Notes |
|---|---|---|
| Raspberry Pi with I2C enabled | ✅ (model to confirm) | `sudo raspi-config` → Interface Options → I2C; then `i2cdetect -y 1` |
| BMP390 breakout (Adafruit #4816, ~$11) or a BMP388 board | ❌ buy | The Adafruit board's default address should be 0x77 (0x76 with SDO low); check the board's docs |
| STEMMA QT / Qwiic → female jumper cable, or headers to solder | ❌ buy | ~$1–2 |
| USB logic analyzer (8-channel clone + PulseView) | optional | Not required, but a bus capture is great evidence when a reviewer asks "did you test it?" |

## Sources

- [Adafruit BMP390 (#4816)](https://www.adafruit.com/product/4816) · [Bosch BMP3 SensorAPI (reference driver, BSD-3)](https://github.com/boschsensortec/BMP3_SensorAPI) @ `db4cf8e`
- dotnet/iot: [`Bmxx80`](https://github.com/dotnet/iot/tree/main/src/devices/Bmxx80) · [`Ina236`](https://github.com/dotnet/iot/tree/main/src/devices/Ina236) · [`Mcp23xxx`](https://github.com/dotnet/iot/tree/main/src/devices/Mcp23xxx) · [PR #2459](https://github.com/dotnet/iot/pull/2459) · [PR #2616](https://github.com/dotnet/iot/pull/2616) · [AM2320 port #1998](https://github.com/dotnet/iot/pull/1998)
- [nanoFramework.IoT.Device](https://github.com/nanoframework/nanoFramework.IoT.Device) (develop branch, cloned 2026-10-04)
