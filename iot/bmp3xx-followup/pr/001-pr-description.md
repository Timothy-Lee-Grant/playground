# PR description draft (v2, 2026-10-22, CLI)

> **v2 (RW2):** added the "Also" paragraph about the test clock (commit `96fd4f61`). The PR is already open: paste
> only that paragraph into the live description, above "Only `src/devices/Bmp3xx` changes".


> For dotnet/iot, from `Timothy-Lee-Grant:fix/bmp3xx-normal-mode-followup` @ `d9a90af6` into `dotnet:main`.
> Checked against the code and the evidence files of 2026-10-22 (`shared/evidence/001`–`010`). Edit freely before
> posting. No `Fixes` line: there is no issue for this.

**Title (pick one):**
1. `Bmp3xx: fix follow-up findings from review`
2. `Bmp3xx: keep the pending measurement time and clear stale configuration errors`

---

Follow-up to #2627: the three findings from the second automated review that weren't addressed before the merge.

**1. The simulator reset its busy window on a repeated normal-mode write.** After raising the oversampling to ×32 and lowering it again, a second write of normal mode shrank the simulated chip's busy window back to the ×1 measurement time. That hid the binding problem in 2. A normal-mode write now resets the window only when normal mode is entered from another mode. Test: `ModeWrite_AfterRepeatedNormalModeWrite_StillWaitsForTheLongerMeasurement`.

**2. A repeated `SetPowerMode` request dropped the pending measurement time.** Requesting the mode that is already active doesn't wait and doesn't end a running measurement, but it reset the longest pending measurement time to the current settings. At 160 ms, ×1 → ×32 → ×1 → `SetPowerMode(Normal)` lowered the bound from 68 ms to 5 ms. The next sleep transition then waited only 6 ms, the sensor ignored the forced write, and `Read()` found no data. A request for the active mode now keeps the maximum. Tests: `Read_AfterPressureSamplingRaisedAndLoweredAndNormalModeRequestedAgain_StartsAMeasurement` and the temperature variant.

**3. A stale configuration error made a valid `SetPowerMode(Normal)` throw.** `conf_err` is cleared only by reading the error register (datasheet table 28). When the sensor rejects a settings change in normal mode, it goes to sleep by itself and nobody reads the register, so the next valid request saw the old flag. `SetPowerMode(Normal)` now reads (and so clears) the error register before writing normal mode. The check after the write is unchanged, so an error caused by that request still throws. Tests: `SetPowerMode_Normal_AfterAnUnreadConfigurationErrorWasCorrected_EntersNormalMode`, plus `SettingsChangeInNormalMode_WithConfigurationError_ReturnsToSleep` for the simulator.

**Also: the simulator's busy-window tests no longer depend on the real clock.** They slept 20 ms and expected the simulated sensor to still be busy (about 67 ms), which failed on slow CI agents (macOS, also on `main` after #2627). The simulator now takes a `TimeProvider`, and those tests use a manual clock that the test advances. Test code only.

Only `src/devices/Bmp3xx` changes; no public API change.

### Verified

- 95 unit tests pass (89 before, 6 new), Debug and Release, 0 warnings; `Iot.Device.Bindings` builds with 0 warnings.
- Raspberry Pi 4 (32-bit Raspberry Pi OS) with a BMP390 at 0x77, .NET SDK 10.0.302:
  - Raising the pressure oversampling to ×32 while in normal mode at 5 ms sets `conf_err` and puts the sensor to sleep (10/10). With the fix, choosing a 160 ms period and calling `SetPowerMode(Normal)` enters normal mode (10/10).
  - ×1 → ×32 → ×1 in normal mode at 160 ms, a repeated `SetPowerMode(Normal)`, then sleep and `Read()`: values every time (pressure 30/30, temperature 15/15).
  - The earlier hardware checks of the binding still pass.
