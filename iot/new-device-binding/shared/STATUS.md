# STATUS (rewritten by the CLI at every /handoff)

| | |
|---|---|
| Date | 2026-10-19 (CLI session 004) |
| Mode | P · G1 granted (D5); PR 2627 open upstream (opened 2026-10-18), in review round 1 |
| Plan step | Stage 8, work order **RW1** (D10): RW1.1 R3, RW1.2 R2, RW1.3 R1 fixed test-first; RW1.4 verify done (hygiene + Pi ALL PASS); RW1.5 drafts written. **"Done when" met**, except the push, which waits for Timothy's word |
| Branch / last commit | `feature/bmp3xx-binding` · `be4c13c3 Document that Read discards unread data and needs no forced mode first` |
| Pushed | **No: ahead of origin by 4** (`32d75c35`, `574026e0`, `9ac45d2f`, `be4c13c3`); origin @ `fc7d7b57` = PR head. `upstream/main` not re-fetched this session (0 behind at last fetch). Clone clean |
| Tests | **89/89 ×5**, 0 warnings; samples and `Iot.Device.Bindings` 0/0; scope only `src/devices/Bmp3xx/` (`evidence/032-rw1-hygiene.txt` @ `9ac45d2f`; `be4c13c3` is README-only). Hardware: hw-check (a)–(g) ALL PASS, (g0) 3/30 lost with the old wait (`evidence/033` = `sample/evidence/007`) |
| Hardware | Pi 4 `pi@raspberrypi.local`, 32-bit userland → publish `-r linux-arm`; BMP390 @ 0x77 bus 1; hw-check at `~/hw-check` on the Pi |
| Upstream | PR 2627 open; 3 Copilot threads (R1–R3) unanswered; nothing posted this session. CI: earlier Windows Release red = Tca955x flake, not ours (2026-10-18 entry) |
| Blockers | none. Builds need the T7 drive mounted (`NUGET_PACKAGES=/Volumes/T7/nuget-packages`) |
| Needs Timothy / desktop | **Timothy:** say "push"; then rewrite + post replies from `../pr/002-review-round-1-replies.md` (3 threads + top-level), edit the PR description (part 3: choice 3, new choice 7, 89 tests); optionally copy `sample/evidence/007` to `playground`. **Desktop:** review the 4 commits and drafts before posting; lectures for R1–R3 + D11. No open CHANGE REQUEST |
| Report | [`sessions/004-2026-10-19.md`](sessions/004-2026-10-19.md) |
