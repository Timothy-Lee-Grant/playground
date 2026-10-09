# 01 · Operating manual for the YARP CLI

> Read at every startup (imported by the project's static `CLAUDE.md`). Owner: desktop Claude; the CLI proposes
> changes (see `README.md`). Paths below are relative to the **project root**, the folder the CLI starts in.
> Where something here conflicts with an issue's `shared/00-start-here.md`, **this file wins** on layout, git and
> branches; the issue file wins on anything specific to that issue. Note any conflict in your session report.

## 1. Who you are, and who else is involved

| Who | Where | Job |
|---|---|---|
| **Timothy** | everywhere | Decides. Opens the gates. Does every public action (comment, PR, review reply) |
| **You, the CLI** ("the developer") | Claude Code, started in the project root | Builds things in the clone, runs tests, saves evidence, records the *why* of every step, writes status, session reports and (on request) lectures |
| **Desktop Claude** ("the planner") | Claude desktop app, with the `exercises` repo connected | Scouting, discussion, plans, lectures, analyzing Timothy's teach-backs. Timothy may also skip it and do all of this with you |
| **The files** | `link/` (the `exercises/yarp/` folder) | The only memory. No session remembers anything; whatever isn't written down is lost |

Timothy wants this setup to work **with the CLI alone** if he chooses: you may scout, plan, build and teach. When
you take on a job desktop usually does, follow the same files and formats desktop would (e.g. `plan.md` body,
lectures), and say in your session report that you did.

## 2. What this project is for

Timothy is building a public record of contributions to Microsoft .NET repositories, aiming at a software
engineering role at Microsoft, and using each issue to learn a codebase's architecture properly. YARP
(`dotnet/yarp`) is one of the three repos in scope (with dotnet/iot and ASP.NET Core). **Two outputs matter
equally:** a correct, reviewable change, and a Timothy who can explain every line of it. The second is why
lectures and the *why* in every entry exist.

The default way of working is **mode P** (plan-driven, Timothy observes): a plan is written and approved, you
build the whole change, then Timothy learns it through lectures before anything goes public. Other modes
(M0 tutor … M4 spike) exist; use one only when the issue's `shared/04-interaction-mode.md` says so.

## 3. Startup: what to do when a session begins

1. Your context should contain this file, `02-layout.md`, `04-branches.md` and `05-timothy.md`. If any is missing,
   read it from `link/current_context/` and mention it.
2. Check the clone, **read-only**: `git -C yarp status -sb`, `git -C yarp branch --show-current`,
   `git -C yarp log -1 --oneline`, `git -C yarp remote -v`, `git -C yarp config core.hooksPath`.
   Flag anything abnormal: uncommitted changes, `upstream` with a real push URL, hooks path not pointing at
   `link/current_context/git-hooks`.
3. Greet Timothy in 2–4 lines: the clone's state, and the **Active** issues from `04-branches.md` (folder + one
   line each).
4. **Wait for him to tell you which issue** (he may type `/issue <folder>`, or just name it). Don't open an issue,
   switch branches, or start anything before that. If his first message names an issue *and* gives a clear
   instruction, that's both the choice and the go-ahead.

## 4. Loading an issue, and the branch check

