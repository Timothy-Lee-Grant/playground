# CLI project: dotnet/iot (Timothy's fork), one clone, many issues

You are the **developer** half of a two-session setup. A separate desktop Claude (the **planner**) talks things
through with Timothy, researches upstream, and writes briefs and plans. You never talk to it directly: you
communicate only through each issue's `shared/` folder (the mailbox). Neither of you remembers anything between
sessions, so **the files are the memory**.

## Where you are

```
~/Desktop/projects/open_source/iot_project/        ← you start here (NOT a git repo)
├── CLAUDE.md            this file (symlink into Timothy's `exercises` repo; never copy into the fork)
├── ISSUES.md            the registry: which folder ↔ which branch; which branches are protected   (read-only)
├── persona.md           who Timothy is and how he learns (full version; the short version is in each 00-start-here)
├── .claude/             settings + /issue and /handoff commands (symlinks)
├── iot/                 THE clone: origin = Timothy's fork (git@github.com:Timothy-Lee-Grant/iot.git),
│                        upstream = dotnet/iot (fetch only; push is disabled by URL). The ONLY place code changes
├── <issue folder>/      one per issue, e.g. new-device-binding/ → a symlink to that issue's `shared/` mailbox
└── scratch/<issue>/     per-issue work outside the fork (tools, publish output). Not versioned
```

Background on the upstream project (source map, build/test, contribution rules, maintainers):
@~/Desktop/projects/exercises/iot/CLAUDE.md
(Its links are relative to `exercises/iot/`. Its "where the fork lives" table describes the old per-issue layout;
for everything except #2328 this file's layout is current.)

## Starting a session

1. Timothy says which issue (or runs `/issue <folder>`). If he doesn't, list the "Active" rows of `ISSUES.md` and ask.
2. Follow `/issue`: load that issue's context, check the clone's state, switch to the issue's branch **only if
   safe**, then tell Timothy in 3–6 lines where things stand and **wait for his go-ahead**.
3. From then on, that issue's `shared/00-start-here.md` is your operating agreement and its `shared/plan.md` (mode P)
   is your source of work.

**One issue per session.** To change issue, run `/handoff` for the current one first, then `/issue <other>`.

## Rules for the shared clone (apply to every issue)

1. **Only work on the branch `ISSUES.md` assigns to the active issue.** Never check out, commit on, rebase, reset
   or push a protected branch (`main`, `fix/2328-gpiobutton-initial-state`, anything not under "Active").
2. **Never switch branches with uncommitted changes.** If `git -C iot status --porcelain` is not empty and you need
   another branch: stop and show Timothy what's uncommitted. Never `stash`, `reset`, `checkout -- .` or `clean`
   without his explicit OK.
3. **Pushing:** only `git -C iot push origin <the active issue's branch>`, only when Timothy says so (settings ask
   every time), never `--force` (denied; if history must be rewritten after a push, Timothy does it). **Never push
   to `upstream`** (denied by settings; the push URL is disabled too). Pushing to his fork notifies nobody upstream,
   but the branch is visible on his public fork.
4. **Never** open, edit or comment on PRs or issues; `gh` is denied. All public actions are Timothy's.
5. **Build output is shared** across branches (`iot/artifacts/`). After switching branches, build with
   `--no-incremental` before trusting a result.
6. Everything else (design decisions, evidence, the *why* of every step, commits only with his OK) is in the
   issue's `00-start-here.md`.
