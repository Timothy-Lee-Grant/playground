# The default way to work a new issue (mode P)

> **Status:** the default for **every new code issue**, set by Timothy on 2026-10-01 after dotnet/iot#2328
> ("I was able to take a more backseat role, see what was done and why, and understand everything before I
> committed to anything"). Desktop Claude follows this without being asked. The steering modes (M0–M4 in
> [`interaction-modes.md`](interaction-modes.md)) are used only when **Timothy** asks for one.
>
> **Reference run:** [`../iot/2328_gpiobutton-ispressed-not-initialized/`](../iot/2328_gpiobutton-ispressed-not-initialized/)
> (plan, lecture 001, teach-back 002, comment). When in doubt, do what was done there.

---

## 1. The idea in four lines

1. **The AI drives:** desktop Claude plans, the CLI checks the plan against the code and builds the whole change
   locally, recording the *why* of every step.
2. **Timothy watches, then learns the finished change** through one lecture ("the change, end to end") and a
   teach-back. Understanding comes **before** anything public.
3. **The public actions stay his:** posting the comment, running the final tests, pushing, opening the PR,
   answering review. Nothing is posted or pushed that he can't explain line by line.
4. **Three gates, only Timothy opens them:** **G1** plan approved → code may start · **G2** before anything is
   posted upstream · **G3** after his teach-back → PR may be opened.

## 2. The sequence

| # | Step | Who | What Timothy does | Output |
|---|---|---|---|---|
| 1 | **Pick** the issue (scouting); re-check the thread | desktop + Timothy | chooses | scouting tracker row |
| 2 | **Scaffold** the folder: `bash ai-workflow/new-issue.sh <owner> <repo> <issue#> <slug> "<title>"` | desktop | — | issue folder, already in mode P |
| 3 | **Briefing**: the issue, the cast, control flow, findings, fit check | desktop | reads if he wants; asks questions | `conversation/` entry #1, `shared/01-brief.md` |
| 4 | **Plan** Stages 1–3: direction; decisions tagged *ours*/*upstream* with proposals; the step table | desktop | reads it, asks, **grants G1** (may reorder) | `shared/plan.md` |
| 5 | **Fork + workspace**: fork on GitHub, run `workspace/setup.sh`. **Already have an old clone?** Put it at `develop/<repo>`; setup leaves it alone, and the plan gets a Step 0 "audit the clone read-only" + Step 1 "bring it to the standard state, old work kept on an `archive/` branch" (model: yarp#275's plan) | **Timothy** | two commands | CLI workspace |
| 6 | **Build**: CLI does Step 0 (plan vs. code), harness, red tests, fix, hygiene; commits with his OK | CLI | watches or leaves it running; approves commits; `/handoff` at the end | Stage 5 entries, `evidence/` |
| 7 | **Implementation summary + diff**: every line by purpose, each why and rejected alternative, what each test proves and doesn't | CLI | — | Stage 5 entry, `evidence/NNN-diff.patch` |
| 8 | **Review + lecture 1, "The change, end to end"** (spec in the plan template, Stage 6), incl. the comment sentence by sentence; audio version only if he asks | desktop | **reads it**, asks questions | `lectures/001-…md` |
| 9 | **Comment** drafted by desktop; Timothy edits it into his own words and posts (**G2**) | **Timothy** | posts | `[Timothy]` entry with the link |
| 10 | **Teach-back** (voice on a walk is fine); analyzed per root `CLAUDE.md` §10.4 (**G3**) | **Timothy** talks; desktop analyzes | talks | `private/teachbacks/` |
| 11 | **PR**: CLI drafts the description, desktop reviews; Timothy runs the final tests, pushes, opens the PR | **Timothy** acts | acts | PR link |
| 12 | **Review loop**: each review comment becomes a plan step; AI drafts code and replies; Timothy posts (G2 per reply) | all | posts | Stage 8 entries |
| 13 | **Reflect** (root `CLAUDE.md` §9.3) | desktop | — | persona, pattern catalog, learner model |

Waiting on maintainers is normal: the code stays local, and if they choose differently from our default, that's a
plan revision (the tests' structure stays; the expected behavior changes).

## 3. Rules that make it work (learned on iot#2328)

- **Build first, comment after understanding.** He won't publicly commit to work he can't yet explain or deliver.
  Code is cheap to redo; a public commitment isn't. (This overrides the generic "comment before substantial work"
  in root `CLAUDE.md` §5 for mode-P issues; if someone else may be about to take the issue, tell Timothy so he can
  decide whether to post a short "looking into this" note early.)
- **Build on the most likely upstream answer**, and tag every decision *ours* or *upstream*. The CLI never decides
  an upstream one; it builds the proposed default so switching later is cheap.
- **The CLI's "why" entries are the lecture's raw material.** Without them the lecture can only describe *what*
  changed. Desktop checks for them at every review.
- **One lecture first, not a stack.** Lecture 1 covers the code, the tests, and the comment. Concept lectures only
  if lecture 1 or the teach-back shows a gap. Never assume he has read anything (§10.2).
- **Don't ask him for session scores at `/handoff`.** Record them only if he volunteers them.

## 4. Moving to the more hands-on modes (later)

Timothy decides when; nobody switches him. Desktop may **suggest** trying one step in a steering mode when the
evidence supports it, e.g. teach-backs that explain the diff and its *why* without gaps on two issues in a row, or
him proposing design choices before the plan does. A gentle first mix: mode P overall, but **M1 for writing the
tests** (he types, the CLI navigates). Record any switch in the issue's `04-interaction-mode.md` and in
[`interaction-modes.md`](interaction-modes.md) §4.

## 5. Changelog

| Date | Change |
|---|---|
| 2026-10-01 | v1: written from the iot#2328 run at Timothy's request; templates in `templates/issue/` and `new-issue.sh` |
