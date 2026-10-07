# 02 — Decisions

> Append-only. Timothy is always the decider; whoever was in the room records it.

| # | Date | Decision | Why | Recorded by |
|---|---|---|---|---|
| D1 | 2026-09-30 | Queue #275 behind iot#2328; ask maintainers first | One active PR at a time; issue is old | desktop |
| D2 | 2026-09-30 | Use the desktop + CLI workflow; experiment with interaction modes | Timothy wants to find what works best | desktop |
| D3 | 2026-10-01 | Switch to mode **P** (the default for every new issue). Build first, comment after lecture 1; replaces D1's "ask first, no edits until they answer" | Worked well on iot#2328; a 'no' costs AI time, not Timothy's | desktop |
| D4 | 2026-10-01 | Timothy already has an old clone of his YARP fork with earlier changes in it; the CLI audits it first and brings it to a standard state without losing anything (plan Steps 0–1, O1) | He doesn't know what state it's in | desktop |
| D5 | 2026-10-02 | **G1 opened**: plan v1 approved; Step 1 run (archive branch `archive/pre-275-2026-10` from the fork's `tgrant/exploration-phase`, PR branch `remove-autofac-275` from `upstream/main`) | Step 0 showed nothing at risk locally; old work is notes only, already on GitHub | CLI |

## Pending (not decided)

- **P1** Still wanted? (maintainers)
- **P2** Phase 1 = Autofac only, Moq stays? (proposed; confirm with maintainers)
