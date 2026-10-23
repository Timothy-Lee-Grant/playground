# STATUS (rewritten by the CLI at every /handoff)

| | |
|---|---|
| Date | 2026-10-22 (CLI session 002) |
| Mode | P · RW2 G1 given ("go") · push not given yet |
| Plan step | Work order **RW2**: steps 1–6 done (step 1 red by injection, the load loop didn't reproduce); step 7 (push, post comment, edit description) waits for Timothy |
| Branch / last commit | `fix/bmp3xx-normal-mode-followup` @ `96fd4f61 Use a controllable clock in the simulator's busy-window tests` (6 commits on `upstream/main` `41086078`) |
| Pushed | **No**: `status -sb` = ahead 1 of `origin/fix/bmp3xx-normal-mode-followup`. Clone clean |
| Tests | 95/95 Debug ×5 and Release, 0 warnings; 80 ms stall injected → 95/95; F1 undone → new test fails; 30× under load 30/30; `Iot.Device.Bindings` 0/0 (`evidence/012-deterministic-clock-green.txt`; red: `011-flaky-under-load.txt`) |
| Hardware | unchanged (session 001, `evidence/009`); RW2 is test-only, no Pi run needed |
| Upstream | PR 2631 open; macOS leg red on `d9a90af6` (our flaky test, also on `main`). Fix not on the PR yet |
| Blockers | none |
| Needs Timothy / desktop | **Timothy:** "push"; post `pr/002-ci-macos-flaky-comment.md` (check hash `96fd4f61`); paste the "Also" paragraph from `pr/001-pr-description.md` v2 into the PR description; watch macOS CI. **Desktop:** optional choice: keep hand-written `ManualTimeProvider` or use `FakeTimeProvider` (package already used by Button tests); lecture on clock injection and proving flaky fixes; check the 2026-10-22/23 date mismatch in `02` D4 / `ISSUES.md`; re-run setup.sh. No open CHANGE REQUESTs |
| Report | [`sessions/002-2026-10-22.md`](sessions/002-2026-10-22.md) |
