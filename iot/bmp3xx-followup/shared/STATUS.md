# STATUS (rewritten by the CLI at every /handoff)

| | |
|---|---|
| Date | 2026-10-22 (CLI session 001) |
| Mode | P · G1 given ("go") · G2 given ("push") · no G3 (D3) |
| Plan step | Steps 1–7 done (all "done when" met); Step 8: pushed, **PR not opened yet** (Timothy) |
| Branch / last commit | `fix/bmp3xx-normal-mode-followup` @ `d9a90af6 Say which simulated configuration check was verified on hardware` (5 commits on `upstream/main` `41086078`) |
| Pushed | **Yes**, to `origin` (Timothy's fork); `git status -sb` in sync with `origin/fix/bmp3xx-normal-mode-followup`. Clone clean |
| Tests | 95/95 (89 + 6 new), Debug ×5 and Release, 0 warnings; `Iot.Device.Bindings` 0/0 (`evidence/010-prepr-checks.txt`) |
| Hardware | Pi 4 + BMP390: ALL PASS incl. (h0) conf_err on OSR change 10/10, (h) F3 10/10, (i) F2 45/45 (`evidence/009-hw-check-followup.txt`) |
| Upstream | PR 2627 merged (`4108607`). Follow-up PR **not opened**. Open PR #2630 (Copilot, GPIO) doesn't touch Bmp3xx |
| Blockers | none |
| Needs Timothy / desktop | **Timothy:** open the PR from the compare link with `pr/001-pr-description.md` (pick a title), watch CI, send the link to desktop. **Desktop:** record the PR number (`02`), update the `ISSUES.md` row, write the lecture (D3); consider an allow rule for `git -C iot switch -c fix/*`, re-run setup.sh (missing `bmp3xx-followup` link), and plan Pi copies as Timothy's step. No open CHANGE REQUESTs |
| Report | [`sessions/001-2026-10-22.md`](sessions/001-2026-10-22.md) |
