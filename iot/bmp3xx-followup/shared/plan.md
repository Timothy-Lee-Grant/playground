# Plan (mode P): Bmp3xx follow-up — F1–F3 from the second review of PR 2627

> Body: desktop (2026-10-22). Entries below the line: append-only (`[CLI — …]`, `[Desktop — …]`, `[Timothy]`).
> Read first: `01-brief.md` (the findings, Copilot's wording, desktop's check). Mode and rules: `00-start-here.md`.

## Gates

- **G1 — start:** Timothy says "go" (or "G1 approved") at the start of the session. Record it as a `[Timothy]` line.
- **G2 — push / PR:** after Step 7's pre-PR check is all ✅, Timothy says "push"; he opens the PR himself.
- No G3 teach-back for this follow-up (D3). Explain each fix in short chunks as you go (what was wrong, the test
  that shows it, the fix), and write the *why* in your Stage 5 entries; desktop turns them into a short lecture.

## Branch and scope

- Branch `fix/bmp3xx-normal-mode-followup` from `upstream/main` (must contain merge commit `4108607`).
- Only `src/devices/Bmp3xx/**`. Expected files: `Bmp3xxBase.cs`, `tests/SimulatedBmp3xx.cs`,
  `tests/SimulatedBmp3xxTests.cs`, `tests/Bmp3xxReadTests.cs` (or `Bmp3xxBaseTests.cs`), maybe `README.md`.
- **Test first in every fix step:** write the test, run it, see it fail **for the predicted reason** (save the red
  run), then fix, then green. Same discipline as round 1 (see `new-device-binding/shared/plan.md`, Stage 8, RW1).

## Steps

