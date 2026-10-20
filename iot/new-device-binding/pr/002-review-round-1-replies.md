# Review round 1: reply drafts (CLI, 2026-10-19)

> For Timothy to post himself on dotnet/iot PR 2627, **after** pushing `feature/bmp3xx-binding` (the hashes only
> resolve on GitHub once the commits are on the fork). **Rewrite in your own words before posting.**
> Checked against the code @ `be4c13c3`, `shared/evidence/026–033` and `sample/evidence/007`.
>
> Order of posting: push → the three thread replies → the top-level reply → edit the PR description (part 3).
> Don't press "Resolve conversation" yourself unless the reviewer is happy; many maintainers prefer to resolve their own.
>
> The PR description links the raw logs in your `playground` repo: copy `sample/evidence/007-hw-check-review-round-1-2026-10-19.txt`
> there too if you want the hardware numbers below to be checkable.

---

## 1. Thread "Clear stale data-ready flags before forced measurements" (R3)

> Good catch, thanks. The datasheet confirms it (table 29: `drdy_press` / `drdy_temp` are only reset when the data registers are read), and so does my BMP390: after a forced measurement that isn't read, STATUS reads `0x70`, and `0x10` after one burst read of the data (10/10).
>
> Fixed in 32d75c35 as you suggested: `Read()` / `ReadAsync()` now go to sleep (which waits if a measurement is still running), read the six data bytes to clear the flags, and then start the forced measurement. Regression tests: `Read_AfterUnreadMeasurement_DoesNotReturnStaleData` and `ReadAsync_AfterUnreadMeasurement_DoesNotReturnStaleData` (both returned the old sample before the fix).
>
> Cost: one extra register write and a 6-byte read per `Read()`. Calling `SetPowerMode(Forced)` right before `Read()` now means two measurements, so I documented in the README (be4c13c3) that this isn't needed.

## 2. Thread "Account for in-flight conversion duration after oversampling changes" (R2)

> Agreed, this is a real gap. Datasheet 3.3.4 says a mode change is delayed until the running measurement ends, and my BMP390 shows the scenario: normal mode at ×1/×1 with a 160 ms period, pressure oversampling raised to ×32 and lowered again, then sleep and forced with the old ~6 ms wait. 3 of 30 forced writes were lost.
>
> Fixed in 574026e0 with your first option: the binding keeps the longest measurement time configured since normal or forced mode was entered (both oversampling setters raise it), and the wait when leaving the mode uses that. I didn't go through sleep in the setters, since a property setter that changes the power mode and blocks seemed surprising. Regression tests: `Read_AfterPressureSamplingRaisedAndLoweredInNormalMode_StartsAMeasurement` and the same for temperature; the simulated chip now models the longer window too. On the Pi, the same sequence through the binding: 45/45, slowest 85 ms.

## 3. Thread "Treat 0x800000 measurements as valid data" (R1)

> You're right, and it isn't only theoretical: with one of the calibration sets in the tests, `0x800000` / `0x800000` is 23.68 °C and 799 hPa, both in range.
>
> Fixed in 9ac45d2f: the reset-value check is gone. The binding now remembers whether it has seen a measurement since the last reset: set when a forced read completes or STATUS shows a data-ready bit, cleared by `Reset()`. Regression tests with both raw fields at `0x800000`: `Read_MeasurementAtResetValue_ReturnsIt`, `TryRead_AfterReadingMeasurementAtResetValue_ReturnsIt` (repeated reads after the flags are cleared), `TryRead_AfterForcedModeWithoutRead_ReturnsMeasurementAtResetValue`, `Read_InNormalMode_MeasurementAtResetValue_ReturnsIt` (sync and async), and `TryRead_AfterMeasurementAndReset_ReturnsFalse`.
>
> One limit, noted in a code comment: a measurement made and read out through another handle to the same sensor isn't seen by this instance.

## 4. Top-level reply (to raffaeler)

> Thanks for the review. I addressed all three findings in 32d75c35, 574026e0 and 9ac45d2f (plus a README note in be4c13c3), each with regression tests; details in each thread. 89 tests now, and the hardware checks pass on a BMP390.

---

## Part 3: PR description edits

Replace design choice 3:

> 3. **`TryRead*` never start a measurement** (same as `Bmp280`); they return `false` if nothing has been measured since the last reset. The binding tracks this itself rather than looking at the data, because the data registers' reset value (`0x800000`) can also be a genuine reading.

Add to the end of design choice 4 (or as a new 7):

> 7. **`Read()` returns only the measurement it started.** Data left from an earlier measurement that wasn't read is discarded first (the data-ready flags are only cleared by reading the data), so calling `SetPowerMode(Forced)` before `Read()` isn't needed.

In "Tests": `79 unit tests` → `89 unit tests`.

In "Verified on hardware", last bullet, append:

> ; after review: stale data-ready flags cleared by a data read, and no lost mode change after raising and lowering oversampling in normal mode.

Optional: the header line says `@ 2950b2d4`; that's only in this draft, not on GitHub.
