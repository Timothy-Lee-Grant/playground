# PR description draft (v1, 2026-10-18, desktop)

> For dotnet/iot, from `Timothy-Lee-Grant:feature/bmp3xx-binding` @ `2950b2d4` into `dotnet:main`.
> **Rewrite it in your own words before posting.** Everything below is checked against the code and the evidence
> files as of 2026-10-18. Delete the video line if you haven't published it.

**Title:** `New binding: Bmp3xx (Bosch BMP390 / BMP388 pressure and temperature sensor)`

---

Adds a binding for the Bosch **BMP390** and **BMP388** barometric pressure and temperature sensors, the successors of the BMP280. Both chips share the same registers and differ only in their chip ID, so they share one base class (`Bmp3xxBase`) with two small classes, `Bmp390` and `Bmp388`.

### What's included

- New folder `src/devices/Bmp3xx` (nothing outside it changes): binding, sample, README, `category.txt`, tests.
- I2C; temperature, pressure and altitude via `Read()` / `ReadAsync()` and `TryReadTemperature` / `TryReadPressure` / `TryReadAltitude`, returning UnitsNet types.
- Pressure and temperature oversampling, IIR filter coefficient, output data rate, power mode (sleep / forced / normal), status and error flags, soft reset.
- The public API mirrors `Bmp280` / `Bmx280Base` (`Read`, `TryRead*`, `SetPowerMode`, `Reset`, `GetMeasurementDuration`, `DefaultI2cAddress` / `SecondaryI2cAddress`), so switching from a BMP280 is a one-line change.

**Not included** (possible follow-ups, listed in the README): SPI, FIFO, interrupt pin, I2C watchdog, sensor time, self-test.

### Design choices worth a look

1. **Separate folder instead of extending `Bmxx80`.** `Bmxx80Base` reads the chip ID at `0xD0` and resets via `0xE0`; the BMP3 family uses `0x00` / `0x7E` and a different calibration layout. Happy to move it if you'd prefer it inside `Bmxx80`.
2. **Out-of-range values are not reported** instead of being clamped: `Read()` returns `null` and `TryRead*` return `false` outside −40…85 °C / 300…1250 hPa. Because the pressure is computed from the temperature, an out-of-range temperature makes the pressure unavailable too.
3. **`TryRead*` never start a measurement** (same as `Bmp280`); they return `false` if nothing has been measured since the last reset (both data fields still at their reset value).
4. **`SetPowerMode` waits when leaving normal or forced mode.** On the real chip, a new mode written while a measurement is running is silently ignored until that measurement finishes (verified on hardware, see below), so the binding waits for it: about 6 ms at the defaults, up to about 145 ms at ×32/×32.
5. **Measurement time** uses each chip's own datasheet formula (the BMP390 datasheet revised the constants; the BMP388's still has the old ones).
6. `SetPowerMode(Normal)` throws `InvalidOperationException` if the output data rate is too fast for the oversampling settings, and leaves the sensor in sleep mode.

### Tests (no hardware needed)

- **79 unit tests**, run against a simulated chip built on `I2cSimulatedDeviceBase` that models the datasheet behaviour (register writes as address/value pairs, auto-increment reads, data-ready bits, soft reset, configuration errors, the mode-change window above).
- The compensation math is checked against reference values from Bosch's open-source driver (BMP3_SensorAPI, run separately; none of its code is in this PR) and an independent implementation of the datasheet formulas. The vectors include calibration bytes and raw readings from a real BMP390.

### Verified on hardware

Raspberry Pi 4, Raspberry Pi OS (32-bit), Adafruit BMP390 (#4816) at 0x77 on I2C bus 1; built with .NET SDK 10.0.302.

- The sample runs in forced and normal mode. Pressure noise is about ±1 Pa at ×8 oversampling with filter coefficient 3.
- Station pressure 1005.5 hPa versus about 1005.1 hPa derived from the local weather report (29.89 inHg sea level, 59 m elevation), within the sensor's ±0.5 hPa absolute accuracy. Lifting the board about 1 m changes the reading by roughly the expected 12 Pa.
- Hardware checks, all passing: normal mode at the default rate; too-fast settings throw and leave the sensor asleep; normal→forced at the defaults and at ×32/×32; normal→sleep→normal.

Raw logs: https://github.com/Timothy-Lee-Grant/playground/tree/main/iot/new-device-binding/sample/evidence
Demo video (sample walkthrough, hardware run, testing approach): <link>

