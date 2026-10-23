# 02 — Decisions

> Append-only. Timothy is always the decider; whoever was in the room records it.

| # | Date | Decision | Why | Recorded by |
|---|---|---|---|---|
| D1 | 2026-09-30 | Queue #275 behind iot#2328; ask maintainers first | One active PR at a time; issue is old | desktop |
| D2 | 2026-09-30 | Use the desktop + CLI workflow; experiment with interaction modes | Timothy wants to find what works best | desktop |
| D3 | 2026-10-01 | Switch to mode **P** (the default for every new issue). Build first, comment after lecture 1; replaces D1's "ask first, no edits until they answer" | Worked well on iot#2328; a 'no' costs AI time, not Timothy's | desktop |
| D4 | 2026-10-01 | Timothy already has an old clone of his YARP fork with earlier changes in it; the CLI audits it first and brings it to a standard state without losing anything (plan Steps 0–1, O1) | He doesn't know what state it's in | desktop |
| D5 | 2026-10-02 | **G1 opened**: plan v1 approved; Step 1 run (archive branch `archive/pre-275-2026-10` from the fork's `tgrant/exploration-phase`, PR branch `remove-autofac-275` from `upstream/main`) | Step 0 showed nothing at risk locally; old work is notes only, already on GitHub | CLI |
| D6 | 2026-10-07 | "There is no unpushed code on my other yarp clone." "Go ahead": recreate `remove-autofac-275` in the new SSD clone from `upstream/main` (`2aa3d835`) and run plan Steps 2–10 autonomously | Step 1 is lossless; the old branch had no commits; the run starts from today's upstream code | CLI |
| D7 | 2026-10-22 | **Open the PR now; no wait for a maintainer reply.** Timothy retired his pre-contribution rules; priority is a high-quality PR per YARP's standards and etiquette. #275 is "help wanted", his comment (2026-10-09) has had no objection in two weeks, and the change is small and test-only. G3 = the pre-PR check passes + his go. Title/body: `pr-description.md` | YARP's CONTRIBUTING lets help-wanted / uncontroversial work go to a PR; the comment already states scope (Autofac only) | desktop |

## Pending (not decided)

- **P1** Still wanted? (maintainers; no reply as of 2026-10-22, the PR asks the same question in practice)
- **P2** Phase 1 = Autofac only, Moq stays? (proposed; confirm with maintainers)
