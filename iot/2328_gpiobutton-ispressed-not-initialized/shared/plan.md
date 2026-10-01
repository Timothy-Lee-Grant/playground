2026_09_30_22_33 — dotnet/iot#2328: initialize `GpioButton.IsPressed` from the pin

> **The living plan for mode P (plan-driven, Timothy observes).** One document, read top to bottom.
>
> **Who writes what:** the plan body (Stages 1–3) is owned by **desktop** and revised in place with a version
> number (v1, v2, …; old versions summarized, not deleted). **Entries** are append-only and tagged
> `[Author — YYYY_MM_DD_HH_MM]`: `[Timothy]` (recorded by whichever Claude he said it to), `[Desktop]`, `[CLI]`.
> Nobody edits someone else's entry. The CLI writes Stage 5 entries and change requests; desktop writes the rest.
>
> **Gates** (only Timothy opens them): **G1** plan approved → implementation may start · **G2** before anything is
> posted upstream · **G3** after the teach-back → PR may be opened. Between gates the CLI works autonomously.
> Deviation rules: `00-start-here.md` §6.

---

# Stage 1 (Direction)

**[Desktop — 2026_09_30_22_33] Direction, from Timothy's decisions on 2026-09-29/30**

1. **Goal:** Timothy's first upstream *code* PR, opened by **Sunday 2026-10-04**. Merge timing is the maintainers'.
2. **Issue:** dotnet/iot#2328. A button already held at startup is reported as released, and with debounce on its
   first release is swallowed (brief §3). Fix scope: `GpioButton` learns the pin's level at construction.
