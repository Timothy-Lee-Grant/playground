# STATUS

> Owner: CLI. Rewritten in place by `/handoff` every session. Desktop reads this first.

| | |
|---|---|
| **Updated** | 2026-10-07 (CLI, `/handoff` of session 002) |
| **Stage** | Stage 6 (understanding). Implementation is done: plan Steps 2–10 finished in one autonomous run (G1, 2026-10-02). Lectures 001 and 002 are written and not yet read |
| **Work order** | Step 11 (lecture 1) is written: `lectures/001-the-change-end-to-end.md`, plus `002-testing-it-yourself.md`. The step's done-when needs Timothy to read it, which he hasn't said he has. Next: his reading and teach-back, then the comment (G2) |
| **Mode** | P |
| **Branch / last commit** | `remove-autofac-275` @ `1665ced5 Remove TestAutoMockBase and the Autofac test dependencies`, 4 commits ahead of `upstream/main` (`2aa3d835`). Clean apart from Finder's `.DS_Store` (untracked; never commit it) |
| **Pushed** | **No.** Local only. The branch tracks `upstream/main`; the first push (Step 14, after G3) must be `git -C yarp push -u origin remove-autofac-275` |
| **Tests** | Green. ReverseProxy.Tests has 3,978 passing, equal to the baseline (`evidence/007`). In the Release build, all 4 test projects match `main` exactly, with 0 warnings (`evidence/011`). All 8 O2 break-it proofs went red (`004`–`006`). Diff: `evidence/010-diff.patch` |
| **Upstream** | Nothing posted. The draft comment is in lecture 001 §7 (G2: Timothy edits and posts) |
| **Blockers** | none |
| **Needs Timothy / desktop** | **Timothy:**<br>1. Read lecture 001, then do the teach-back (§9 checklist).<br>2. Decide whether to mention the vacuous `Verify()` in `ForwarderMiddlewareTests.NoDestinations_503` (lecture 001 §8).<br>3. Post the comment (G2).<br>**Desktop:**<br>1. Correct the test commands in `03-yarp-project.md` §3 and `02-layout.md` §4 (session report §9).<br>2. Look at the `switch -c` permission denial.<br>No open CHANGE REQUESTs |
| **Last session report** | [`sessions/002-2026-10-07.md`](sessions/002-2026-10-07.md) |
