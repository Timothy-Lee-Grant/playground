# 04 · Branch registry: which branch belongs to which issue

> Read at every startup. **The single place to look up what a branch is for.** One row per branch in Timothy's
> YARP fork (and the clone). Full history and reasoning for a branch live in its issue folder; here, one sentence.
>
> **Who edits:** the CLI adds a row in the same step it creates a branch, and updates *Status* at `/handoff`;
> desktop may edit anything. Rows are never deleted: finished branches move to **Done**.
>
> **The git hooks read the "Active" table** (`git-hooks/pre-commit` and `pre-push`): commits and pushes are only
> allowed on branches listed there. Keep the format: the branch in backticks in the second column.

## Active (the CLI may switch to, commit on, and with Timothy's OK push these)

| Issue folder (`link/issues/…`) | Branch | What it's for | Base | On origin? | Status (date) |
|---|---|---|---|---|---|
| `275_remove-autofac-from-tests` | `remove-autofac-275` | dotnet/yarp#275 phase 1: build each test class's subject directly instead of through Autofac's AutoMock (7 test files), then delete `TestAutoMockBase` and the two Autofac packages. Moq stays | `upstream/main` | **not created yet** in this clone (it existed, empty, in the old `oss-work/yarp-275` clone) | G1 granted 2026-10-02; plan Steps 0–1 done; next Step 2 (build + baseline). Create from `upstream/main` when work starts (2026-10-07) |

## Protected (never commit on, rebase, reset or push from this project)

| Branch | Why |
|---|---|
| `main` | Mirrors `upstream/main`. Update it only when Timothy asks (`git -C yarp switch main && git -C yarp merge --ff-only upstream/main`); never commit on it |
| `tgrant/exploration-phase` | On origin. Timothy's study notes from June 2026 (`concepts/` folder, no code, unrelated to any issue). Keep as is |
| `archive/pre-275-2026-10` | Local copy of `tgrant/exploration-phase`, made 2026-10-02 in the old `oss-work/yarp-275` clone. Not needed in the new clone |
| anything not listed under Active | Ask Timothy first |

## Done (merged, abandoned or superseded; kept for the record)

| Issue folder | Branch | What it was for | Outcome (date) |
|---|---|---|---|
| — | — | — | — |

## How to add a branch

1. The issue's plan says to create it (normally right after G1), or Timothy asks.
2. Name: `<short-purpose>-<issue#>` for issue work (e.g. `remove-autofac-275`); `try/<idea>-<issue#>` for an
   experiment that may be thrown away (a spike). Several branches per issue are fine: one row each, and say what
   each is testing.
3. Create it: `git -C yarp fetch upstream && git -C yarp switch -c <branch> upstream/main` (or the base the plan
   names).
4. Add the row here **before the first commit** (the `pre-commit` hook checks).
5. When it's pushed to origin for the first time, set *On origin?* to `yes (date)`.

## How to look for a branch that should exist

`git -C yarp branch --list '<branch>'` (local) → `git -C yarp fetch origin && git -C yarp branch -r --list 'origin/<branch>'`
(Timothy's fork). If the row says it exists but neither has it, or a branch exists that isn't listed, **tell
Timothy**; don't guess.
