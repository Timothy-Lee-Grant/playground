# 02 — Decisions

> Append-only. Timothy is always the decider; whoever was in the room records it. Never edit an old entry; a
> change of mind is a new entry that references the old one.

| # | Date | Decision | Why | Recorded by |
|---|---|---|---|---|
| D1 | 2026-09-30 | Work on #2328 as the first code PR, target PR opened by Sun 2026-10-04 | Right size, any machine, uses hardware instincts (scouting 002 §5) | desktop |
| D2 | 2026-09-30 | Use the desktop + CLI workflow; experiment with interaction modes | Timothy wants to find what works best | desktop |
| D3 | 2026-09-30 | Switch to mode **P** (plan-driven; Timothy observes, learns via lectures + teach-back before the PR). Desktop plans, CLI implements within deviation rules; gates G1–G3. M0–M4 saved for later | Not yet confident making the design calls; wants a working example to learn from, while keeping the public actions his own | desktop |
| D4 | 2026-10-01 | **G1 granted.** Build the whole change tonight on the Stage 2 defaults; post the upstream comment only after lecture 1, once he understands it | Won't commit publicly to what he can't explain; code isn't the bottleneck, and re-implementing is cheap if maintainers differ | desktop |
| D5 | 2026-10-01 | **G2 opened: comment posted** on #2328 (his own wording of v3). Keep code local until the PR (push after G3) | He understood every sentence after lecture 001 and TB 002; posting before pushing keeps the order right | desktop |

## Pending (not decided)

- **P1** First pin reading: (a) immediate / (b) settle delay / (c) lazy with #1715 (brief §5). Wait for maintainers.
- **P2** Base branch: on top of PR #2608, or on `main` and rebase later (brief §6).
- **P3** Order of the initial read vs. callback registration (brief §5, second question; raised by the CLI, session 001).
- P1–P3 now live in `plan.md` Stage 2 as U1, O1, U2 with proposals; Timothy grants them with **G1**.
