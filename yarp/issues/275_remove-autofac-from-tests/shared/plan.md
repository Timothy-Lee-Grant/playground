2026_10_01_19_45 — dotnet/yarp#275: remove Autofac from the tests (phase 1)

> **The living plan for mode P (plan-driven, Timothy observes).** One document, read top to bottom.
>
> **Who writes what:** the plan body (Stages 1–3) is owned by **desktop** and revised in place with a version
> number (v1, v2, …; old versions summarized, not deleted). **Entries** are append-only and tagged
> `[Author — YYYY_MM_DD_HH_MM]`: `[Timothy]` (recorded by whichever Claude he said it to), `[Desktop]`, `[CLI]`.
> Nobody edits someone else's entry. The CLI writes Stage 5 entries and change requests; desktop writes the rest.
>
> **Gates** (only Timothy opens them): **G1** plan approved → implementation may start · **G2** before anything is
> posted upstream · **G3** after the teach-back → PR may be opened. Between gates the CLI works autonomously.
> Deviation rules: `00-start-here.md` §6. The standard sequence is `exercises/ai-workflow/default-workflow.md`;
> the reference run is iot#2328.

---

# Stage 1 (Direction)

**[Desktop — 2026_10_01_19_45] Direction, from Timothy's decisions on 2026-09-30 and 2026-10-01**

1. **Goal:** Timothy's second upstream code PR. It may be **built and understood** now; the PR itself waits until
   iot#2328's PR is open and settled enough (one active upstream PR at a time).
2. **Issue:** dotnet/yarp#275. The tests build classes through `TestAutoMockBase` (Autofac.Extras.Moq `AutoMock`),
   which hides what each class needs. **Phase 1:** remove Autofac only; Moq stays. It's a refactor: **no test may
   change what it proves** (brief §1, §4).
3. **How we work (mode P, switched from M0 on 2026-10-01 before any session):** desktop plans, the CLI checks the
   plan against the code and builds the whole change locally; Timothy learns the finished change through lecture 1
   and a teach-back, then posts the comment, pushes and opens the PR himself. The earlier "ask maintainers first,
   no edits until they answer" (D1, WO-1) is replaced by the mode-P rule: **build first, comment after he
   understands it** (D3).
4. **Special first step: Timothy's existing clone.** He forked and cloned YARP long ago and made some changes in
   it; he doesn't know what state it's in. The CLI audits it read-only (Step 0) and brings it to the standard state
   (Step 1) **without losing anything** (O1).
5. **What success looks like:** a mechanical, easy-to-review PR (7 test files + helper and package removal) where
   every rewritten test is shown to still catch the bug it guards; and a Timothy who can explain the pattern and
   the shared-mock trap.
6. **Not in scope:** removing Moq (later PRs, folder by folder); any product-code change; any test behavior change.

# Stage 2 (Discussion)

**[Desktop — 2026_10_01_19_45] Opening entry: the decisions, and whose they are**

