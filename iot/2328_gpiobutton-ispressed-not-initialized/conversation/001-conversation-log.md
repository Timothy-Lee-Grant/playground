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
| **Stage** | Mode P. Plan v1 written, **waiting for G1** (Timothy approves the plan). Nothing posted upstream. |
| **Last entry** | #3 (2026-09-30) |
| **Open decisions** | G1: approve plan v1, including the proposals for U1/U2 (upstream) and O1–O4 (ours) |
| **Next step** | Timothy reads `shared/plan.md` (Stages 1–4), grants G1, posts the comment (G2). Then a CLI session runs Step 0. |

## Index

| # | Date | Type | Title |
|---|---|---|---|
| 1 | 2026-09-30 | 📍 | Folder set up with the AI workflow; briefing lives in scouting |
| 2 | 2026-09-30 | 📍 | First CLI session read: setup works, imports fixed, P3 found |
| 3 | 2026-09-30 | 🧭 | Switch to mode P (plan-driven); plan v1 written |

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

---

## #2 · 2026-09-30 · 📍 First CLI session read (session 001)

Report: [`../shared/sessions/001-2026-09-30.md`](../shared/sessions/001-2026-09-30.md).

- **The setup works:** the fork is cloned with the right remotes, the permissions and `/handoff` work, and the CLI
  writes into `shared/` through the symlink. Baseline: Button tests 8/8 on the Mac (SDK 10.0.302, net8.0),
  `shared/evidence/001`.
- **One defect, fixed:** the `@shared/...` imports in `workspace/CLAUDE.md` didn't load (relative paths through the
  symlink). Now absolute (`@~/Desktop/projects/exercises/...`), with a fallback line telling the CLI to read the
  files itself if they're missing. Same fix applied to yarp#275. Claude Code may ask once to allow imports from
  outside the workspace: approve it. Unconfirmed until the next session.
- **The CLI found something real:** besides *when* to read the pin (P1), there's *in what order* relative to
  registering the callback (P3): the classic "sample the level vs. enable the interrupt" race. Added to brief §5
  and `02` as P3. Good material for the upstream comment.
- **Learning note:** the tour came as one long message with no back-and-forth, and Timothy (focused on setup)
  asked no code questions. `00` now tells the CLI to explain in short chunks with a checkpoint after each.
  Scores were skipped; `/handoff` now treats them as optional.
- WO-1 remains open: steps 4 (PR #2608 diff) and 5 (`MockableGpioDriver`), then the own-words check.

---

## #3 · 2026-09-30 · 🧭 Decision: mode P (plan-driven, Timothy observes)

**Timothy:** the steering modes put him in charge of decisions he isn't confident making yet. Save them for later;
for now the AI drives, and he learns from a working example afterwards, as with the staged implementation plans on
his own projects (he shared `003-ToolBox_Integration_And_Hosted_LLM_Migration.md` as the model).

**Agreed design** (desktop proposal, accepted):
- Desktop writes the plan; the CLI reviews it against the code first, then implements with deviation rules
  (details: decide and record; scope: CHANGE REQUEST; behavior/API/ours/upstream decisions: stop and lay out options).
- Gates only Timothy opens: **G1** plan approved, **G2** before anything public, **G3** after his teach-back. No
  per-step grants in between.
- Changes from his old format for open source: decisions are tagged ours vs. upstream; communication is planned
  (AI drafts, Timothy posts); the **understanding stage comes before the PR**; review reopens the plan.
- The public, hands-on actions stay his: post comments, run the final tests, push, open the PR, answer review.

Written: [`../shared/plan.md`](../shared/plan.md) v1; `00-start-here.md` §6 (mode P rules); `04` (current mode P);
`03-next.md` marked superseded; `/handoff` and the workspace `CLAUDE.md` updated;
[`ai-workflow/interaction-modes.md`](../../../ai-workflow/interaction-modes.md) gained mode P.
