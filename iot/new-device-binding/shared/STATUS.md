# STATUS (rewritten by the CLI at every /handoff)

| | |
|---|---|
| Date | 2026-10-11 (CLI session 002) |
| Mode | P · G1 granted (D5); G2, G3 closed |
| Plan step | Steps 0, 1, 4–11 done (session 001). Step 13 partly done by Timothy + desktop on hardware (`sample/evidence/000–002`: sample runs, reference pressure +0.4 hPa). **Nothing done this session: Timothy paused for a demo.** **Next: Timothy's work order** (session 002 §6): Step 3 probe → V1–V5 · hardware checks (Normal @ 200 Hz ×1/×1; ×32/×32 @ 200 Hz → conf_err; if the first fails, default ODR → 10 ms) · U4 change (D7) · merge `upstream/main` · Step 12 + `dotnet build src/Iot.Device.Bindings/` · Step 15 · commit, push on his OK |
| Branch / last commit | `feature/bmp3xx-binding` · `8d23d3d4 Added README and sample` |
| Pushed | **Yes**: level with `origin/feature/bmp3xx-binding`. Clone clean |
| Tests | 65/65 pass, 0 warnings (`evidence/012-device-read-green.txt`, `013-sample-build.txt`); not re-run this session |
| Hardware | Pi 4, **32-bit userland → publish `-r linux-arm`**; BMP390 @ 0x77 bus 1; sample verified @ `8d23d3d4` (`sample/evidence/001`) |
| Upstream | nothing posted. #2611: `ISSUES.md` says open, `exercises/iot/CLAUDE.md` says merged 2026-10-08 (desktop: reconcile) |
| Blockers | none; hardware items need Timothy at the Pi |
| Needs Timothy / desktop | Timothy: say "go" on the work order next session. Desktop: reconcile #2611 status; update `ISSUES.md` row and plan header ("waiting for G1" is stale); consider adding the upstream merge + `Iot.Device.Bindings` build to the plan body. No open CHANGE REQUEST |
| Report | [`sessions/002-2026-10-11.md`](sessions/002-2026-10-11.md) |