| ID | Decision | Owner | Proposal | Why |
|---|---|---|---|---|
| U1 | Is the work still wanted (issue from 2020, 0 comments)? | **upstream** | Build it anyway; ask in the comment (after lecture 1) | Building is cheap; a "no" costs AI time, not Timothy's. The comment can say the change is ready |
| U2 | Scope: Autofac only, Moq stays | **upstream** | Phase 1 = Autofac only | Smallest reviewable step; Moq is in 25 files |
| U3 | Style of the replacement: how a test class builds its subject | **upstream** (reviewers' taste) | Explicit construction in each class: mocks as fields, one small private `Create…()` method that calls the real constructor. No new shared base class | The issue's point is to make dependencies visible; a new base class would hide them again |
| O1 | **Target state of the existing clone** | ours | See Step 1: `origin` = `Timothy-Lee-Grant/yarp`, `upstream` = `dotnet/yarp`; local `main` = `upstream/main`; **all old work kept** on `archive/pre-275-2026-10` (uncommitted changes committed there, not stashed); new branch `remove-autofac-275` from `upstream/main`; clean tree | Nothing is lost, and the PR branch has no unrelated history. A backup branch is visible in `git branch`; a stash is easy to forget |
| O2 | Proof that each test still proves the same thing | ours | Per rewritten file: run green; temporarily break the product code one test guards → red; revert. Save as evidence | Brief §4 trap 3; a refactor that silently weakens tests is the main risk |
| O3 | Commits | ours | One commit per file group (small / medium / large), then one for deleting the helper and package refs | A reviewer can follow the pattern once, then skim |
| O4 | Platform | ours | Mac (SDK 11 RC per `global.json`). If the HttpSys tests can't run on macOS, CHANGE REQUEST with options | Brief §5; unverified |

# Stage 3 (Implementation Planning)

**[Desktop — 2026_10_01_19_45] Implementation Plan v1**

| Step | What | Who | Proof | Gate |
|---|---|---|---|---|
| **0** | **Audit the existing clone, read-only** (no checkout, reset, stash, clean, fetch-with-prune or commit). Find it: `develop/yarp/`, or any git repo under `develop/`. Record: `git remote -v` (old names such as `microsoft/reverse-proxy`, or `origin` pointing at upstream?); current branch; `git status` (modified, staged, untracked); `git stash list`; every local branch with its last commit date and subject; commits each branch has that `upstream/main` doesn't (`git fetch upstream` **without** `--prune` is allowed, adding the `upstream` remote if missing is allowed); what's on Timothy's fork (`git ls-remote origin`); how far `main` is behind. **Describe his old changes in plain words** (what files, what they seem to be for; related to #275 or #1764 or something else?) | CLI | `evidence/000-fork-audit.txt` + Stage 5 entry: what the repo looks like, and the exact commands Step 1 would run | G1 |
| **1** | **Bring the clone to the O1 target state.** Show Timothy the Step 0 summary in ≤6 lines and the command list; run it on his "yes". Allowed: create the archive branch, commit uncommitted work onto it (message "WIP: Timothy's earlier changes, archived <date>"), fix/add remotes, fast-forward or recreate `main` from `upstream/main` **only after** the archive branch holds every local commit, create `remove-autofac-275`. **Never:** delete a branch, `reset --hard` on a branch that isn't archived, `clean`, drop a stash, push. If anything doesn't fit this recipe, stop and write a CHANGE REQUEST | CLI, Timothy says yes | `evidence/001-fork-state-after.txt` (remotes, branches, status) | G1 |
| **2** | **Build + baseline:** `./restore.sh`; record SDK and `du -sh .dotnet`; `dotnet test test/ReverseProxy.Tests/`; answer the macOS HttpSys question (ran / skipped / excluded); summarize `CONTRIBUTING.md`, the PR template and any AI policy | CLI | `evidence/002-baseline-reverseproxy-tests.txt` | — |
| **3** | **Plan check against the code:** confirm brief §3 (7 files, usages, csproj and `Versions.props` entries) on today's `upstream/main`; any new users of `TestAutoMockBase`? Stage 5 entry: "plan holds" or CHANGE REQUESTs | CLI | Stage 5 entry | — |
| **4** | **Small files:** `StreamCopierTests`, `ForwarderHttpClientFactoryTests`, `LoadBalancingPoliciesTests`. Green + O2 proof per file | CLI | `evidence/004-*.txt` | — |
| **5** | **Medium files:** `ForwarderMiddlewareTests`, `ProxyPipelineInitializerMiddlewareTests`, `HttpSysDelegatorMiddlewareTests`. Watch the shared-mock trap (brief §4.1) | CLI | `evidence/005-*.txt` | — |
| **6** | **Largest:** `HttpSysDelegatorTests` (mocks wired into mocks) | CLI | `evidence/006-*.txt` | — |
| **7** | **Remove** `TestAutoMockBase.cs`, the two package refs, the two `Versions.props` entries; `git grep -i autofac` finds nothing; full test project green | CLI | `evidence/007-autofac-gone.txt` | — |
| **8** | **Hygiene:** no new build warnings; test project run 3×; diff only under `test/` and `eng/Versions.props` | CLI | `evidence/008-hygiene.txt` + `git diff --stat` | — |
| **9** | **Commits** per O3 | CLI proposes, Timothy approves | `git log --oneline` | — |
| **10** | **Implementation summary:** the pattern once (before/after for one file), then a table per file; each trap and how it was handled; what each O2 proof shows and doesn't; full diff saved | CLI | Stage 5 entry + `evidence/010-diff.patch` | — |
| **11** | **Lecture 1** (spec in Stage 6) | Desktop writes; **Timothy** reads | lecture link | — |
| **12** | **Comment** (Stage 4 draft, revised after lecture 1); Timothy posts in his own words | **Timothy** | link | **G2** |
| **13** | **Teach-back** | **Timothy** talks; desktop analyzes | `private/teachbacks/` | **G3** |
| **14** | **PR** once G3 is open **and** iot#2328's PR is out (one active PR at a time): Timothy runs the tests, pushes `remove-autofac-275`, opens the PR; CLI drafts the description | **Timothy** acts | PR link | after G3 |

**Timothy's own work:** say "yes" to Step 1's commands; approve commits (Step 9); read lecture 1; post the comment;
teach back; push and open the PR. Everything else he may watch or skip.

**Acceptance criteria**

1. No Autofac reference anywhere in the repo; the test project passes with the same test count as the baseline
   (minus none, plus none).
2. Every rewritten file has an O2 proof (red when its product code is broken).
3. The diff touches only the 7 test files, `TestAutoMockBase.cs`, the two `.csproj` files and `eng/Versions.props`.
4. Timothy's earlier work still exists on `archive/pre-275-2026-10` (Step 1 evidence shows it).

### Stage 3 Discussion Subsection

*(Questions about the plan, and the G1 grant, go here.)*

**[Timothy — 2026_10_02_00_27] G1: plan approved; go ahead with Step 1.** (Said to the CLI in session 1; recorded by CLI.)

**[Desktop — 2026_10_07_07_30] Moved to the YARP project setup (Timothy's design, 2026-10-07)**

- This folder moved to `exercises/yarp/issues/275_remove-autofac-from-tests/`. The CLI now starts in the YARP project
  folder (on Timothy's SSD) with one clone at `yarp/` and reaches this folder as `link/issues/275_…/`. General rules:
  `link/current_context/01-operating-manual.md`.
- **The clone is new.** The old `oss-work/yarp-275/develop/yarp` clone had no unpushed work as of 2026-10-02 (Timothy
  re-checks during setup). `remove-autofac-275` was created there but had no commits, so it is **recreated** from
  `upstream/main` in the new clone when work resumes (registry row says "not created yet"). The local
  `archive/pre-275-2026-10` branch isn't needed: it was a copy of `origin/tgrant/exploration-phase`, which is still on
  the fork (protected in the registry). Acceptance criterion 4 is met by that origin branch.
- **G1 stands** (granted 2026-10-02). Next: Step 2 (build + baseline), then Steps 3–10 as an autonomous run. Changes
  to how the run works: no go-ahead between steps; the CLI commits on its own at the O3 commit points (Step 9 no
  longer waits for approval, except a message that would reference an issue or person); Step 11 (lecture 1) and a
  new "testing it yourself" lecture are written on request (`/lecture change`, `/lecture testing`).

# Stage 4 (Upstream Communication)

**[Desktop — 2026_10_01_19_45] Draft comment v1** (from scouting 002; revised after lecture 1 to say the change is
ready; Timothy posts in his own voice after G2)

> Is this still wanted? I'd like to take a first, bounded step: remove Autofac only. It's used by
> `TestAutoMockBase` and 7 test classes. I'd construct the classes under test directly, keep Moq as is, and delete
> the helper and the package refs. Moq removal could follow folder by folder in later PRs if you'd like.

# Stage 5 (Implementation)

*(CLI entries go here, one per step or per meaningful finding, each with: what changed, deviations, evidence,
and **why**. Change requests are marked **CHANGE REQUEST** and stop work on the affected steps until Timothy
grants or declines them.)*

**[CLI — 2026_10_02_00_15] Step 0: workspace repaired; clone audited. The plan's premise ("an old clone with
local changes") doesn't hold on the Mac**

- *Changed:* (1) Workspace: `oss-work/yarp-275/` had a hand-copied desktop `CLAUDE.md` and a `workspace/` folder,
  but no `shared/` link and no `.claude/settings.json` (so no `git push` deny). Both copies were byte-identical to
  their sources; I moved `workspace/` to the session scratchpad (not deleted) and ran `workspace/setup.sh`, which
  created the four symlinks. Layout now matches `iot-2328`. (2) Clone: added remote `upstream` = `dotnet/yarp`
  and fetched it (no `--prune`). Nothing else in the repo changed.
- *Findings (verified, `evidence/000-fork-audit.txt`):*
  - `develop/yarp` is a **fresh clone made 2026-10-02 00:05**, not the old one. No other YARP clone exists on the
    Mac (Spotlight + `find`).
  - Clean tree, no stash, one local branch `main` = `origin/main` = `upstream/main` (0 behind, 0 ahead).
    `origin` is SSH to `Timothy-Lee-Grant/yarp`, correct.
  - Timothy's earlier work is **on GitHub only**: `origin/tgrant/exploration-phase`, 5 commits from 2026-06-14,
    22 new files and 7,459 lines, **all study notes under `concepts/`** (architecture, code concepts, open issues,
    a persona file). No code; unrelated to #275 or #1764. Those notes aren't copied into `exercises/yarp/yarp_concepts/`.
  - *Unverified:* whether the "old clone" lives on the Linux desktop with unpushed work. Only Timothy can check.
- *Deviations:* The first `git remote add` + `fetch` was blocked by Claude Code's permission check. I stopped,
  explained, and ran it after Timothy said "continue Step 0".
- *Step 1, as it would now run* (fits the existing recipe, so no CHANGE REQUEST; needs G1 + Timothy's "yes"):
  ```
  git -C develop/yarp branch archive/pre-275-2026-10 origin/tgrant/exploration-phase   # local copy of the old notes
  git -C develop/yarp switch -c remove-autofac-275 upstream/main                        # PR branch, no unrelated history
  ```
  `main` needs nothing: it already equals `upstream/main`. No uncommitted work, so there's nothing to commit onto the archive.
- **Why:** The plan's archive branch exists so that nothing is lost when `main` gets reset. Here nothing is at
  risk locally. The old work is already pushed and has no code, so no reset is needed. I still propose creating
  `archive/pre-275-2026-10` locally from the fork branch. It costs nothing, it meets acceptance criterion 4 as
  written, and it keeps a copy if the GitHub branch is ever deleted. The alternative is to skip the archive and
  treat the fork branch as the archive. That's equally safe today, but criterion 4 would need rewording. Also
  rejected: rebasing the notes onto `upstream/main`. Nothing needs them current, and they must never reach the
  PR branch.
- *Evidence:* `evidence/000-fork-audit.txt`.
- *Open for Timothy:* (a) G1. (b) Is there an old YARP clone on the Linux desktop? If so, run `git status` and
  `git log --branches --not --remotes` there before we treat Step 1 as lossless.

**[CLI — 2026_10_02_00_27] Step 1: clone brought to the O1 target state**

- *Changed:* created local branch `archive/pre-275-2026-10` at `5892025e` (= `origin/tgrant/exploration-phase`;
  all 5 old commits present, 0 missing), and `remove-autofac-275` at `0cae8cac` (= `upstream/main`, 0/0). Now on
  `remove-autofac-275`, clean tree. `main` untouched (already equal to `upstream/main`). Nothing deleted, reset,
  stashed or pushed.
- *Deviations:* (1) Ran with the Linux-desktop question still open. Step 1 only adds local branches here, so it
  can't lose anything over there; the question still stands for that machine. (2) Implementation detail:
  `remove-autofac-275` tracks `upstream/main` (git's default for `switch -c <name> upstream/main`). That's
  useful now, because `git status` shows how far `dotnet/yarp` has moved on. Timothy's first push in Step 14 must
  be `git push -u origin remove-autofac-275`, which retargets tracking to his fork.
- *Evidence:* `evidence/001-fork-state-after.txt` (remotes, branches, status, archive completeness check).
- **Why:** Branching from `upstream/main`, not from the old notes branch, keeps the PR diff to the #275 change
  only. The archive branch is a local second copy of work that already lives on GitHub. It's cheap insurance and
  meets acceptance criterion 4 as written.

# Stage 6 (Understanding)

**[Desktop — 2026_10_01_19_45] Lecture 1 spec: "The change, end to end"** (built from the Stage 5 entries,
`evidence/` and the diff; destination `../lectures/001-the-change-end-to-end.md`)

0. Watch it pass, then watch one test go red when the product code is broken (5 minutes, commands to run).
1. **Your fork:** what state it was in, what the CLI did, where your old work is now (short).
2. The problem: what `AutoMock` does for a test, and why the issue wants it gone.
3. **The pattern, once, in depth:** one file before → after, line by line, and why.
4. The shared-mock trap and the loose-default trap, shown in the real code.
5. The other six files as a table (what was special about each).
6. The proofs: what O2 shows, what it doesn't.
7. The comment, sentence by sentence; what each likely answer would mean.
8. Teach-back checklist (5–10 ideas).

# Stage 7 (Contribution)

*(PR description draft; Timothy's final test run, push, PR link.)*

# Stage 8 (Review)

*(One entry per review comment and its resolution.)*
