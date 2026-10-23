# 01 — Brief: Bmp3xx follow-up (findings left after the merge)

> Owner: desktop Claude (2026-10-22). CLI: read-only. Sources: the PR 2627 conversation (read in the browser on
> 2026-10-22), the BMP390 datasheet BST-BMP390-DS002-07 rev 1.7 (Timothy's copy), the merged code at `4108607`.

## 1. What and why

The Bmp3xx binding (Bosch BMP390/BMP388) was **merged** into dotnet/iot `main` on 2026-10-22
(PR 2627 by Timothy, merged by raffaeler as `4108607`, approved by raffaeler and pgrawehr). After our round-1 fixes,
a maintainer (krwq) requested a second Copilot review. It reported **three findings, all Medium**, that were not
addressed before the merge. One is in the test simulator; **two are in the binding itself**. This effort fixes
all three in one small follow-up PR. Following through after a merge is good etiquette and cheap here.

Upstream has no issue for this; the PR description references PR 2627 (Timothy writes it).

## 2. The three findings (Copilot's text, condensed; quotes are Copilot's)

**F1 — simulator: "Avoid resetting simulator busy duration on repeated Normal mode writes"**
(`tests/SimulatedBmp3xx.cs`, the `else if (mode == 0b11)` branch of `OnPowerControlWritten`).
A Normal → Normal write resets `_longestNormalModeMeasurementMicroseconds`, "even though it does not finish a
conversion already in progress. After pressure oversampling X1 → X32 → X1, another 0x33 write therefore erases the
longer busy window. This masks the binding's corresponding duration-tracking defect." Suggested change: set it only
when entering normal mode from another mode (`if (previousMode != 0b11)`), "and add a simulator regression for the
repeated-mode write."

**F2 — binding: "Preserve pending conversion duration on redundant Normal mode request"** (`Bmp3xxBase.cs:274`).
"A redundant `SetPowerMode(Normal)` skips the sleep/wait but resets the longest pending measurement time. At a
160 ms sampling period, change pressure oversampling X1 → X32 → X1, reassert Normal, then switch to sleep and forced
mode. The recorded bound drops from 68 ms to 5 ms even though a roughly 67 ms conversion may still be running. The
sleep transition then waits only 6 ms, so the forced-mode write can be ignored and `Read()` can time out. Preserve
the maximum when the requested mode is already active, and extend the oversampling regression to include the
redundant Normal request."

**F3 — binding: "Clear stale configuration errors before validating Normal mode requests"** (`Bmp3xxBase.cs:275`).
"The configuration-error flag remains set until the error register is read, so this check can reject a valid
normal-mode request because of an earlier error. For example, an oversampling change during normal operation can
trigger a configuration error and put the chip to sleep. After the caller corrects the output data rate,
`SetPowerMode(Normal)` still sees the old flag and puts the valid configuration back to sleep. Clear stale errors
before writing the new Normal request, then retain this post-write check for errors from that request. Add a
regression with an unread configuration error followed by a valid configuration."

## 3. Desktop's check (2026-10-22)

| | Valid? | Evidence |
|---|---|---|
| F1 | **Yes** | Code at `4108607`: the branch sets the field on every normal-mode write. A test-only change, but it must come **first**: with it, F2's binding test can fail. |
| F2 | **Yes** | Code: in `SetPowerMode`, `if (powerMode != Sleep) { _longestPendingMeasurementMilliseconds = GetMeasurementDuration(); … }` runs even when `currentMode == powerMode` (no wait, no new mode entered). Same pattern as round-1 R2; the redundant request is the gap. |
| F3 | **Yes** | Datasheet table 28: `conf_err` "sensor configuration error detected (only working in normal mode). **Cleared on read.**" Nothing else clears it. Hardware (2026-10-17, conf-diag): on conf_err the chip drops to sleep at once, so the binding never reads ERR in that path → the flag stays set → the next valid `SetPowerMode(Normal)` throws. |

## 4. Constraints

- dotnet/iot: PR template asks for `Fixes #n` only if fixing an issue (none here), a description, follow-ups as
  issues, **no force-push** during review. No AI-disclosure policy (checked 2026-09-27 and 2026-10-19).
- Squash or merge: PR 2627 was merged as a merge commit with its 15 commits; small logical commits are fine.
- Builds: SDK 10.0.302 via `global.json`; `TreatWarningsAsErrors`, StyleCop, XML docs.
- CI: Windows Release sometimes fails in `Tca955x.Tests` (`VSTestTask` returned false, no error); not ours
  (lecture `exercises/lectures/003-reading-ci-logs.md`).
