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

[Timothy — 2026_10_22] G1 approved ("go"), at the start of the first CLI session. Recorded by the CLI.

[CLI — 2026_10_22_20_05] **Step 1 done: branch + baseline.** `upstream/main` fetched (now `41086078`, the PR 2627 merge; `merge-base --is-ancestor 4108607 upstream/main` ✅). Branch `fix/bmp3xx-normal-mode-followup` created from `upstream/main`, tracking unset (`branch -vv` shows no upstream). Baseline: tests + samples build `--no-incremental` **0 warnings, 0 errors**; **89/89** on net8.0, SDK 10.0.302 from `global.json`.
*Changed:* no files; new branch only. *Evidence:* `001-baseline.txt`.
*Deviations:* **Timothy created the branch by hand** (`! git -C iot switch -c … && git -C iot branch --unset-upstream`). The CLI's `git -C iot switch -c …` was auto-denied twice with no prompt. No project deny rule matches it, so auto mode's permission checker most likely blocked branch creation. **Desktop:** consider an explicit `allow` for `git -C iot switch -c fix/*` in the project settings so the next follow-up doesn't need it.
**Why:** the branch starts from `upstream/main` rather than the old `feature/bmp3xx-binding` because the feature branch is merged and protected; upstream's merge commit is the true state the fixes must apply to. Tracking is unset so a bare `git push`/`pull` can't talk to `upstream`. `--no-incremental` because `artifacts/` is shared across branches and was last built from the feature branch.

[CLI — 2026_10_22_20_15] **Step 2 done: F1, the simulator keeps its busy window on a repeated normal-mode write.** Commit `005ae214` `Keep the simulated busy window on a repeated normal-mode write` (author `timothyleegrant1995@gmail.com`; Timothy approved the commit prompt).
*Changed:* `tests/SimulatedBmp3xx.cs`: in `OnPowerControlWritten`'s normal branch, `_longestNormalModeMeasurementMicroseconds = MeasurementMicroseconds()` now runs only `if (previousMode != 0b11)`, with the comment "A repeated normal-mode write doesn't end a running measurement, so it keeps the longest one." `tests/SimulatedBmp3xxTests.cs`: new `ModeWrite_AfterRepeatedNormalModeWrite_StillWaitsForTheLongerMeasurement`.
*Evidence:* `002-f1-red.txt`: the new test fails with PWR_CTRL `51` (0x33, the write was accepted) instead of `3`, the predicted reason; the other 9 simulator tests pass. `003-f1-green.txt`: build 0/0, **90/90**.
*Deviations:* the test follows round 1's neighbour `ModeWrite_AfterOversamplingWasRaisedAndLowered_WaitsForTheLongerMeasurement` rather than the plan's "sleep, then forced immediately": (1) with **no** wait, even the shrunken ×1/×1 window (4.8 ms) still ignores the write, so the test would pass before the fix (no red). It waits 20 ms instead: longer than ×1/×1, shorter than ×32 (67.4 ms). (2) It asserts with a **normal** write, not forced: the simulated chip finishes a forced measurement instantly and returns to sleep, so PWR_CTRL reads sleep whether the forced write was ignored or accepted; a normal write distinguishes them (`0x03` ignored vs `0x33` accepted).
**Why:** the simulator has two inputs to its busy window: entering normal mode (reset to the current measurement time) and OSR writes during normal mode (raise to the max). Before, every normal-mode write counted as "entering", so ×1 → ×32 → ×1 → normal-again erased the ×32 maximum. On the real chip a repeated PWR_CTRL normal write doesn't restart or end the measurement in flight (*unverified* directly; consistent with the 2026-10-17 hardware finding that a running measurement finishes first), so the worst case stays the longest since normal mode was really entered. This is test-only, and it comes first because with the old simulator F2's binding bug can't show up: the fake chip forgot the long measurement exactly as the binding does. All 89 existing tests still pass with the stricter simulator, so none of them exercised a repeated normal request.