3. **How we work (mode P):** Timothy isn't yet confident making the design and implementation calls, so the AI
   drives: desktop plans, the CLI checks the plan against the code and implements it. Timothy watches, then learns
   the finished change through lectures and a teach-back **before** the PR exists (his rule: never submit code he
   can't explain line by line). The hands-on public actions stay his: posting comments, running the final tests,
   pushing, opening the PR, answering review.
4. **What success looks like:** a small, conventional PR that a maintainer can review in minutes, with tests
   that fail before the fix and pass after; and a Timothy who can explain every line and every choice in it.
5. **Not in scope:** #1715 (startup `Press` event), the `GpioButton.cs` double-assignment nit, anything in PR
   #2608's territory (the time source).

# Stage 2 (Discussion)

**[Desktop — 2026_09_30_22_33] Opening entry: the decisions, and whose they are**

Every decision is tagged **ours** (we decide, Timothy grants at G1) or **upstream** (the maintainers decide; we
propose and build so it's cheap to change).

| ID | Decision | Owner | Proposal | Why |
|---|---|---|---|---|
| U1 (= P1) | **When** to take the first reading: (a) immediately, (b) after a settle delay, (c) lazily on first subscription (#1715's fix) | **upstream** | Ask in the comment. Build (a) as the default | (a) is the smallest change; (b) and (c) are easy to switch to later because the tests pin *behavior*, not timing |
| U2 (= P3) | **Order** of the reading vs. registering the edge callback | **upstream** | Ask. Build **register, then read** as the default | Firmware rule: enable the interrupt, then sample the level. Read-then-register can permanently miss an edge between the two calls (state wrong until the next edge). Register-then-read can overlap with a callback, but both end at the level that was actually read. The CLI checks this in Step 0 |
| O1 (= P2) | Base branch | ours | Branch from `upstream/main`; rebase after #2608 merges | #2608 has no review yet and may change; basing on an unmerged PR ties our PR to its fate. Expected conflict is small (#2608 edits the ctor's *defaults*, we add lines after `OpenPin`) |
| O2 | How to test without hardware | ours | Copy the in-repo `MockableGpioDriver` + Moq pattern (`src/devices/Ili934x/tests/`) into the Button tests | Precedent maintainers already accept. Step 0 checks whether a better in-repo fake exists (e.g. the virtual GPIO controller) |
| O3 | Scope of the change | ours | `GpioButton` constructor + tests + `IsPressed` XML doc remark. Nothing in `ButtonBase` logic | Smallest diff that fixes the issue; keeps clear of #2608 |
| O4 | Commits | ours | Two commits: (1) tests that fail, (2) the fix | Lets a reviewer check out commit 1 and see the bug proven |

# Stage 3 (Implementation Planning)

**[Desktop — 2026_09_30_22_33] Implementation Plan v1**

Ordering: check the plan against reality first, then the public question, then local work (which doesn't wait for
a maintainer reply), then understanding, then the PR.

| Step | What | Who | Proof | Gate |
|---|---|---|---|---|
| **0** | **Plan review against the code** (no edits): confirm brief §3 paths and line numbers; read PR #2608's diff for `src/devices/Button` and list exact collisions; evaluate O2 (Mockable driver vs. any in-repo virtual/fake controller); sanity-check U2's reasoning in the code (which thread callbacks run on, whether `IsPressed` writes race). Write one Stage 5 entry: "plan holds" or change requests | CLI | Stage 5 entry | — |
| **1** | **Post the upstream comment** (draft in Stage 4) | **Timothy** posts; desktop drafted | Link to the comment in a `[Timothy]` entry | **G2** |
| **2** | Branch `fix/2328-gpiobutton-initial-state` from `upstream/main`. Add the mock driver to `src/devices/Button/tests/` and a helper that builds a `GpioButton` over it. One smoke test (construct + dispose) | CLI | `evidence/002-harness-smoke.txt` | G1 must be open |
| **3** | **Failing tests** (red): pull-up + Low → pressed; pull-up + High → not pressed; pull-down mirror (High → pressed); external resistor (pull-up wiring, `hasExternalResistor: true`); held at startup + debounce on → releasing raises `ButtonUp` and `Press` | CLI | `evidence/003-red.txt`: each new test fails **for the stated reason** (assertion on `IsPressed` / missing event), not a crash | — |
| **4** | **The fix** in the `GpioButton` constructor, per U1/U2 defaults: after registering the callback, read the pin once and set `IsPressed` from the active level, raising no events. XML-doc remark on `IsPressed` | CLI | `evidence/004-green.txt`: all Button tests pass (old 8 + new) | — |
| **5** | **Hygiene:** build with no new warnings (StyleCop/analyzers); run the Button tests 5× to check for flakiness; diff is only the intended files | CLI | `evidence/005-hygiene.txt` + `git diff --stat` | — |
| **6** | **Commits** per O4 (CLI asks; settings require approval) | CLI proposes, Timothy approves | `git log --oneline` in the entry | — |
| **7** | **Implementation summary entry:** every changed line grouped by purpose; the "why" of each choice and the alternatives rejected; open questions. This is the raw material for the lectures | CLI | Stage 5 entry | — |
| **8** | **Lectures + teach-back** (Stage 6) | Desktop writes; **Timothy** reads, then teaches back | Teach-back analysis | **G3** |
| **9** | **Contribution** (Stage 7): Timothy runs the final test command himself, pushes the branch, opens the PR. CLI drafts the description; desktop reviews it | **Timothy** acts; AI drafts | PR link | after G3 |
| **10** | **Review** (Stage 8): each review comment becomes a step here; the AI drafts code and replies, Timothy posts | all | — | G2 applies to each reply |

**Timothy's own work, per step:** Step 1 (post), Step 6 (approve commits), Step 8 (read, teach back), Step 9
(test, push, open PR), Step 10 (post replies). Everything else he may watch or skip.

**Acceptance criteria**

1. New tests fail on `upstream/main` and pass with the fix; the 8 existing Button tests still pass.
2. The diff touches only `src/devices/Button/GpioButton.cs`, `ButtonBase.cs` (doc remark only, if needed) and
   files under `src/devices/Button/tests/`. No public API added or removed.
3. No new build warnings. Tests stable across 5 runs.
4. PR description: first line `Fixes #2328`; the problem (including the swallowed release); the U1/U2 defaults
   stated as open questions; verified environment; brief AI-assistance disclosure.
5. Timothy passes his own teach-back on the change (G3) before the PR is opened.

**Risks**

| Risk | If it happens |
|---|---|
| A maintainer prefers (b) or (c), or the other order | A plan revision: change one step and the expected values; the tests' structure stays |
| pgrawehr folds the fix into #2608 | Switch to 📖 learn mode; review his version against ours. Still a win for understanding |
| #2608 merges first and conflicts | Rebase (a learning moment for Timothy; desktop explains it) |
| No maintainer reply by Sunday | Open the PR anyway: we asked first, and the description states the defaults as questions |
| Understanding isn't ready by Sunday | **The PR slips.** Understanding beats the deadline (Timothy's rule) |
| The mock pattern doesn't fit `GpioButton` | Step 0 catches it; change request |

**Timeline:** Thu 10/1: Step 0, Timothy posts the comment, Steps 2–4. Fri 10/2: Steps 5–7, lectures start.
Sat 10/3: lectures, teach-back on a walk. Sun 10/4: G3, Step 9.

### Stage 3 Discussion Subsection

*(Questions about the plan, and the G1 grant, go here.)*

# Stage 4 (Upstream Communication)

**[Desktop — 2026_09_30_22_33] Draft comment for #2328 (Timothy posts in his own voice after G2)**

> I'd like to pick this up if it's still wanted. On `main`, `GpioButton` never reads the pin's current level, so
> `IsPressed` stays `false` until the first edge. It's more than cosmetic: with debounce enabled,
> `HandleButtonReleased` returns early when `!IsPressed`, so the first release of a button held at startup is
> swallowed (no `ButtonUp`/`Press`).
>
> My plan: read the pin once in the constructor and set `IsPressed` from the active level (Low for pull-up, High for
> pull-down), without raising events, plus unit tests with a mockable `GpioDriver`. Two questions first:
> 1. **Timing:** #1715 says the pull-up may not have settled right after `OpenPin`. Is an immediate read acceptable,
>    or would you prefer a short settle delay, or reading lazily on first subscription as #1715's agreed fix suggests?
> 2. **Ordering:** read before registering the edge callback (can miss an edge in between) or after (the callback
>    can race the constructor)? I'd lean towards registering first, then reading.
>
> I see @pgrawehr's #2608 touches the same files; I'll keep my change separate and rebase once it's in.
> @raffaeler, OK for me to take this?

# Stage 5 (Implementation)

*(CLI entries go here, one per step or per meaningful finding, each with: what changed, deviations, evidence,
and **why**. Change requests are marked **CHANGE REQUEST** and stop work on the affected steps until Timothy
grants or declines them.)*

# Stage 6 (Understanding)

*(Desktop: lecture list and links; Timothy: reading confirmations and teach-back; G3.)*

# Stage 7 (Contribution)

*(PR description draft; Timothy's final test run, push, PR link.)*

# Stage 8 (Review)

*(One entry per review comment and its resolution.)*
