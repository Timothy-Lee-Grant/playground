# dotnet/iot#2328 — Conversation Log (desktop)

> **What this is:** the linear record of Timothy ↔ desktop Claude on this issue: questions, answers, decisions.
> What the CLI did lives in [`../shared/sessions/`](../shared/sessions/); this log links to those reports rather than
> repeating them. Append only; only "Where we are now" and the Index are edited in place.
>
> **Entry types:** 📍 Progress · ❓ Question → 💬 Answer · 🧭 Decision · 🔁 Correction · **Started:** 2026-09-30

---

## Where we are now *(updated in place)*

| | |
|---|---|
| **Stage** | Set up. Nothing posted upstream; no code yet. |
| **Last entry** | #1 (2026-09-30) |
| **Open decisions** | D-open-1: how to take the first pin reading (brief §5). Needs maintainer input. |
| **Next step** | Timothy posts the upstream comment; forks dotnet/iot; runs `workspace/setup.sh`; starts the CLI on WO-1. |

## Index

| # | Date | Type | Title |
|---|---|---|---|
| 1 | 2026-09-30 | 📍 | Folder set up with the AI workflow; briefing lives in scouting |

---

## #1 · 2026-09-30 · 📍 Folder set up

- The in-depth briefing for this issue is
  [scouting conversation #1 §1](../../../scouting/conversation/001-conversation-log.md). It isn't repeated here.
- This is the first issue using the desktop + CLI workflow ([`ai-workflow/README.md`](../../../ai-workflow/README.md)).
  Timothy wants to experiment with *how* to work with the CLI; the modes are in
  [`ai-workflow/interaction-modes.md`](../../../ai-workflow/interaction-modes.md). WO-1 starts in **M0 (Tutor)**
  because it's orientation: build, run the existing tests, read the code.
- Timothy's to-dos before the first CLI session: (1) post the comment on #2328 (draft in the brief §8);
  (2) fork dotnet/iot on GitHub; (3) `bash workspace/setup.sh`.
