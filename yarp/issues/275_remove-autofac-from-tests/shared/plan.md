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

**[Timothy — 2026_10_07] "There is no unpushed code on my other yarp clone."** (Said to the CLI in session 2;
recorded by CLI.) Closes the Step 0/1 Linux-desktop question: Step 1 was lossless.

**[CLI — 2026_10_07] New clone: branch recreated**

- *Changed:* `remove-autofac-275` created in the new project clone from `upstream/main` @ `2aa3d835` (after
  `git fetch upstream`). Registry row updated. The branch creation was denied twice by Claude Code's permission
  check (although `settings.json` allows `git -C * switch*`), so **Timothy ran the `switch -c` himself** (`!` prefix).
- **Why:** per the desktop entry of 2026-10-07: the old clone's branch had no commits, so recreating it from
  today's `upstream/main` loses nothing and starts the work on the newest code.

**[CLI — 2026_10_07] Step 2: build + baseline. Green; HttpSys tests run on macOS**

- *Changed:* nothing in the fork. `./restore.sh` installed the repo-local SDK into `yarp/.dotnet/` (gitignored).
- *Findings (verified, `evidence/002-baseline-reverseproxy-tests.txt`):*
  - SDK `11.0.100-rc.1.26420.103`, runtimes 8.0.13 / 9.0.2 / 11.0.0-rc.1; `.dotnet` is **760 MB** (not "a few GB").
  - **Baseline: 3,978 passed, 0 failed, 0 skipped** (1,989 on net8.0 + 1,989 on net9.0; the test project targets
    only those two). This is the number Step 7 must match.
  - **The 7 files hold 71 tests per framework:** StreamCopier 12, ForwarderHttpClientFactory 23,
    LoadBalancingPolicies 6, ForwarderMiddleware 3, ProxyPipelineInitializerMiddleware 5,
    HttpSysDelegatorMiddleware 5, HttpSysDelegator 17. Each rewritten file must keep its count.
  - **O4 answered: the HttpSys delegation tests run and pass on macOS** (22/22). They mock HTTP.sys; there's no
    platform gate. No CHANGE REQUEST needed.
  - Contribution rules match `03-yarp-project.md` §4: no PR template, no AI policy (`eng/common/AGENTS.md` is
    only Arcade's "don't edit eng/common" note).
- *Deviations:* (1) **The documented test command fails.** With the Microsoft.Testing.Platform runner,
  `dotnet test test/ReverseProxy.Tests/` and `dotnet test --project <relative path>` both fail with a *doubled*
  path (`…/test/ReverseProxy.Tests/test/ReverseProxy.Tests/…csproj`). An **absolute** `--project` path works.
  Cause not established (unverified); I used the workaround rather than spend more time on it, since it doesn't
  affect the change. `03-yarp-project.md` §3 should be updated (desktop's file; flagged in the session report).
  (2) Some test classes are in `…Tests` sub-namespaces (`Yarp.ReverseProxy.Forwarder.Tests.…`), others not
  (`Yarp.ReverseProxy.Delegation.…`); the filter needs the exact full name or it silently matches 0 tests.
- **Why:** per-class counts are recorded now because "the test still passes" isn't enough for a refactor: a
  rewritten file that accidentally lost a test (or a theory case) would still be green. Comparing counts per class
  catches that; the O2 break-it proofs (Steps 4–6) catch the other failure, a test that runs but no longer checks.

**[CLI — 2026_10_07] Step 3: plan check against the code. The plan holds; three findings shape the rewrite**

- *Changed:* nothing in the fork. One throwaway console program in `scratch/275_remove-autofac-from-tests/automock-defaults/`.
- *Confirmed (verified, `evidence/003-plan-check-and-automock-defaults.txt`):* brief §3 is exact on today's
  `upstream/main` (`2aa3d835`). The same 7 users of `TestAutoMockBase`, no new ones. Autofac appears only in
  `TestAutoMockBase.cs`, the two test `.csproj` files and 2 lines of `eng/Versions.props`. Every project that uses
  Moq references it directly, so removing `Autofac.Extras.Moq` can't take Moq away as a side effect.
- *Findings:*
  1. **AutoMock's mocks are plain loose Moq mocks** (verified by experiment, same package versions as the repo):
     `Behavior=Loose`, `DefaultValue=Empty`, the same as `new Mock<T>()`. An unscripted property such as
     `IServer.Features` returns `null` in both. So `new Mock<T>()` is an exact drop-in; the "loose default" trap
     (brief §4.2) is met by Moq's own default.
  2. **`HttpSysDelegator` is a sealed concrete class**, and `HttpSysDelegatorMiddleware` takes it directly (not
     an interface). AutoMock didn't mock it; it built a **real** one with auto-mocked `IServer` and logger (the
     experiment confirms that's what `Create<>` does for a sealed class). The rewrite must therefore call
     `new HttpSysDelegator(serverMock.Object, NullLogger…)`, not substitute a mock, or the middleware tests would
     exercise less real code than before.
  3. **No test in the 7 files scripts or verifies a logger.** Each logger can become `NullLogger<T>.Instance`
     (already used by sibling tests, e.g. `LimitsMiddlewareTests`). Both a loose mock and `NullLogger` answer
     `IsEnabled` with `false`, so log calls do nothing in both cases.
  4. *(Not ours to fix)* `ForwarderMiddlewareTests.NoDestinations_503` calls `Mock<IHttpForwarder>().Verify()`,
     but its only setup isn't marked `.Verifiable()`, so that line checks nothing. The test's real assertions
     (503, error feature) still guard the behavior. The refactor keeps the line as is: changing what a test
     checks is out of scope. Possible follow-up for the PR description or a later PR.
- *Deviations:* none. The scope, files and steps are unchanged; no CHANGE REQUEST.
- **Why:** the two traps in the brief are about *equivalence*: the rewritten test must build the same object graph
  (which objects are real, which are fake, which fakes are shared) as AutoMock did. Reading the code tells us
  which constructor parameters exist, but not what AutoMock *does* with each one. An experiment answered that,
  instead of assuming. That's how finding 2 surfaced: an assumption that "AutoMock mocks every parameter"
  would have replaced a real `HttpSysDelegator` with a fake.

**[CLI — 2026_10_07] Step 4: small files rewritten; green, same counts, O2 red for each**

- *Changed:* commit `9db5c959` "Construct simple test subjects directly instead of via AutoMock" (3 files,
  +19 −22 lines).
  - `StreamCopierTests`: only `: TestAutoMockBase` removed (it never called the helper).
  - `ForwarderHttpClientFactoryTests`: 11× `Mock<ILogger<ForwarderHttpClientFactory>>().Object` →
    `NullLogger<ForwarderHttpClientFactory>.Instance`; base class and the two now-unused `using`s removed
    (`Yarp.Tests.Common`, `Microsoft.Extensions.Logging` → `…Logging.Abstractions`).
  - `LoadBalancingPoliciesTests`: `Provide<IRandomFactory>(RandomFactory)` deleted; 6× `Create<XPolicy>()` →
    `new XPolicy()`, or `new XPolicy(RandomFactory)` for Random and PowerOfTwoChoices; base class and the unused
    `using Yarp.ReverseProxy.Utilities` removed.
- *Evidence (verified, `evidence/004-small-files.txt`):* build 0 warnings; counts 24 / 46 / 12 (= baseline 12 / 23 /
  6 × 2 frameworks), all green. O2: `StreamCopier` writing 0 bytes → 12 red; `EnabledSslProtocols = default` →
  `CreateClient_ApplySslProtocols_Success` red; `RandomLoadBalancingPolicy` always picking `[0]` →
  `PickDestination_Random_Works` red. `src/` clean after each.
- *Deviations:* (1) Inline `new` instead of a private `Create…()` method (U3 proposed "one small private
  `Create…()`"). Here each constructor is one short expression and the tests already build their own objects
  inline; a helper method would add a layer without hiding anything worth hiding. The medium files, where a
  constructor takes 3–4 arguments, are where a helper may earn its place. (2) **Finding about the method:** the
  first O2 run built *incrementally* and the SslProtocols break did **not** go red. Same break with
  `--no-incremental` → red. The incremental build had left a stale `Yarp.ReverseProxy.dll` next to the tests. All
  O2 runs now use `--no-incremental`.
- **Why:**
  - `NullLogger` over `new Mock<ILogger<T>>().Object`: no test looks at the logger (Step 3 finding 3). `NullLogger`
    says "logging is irrelevant here" in the code itself, and it's already the convention in sibling test files.
    A mock would suggest someone might `Verify` it.
  - Passing `RandomFactory` explicitly is the shared-object trap in its simplest form. `PickDestination_Random_Works`
    asserts the exact sequence from `RandomInstance`, so it only passes if the policy uses *our* factory. The
    green run proves the wiring; the O2 run proves the test still checks the policy.
  - The stale-build episode shows why O2 exists at all: a "green" that comes from not running the code you think
    you're running looks exactly like a real green.

**[CLI — 2026_10_07] Step 5: medium files rewritten; shared mocks are fields; O2 red on the wiring itself**

- *Changed:* commit `4526c6a5` "Construct middleware under test directly instead of via AutoMock" (3 files).
  - `ForwarderMiddlewareTests`: field `private readonly Mock<IHttpForwarder> _forwarder = new();` replaces the 4
    `Mock<IHttpForwarder>()` calls; a private `CreateMiddleware()` calls the real constructor with
    `_ => Task.CompletedTask`, `NullLogger`, `_forwarder.Object` and `new Mock<IRandomFactory>().Object`.
  - `ProxyPipelineInitializerMiddlewareTests`: the test-class constructor that `Provide`d the 418 "next" delegate
    is gone; that delegate now sits inside a `private static CreateMiddleware()`, with `NullLogger` and
    `new Mock<IOptionsMonitor<RequestTimeoutOptions>>().Object`.
  - `HttpSysDelegatorMiddlewareTests`: fields `_delegationFeature` and `_randomFactory` (both `Mock<…> = new()`); the
    existing constructor now builds `_sut` with `new HttpSysDelegatorMiddleware(_next, NullLogger,
    new HttpSysDelegator(new Mock<IServer>().Object, NullLogger), _randomFactory.Object)`. `using Moq` added (the
    file never needed it before: `Mock<T>()` came from the base class).
- *Evidence (verified, `evidence/005-medium-files.txt`):* 0 warnings; 6 / 10 / 10 (= 3 / 5 / 5 × 2), green. O2:
  URL + "x" to `SendAsync` → `Invoke_Works` red; `? _next(context)` → `? Task.CompletedTask` → `Invoke_SetsFeatures`
  and `Invoke_NoHealthyEndpoints_CallsNext` red; random ignored → `…ProxyChosen…` red. `src/` clean after each.
- *Deviations (implementation details):* (1) A `CreateMiddleware()` helper where the class built its subject in
  several tests (U3's shape; it copies `LimitsMiddlewareTests.CreateMiddleware()` in the same project). In
  `HttpSysDelegatorMiddlewareTests` the subject was already built once in the test-class constructor, so it stays
  there. (2) Mock fields use `= new();`, as `Kubernetes.Tests/IngressControllerTests` does.
- **Why:**
  - **Which mocks became fields, and which stayed inline `new Mock<…>().Object`:** the rule is "does the test touch
    this mock after handing it over?" `_forwarder` is `Setup` and `Verify`'d; `_randomFactory` is `Setup` *after*
    `_sut` is built (`SetupRandomToReturn`); `_delegationFeature` is put on the `HttpContext` *and* scripted. Each
    must be the very object the middleware or context holds: that's the shared-mock trap (brief §4.1). Mocks
    nobody looks at again (`IRandomFactory` in the forwarder tests, `IOptionsMonitor`, `IServer`) stay inline, so
    a reader can see they're inert.
  - **Why the O2 breaks were chosen this way:** each break can *only* be caught through the wiring that was
    rewritten. If `_forwarder` weren't the injected instance, the URL break would go unnoticed (the setup would
    sit on an unused mock). If `CreateMiddleware()` passed a different "next", the 418 checks would already fail
    when green. So green + red together show the graph is wired as before.
  - **`_ => Task.CompletedTask` for the forwarder's unused "next"** instead of a mocked delegate: the middleware
    never calls it (it forwards or returns 503), and the lambda says "does nothing" plainly. AutoMock gave a loose
    delegate mock, which also returns a completed `Task`, so the behavior is the same.
  - **Real `HttpSysDelegator`** (Step 3 finding 2): its `IServer` mock has `Features == null` in both the old and
    new setups, so the delegator behaves identically: it has no delegation feature.

**[CLI — 2026_10_07] Step 6: HttpSysDelegatorTests; the mock graph is now spelled out**

- *Changed:* commit `f5f16fef` "Build HttpSysDelegator and its mock graph explicitly in tests" (1 file).
  Before, AutoMock wired four mocks together invisibly. Now the constructor builds them in order:
  ```
  server (local Mock<IServer>) ──.Features──► serverFeatures (local Mock<IFeatureCollection>)
        ──.Get<IServerDelegationFeature>()──► _serverDelegationFeature (field: Setup / Verify / Reset in tests)
  _context.Features ──► _requestDelegationFeature (field: CanDelegate scripted, DelegateRequest verified)
  _delegator = new HttpSysDelegator(server.Object, NullLogger<HttpSysDelegator>.Instance)   ← real object
  ```
  5× `Mock<IServerDelegationFeature>()` → `_serverDelegationFeature`; 4× `Mock<IHttpSysRequestDelegationFeature>()`
  → `_requestDelegationFeature`.
- *Evidence (verified, `evidence/006-httpsysdelegator-tests.txt`):* 0 warnings; 34 (= 17 × 2) green. O2: (a) the
  delegator ignores `IServer.Features` → 18 red; (b) it never calls `DelegateRequest` on the request feature → 8 red.
  `src/` clean after each.
- *Deviations:* naming only. Here there are two "delegation feature" mocks, so they're named
  `_serverDelegationFeature` and `_requestDelegationFeature`. In `HttpSysDelegatorMiddlewareTests` (Step 5) only
  the request one exists, and it's `_delegationFeature`. I left that as it is; within each file the name is
  unambiguous.
- **Why:**
  - **Fields vs locals:** same rule as Step 5. The server and feature-collection mocks are only plumbing to get
    the delegation feature into the delegator; nothing touches them after the constructor, so they're locals.
  - **`Reset()` still works the same:** `_serverDelegationFeature.Reset()` (in two tests) clears setups and
    recorded calls on the same object the delegator already holds. That's what `Mock<IServerDelegationFeature>().Reset()`
    did under AutoMock, which handed back the same instance every time.
  - **The O2 breaks map one-to-one onto the two chains:** (a) proves the chain through `IServer.Features` reaches
    the verified mock (if the rewrite had wired a different `IServerDelegationFeature`, the green run would
    already have failed every "rule created" check); (b) proves the request feature on the `HttpContext` is the
    verified one.

**[CLI — 2026_10_07] Step 7: Autofac removed; whole repo rebuilds; ReverseProxy.Tests = baseline**

- *Changed:* commit `1665ced5` "Remove TestAutoMockBase and the Autofac test dependencies": deleted
  `test/Tests.Common/TestAutoMockBase.cs` (−99 lines); removed the `Autofac` and `Autofac.Extras.Moq`
  `PackageReference`s from `Yarp.Tests.Common.csproj` and `Yarp.ReverseProxy.Tests.csproj`; removed `AutofacVersion`
  and `AutofacExtrasMoqVersion` from `eng/Versions.props`.
- *Evidence (verified, `evidence/007-autofac-gone.txt`):* `git grep -i autofac` finds nothing. `./build.sh --rebuild`
  (all 33 projects): 0 warnings, 0 errors. Zero Autofac mentions in the test projects' `.deps.json` files and in
  the compiled test DLLs. **ReverseProxy.Tests: 3,978 passed, 0 failed, 0 skipped: identical to the Step 2
  baseline** (acceptance criterion 1). The other two projects that reference `Tests.Common` also pass:
  Kubernetes.Tests 182 + 4 skipped, FunctionalTests 470 + 68 skipped.
- *Deviations:* (1) Built the **whole repo**, not just the test project. `Tests.Common` is also referenced by
  `Kubernetes.Tests` and `ReverseProxy.FunctionalTests`, so a package removed from it could break them through
  transitive dependencies (e.g. if they had used something Autofac pulled in). It didn't. (2) No Step 2 baseline
  exists for those two projects, so their skip counts can't be compared. I checked the reasons instead: every
  skip is declared in the test source (a known-issue `Skip`, "HTTP/2 over TLS not supported on macOS", the
  Windows-only `[HttpSysDelegationFact]`); none relates to Autofac. **Unverified:** that the counts equal the
  pre-change counts.
- **Why:**
  - **The order: rewrite every user first (Steps 4–6), delete the helper last.** Each earlier commit compiles and
    passes on its own, and this commit is pure deletion; a reviewer can check it with `git grep`.
  - **Why `.deps.json` and not just "the build passed":** a successful build proves nothing *compiles* against
    Autofac. `.deps.json` is the list the .NET runtime uses to find assemblies when a test runs, so zero mentions
    there means nothing can *load* it either. The 4 stale `Autofac*.dll` files still in `artifacts/` (left over
    from earlier builds) are therefore inert.
  - **Moq stays** (U2): it's referenced directly by every project that uses it (Step 3), so removing
    `Autofac.Extras.Moq` didn't remove Moq.

**[CLI — 2026_10_07] Steps 8–9: hygiene passes; 4 commits as O3 planned**

- *Evidence (verified, `evidence/008-hygiene.txt`):* ReverseProxy.Tests run 3× in a row: 3,978 / 3,978 each time.
  Build warnings 0. `git diff --name-only upstream/main...HEAD` lists only `test/…` files and
  `eng/Versions.props` (acceptance criterion 3). `git diff --check`: no whitespace errors. Total +96 −176 lines
  in 11 files.
- *Commits (Step 9, made during Steps 4–7 under the 2026-10-07 autonomy rule; no message references an issue or
  person):*
  ```
  9db5c959 Construct simple test subjects directly instead of via AutoMock      (small: 3 files)
  4526c6a5 Construct middleware under test directly instead of via AutoMock     (medium: 3 files)
  f5f16fef Build HttpSysDelegator and its mock graph explicitly in tests        (large: 1 file)
  1665ced5 Remove TestAutoMockBase and the Autofac test dependencies            (helper + 2 csproj + Versions.props)
  ```
- *Not pushed.* `remove-autofac-275` exists only locally; the first push is Timothy's (`git push -u origin
  remove-autofac-275`), and the plan puts it at Step 14, after G3.
- **Why 3 runs:** the forwarder test (`Invoke_Works`) coordinates two `TaskCompletionSource`s across an `await`.
  Timing-dependent tests can pass once by luck, so repeated green runs are cheap insurance that the rewrite
  didn't make one flaky.

**[CLI — 2026_10_07] Step 10: implementation summary (the run is finished)**

**The change in one line:** seven test classes stop asking Autofac's `AutoMock` to build the class under test and
build it themselves through its real constructor; then the helper and both Autofac packages are deleted. No
product code changed, and no test changed what it checks.

**The pattern, once (`ForwarderMiddlewareTests`, before → after):**

```
BEFORE                                              AFTER
class ForwarderMiddlewareTests : TestAutoMockBase   class ForwarderMiddlewareTests
                                                    {
                                                        private readonly Mock<IHttpForwarder> _forwarder = new();
  Mock<IHttpForwarder>().Setup(...)                     _forwarder.Setup(...)
  var sut = Create<ForwarderMiddleware>();              var sut = CreateMiddleware();
  Mock<IHttpForwarder>().Verify();                      _forwarder.Verify();
                                                        private ForwarderMiddleware CreateMiddleware()
                                                            => new ForwarderMiddleware(
                                                                   _ => Task.CompletedTask,                  // next: never called
                                                                   NullLogger<ForwarderMiddleware>.Instance, // logging irrelevant
                                                                   _forwarder.Object,                        // THE shared mock
                                                                   new Mock<IRandomFactory>().Object);       // inert stand-in
```

Before, `Mock<IHttpForwarder>()` asked AutoMock for "the" mock of that type: the same object every time, and
the same one it injected into `Create<…>()`. After, that sameness is explicit: one field, used in both places.
The constructor call lists all four dependencies, which is the issue's point: you can see what the class needs.

**Per file:**

| File | Tests ×2 | What was special | How it's handled | O2 break → red |
|---|---|---|---|---|
| `StreamCopierTests` | 12 | Inherited the base class, never used it | Remove `: TestAutoMockBase` | write 0 bytes → 12 red |
| `ForwarderHttpClientFactoryTests` | 23 | Already called `new`; only the logger came from AutoMock | `NullLogger<T>.Instance` ×11 | `EnabledSslProtocols = default` → 2 red |
| `LoadBalancingPoliciesTests` | 6 | `Provide<IRandomFactory>(RandomFactory)` registered a test fake | pass `RandomFactory` to the 2 policies that take one | always pick `[0]` → 2 red |
| `ForwarderMiddlewareTests` | 3 | Shared `IHttpForwarder` mock: Setup + Verify | field `_forwarder` + `CreateMiddleware()` | URL + "x" → 2 red |
| `ProxyPipelineInitializerMiddlewareTests` | 5 | `Provide<RequestDelegate>` (the 418 "next") | the delegate moves into `CreateMiddleware()` | `next` not called → 4 red |
| `HttpSysDelegatorMiddlewareTests` | 5 | Sealed `HttpSysDelegator` was built **real** by AutoMock; random factory scripted after construction | `new HttpSysDelegator(...)`; fields `_delegationFeature`, `_randomFactory` | random ignored → 2 red |
| `HttpSysDelegatorTests` | 17 | 4 mocks wired into each other | chain built in the constructor; 2 verified mocks as fields | (a) ignore `IServer.Features` → 18 red; (b) skip `DelegateRequest` → 8 red |

**The two traps from the brief, and how each was handled:**

1. *Shared mock* (brief §4.1). The rule used: a mock becomes a **field** if the test touches it after handing it
   over (`Setup`, `Verify`, `Reset`), and stays an inline `new Mock<…>().Object` if nobody looks at it again.
   Each O2 break was chosen to be detectable **only** through that shared mock, so green + red together show
   the wiring is the same as before.
2. *Loose defaults* (brief §4.2). Verified by experiment (Step 3): AutoMock's mocks are ordinary Moq mocks
   (`Loose`, `DefaultValue.Empty`), so `new Mock<T>()` behaves identically, and `NullLogger` matches a loose
   logger mock (`IsEnabled` → `false`).

**A third trap the brief didn't list:** AutoMock doesn't mock *every* constructor parameter. For a sealed
concrete class (`HttpSysDelegator`), it builds the real thing. Replacing it with a fake would have quietly
reduced what `HttpSysDelegatorMiddlewareTests` exercises.

**What the proofs show, and what they don't:**

| Proof | Shows | Doesn't show |
|---|---|---|
| Same per-class counts (Step 2 vs after) | No test or theory case was lost or added | That each test still checks the same thing |
| O2 break → red (one break per file, two for the largest) | The rewritten test still reaches and checks the product code it guards, through the rewritten wiring | That *every* assertion in the file still bites (one break per file is a sample, not exhaustive) |
| Full project 3,978 ×3, other test projects green | Nothing else broke; no flakiness introduced | Behavior on Windows/Linux CI (only macOS was run; Azure Pipelines runs the PR) |
| `.deps.json` / `git grep` | Autofac is gone at compile time and at run time | — |

**Rejected alternatives:**

- *A new shared base class or helper* (e.g. a smaller `TestBase` with `CreateMock<T>()`): it would hide the
  dependencies again, which is what the issue objects to (U3).
- *`MockRepository` / strict mocks*: would change behavior (unscripted calls would throw) and so change what tests
  prove. Out of scope for a refactor.
- *Mocking `HttpSysDelegator`*: impossible (sealed), and it would diverge from what AutoMock did.
- *Fixing the vacuous `Verify()` in `ForwarderMiddlewareTests.NoDestinations_503`* (Step 3 finding 4): it would
  change what a test checks. It could go in the PR description as a possible follow-up, or not at all.

**Deviations from plan v1, collected:** inline `new` instead of a `Create…()` helper where construction is one
short expression (Step 4); the documented `dotnet test` command fails, so an absolute `--project` path was used
(Step 2); the whole repo was built, not just one project (Step 7); O2 needs `--no-incremental` (Step 4).
No CHANGE REQUESTs.

**Open items for Timothy / desktop:**

1. `03-yarp-project.md` §3: the test command should be `./.dotnet/dotnet test --project <absolute path to csproj>`;
   `--filter-class <FullName>` works without `--` (verified); `.dotnet` is 760 MB, not "a few GB".
2. Kubernetes.Tests and FunctionalTests had no pre-change baseline (their skips are explained by the source, but
   the counts are unverified against `main`).
3. Next by the plan: Step 11 (lecture 1, now `/lecture change`), optionally `/lecture testing`; then the
   upstream comment (G2) and the teach-back (G3). Nothing has been pushed.

*Evidence:* `evidence/002`–`008`, full diff `evidence/010-diff.patch` (808 lines, `git format-patch` of the 4 commits).

**[CLI — 2026_10_07] Verification while writing lectures 001/002: Release, all 4 test projects, branch = main**

- *Changed:* nothing in the fork. The clone was switched to `upstream/main` (detached) and back, to take the baseline.
- *Evidence (verified, `evidence/011-release-all-tests-branch-vs-main.txt`):* `./build.sh --configuration Release
  --rebuild --test` on `main` and on the branch: **identical** totals, passes and skips in every project and
  framework (Application 27; Kubernetes 93 ×2 with 2 skipped each; Functional 269 ×2 with 34 skipped each;
  ReverseProxy 1,989 ×2). 0 warnings. **This closes Step 7's open item 2** (skips now verified equal to `main`) and
  covers the Release configuration that CI uses.
- *Found:* `./test.sh` only runs tests; with no Release build it "fails" in 1 s. `--filter-method` needs the full
  name, or it silently matches 0 tests. Both are in lecture 002 §8.

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
