# 02 — Decisions

> Append-only. Timothy is always the decider; whoever was in the room records it. Never edit an old entry; a
> change of mind is a new entry that references the old one.

| # | Date | Decision | Why | Recorded by |
|---|---|---|---|---|
| D1 | 2026-10-05 | Work in mode **P** (the default since 2026-10-01): AI plans and builds, Timothy learns the finished change through lectures + teach-back before anything public; gates G1–G3 | Worked well on iot#2328; not yet confident making design calls; M0–M4 saved for later | desktop |
| D2 | 2026-10-05 | Device: **Bosch BMP390 + BMP388** (`src/devices/Bmp3xx/`), per the investigation in the issue folder (`002-choosing-the-device.md`). Timothy asked for the implementation blueprint for it; the device choice is confirmed at G1 | Popular, missing upstream, cheap, highly unit-testable; sibling `Bmp280` to mirror | desktop |
| D3 | 2026-10-05 | **Shared-clone project layout**: one clone at `~/Desktop/projects/open_source/iot_project/iot/`, one mailbox link per issue, CLI told which issue at start and switches to its branch (`/issue`). Config in `exercises/iot/cli-project/`. #2328 stays in its old workspace, untouched | No re-cloning per issue; one place for the code | Timothy (recorded by desktop) |
| D4 | 2026-10-05 | **Push `feature/bmp3xx-binding` to Timothy's fork (`origin`)** with his OK; never to `upstream`, never force, no PR or issue until he's ready (G2/G3) | Back up the work and browse it on GitHub without announcing anything | Timothy (recorded by desktop) |
| D5 | 2026-10-05 | **G1 granted**: plan v2 approved as written (all Stage 2 proposals O1–O9 and U1–U8 accepted as proposed); implementation starts at Step 0 | Said "G1 approved" at the start of the first CLI session | Timothy (recorded by CLI) |
| D6 | 2026-10-05 | `Bmp3xxOutputDataRate` values are named by **sampling period in milliseconds**, unit spelled out: `Period5Milliseconds` (200 Hz) … `Period655360Milliseconds` (655.36 s). Replaces the `Hz200 … Hz12_5` names proposed in O7 | Exact for all 18 values, one unit, no abbreviation (`src/devices/README.md`); frequency names become long fractions below 12.5 Hz. Alternatives offered: sibling `Ms…` style, O7 frequency style, ms-then-seconds | Timothy (recorded by CLI) |
