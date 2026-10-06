# iot CLI project: one clone, one folder per issue

> **What this is:** the versioned configuration for Timothy's Claude Code (CLI) project at
> `~/Desktop/projects/open_source/iot_project/`. Introduced 2026-10-05 (Timothy's design). Replaces the
> one-workspace-and-one-clone-per-issue layout (`oss-work/<repo>-<issue#>/`) for **new** dotnet/iot work.
> dotnet/iot#2328 keeps the old layout (`~/Desktop/projects/oss-work/iot-2328/`) until its PR #2611 is done.

## The layout

```
~/Desktop/projects/exercises/iot/                     git repo #1 (desktop Claude sees this)
├── cli-project/            ◄── this folder: the real copies of the CLI project's config
│   ├── CLAUDE.md               project instructions for the CLI (shared clone rules, how to start)
│   ├── issues.md               registry: issue folder ↔ branch; protected branches
│   ├── .claude/settings.json   permissions (push only to origin with OK; never upstream, never force, no gh)
│   ├── .claude/commands/       /issue <folder> (load context + switch branch) and /handoff
│   └── setup.sh                creates/refreshes the links below; configures remotes
└── <issue folder>/shared/  each issue's mailbox (brief, plan, decisions, STATUS, sessions, evidence)

~/Desktop/projects/open_source/iot_project/           the CLI starts here (not a git repo)
├── CLAUDE.md, ISSUES.md, persona.md, .claude/ → symlinks into exercises
├── iot/                    git repo #2: THE clone (origin = fork, upstream = dotnet/iot fetch-only)
├── new-device-binding/  →  exercises/iot/new-device-binding/shared
├── <issue#>_<slug>/     →  exercises/iot/<issue#>_<slug>/shared          (future issues)
└── scratch/<folder>/       per-issue work outside the fork (oracles, publish output); not versioned
```

## How a session goes

```bash
cd ~/Desktop/projects/open_source/iot_project && claude
/issue new-device-binding      # loads that issue's shared/ files, checks the clone, switches to its branch if safe
...work...
/handoff                        # session report + STATUS into that issue's shared/
```

## Safety, in layers

| Risk | Guard |
|---|---|
| Touching #2328's branch / PR #2611 | Listed as protected in `issues.md`; checkout/switch/push of `fix/2328*` denied in settings; and #2328 lives in a different clone anyway |
| Pushing to dotnet/iot | `upstream` push URL is set to an invalid value by `setup.sh`; `git push upstream` denied in settings |
| Opening a PR or posting by accident | `gh` denied; the CLI never uses the GitHub web UI |
| Losing work when switching issues | CLI never switches with uncommitted changes; stash/reset/clean ask first |
| Rewriting pushed history | force-push denied (Timothy does it himself if ever needed) |

Pushing a branch to the fork (`origin`) notifies nobody at dotnet/iot and creates no PR. The branch is visible to
anyone who browses the fork, and GitHub offers *you* a "Compare & pull request" button: just don't click it.

## Adding an issue

See `issues.md` → "Adding an issue". Then re-run `setup.sh`.
