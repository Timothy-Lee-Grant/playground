# dotnet/yarp#275 — Conversation Log (desktop)

> Linear record of Timothy ↔ desktop Claude on this issue. CLI work is in [`../shared/sessions/`](../shared/sessions/).
> Append only; only "Where we are now" and the Index are edited in place. **Started:** 2026-09-30

---

## Where we are now *(updated in place)*

| | |
|---|---|
| **Stage** | Mode P; plan v1 written, waiting for G1. PR queued behind iot#2328's PR. |
| **Last entry** | #2 (2026-10-01) |
| **Open decisions** | G1 (Timothy). Upstream: still wanted? scope? style? (asked in the comment after lecture 1) |
| **Next step** | Timothy reads `shared/plan.md` and grants G1. Then `workspace/setup.sh` (keeps his old clone) and `claude` in `~/Desktop/projects/oss-work/yarp-275/`: the CLI starts with Step 0, the fork audit. |

## Index

| # | Date | Type | Title |
|---|---|---|---|
| 1 | 2026-09-30 | 📍 | Folder set up with the AI workflow; briefing lives in scouting |
| 2 | 2026-10-01 | 🧭 | Switched to mode P; plan v1; the old clone gets audited first |

---

## #1 · 2026-09-30 · 📍 Folder set up

- In-depth briefing: [scouting conversation #1 §2](../../../../scouting/conversation/001-conversation-log.md).
- Second issue on the desktop + CLI workflow. Its main work (rewriting 7 test files) is repetitive, which makes it
  a natural place to try **M3 (Delegate + review)** and compare it with the M1/M2 sessions on iot#2328
  ([`ai-workflow/interaction-modes.md`](../../../../ai-workflow/interaction-modes.md)).

## #2 · 2026-10-01 · 🧭 Decision: mode P, and the old clone gets audited first

- Timothy made mode P the default for every new issue after iot#2328 and asked for #275 to follow it too (D3). The
  M3-comparison idea in #1 is dropped for now.
- He already forked and cloned YARP long ago and made some changes there; he doesn't know what state it's in (D4).
  Plan Step 0 audits the clone read-only and describes his old changes; Step 1 (on his "yes") keeps all of it on
  `archive/pre-275-2026-10`, resets `main` to `upstream/main`, fixes the remotes, and starts `remove-autofac-275`.
- `workspace/setup.sh` won't clone over it: it leaves an existing `develop/yarp` alone, and stops if it finds a
  repo elsewhere under `develop/`.
- Plan: [`../shared/plan.md`](../shared/plan.md) v1. Next: G1.

## #3 · 2026-10-07 · 📍 Moved into the YARP project setup

- Timothy redesigned the CLI setup around his new SSD: one YARP project folder with static entry files, one `link`
  symlink to `exercises/yarp/`, and a dynamic `current_context/` (operating manual, layout, branch registry, his
  profile, YARP competency, lecture specs, procedures, git hooks). This folder moved to `yarp/issues/`.
- The old per-issue workspace (`workspace/`, `oss-work/yarp-275/`) is retired; the branch gets recreated in the new
  clone; G1 stands. Details: plan Stage 3 discussion, `[Desktop — 2026_10_07_07_30]`.