[CLI — 2026_10_22_20_25] **Step 3 done: F2, the binding keeps the pending measurement time on a repeated power-mode request.** Commit `949dd535` `Keep the pending measurement time on a repeated power-mode request` (Timothy approved the prompt).
*Changed:* `Bmp3xxBase.cs` `SetPowerMode`: `_longestPendingMeasurementMilliseconds = currentMode == powerMode ? Math.Max(field, GetMeasurementDuration()) : GetMeasurementDuration();`, comment "Requesting the mode that is already active doesn't end a running measurement, so the longest is kept." Field comment: "…since normal or forced mode was last entered **from sleep**…". `tests/Bmp3xxReadTests.cs`: `Read_AfterPressureSamplingRaisedAndLoweredAndNormalModeRequestedAgain_StartsAMeasurement` and `…TemperatureSampling…`, both through round 1's helper `AssertReadStartsMeasurementAfterOversamplingChange` (×32 → ×1 → `SetPowerMode(Normal)` inside the lambda, at `Period160Milliseconds`).
*Evidence:* `004-f2-red.txt`: both new tests fail, 3 round-1 tests pass. `005-f2-green.txt`: tests + samples 0/0, **92/92**.
*Deviations:* (1) The red shows as `Assert.NotNull` on the temperature, not a `TimeoutException`: the binding's `Read()` returns `(null, null)` on a timeout (XML doc: "Both are null if the measurement didn't finish in time"); 31–60 ms per test ≈ measurement + timeout margin. This is the predicted cause (forced write ignored → timeout), only its surface differs from Copilot's wording "can time out". Annotated in `004`. (2) The tests call `SetPowerMode(Sleep)` explicitly before `Read()` (the helper does), rather than relying on `Read()`'s own sleep transition; same wait code either way.
**Why:** `SetPowerMode` decides how long to wait when leaving normal/forced mode from `_longestPendingMeasurementMilliseconds`, the slowest settings a measurement in flight could be using. The OSR setters raise it to the max; entering a mode resets it. Before, *every* non-sleep request reset it, including one for the already-active mode, which neither waits nor starts a new measurement cycle. So ×1 → ×32 → ×1 → Normal-again dropped the bound from 68 ms to 5 ms while a 67 ms conversion could still run; the next sleep waited only 6 ms, the chip ignored the forced write, and `Read()` gave up. The condition `currentMode == powerMode` (with `powerMode != Sleep`) is exactly "no leave-wait ran and the chip wasn't in sleep", the only case where nothing new is entered. Rejected: (a) skipping the write entirely for a redundant request: changes what's on the bus and would also skip F3's error check path; (b) always `Math.Max` (never reset): correct but the bound would grow forever, so every later sleep transition would wait for the slowest settings ever used. Forced → forced (a measurement still running) falls in the same branch, which is the safe choice.

