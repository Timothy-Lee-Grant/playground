# STATUS (rewritten by the CLI at every /handoff)

| | |
|---|---|
| Date | 2026-10-22 (set up by desktop; no CLI session yet) |
| Mode | P · **G1 not yet given** (Timothy says "go" at the start) |
| Plan step | Step 1 next (branch + baseline) |
| Branch / last commit | `fix/bmp3xx-normal-mode-followup` — **not created yet** (Step 1 creates it from `upstream/main` ≥ `4108607`) |
| Pushed | — |
| Tests | baseline expected 89/89 (as merged) |
| Hardware | Pi 4 `pi@raspberrypi.local`, 32-bit userland → publish `-r linux-arm`; BMP390 @ 0x77 bus 1 |
| Upstream | PR 2627 merged 2026-10-22 (`4108607`). Follow-up PR not opened |
| Blockers | none. Builds need the T7 drive mounted (`NUGET_PACKAGES=/Volumes/T7/nuget-packages`) |
| Needs Timothy / desktop | Timothy: run setup.sh once (creates the `bmp3xx-followup` link), then `/issue bmp3xx-followup` and say "go" |
| Report | — |
