# 03 — Next: the current work order

> Owner: desktop Claude. CLI: read-only. One order at a time; finished orders move to "Done" (one line each).

## WO-1 · Orientation: build, baseline, and a guided tour  *(mode: M0 Tutor)*

> **Progress (session 001):** steps 1–3 done (evidence 001; tour given). **Remaining: steps 4 and 5, then the
> own-words check.** First thing next session: confirm the fixed imports loaded (ask Timothy to run `/memory`, or
> just say whether 00–04 were in context at start).

**Goal:** Timothy has the Button tests running on his machine and understands how `GpioButton`, `ButtonBase` and
`GpioController` fit together, and what PR #2608 changes. No product or test code is written.

**Steps (suggested):**
1. Check the fork: remotes (`origin` = Timothy-Lee-Grant/iot, `upstream` = dotnet/iot), branch, `dotnet --version`.
2. `dotnet test src/devices/Button/tests/` → save to `shared/evidence/001-baseline-button-tests.txt`.
3. Tour, with Timothy driving the questions: `GpioButton` constructor → `PinStateChanged` → `ButtonBase`
   handlers. Show the two §3 paths in the brief in the code (don't prove them yet).
4. Show PR #2608's diff (`git fetch upstream pull/2608/head:pr-2608` then `git diff main...pr-2608 -- src/devices/Button`)
   and explain which lines would collide with a #2328 fix.
5. Find and explain the `MockableGpioDriver` + Moq pattern in `src/devices/Ili934x/tests/`.

**Done when:** baseline evidence saved; Timothy can say in his own words where the fix goes and why a mocked
driver is needed; the session report lists his questions.

**Not doing:** writing tests, changing product code, choosing P1/P2.

## Queued (desktop will expand when it's time)

- **WO-2** Failing tests that prove the bug (mocked driver). Suggested mode: M1 Navigator.
- **WO-3** The fix, once P1 is decided. Suggested mode: M2 Pair.
- **WO-4** Rebase, final test run, PR description draft (Timothy opens the PR).

## Done

(none yet)