[CLI — 2026_10_22_20_40] **Step 4 done: F3, a stale configuration error is cleared before entering normal mode.** Commit `a197ab8d` `Clear a stale configuration error before entering normal mode` (Timothy approved the prompt).
*Changed:* `Bmp3xxBase.cs` `SetPowerMode`: when `powerMode == Normal`, `ReadErrors()` (result discarded) right before the mode write, comment citing table 28; the post-write conf_err check is unchanged. `tests/SimulatedBmp3xx.cs`: new `ReportConfigurationErrorIfNormalMode()`, called after an OSR or ODR write: in normal mode with `ConfigurationErrorOnNormalMode` set, it sets conf_err and clears the mode bits (sleep). Marked **unverified** in its comment. `tests/SimulatedBmp3xxTests.cs`: `SettingsChangeInNormalMode_WithConfigurationError_ReturnsToSleep` (theory: OSR ×32, ODR 5 ms). `tests/Bmp3xxBaseTests.cs`: `SetPowerMode_Normal_AfterAnUnreadConfigurationErrorWasCorrected_EntersNormalMode`.
*Evidence:* `006-f3-red.txt`: the binding test throws `InvalidOperationException` "…Choose an OutputDataRate period longer than 68 ms" although ODR is already 160 ms: the stale flag, as predicted; 94 others pass. `007-f3-green.txt`: tests + samples 0/0, **95/95**.
*Deviations:* (1) The simulator tests are green from their first run (new simulator behaviour, not a bug fix), so they have no red run. (2) Only OSR and ODR writes trigger the simulated check, not CONFIG (filter): the datasheet ties conf_err to ODR vs measurement time only. (3) The simulated drop to sleep doesn't start a busy window, matching the existing conf_err-on-entry path (hardware 019: the chip drops to sleep at once, ERR 0x04, at 0.34 ms). (4) The binding test also sets `OutputDataRate = Period160Milliseconds` before clearing the fake's flag, so it reads like the real caller story (fix the rate, then retry).
**Decision the plan left to the CLI, fatal_err:** nothing special. `ReadErrors()` returns `fatal_err` too, but it isn't cleared on read (table 28) and the binding has never acted on it; doing so would be new behaviour, out of scope. The pre-read also clears a stale `cmd_err`; that is harmless (the only `cmd_err` check, in `Reset()`, follows its own command) and, if anything, prevents the same stale-flag problem there.
**Why:** conf_err is sticky: set by the chip when normal mode is entered (or, *unverified*, when settings change in normal mode) with a period too short for the measurement, cleared **only** by reading ERR. The binding read ERR only after its own normal-mode write. On the path where the chip rejects a settings change by itself and drops to sleep, nobody reads ERR, so the next valid `SetPowerMode(Normal)` saw the old flag, put the chip back to sleep and threw. Reading ERR right before the write makes the post-write check mean "this request caused it". Rejected: (a) reading ERR in the OSR/ODR setters: doesn't cover changes made through another connection or a sensor that set the flag for another reason; (b) re-checking whether the settings are valid in software instead of trusting the flag: duplicates the chip's rule, which hardware 019 showed agrees with the formula but is still the chip's call. Desktop-check note: Step 6 (h) must confirm whether an OSR change in normal mode really sets conf_err on hardware; if not, adjust the simulator comment/test name as the plan says (the binding fix stands either way, per table 28).

[CLI — 2026_10_22_20_45] **Step 5 done: hygiene all ✅** @ `a197ab8d` (clean). Tests + samples `--no-incremental` 0/0; **95/95 ×5** (89 + 6 new: F1 1, F2 2, F3 1 binding + 2 simulator theory cases), ~600 ms each; `diff --stat upstream/main...HEAD` 5 files, +99/−4, all under `src/devices/Bmp3xx/`; `diff --check` empty; 3 commits, all `timothyleegrant1995@gmail.com`; clean-room, TODO, pragma: none; `dotnet build src/Iot.Device.Bindings/` 0/0. Item 7: no public surface change; README line 66 still accurate → no README change.
*Changed:* nothing. *Evidence:* `008-hygiene.txt`. *Deviations:* none.
**Why** a README check at all: F3 changes when `SetPowerMode(Normal)` throws, which the README documents; the documented contract ("throws if the rate is too fast") was always the intent, and the fix makes the code match it.

