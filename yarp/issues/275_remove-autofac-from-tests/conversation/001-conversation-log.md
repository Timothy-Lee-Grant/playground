# dotnet/yarp#275 — Conversation Log (desktop)

> Linear record of Timothy ↔ desktop Claude on this issue. CLI work is in [`../shared/sessions/`](../shared/sessions/).
> Append only; only "Where we are now" and the Index are edited in place. **Started:** 2026-09-30

---

## Where we are now *(updated in place)*

| | |
|---|---|
| **Stage** | PR next (D7, 2026-10-22). Built and verified locally; comment posted 2026-10-09, no reply |
| **Last entry** | #6 (2026-10-22) |
| **Open decisions** | Mention the vacuous `Verify()` in `ForwarderMiddlewareTests.NoDestinations_503`? (lecture 001 §8, Timothy). Upstream: still wanted? Autofac-only phase 1? (asked in the G2 comment) |
| **Next step** | CLI: `/issue 275_remove-autofac-from-tests` → pre-PR check → Timothy pushes and opens the PR (draft text in `shared/pr-description.md`) |

## Index

| # | Date | Type | Title |
|---|---|---|---|
| 1 | 2026-09-30 | 📍 | Folder set up with the AI workflow; briefing lives in scouting |
| 2 | 2026-10-01 | 🧭 | Switched to mode P; plan v1; the old clone gets audited first |
| 3 | 2026-10-07 | 📍 | Moved into the YARP project setup |
| 4 | 2026-10-08 | 📍 | Change built and verified (CLI session 002); lectures written |
| 5 | 2026-10-08 | ❓💬 | Review of the G2 comment draft; Moq left out although the issue names both |
| 6 | 2026-10-22 | 🧭 | New rules: quality PRs per repo etiquette; open the PR now; pre-PR check |

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

## #4 · 2026-10-08 · 📍 Change built and verified (CLI session 002)

- The CLI ran plan Steps 2–10 autonomously after Timothy's "go ahead", then wrote lectures 001 and 002 on request.
  Report: [`../shared/sessions/002-2026-10-07.md`](../shared/sessions/002-2026-10-07.md).
- **Verified** (evidence in `../shared/evidence/`): the 7 test files no longer use AutoMock; `TestAutoMockBase` and
  the Autofac packages are gone (`007`); ReverseProxy.Tests 3,978 passed = baseline; Release build of all 4 test
  projects identical to `main` with 0 warnings (`011`); all 8 break-it proofs went red (`004`–`006`).
- Worth knowing for the comment and the PR: `HttpSysDelegator` is `internal sealed`, so AutoMock had been building a
  **real** one; the rewrite builds it explicitly, so the tests still exercise the same code (`003`).
- Desktop follow-ups done 2026-10-08: test commands corrected in `current_context/03` §3 and `02` §4; the
  `switch -c` denial explained (the deny rule `git -C * -c *` matches it) and the recipe changed to
  `switch --create`.
- Next: Timothy reads lecture 001 → teach-back → posts the comment (G2).

## #5 · 2026-10-08 · ❓ Question → 💬 Answer: is the comment right, and what about Moq?

**❓ Timothy:** shared his own draft of the G2 comment (still wanted? / Autofac-only first step / constructor + explicit
Moq mocks, helper and packages deleted, no product change / counts unchanged, break-it checked / PR if useful, Moq
folder by folder later) and asked: "the issue says both [Autofac and Moq], but we didn't remove Moq."

**💬 Answer (short):** the draft is accurate and the right size. Add one sentence that says openly the issue asks for
both and this covers only the Autofac half, and ask the maintainers what they want for Moq. Leaving Moq out is a
defensible scope choice (Autofac is fully gone in 11 files; Moq is in ~25 files (brief count; re-check with
`git grep -l "using Moq" -- test | wc -l` before posting) and replacing it is a design choice the maintainers
should make), not a gap, as long as it's stated. The PR then says "Contributes to #275", not "Fixes #275", so the
issue stays open. The `Verify()` note, the `HttpSysDelegator` detail and the AI disclosure go in the PR description,
not the comment.

## #6 · 2026-10-22 · 🧭 Decision: open the PR now, after a pre-PR check

- The comment (posted 2026-10-09, https://github.com/dotnet/yarp/issues/275#issuecomment-6073544881) had no reply
  after two weeks. YARP is slow in general: even its automated dependency PRs wait about 3 weeks to be merged.
- Timothy retired the rules he set before his first contributions (understand before posting, one PR at a time,
  wait for maintainers): "I want the priority to be creating high-quality PRs that conform to the standards and
  etiquette of the repo and normal procedure."
- Opening the PR now is normal etiquette: #275 is "help wanted", YARP's CONTRIBUTING lets such work go to a PR
  after a comment, and the change is small and test-only. Closest precedent: MihaZupan's #2828 "Remove
  FluentAssertions".
- Set up: `current_context/procedures/pre-pr.md` (+ `/pre-pr`), the PR text draft `shared/pr-description.md`, D7,
  plan Steps 12–15 replaced. Rules changed in root `CLAUDE.md` §5, `ai-workflow/default-workflow.md`, the YARP
  operating manual §7 and `05-timothy.md`.

