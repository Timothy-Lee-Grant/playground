# STATUS (rewritten by the CLI at every /handoff)

| | |
|---|---|
| Date | 2026-10-05 (CLI session 001) |
| Mode | P · **G1 granted** (D5); G2, G3 closed |
| Plan step | Steps 0, 1, 4, 5, 6, 7, 8, 9, 10, 11 **done** ("done when" met; Step 4 without V1–V5, Step 11 sample not run on hardware). Hardware Steps 2, 3, 13 **not started** (no sensor yet). **Next: Step 12** (hygiene), then 15 |
| Branch / last commit | `feature/bmp3xx-binding` · `8d23d3d4 Added README and sample` (5 commits on `upstream/main` @ `336e4696`) |
| Pushed | **Yes**: level with `origin/feature/bmp3xx-binding` (Timothy pushed). Clone clean |
| Tests | 65/65 pass, 0 warnings (`evidence/012-device-read-green.txt`, `013-sample-build.txt`) |
| Upstream | nothing posted; #2611 status not checked this session |
| Blockers | no hardware: E1 (Step 3), Step 13 and real-chip vectors V1–V5 wait for the sensor; four behaviors marked unverified until then |
| Needs Timothy / desktop | **U4 question** (plan.md, Step 10 entry: report pressure when only temperature is out of range?) · fritzing diagram: none / photo / draw (Step 11 entry) · optional veto of the CLI's Step 0 implementation-level choices · desktop: update `ISSUES.md` row status and the stale `exercises/iot/CLAUDE.md` §1/§6. No open CHANGE REQUEST |
| Report | [`sessions/001-2026-10-05.md`](sessions/001-2026-10-05.md) |