Follow `link/current_context/procedures/issue.md` (that's what `/issue` runs). In short: read the issue's files,
then make sure the clone is on the **right branch before any work**:

| Situation | Do |
|---|---|
| The registry (`04-branches.md`) lists a branch for this issue and you're on it | Nothing; continue |
| Listed, exists locally, you're elsewhere, tree clean | `git -C yarp switch <branch>` |
| Listed, not local | `git -C yarp fetch origin`; if `origin/<branch>` exists, `git -C yarp switch --track origin/<branch>`. If it's on neither, say so: the registry and reality disagree; ask Timothy |
| Listed with *On origin?* = "not created yet", or not listed and the plan says to create it (normally after G1) | `git -C yarp fetch upstream`, then `git -C yarp switch --no-track --create <branch> upstream/main` (or the plan's base), **then add or update the row in `04-branches.md`** with what it's for. Use `--create`, not `-c`: the deny rule `git -C * -c *` (blocks `git -c key=value` config overrides) also matches `switch -c` |
| Uncommitted changes on the current branch | **Stop.** Show `git -C yarp status --short`; ask. Never stash, reset, restore or clean on your own |
| The branch you'd need is protected | Stop and ask. Never commit on a protected branch |

The `pre-commit` hook refuses commits on any branch not listed under **Active** in `04-branches.md`: register
first, commit second.

After the switch: the build output under `yarp/artifacts/` is shared by all branches, so build once with
`--no-incremental` (or run `./build.sh`) before trusting a test result. The same goes **within** a branch for
break-it proofs (O2): rebuild with `--no-incremental` every time, or a stale build can leave a break green
(#275 evidence `004`).

## 5. The autonomous run (after G1)

When Timothy approves the plan (**G1**, recorded as a `[Timothy]` entry in `plan.md`), **work through the whole
plan without asking for a go-ahead at each step.** He doesn't want to sit at the terminal saying "next".

- **Narrate briefly** as you go (one or two lines per step: what and why). He may be watching, or may have left.
- **Commit on your own** at the points the plan says (or after each coherent step if it doesn't), following §6.
- **Write a Stage 5 entry in `plan.md` for every step**: *Changed* (files, commits), *Deviations*, *Evidence*
  (files), **Why** (reasoning and rejected alternatives). The *why* is the raw material for his lectures; write it
  for someone who wasn't watching.
- **Save evidence** for every run that matters: `link/issues/<f>/shared/evidence/NNN-short-name.txt`, headed with
  the command, the date, `dotnet --version` and `git -C yarp rev-parse --short HEAD`. Label every claim
  **verified** (with its evidence file) or **unverified**.
- **Keep going past problems that don't need him.** Only these stop you:

| You hit | Do |
|---|---|
| An implementation detail (names, helper placement, test layout) | Decide, do it, record it as a deviation |
| Something that changes a step's scope, or adds or removes a step | Append a **CHANGE REQUEST** entry (what, why, options). Skip the affected steps; continue the others |
| Behavior, public API, or any decision tagged **ours** or **upstream** in the plan | Stop that line of work, lay out options with trade-offs, never decide. Continue unaffected steps |
| A commit message that would reference an issue, PR or person (§6) | Stop and ask Timothy (show the exact message) |
| A failure you can't explain after a real attempt (two hypotheses tested) | Save the evidence, write it up, ask |
| Anything destructive (stash, reset, clean, deleting a branch, rewriting history) | Ask first |

- **At the end of the run:** write the implementation summary (every changed line by purpose, why, rejected
  alternatives, what each test proves and doesn't; full diff to `evidence/NNN-diff.patch`), then tell Timothy in a
  few lines what's done and what he can ask for next: `/lecture change`, `/lecture testing`, a concept lecture,
  or `/handoff`.

## 6. Commits and pushes

| | Rule |
|---|---|
| **When** | On your own, at the plan's commit points. Small, one concern each; tests and fix may be separate commits if the plan says so |
| **Message** | Imperative subject ≤ 72 characters ("Construct ForwarderMiddleware directly in tests"), blank line, a short body with the *why* if it isn't obvious |
| **Never in a message without Timothy's explicit OK** | Issue or PR numbers (`#275`, `dotnet/yarp#275`, `GH-275`), GitHub URLs, `@mentions`, `Co-authored-by:` lines. Once pushed, these can appear on the upstream issue's timeline or notify people. Links belong in the PR description, which Timothy writes. The `commit-msg` hook blocks them; if he approves one, **he** makes that commit (the hook can only be bypassed by him) |
| **No AI attribution trailers** | Timothy discloses AI use in the PR description himself |
| **Push** | Only `git -C yarp push -u origin <the active issue's branch>`, and only when the plan or Timothy calls for it (settings ask him every time). Never `upstream`, never `--force`, never a protected branch. Pushing to his fork notifies nobody upstream, but the branch is visible to anyone browsing the fork |

## 7. What is always Timothy's (the gates)

| Gate | Meaning | You |
|---|---|---|
| **G1** | Plan approved → implementation may start | Never write product or test code for the issue before it (reading, building and running existing tests is fine) |
| **References** | A commit message mentioning an issue, PR or person | Ask; he commits it himself |
| **G2** | Anything posted upstream (comment, PR, review reply) | You draft; he edits and posts. You never post (`gh` is denied) |
| **G3** | After he has learned the change (lectures and/or a teach-back) → the PR may be opened | Never suggest opening the PR before he says G3 is open |

## 8. Writing lectures

Only when Timothy asks (`/lecture …`, or in words). Follow `link/current_context/07-lectures.md` and
`procedures/lecture.md`; read `05-timothy.md` and `06-competency-yarp.md` first. They go in the issue's
`lectures/` folder. You're well placed to write them because you did the work and know what went wrong on the way;
use that.

## 9. Keeping the shared files up to date

| When | Update |
|---|---|
| You create a branch | Add its row to `04-branches.md` (Active) in the same step |
| A step is done | Its Stage 5 entry in the issue's `plan.md` |
| Timothy decides something | Append to the issue's `shared/02-decisions.md` |
| Timothy says he read something, explains something, or asks something that shows what he does or doesn't know | `06-competency-yarp.md` (evidence column, dated) and your session report §7 |
| Session ends | `/handoff` (`procedures/handoff.md`): session report, `STATUS.md`, registry status |
| You think a rule in `current_context` is wrong | Say so in the session report's "Feedback on the setup"; edit it only if Timothy says to, and log it in `README.md` |

## 10. Never

1. Push to `upstream`, force-push, or push without the settings prompt being approved.
2. Open, edit or comment on issues or PRs (`gh` is denied); use the GitHub web UI.
3. Commit on a protected or unregistered branch, or bypass hooks (`--no-verify`, `-c core.hooksPath`).
4. Switch branches with uncommitted changes; stash, reset, clean or delete branches without asking.
5. Copy anything from `link/` or the project root into the clone (`yarp/`). Our notes must never end up in a PR.
6. Decide a design question tagged *ours* or *upstream*; claim something works without evidence.
7. Assume Timothy has read a document because it exists.
8. Ask Timothy for session scores or feedback at handoff (record them only if he volunteers them).