[CLI — 2026_10_22_20_55] **Step 6 prepared: hw-check extended with (h0), (h), (i); published for the Pi.** Not run yet (needs Timothy).
*Changed:* `../sample/hw-check/Program.cs` (outside the fork): header lines for the follow-up; `RaiseAndLower` gets `bool requestNormalAgain = false`; new (h0), (h), (i) after (g0). Published `-c Release -r linux-arm --self-contained` to `scratch/bmp3xx-followup/publish/hw-check/`, against the binding @ `a197ab8d`. The csproj's relative path to `Bmp3xx.csproj` was already right.
*Deviations (plan wording vs what the chip/binding allow):* (1) **(i):** the plan says "…`SetPowerMode(Normal)` again → `Read()`", but in normal mode `Read()` returns the latest values and starts no forced measurement (XML doc), so that sequence can't exercise F2. (i) reuses (g)'s `RaiseAndLower` (sleep, then `Read()`) with the redundant request added; the Step 3 unit tests already do the same (the plan's Step 3.1 "Read() (which goes to sleep…)" has the same slip). (2) **(h):** the plan's raw control "print ERR" can't share a try with the binding check: reading ERR clears conf_err and would remove what the fix has to clear. Split into **(h0)** (raw, informational: OSR ×32 written in normal mode at 5 ms → print PWR_CTRL and ERR; this verifies or refutes the Step 4 simulator assumption) and **(h)** (binding: same change, only PWR_CTRL read and counted as "dropped to sleep n/10", ODR → 160 ms, `SetPowerMode(Normal)` must not throw and must end in normal). (3) (h0) ends each try with x1/x1, sleep and a 100 ms pause, like (g0): its raw x32 write is invisible to the binding's busy-wait bound.
**Why:** the hardware run answers the one open question in the change: whether a settings change in normal mode really sets conf_err (the simulator models it; datasheet table 28 only says conf_err is "only working in normal mode"). If (h0) shows no conf_err, the binding fix still stands (any stale conf_err from an earlier normal-mode error), and the simulator comment and test name need adjusting per the plan.

[CLI — 2026_10_22_21_10] **Step 6 done: hardware check on the Pi, ALL PASS.** Timothy copied and ran hw-check (built @ `a197ab8d`). **(h0)** conf_err + sleep **10/10**: an OSR change to ×32 in normal mode at 5 ms sets ERR 0x04 and drops to sleep within 5 ms → Step 4's simulator assumption is **verified**. **(h)** F3 10/10 (chip had dropped to sleep 10/10). **(i)** F2 30/30 + 15/15, slowest sleep + Read 85 ms. (a)–(g) all pass, (g0) 3/30 lost with the old 6 ms wait, as on 2026-10-19.
*Changed:* commit `aefb18cc` `Mark the simulated configuration check on a settings change as verified`: the `ReportConfigurationErrorIfNormalMode` comment now says verified (build 0/0, 95/95 before commit). *Evidence:* `sample/evidence/001-hw-check-followup-2026-10-22.txt`, copied to `evidence/009-hw-check-followup.txt` (only the hw4 run; the paste also held the 2026-10-19 hw3 run).
*Deviations:* (1) Timothy ran `scp` himself: the CLI's `scp` failed twice with "scp: Connection closed" (exit 255) while the Pi was up, most likely because the CLI's shell has no access to his SSH credentials (he logs in with a password) or no network permission. **Desktop:** for future Pi steps, plan for Timothy to copy, or set up key-based login. (2) A separate 4th commit for the comment rather than amending `a197ab8d`, so history isn't rewritten.
**Why** the comment change: the simulator's comments mark every assumption as verified or not; leaving "unverified" after a hardware run that settles it would mislead the next reader of the fake chip.
*Side note for Timothy (not part of the PR):* the Pi's login banner says the `pi` user still has the default password while SSH is enabled.

[CLI — 2026_10_22_21_30] **Step 7 done: pre-PR check all ✅ (go), PR text drafted.** HEAD `d9a90af6`, 0 behind / 5 ahead of `upstream/main` (`41086078`), not on origin yet.
*Changed:* commit `d9a90af6` `Say which simulated configuration check was verified on hardware` (check 8 finding: `aefb18cc`'s comment overclaimed; (h0) verified the OSR trigger only, so ODR is now marked unverified). New `../pr/001-pr-description.md` (2 title options; body per finding; verified numbers).
*Evidence:* `010-prepr-checks.txt` (0, 1, 3–7: clean, behind 0, scope 5 files +99/−4 under `src/devices/Bmp3xx/`, diff --check empty, 4→5 commits all `timothyleegrant1995@gmail.com` with imperative subjects, `dotnet format whitespace` exit 0, Debug ×5 and Release 95/95, `Iot.Device.Bindings` 0/0), `010-prepr-review.txt` (2, 8, 9).
*Deviations:* check 7 uses the plan's commands (Bmp3xx tests/samples + `Iot.Device.Bindings`, Debug and Release) instead of the full `./build.sh --test` (30–45 min); CI covers the rest. Check 2: the "Bmp3xx" search hit open PR #2630 (Copilot, GPIO driver lifetimes); its file list has nothing under `src/devices/Bmp3xx`.
**Why** the extra commit instead of amending: the branch isn't pushed, so amending would be allowed, but the plan says small, one-concern commits and the template says the PR is merged with its commits; a second comment commit is harmless and avoids rewriting history. Timothy may prefer one commit for the two comment tweaks: that would be an interactive rebase, which he would run himself.

[Timothy — 2026_10_22] G2: "push". Recorded by the CLI.

[CLI — 2026_10_22_21_35] **Pushed.** `git -C iot push -u origin fix/bmp3xx-normal-mode-followup` (Timothy's fork only; the pre-push hook passed); `origin/fix/bmp3xx-normal-mode-followup...HEAD` = `0 0` @ `d9a90af6`. Tracking now points at `origin`, not `upstream`. PR not opened: Timothy opens it from the compare link with `pr/001-pr-description.md`.

[Desktop — 2026_10_23] **PR opened by Timothy: dotnet/iot PR 2631. CI red: macOS leg, a timing-flaky simulator test — ours, and it also broke `main`.** Read from Azure (build 133266, log 24):
`SimulatedBmp3xxTests.ModeWrite_AfterRepeatedNormalModeWrite_StillWaitsForTheLongerMeasurement [FAIL]` — `Expected: 3, Actual: 51` (PWR_CTRL 0x33: the "ignored" write was accepted), `SimulatedBmp3xxTests.cs:line 132`; 94/95 passed.
**Same failure on `main` right after our merge:** build 133239 (`main` @ `41086078`, macOS Debug): round 1's `ModeWrite_AfterOversamplingWasRaisedAndLowered_WaitsForTheLongerMeasurement` (line 115), `Expected: 3, Actual: 51`. Also in other people's PR builds (e.g. 133248, PR 2630, macOS Release). It passes sometimes (flaky).
**Cause (desktop's read; high confidence):** these tests `Thread.Sleep(20)` and then expect the simulated chip to still be busy (window ≈ 67 ms, `Stopwatch` wall clock). On a loaded macOS CI agent the sleep (and the gaps between writes) can overshoot past the window, so the write is accepted. A test that asserts "still busy" against the real clock is inherently timing-dependent. The Mac/Pi never showed it because they're fast and idle.

### Work order RW2 — make the simulator's busy window deterministic (on PR 2631's branch)
G1 for RW2: Timothy says "go". Push only on his word (adds commits to the open PR; no force-push).
1. **Show it first (red):** run the Bmp3xx tests 30× in a loop while the Mac is loaded (e.g. start `yes > /dev/null &` once per CPU core; kill them afterwards!) and count failures of the three `ModeWrite_*` tests. Save `evidence/011-flaky-under-load.txt`. If it won't reproduce, say so and continue: the logic argument stands, and CI is the evidence (quote the Azure lines above).
2. **Fix (test code only):** give `SimulatedBmp3xx` a controllable clock: e.g. a `Func<long> GetTimestamp` (or a small manual-clock class) defaulting to `Stopwatch.GetTimestamp`, used everywhere the window is set or checked. Tests that assert the chip is **still busy** use a manual clock that only moves when the test says so (`AdvanceMicroseconds(…)`), replacing `Thread.Sleep(20)` with "advance 20 ms". Tests that drive the **binding** keep the real clock (the binding really sleeps; a slow machine only makes those pass more easily — check that claim for each binding test that relies on the fake's window, and note it). `ModeWrite_AfterTheRunningMeasurement_IsAccepted` also moves to the manual clock (advance past the window). Prefer the simplest design that matches the repo's test style; `TimeProvider` is fine if available on net8.0 without new packages (it is in-box since .NET 8: `System.TimeProvider`; a `FakeTimeProvider` needs `Microsoft.Extensions.TimeProvider.Testing`, so **don't** add that package; write the few lines by hand).
3. **Green:** the same 30× loop under load: 0 failures. Plus the normal hygiene (Step 5 list), 95/95 (or the new count).
4. **Commit:** `Use a controllable clock in the simulator's busy-window tests` (plain subject; no `#`).
5. **Draft a PR comment** for Timothy in `../pr/002-ci-macos-flaky-comment.md` (no AI statements). Content: the macOS failure is in the simulator tests from the original binding PR, which slept 20 ms and expected the simulated chip to still be busy; on a loaded macOS agent the sleep overshoots past the ~67 ms window; it also made `main`'s macOS leg fail after the merge (build 20261022.5); fixed here by giving the simulator a controllable clock (commit hash), no change to the binding; 30 runs under CPU load: 0 failures. Offer: "happy to split this into its own PR if you'd rather unblock main separately."
6. Update `../pr/001-pr-description.md` with a short "Also" paragraph about the test-clock fix (Timothy edits the PR description himself).
7. Hand back; Timothy says "push", posts the comment, edits the description.

[Timothy — 2026_10_23] G1 for RW2: "go". Recorded by the CLI.

[CLI — 2026_10_22_22_45] **RW2 steps 1–6 done: the busy-window tests use a manual clock.** Commit `96fd4f61` `Use a controllable clock in the simulator's busy-window tests` (2 files, +25/−11, test code only; `Bmp3xxBase.cs` untouched). **Not pushed** (1 ahead of `origin`).
*Changed:* `tests/SimulatedBmp3xx.cs`: `public TimeProvider TimeProvider { get; init; } = TimeProvider.System`, used for both reads of the clock (`_modeChangesIgnoredUntil` set and checked); `using System.Diagnostics` gone. `tests/SimulatedBmp3xxTests.cs`: the 4 `ModeWrite_*` tests construct the chip with `{ TimeProvider = clock }`; `Thread.Sleep(20)` → `clock.Advance(TimeSpan.FromMilliseconds(20))`; private nested `ManualTimeProvider : TimeProvider` (overrides `GetTimestamp`, adds `Advance`). Outside the fork: `pr/002-ci-macos-flaky-comment.md` (new), `pr/001-pr-description.md` v2 ("Also" paragraph).
*Evidence:* `011-flaky-under-load.txt` (red), `012-deterministic-clock-green.txt` (green + hygiene).
*Findings:*
- **Step 1 did not reproduce** (verified, `011` A/B): 30/30 green with 8× `yes`, and 30/30 with 32× `yes`. A CPU-bound load on a fast 8-core Mac doesn't stretch `Thread.Sleep(20)` past ~67 ms; a CI VM with stolen CPU time does. So instead I **showed the mechanism by injection** (`011` C): temporarily `Thread.Sleep(80)` → the two "still busy" tests fail with CI's exact message and lines (132 = this PR, 115 = `main`). Reverted.
- **Green proof by the same injection** (`012` A): an 80 ms real stall after each `Advance` → 95/95. And the tests still have teeth (`012` B): undoing the F1 simulator fix → `ModeWrite_AfterRepeatedNormalModeWrite_…` fails deterministically, `Expected: 3, Actual: 51`. The 30× load loop is 30/30 (`012` C), but it was 30/30 before too, so it proves little; the PR comment cites the injection, not the loop.
- **The repo already has a fake-clock pattern** (verified): `src/devices/Button/tests` references `Microsoft.Extensions.TimeProvider.Testing` 8.0.0 and uses `FakeTimeProvider.Advance(…)`. The plan said don't add that package; I followed the plan but made the hand-written clock the same shape (in-box `TimeProvider`, `Advance(TimeSpan)`), so switching to the package is a one-line change if a reviewer prefers it. **Option for Timothy/desktop, not a decision I made.**
- **Binding tests keep the real clock** (checked each one, code reading): every binding test that touches the window needs it to have **expired** (`Read_After…RaisedAndLowered…` ×4, `SetPowerMode_FromNormalToForced_StartsAMeasurement`, `SetPowerMode_NormalSleepNormal_EndsInNormalMode`). The binding sleeps ≥ window ×1.1 + 1 ms on the real clock (`Bmp3xxBase.cs:268-269`), so a slow machine only adds elapsed time: those tests can't fail from slowness. No binding test asserts that a write is *ignored*.
- `ModeWrite_RightAfterLeavingNormalMode_IsIgnored` had no sleep but was wall-clock too (needed two writes within 10.9 ms); moved to the manual clock as well (plan listed 3 tests; this is the 4th, same class).
*Deviations:* (1) evidence: red by injection instead of by load (plan allowed "if it won't reproduce, say so and continue"). (2) Clock as an `init` property, not a constructor parameter: matches how the simulator's other options are set (`{ NeverBecomesReady = true }`) and leaves the ~40 existing `new SimulatedBmp3xx(…)` calls alone. (3) `ManualTimeProvider` nested private in `SimulatedBmp3xxTests` (its only user) rather than a new file.
**Why this design:** the bug is that a test asserted "still busy" against a clock it didn't control; any assertion about *how much time has passed* is only deterministic if the test owns the clock. `TimeProvider` is the .NET 8 in-box abstraction for exactly that (and what Button already uses), so no new concept for reviewers. Rejected: a longer window or shorter sleep (just lowers the odds, still flaky); `Func<long>` timestamp delegate (works, but a home-made shape when the BCL has one); adding `Microsoft.Extensions.TimeProvider.Testing` (plan said no; see the option above).
*Next:* Timothy: "push" (adds `96fd4f61` to PR 2631; no force), then post `pr/002-…` and paste the "Also" paragraph from `pr/001-…` v2 into the PR description. Watch the macOS leg.

[Desktop — 2026_10_23] **RW2 reviewed: good to push.** `96fd4f61` touches only `tests/SimulatedBmp3xx.cs` and `tests/SimulatedBmp3xxTests.cs`; no `Stopwatch` or `Thread.Sleep` is left in the simulator or its tests. All four busy-window tests use the manual clock, including `ModeWrite_RightAfterLeavingNormalMode_IsIgnored`, which had no sleep but had the same exposure. Binding-driven tests keep the real clock; that's safe because `Thread.Sleep` never returns early, so a slow machine only makes the simulated measurement finish sooner. Evidence is convincing even though the load loop didn't reproduce: an injected 80 ms stall reproduced CI's exact failure (red) and passes with the fix, and undoing F1 still makes the F1 test fail. Choice on the CLI's open question: **keep the hand-written `ManualTimeProvider`** (8 lines, no package reference added to this test project). Comment draft `pr/002` is accurate. Next: Timothy pushes, posts the comment, edits the description, watches macOS.

[Timothy — 2026_10_22] RW2 step 7: "push". Recorded by the CLI.

[CLI — 2026_10_22_23_00] **Pushed RW2.** `git -C iot push origin fix/bmp3xx-normal-mode-followup`: `d9a90af6..96fd4f61` (fast-forward, pre-push hook passed); `status -sb` in sync with `origin`. PR 2631 now carries the commit; CI result not seen yet. Remaining for Timothy: post `pr/002-…`, paste the "Also" paragraph, watch macOS CI.

[Desktop — 2026_10_23] **RW2 done.** Timothy pushed `96fd4f61` to PR 2631; CI re-ran and **all checks passed**. He posted a short heads-up comment: the cause was the timing-dependent simulator test from PR 2627, which also affected `main` and other PRs; it's fixed here with a controllable clock (test code only), and he offered to split it out. Now waiting for maintainer review. Next action only if a reviewer comments.
