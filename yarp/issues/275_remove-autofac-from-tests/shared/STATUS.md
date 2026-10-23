# STATUS

> Owner: CLI. Rewritten in place by `/handoff` every session. Desktop reads this first.

| | |
|---|---|
| **Updated** | 2026-10-22 (CLI, `/handoff` of session 003) |
| **Stage** | Stage 7 (contribution). The pre-PR check (plan Step 12, D7) passed at `1665ced5`, so its done-when is met. Next: Step 14, Timothy opens the PR. Step 13 (push) isn't needed, because the fork already has this commit |
| **Work order** | Timothy opens the PR from the compare page, with the title and body from [`pr-description.md`](pr-description.md). Then the review loop (Step 15) |
| **Mode** | P |
| **Branch / last commit** | `remove-autofac-275` @ `1665ced5 Remove TestAutoMockBase and the Autofac test dependencies`. 4 commits on `upstream/main` (`2aa3d835`, unchanged as of 2026-10-22). Clean apart from Finder's `.DS_Store` (untracked; never commit it). `artifacts/` holds a failed build from `014`: the next local build must be `--rebuild` / `--no-incremental` |
| **Pushed** | **Yes**, since 2026-10-08. `origin/remove-autofac-275` = `1665ced5`; the branch tracks it, 0 ahead / 0 behind |
| **Tests** | Green. Release `./build.sh --rebuild --test`: 0 warnings, and all 7 project/framework totals equal `main`. ReverseProxy.Tests has 1,989 per framework (`evidence/013`). O2 break-it proofs `004`–`006` |
| **Upstream** | Timothy's comment on #275 (2026-10-09) has no reply. No PR yet, his or anyone else's (checked 2026-10-22, `012`) |
| **Blockers** | none |
| **Needs Timothy / desktop** | **Timothy:**<br>1. Decide whether to keep the optional `Verify()` paragraph.<br>2. Edit the text into his own words.<br>3. Open the PR: https://github.com/dotnet/yarp/compare/main...Timothy-Lee-Grant:yarp:remove-autofac-275?expand=1<br>4. Watch the CLA bot and the Windows/Linux CI checks.<br>5. Send the link.<br>**Desktop:**<br>1. Consider the `pre-pr.md` and `03-yarp-project.md` notes (session report §9).<br>2. The session 002 items are still open: the `switch -c` permission, and the test-command fixes in `02-layout.md` §4.<br>No open CHANGE REQUESTs |
| **Last session report** | [`sessions/003-2026-10-22.md`](sessions/003-2026-10-22.md) |
