{{DATE}} — {{OWNER}}/{{REPO}}#{{ISSUE}}: {{TITLE}}

> **The living plan for mode P (plan-driven, Timothy observes).** One document, read top to bottom.
>
> **Who writes what:** the plan body (Stages 1–3) is owned by **desktop** and revised in place with a version
> number (v1, v2, …; old versions summarized, not deleted). **Entries** are append-only and tagged
> `[Author — YYYY_MM_DD_HH_MM]`: `[Timothy]` (recorded by whichever Claude he said it to), `[Desktop]`, `[CLI]`.
> Nobody edits someone else's entry. The CLI writes Stage 5 entries and change requests; desktop writes the rest.
>
> **Gates** (only Timothy opens them): **G1** plan approved → implementation may start · **G2** before anything is
> posted upstream · **G3** after the teach-back → PR may be opened. Between gates the CLI works autonomously.
> Deviation rules: `00-start-here.md` §6. The standard sequence is `exercises/ai-workflow/default-workflow.md`.

---

# Stage 1 (Direction)

*(Desktop: goal, the issue in two sentences, fix scope, what success looks like, not in scope.)*

# Stage 2 (Discussion)

*(Desktop: every decision in a table, tagged **ours** (Timothy grants at G1) or **upstream** (maintainers decide;
we build the most likely default so it's cheap to change), each with a proposal and the why.)*

| ID | Decision | Owner | Proposal | Why |
|---|---|---|---|---|

# Stage 3 (Implementation Planning)

*(Desktop: Implementation Plan v1. Default step shape, adjust per issue:)*

| Step | What | Who | Proof | Gate |
|---|---|---|---|---|
| **0** | Plan review against the code (no edits): paths, line numbers, competing PRs, test approach. One Stage 5 entry: "plan holds" or change requests | CLI | Stage 5 entry | G1 |
| **1** | Branch from `upstream/main`; test harness + smoke test | CLI | `evidence/NNN-harness-smoke.txt` | G1 |
| **2** | Failing tests (red), each failing **for the stated reason** | CLI | `evidence/NNN-red.txt` | — |
| **3** | The fix (green); all old tests still pass | CLI | `evidence/NNN-green.txt` | — |
| **4** | Hygiene: no new warnings, repeated runs for flakiness, diff only the intended files | CLI | `evidence/NNN-hygiene.txt` | — |
| **5** | Commits (tests first, then fix), Timothy approves | CLI proposes, Timothy approves | `git log --oneline` | — |
| **6** | Implementation summary: every changed line by purpose, each choice's why and rejected alternatives, what each test proves and doesn't; full diff saved | CLI | Stage 5 entry + `evidence/NNN-diff.patch` | — |
| **7** | Lecture 1 "The change, end to end" (Stage 6 spec), incl. the upstream comment explained sentence by sentence | Desktop writes; Timothy reads | lecture link | — |
| **8** | Timothy posts the comment in his own words | **Timothy** | link in a `[Timothy]` entry | **G2** |
| **9** | Teach-back (voice on a walk is fine), analyzed by desktop | **Timothy** talks; desktop analyzes | `private/teachbacks/` | **G3** |
| **10** | Timothy runs the final tests, pushes, opens the PR (CLI drafts the description; desktop reviews it) | **Timothy** acts; AI drafts | PR link | after G3 |
| **11** | Review: each review comment becomes a step; AI drafts code and replies, Timothy posts | all | — | G2 per reply |

### Stage 3 Discussion Subsection

*(Questions about the plan, and the G1 grant, go here.)*

# Stage 4 (Upstream Communication)

*(Desktop: draft comment(s), versioned. Timothy posts in his own voice after G2.)*

# Stage 5 (Implementation)

*(CLI entries go here, one per step or per meaningful finding, each with: what changed, deviations, evidence,
and **why**. Change requests are marked **CHANGE REQUEST** and stop work on the affected steps until Timothy
grants or declines them.)*

# Stage 6 (Understanding)

*(Desktop: lecture spec and links; Timothy: reading confirmations and teach-back; G3.)*

**Default lecture 1 spec: "The change, end to end"** (built from the Stage 5 entries, `evidence/` and the diff):
0. Watch it fail, then pass (5 minutes, commands to run). 1. The problem, shown happening in the real code.
2. What changed, line by line, and why (alternatives rejected). 3. How it fits together and solves the issue.
4. The tests: why this harness, each test red → green, what each proves, what none of them prove.
5. The upstream comment, sentence by sentence: what it commits him to, and what each likely maintainer answer
would mean. 6. Small calls left for him. 7. Teach-back checklist (5–10 ideas). Concept lectures only if needed.

# Stage 7 (Contribution)

*(PR description draft; Timothy's final test run, push, PR link.)*

# Stage 8 (Review)

*(One entry per review comment and its resolution.)*
