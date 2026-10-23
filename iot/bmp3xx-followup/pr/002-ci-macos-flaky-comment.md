# PR 2631 comment draft: the macOS CI failure (v1, 2026-10-22, CLI)

> For Timothy to post on PR 2631 after pushing `96fd4f61`. Edit freely. Sources: Azure builds 133266 (this PR,
> log 24) and 133239 (`main` @ `41086078`), quoted in `shared/plan.md`; local runs in `shared/evidence/011`, `012`.
> Before posting, check the commit hash on the PR's Commits tab matches.

---

The macOS failure is a flaky test from the original Bmp3xx PR, not something this change breaks.
`ModeWrite_AfterRepeatedNormalModeWrite_StillWaitsForTheLongerMeasurement` failed with `Expected: 3, Actual: 51`, and
the same thing happened on `main` right after #2627 was merged (macOS Debug, in
`ModeWrite_AfterOversamplingWasRaisedAndLowered_WaitsForTheLongerMeasurement`).

The cause: these simulator tests slept 20 ms on the real clock and then expected the simulated sensor to still be busy
with a measurement of about 67 ms. When the agent is slow, the sleep can take longer than that, so the mode write is
accepted and the assertion fails. Lengthening the sleep to 80 ms reproduces exactly this failure locally.

I've pushed a fix to this PR (96fd4f61): the simulator now takes a `TimeProvider`, and the tests that expect the
sensor to still be busy use a manual clock that only moves when the test advances it. Test code only, no change to
the binding. With an 80 ms stall injected into those tests they now pass, and undoing the simulator fix from this PR
still makes the new test fail, so it still catches the bug. 95/95 in Debug and Release.

Happy to split this into its own PR if you'd rather unblock `main` separately.
