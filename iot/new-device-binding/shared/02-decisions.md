# 02 — Decisions

> Append-only. Timothy is always the decider; whoever was in the room records it. Never edit an old entry; a
> change of mind is a new entry that references the old one.

| # | Date | Decision | Why | Recorded by |
|---|---|---|---|---|
| D1 | 2026-10-05 | Work in mode **P** (the default since 2026-10-01): AI plans and builds, Timothy learns the finished change through lectures + teach-back before anything public; gates G1–G3 | Worked well on iot#2328; not yet confident making design calls; M0–M4 saved for later | desktop |
| D2 | 2026-10-05 | Device: **Bosch BMP390 + BMP388** (`src/devices/Bmp3xx/`), per the investigation in the issue folder (`002-choosing-the-device.md`). Timothy asked for the implementation blueprint for it; the device choice is confirmed at G1 | Popular, missing upstream, cheap, highly unit-testable; sibling `Bmp280` to mirror | desktop |
| D3 | 2026-10-05 | **Shared-clone project layout**: one clone at `~/Desktop/projects/open_source/iot_project/iot/`, one mailbox link per issue, CLI told which issue at start and switches to its branch (`/issue`). Config in `exercises/iot/cli-project/`. #2328 stays in its old workspace, untouched | No re-cloning per issue; one place for the code | Timothy (recorded by desktop) |
| D4 | 2026-10-05 | **Push `feature/bmp3xx-binding` to Timothy's fork (`origin`)** with his OK; never to `upstream`, never force, no PR or issue until he's ready (G2/G3) | Back up the work and browse it on GitHub without announcing anything | Timothy (recorded by desktop) |
