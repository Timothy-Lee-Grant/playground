2026-10-07 — dotnet/yarp#9999: Test title

> **The living plan (mode P).** One document, read top to bottom.
>
> **Who writes what:** the plan body (Stages 1–3) is owned by the **planner** (desktop, or the CLI if Timothy works
> without desktop) and revised in place with a version number (v1, v2, …; old versions summarized, not deleted).
> **Entries** are append-only and tagged `[Author — YYYY_MM_DD_HH_MM]`: `[Timothy]` (recorded by whichever Claude
> he said it to), `[Desktop]`, `[CLI]`. Nobody edits someone else's entry.
>
> **Gates** (only Timothy opens them): **G1** plan approved → the CLI runs the whole plan autonomously ·
> **References** a commit message naming an issue/PR/person needs his OK · **G2** before anything is posted
> upstream · **G3** after he has learned the change → the PR may be opened. Rules for the run:
> `link/current_context/01-operating-manual.md` §5–§7.

---

# Stage 1 (Direction)

*(Goal, the issue in two sentences, fix scope, what success looks like, not in scope.)*

# Stage 2 (Discussion)

*(Every decision in a table, tagged **ours** (Timothy grants at G1) or **upstream** (maintainers decide; we build
the most likely default so it's cheap to change), each with a proposal and the why.)*

| ID | Decision | Owner | Proposal | Why |
|---|---|---|---|---|

# Stage 3 (Implementation Planning)

*(Implementation Plan v1. Default step shape, adjust per issue. Steps 0–7 are the CLI's **autonomous run** after
G1: no go-ahead between them. Mark the commit points.)*

| Step | What | Who | Proof | Commit? |
|---|---|---|---|---|
| **0** | Plan review against the code (no edits): paths, line numbers, competing PRs, test approach. One Stage 5 entry: "plan holds" or CHANGE REQUESTs | CLI | Stage 5 entry | — |
| **1** | Create the branch from `upstream/main` (register it in `04-branches.md`); `./restore.sh`; baseline build + the relevant test project | CLI | `evidence/NNN-baseline.txt` | — |
| **2** | Failing tests (red), each failing **for the stated reason** (skip for pure refactors: then "prove each touched test can still go red") | CLI | `evidence/NNN-red.txt` | yes: tests |
| **3** | The change (green); all old tests still pass | CLI | `evidence/NNN-green.txt` | yes: change |
| **4** | Hygiene: no new warnings, test project run 3× for flakiness, diff only the intended files, commit messages clean | CLI | `evidence/NNN-hygiene.txt` | — |
| **5** | Full `./test.sh` once | CLI | `evidence/NNN-full-tests.txt` | — |
| **6** | Implementation summary: every changed line by purpose, each choice's why and rejected alternatives, what each test proves and doesn't; full diff saved | CLI | Stage 5 entry + `evidence/NNN-diff.patch` | — |
| **7** | Push the branch to origin (settings ask Timothy), only if this plan says so | CLI, Timothy approves the push | `git status -sb` | — |
| **8** | Lectures on request: `/lecture change`, `/lecture testing` | CLI or desktop; **Timothy reads** | lecture links | — |
| **9** | Timothy verifies it himself (the testing lecture), edits and posts the comment | **Timothy** | link in a `[Timothy]` entry | **G2** |
| **10** | Teach-back (voice on a walk is fine), analyzed by desktop | **Timothy** talks | `exercises/private/teachbacks/` | **G3** |
| **11** | Timothy opens the PR (the CLI drafts the description; desktop reviews it) | **Timothy** acts | PR link | — |
| **12** | Review: each review comment becomes a step; AI drafts code and replies, Timothy posts | all | Stage 8 entries | G2 per reply |

### Stage 3 Discussion Subsection

*(Questions about the plan, and the G1 grant, go here.)*

# Stage 4 (Upstream Communication)

*(Draft comment(s), versioned. Timothy posts in his own voice after G2.)*

# Stage 5 (Implementation)

*(CLI entries, one per step or meaningful finding: **Changed** (files, commits), **Deviations**, **Evidence**
(files), **Why** (reasoning, alternatives rejected). CHANGE REQUESTs stop the affected steps until Timothy grants
or declines them; unaffected steps continue.)*

# Stage 6 (Understanding)

*(Lecture links; Timothy's reading confirmations; teach-back; G3.)*

# Stage 7 (Contribution)

*(PR description draft; Timothy's final test run, push, PR link.)*

# Stage 8 (Review)

*(One entry per review comment and its resolution.)*