### Step 1 — Branch and baseline
1. `git -C iot status --porcelain` empty. `git -C iot fetch upstream` and `git -C iot fetch origin`.
2. Check `git -C iot merge-base --is-ancestor 4108607 upstream/main` succeeds.
3. `git -C iot switch -c fix/bmp3xx-normal-mode-followup upstream/main`, then `git -C iot branch --unset-upstream`
   (so a bare push/pull can't talk to upstream).
4. Baseline: build `src/devices/Bmp3xx/tests/` and `samples/` with `--no-incremental` (0 warnings), run the tests
   (expect **89/89**).
**Evidence:** `evidence/001-baseline.txt`. **Done when:** on the new branch, clean, 89/89.

### Step 2 — F1: simulator keeps its busy window on a repeated Normal write (test-only)
1. **Test first** (`SimulatedBmp3xxTests`): in normal mode at ×1/×1, write OSR pressure ×32, then ×1, then write
   PWR_CTRL normal again (`0x33`), then sleep, then forced **immediately**. Expect the forced write to be ignored
   (PWR_CTRL reads sleep and no new measurement/data-ready), because the ×32 conversion may still be running. Red
   today: the repeated normal write shrinks the window.
2. **Fix** as Copilot suggested: in `OnPowerControlWritten`, set `_longestNormalModeMeasurementMicroseconds` only
   when `previousMode != 0b11`. Comment: a repeated normal request doesn't end a running conversion.
3. Green; all other tests still pass.
**Commit:** `Keep the simulated busy window on a repeated normal-mode write`.
**Evidence:** `002-f1-red.txt`, `003-f1-green.txt`.

### Step 3 — F2: binding keeps the pending measurement time on a repeated mode request
1. **Test first** (next to round 1's `Read_AfterPressureSamplingRaisedAndLoweredInNormalMode_StartsAMeasurement`):
   the same sequence plus a redundant `SetPowerMode(Normal)` after the ×32 → ×1 change, then `Read()` (which goes
   to sleep, waits, then forced). Expect a real measurement (not a timeout). Same for temperature oversampling.
   Red on `4108607`'s binding **with Step 2's simulator** (that's why Step 2 comes first). Check it fails for that
   reason (the forced write was ignored → `Read()` timed out).
2. **Fix** in `SetPowerMode`: reset `_longestPendingMeasurementMilliseconds` to `GetMeasurementDuration()` only when
   a mode is actually entered from sleep (the chip was in sleep, or the leave-wait just ran). When the requested
   mode is already active (`currentMode == powerMode`, normal or a forced measurement still running), keep the
   maximum: `Math.Max(field, GetMeasurementDuration())`. Update the field's comment.
3. Green; all tests pass.
**Commit:** `Keep the pending measurement time on a repeated power-mode request`.
**Evidence:** `004-f2-red.txt`, `005-f2-green.txt`.

### Step 4 — F3: clear a stale configuration error before entering normal mode
1. **Simulator first:** model what the chip did on hardware (2026-10-17 conf-diag, `new-device-binding/shared/evidence/019`):
   when `ConfigurationErrorOnNormalMode` is true and an OSR or ODR write happens **while in normal mode**, set
   `conf_err` and drop to sleep (without the binding reading ERR). Mark the "triggered by a settings change in
   normal mode" part **unverified** until Step 6 (h).
2. **Test first:** `SetPowerMode(Normal)` succeeds → set `ConfigurationErrorOnNormalMode = true` → change
   `PressureSampling` (fake sets conf_err, sleeps) → set it back to `false` (the caller fixed the settings) →
   `SetPowerMode(Normal)` must **not** throw and `ReadPowerMode()` is `Normal`. Red today (throws on the old flag).
   Keep the existing test that a real conf_err from the new request still throws.
3. **Fix** in `SetPowerMode`: when `powerMode == Normal`, call `ReadErrors()` (discarding the result; cmd_err and
   conf_err are cleared on read, table 28) **before** writing normal mode, then keep the post-write check. Comment
   cites table 28. Decide: if the pre-write read shows `fatal_err`, do nothing special (out of scope); note it.
4. Green; all tests pass.
**Commit:** `Clear a stale configuration error before entering normal mode`.
**Evidence:** `006-f3-red.txt`, `007-f3-green.txt`.

### Step 5 — Hygiene
The round-1 hygiene list (`new-device-binding/shared/plan.md`, Step 12, items 1–7) at HEAD: `--no-incremental`
builds of tests and samples with 0 warnings; tests ×5 (report the new count: 89 + new tests);
`git -C iot diff --stat upstream/main...HEAD` only `src/devices/Bmp3xx/**`; `git -C iot diff --check upstream/main...HEAD`
empty; no TODO/pragma; `dotnet build src/Iot.Device.Bindings/` 0/0.
**Evidence:** `008-hygiene.txt`.

### Step 6 — Hardware check on the Pi (Timothy runs it)
Extend `../sample/hw-check/Program.cs` (copied from the binding's; its project references the clone's `Bmp3xx.csproj`;
fix the relative path if needed). Publish `-r linux-arm --self-contained` to `scratch/bmp3xx-followup/publish/`,
`scp` (asks), Timothy runs `./hw-check | tee hw4.txt` and pastes the output.
- **(h) F3:** normal mode at the default 5 ms period, ×1/×1 → raise pressure OSR to ×32 (too slow for 5 ms) →
  read PWR_CTRL (expect sleep) **without reading ERR** → set ODR to a period long enough → `SetPowerMode(Normal)`
  must succeed. Also a raw-register control showing the old flag is still set before the fix's read (print ERR).
  This also verifies (or refutes) Step 4's simulator assumption.
- **(i) F2:** normal ×1/×1 at `Period160Milliseconds`; loop 30×: ×32 → random short gap → ×1 →
  `SetPowerMode(Normal)` again → `Read()`; count timeouts. Expect 0.
- Re-run (a)–(g): all pass.
**Evidence:** `sample/evidence/001-hw-check-followup-<date>.txt` (copy to `evidence/009-…`).
**If (h) shows the chip does *not* set conf_err on an OSR change in normal mode:** keep the fix (it is still right
per table 28: a stale flag from any earlier normal-mode error), adjust the simulator comment and test name, and say
so in the PR description.

### Step 7 — Pre-PR check and PR text
Run the YARP pre-PR checklist adapted to dotnet/iot (`~/Desktop/projects/exercises/yarp/current_context/procedures/pre-pr.md`,
checks 0–9; read it), with these substitutions: clone `iot`, branch above, **no issue** (skip check 2's issue
thread; instead check that nobody else has an open PR touching `src/devices/Bmp3xx` — ask Timothy to look if
WebFetch can't), build/test = Step 5's commands plus `dotnet build src/Iot.Device.Bindings/`, reviewers =
raffaeler / pgrawehr / krwq (Copilot reviews are requested by maintainers). Commit author email must be
`timothyleegrant1995@gmail.com`.
Write `../pr/001-pr-description.md`:
- **Title** (like upstream's, short): e.g. `Bmp3xx: fix follow-up findings from review` — propose 2 options.
- **Body:** one line "Follow-up to #2627: the three findings from the second automated review that weren't
  addressed before the merge." Then one short paragraph per finding: what was wrong, the fix, the regression test
  name. Then "Verified": test count, hardware results from Step 6 (numbers, Pi 4 + BMP390). No AI statements,
  no link to the playground repo unless Timothy asks (root `CLAUDE.md` §5).
**Evidence:** `010-prepr-*.txt`. **Report:** go / no-go table.

### Step 8 — Hand back (don't push)
Session report + STATUS with Timothy's next actions, in order:
1. Say "push" → `git -C iot push -u origin fix/bmp3xx-normal-mode-followup` (settings ask).
2. Open `https://github.com/dotnet/iot/compare/main...Timothy-Lee-Grant:iot:fix/bmp3xx-normal-mode-followup?expand=1`,
   paste title and body from `pr/001-pr-description.md`, create the PR.
3. Send the PR link to desktop.

---

## Entries

[Desktop — 2026_10_22] Plan written from the PR 2627 conversation (second Copilot review: 1 open thread + 2
"previously missed issues", read in full in the browser) and checked against the datasheet (table 28) and the code
at `4108607`. Order F1 → F2 → F3 is deliberate: F2's binding test can only fail once the simulator stops hiding the
problem (F1).
