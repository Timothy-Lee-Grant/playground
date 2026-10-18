# STATUS (rewritten by the CLI at every /handoff)

| | |
|---|---|
| Date | 2026-10-17 (CLI session 003) |
| Mode | P · G1 granted (D5); G2, G3 closed |
| Plan step | Steps 0–1, 3–12 done; Step 13 done on hardware (`sample/evidence/000–006`). This session: upstream merge, D7, Step 3 (V1–V5), work-order hardware checks, a hardware-found bug fixed (leaving normal mode), Step 12 complete ("done when" met, `evidence/024`). **Code final for the video.** Next: push, then Step 15 |
| Branch / last commit | `feature/bmp3xx-binding` · `2950b2d4 Document the wait when leaving normal or forced mode` |
| Pushed | **No: ahead of origin by 3** (`73b2880a`, `69009f1a`, `2950b2d4`); origin @ `17779fa9`. 0 behind `upstream/main` (@ `1856d767`, fetched 2026-10-17). Clone clean |
| Tests | 79/79 ×5, 0 warnings; samples and `Iot.Device.Bindings` build 0/0 (`evidence/024-hygiene-69009f1a.txt`). Hardware: hw-check (a)–(e) ALL PASS @ `69009f1a` (`evidence/023`) |
| Hardware | Pi 4 `pi@raspberrypi.local`, 32-bit userland → publish `-r linux-arm`; BMP390 @ 0x77 bus 1. Default ODR stays 5 ms (verified) |
| Upstream | nothing posted. #2611 **merged** (in upstream main as `c8e4589f`) |
| Blockers | none. Note: builds need the T7 drive mounted (`NUGET_PACKAGES=/Volumes/T7/nuget-packages`) |
| Needs Timothy / desktop | Timothy: say "push" (then record; commit on screen `2950b2d4`); may veto the `SetPowerMode` wait (blocks ≤ ~145 ms). Desktop: reconcile `ISSUES.md` (#2611 merged, row status); update lectures 001/003 for the verified hardware behavior + the new README bullet; lecture 003's "Phase A" ≠ plan's Phase A. No open CHANGE REQUEST (19:45 one withdrawn) |
| Report | [`sessions/003-2026-10-17.md`](sessions/003-2026-10-17.md) |
