# Procedure: pre-PR check (`/pre-pr`, or "run the pre-PR check")

> Added 2026-10-22 (desktop). Run when the issue's `shared/02-decisions.md` or `STATUS.md` says the PR is the next
> step. Goal: a PR that meets YARP's standards and the usual etiquette **before** Timothy pushes it. You check,
> fix trivial things (with his OK), and report go / no-go. **You never push without his say-so and never open the
> PR** (`gh` is denied); he does both. `<f>` = issue folder, `<b>` = its branch, `<n>` = issue number.
>
> What YARP asks (CONTRIBUTING, checked 2026-10-22): the repo builds and all tests pass; follow the project's
> conventions. No PR template, no AI policy. `CODEOWNERS` auto-requests **@MihaZupan** as reviewer. PRs are
> squash-merged (merged commits end in `(#NNNN)`), so a few logical commits are fine: don't squash unless asked.

Save every run's raw output to `link/issues/<f>/shared/evidence/NNN-prepr-*.txt` (header: date, machine, SDK,
commit, exact command). Number them after the newest file there.

| # | Check | How | Pass when |
|---|---|---|---|
| 0 | **Ready** | `/issue <f>` done; `git -C yarp status --short` | On `<b>`; nothing modified or staged (an untracked `.DS_Store` is fine; never add it) |
| 1 | **Up to date with upstream** | `git -C yarp fetch upstream`; `git -C yarp rev-list --left-right --count upstream/main...HEAD` | Behind = 0. If behind and `<b>` isn't on origin yet: propose `git -C yarp rebase upstream/main` (he approves), then `--no-incremental` rebuild. Conflicts → stop and report. Already pushed → ask him (no force-push) |
| 2 | **Thread unchanged** | WebFetch `https://api.github.com/repos/dotnet/yarp/issues/<n>/comments` and `https://api.github.com/search/issues?q=repo:dotnet/yarp+is:pr+<n>` (if WebFetch is unavailable, ask Timothy to look) | No new maintainer reply that changes scope, no one else's open PR for it. Anything new → stop and report it verbatim |
| 3 | **Scope** | `git -C yarp diff --stat upstream/main...HEAD`; compare with the plan's file list | Only the planned files; no product code if the plan says test-only; no scratch, evidence, `.DS_Store`, IDE files |
| 4 | **Whitespace** | `git -C yarp diff --check upstream/main...HEAD` | No output |
| 5 | **Commits** | `git -C yarp log --format='%h %an <%ae>%n  %s%n%b' upstream/main..HEAD`; upstream style: `git -C yarp log upstream/main --no-merges -15 --format=%s` | Author email is one on his GitHub account (no `*.local` hostname emails; the CLA bot matches by email). Subjects imperative, ≤ ~72 chars, like upstream's. No WIP/fixup commits, no AI trailers, no issue/person references he hasn't approved |
| 6 | **Formatting** | For each touched project: `./.dotnet/dotnet format whitespace <csproj> --verify-no-changes --include <changed files>` | No findings **on changed lines** (pre-existing ones elsewhere aren't ours; note them, don't fix) |
| 7 | **Build + all tests, like CI** | `./build.sh --configuration Release --rebuild --test` on `<b>`. If upstream moved since the last baseline, run the same on detached `upstream/main` (then switch back; `--no-incremental`) | 0 errors, **0 warnings** (it passes `/warnaserror`), 0 failed; per-project, per-framework totals equal the baseline (or differ exactly as the plan says). A failure on both branch and `main` is pre-existing: rerun once, then note it |
| 8 | **Reviewer read-through** | Read the whole `git -C yarp diff upstream/main...HEAD` as MihaZupan would | No leftover comments/TODOs, dead code, unused usings, naming that differs from the surrounding tests, or quiet behavior changes (e.g. loose vs strict mocks). Trivial fixes: propose a new commit, he OKs it, then redo 4–7 |
| 9 | **PR text** | Check `link/issues/<f>/shared/pr-description.md` against this run's evidence | Title: short, imperative, like upstream's. Body: what changed and why, scope, how it was verified (numbers + environment from **this** run), follow-ups. Linking keyword right: `Fixes #<n>` only if it resolves the whole issue, otherwise `Contributes to #<n>`. No AI statement and no link to the playground repo (exercises root `CLAUDE.md` §5), unless upstream policy requires one: re-check CONTRIBUTING and `.github/` first |

## Report (go / no-go)

A table of checks 0–9 with ✅ / ❌, one line each, and the evidence file. Then:

- **All ✅:** give Timothy the next actions, in order:
  1. Push: `git -C yarp push -u origin <b>` (run it yourself only when he says so; settings will ask him).
  2. Open `https://github.com/dotnet/yarp/compare/main...Timothy-Lee-Grant:yarp:<b>?expand=1`, paste the title and
     body from `pr-description.md`, and create the PR (not a draft, unless he wants one).
  3. After it's open: the CLA bot should pass (CLA signed 2026-10-02); CI runs on Windows and Linux too, which we
     can't run on the Mac, so watch the checks. Send the PR link to the session.
- **Any ❌:** say what's needed, propose the fix, and wait.

## Afterwards

Update `STATUS.md`, the `04-branches.md` row (*On origin?*), a Stage 5 entry in `plan.md`, and (once he sends the
link) a `[Timothy]` line in `02-decisions.md` with the PR number. Then `/handoff`.
