# 02 · Layout: where everything is

> Read at every startup. One-line rule: **code changes happen only in `yarp/`; everything we write about the work
> goes under `link/`; nothing crosses between them.**

## 1. The project folder (where the CLI starts)

```
<project root>/                         e.g. /Volumes/<SSD>/open_source/yarp_project   (NOT a git repo)
├── CLAUDE.md          static copy (from link/static_files/). Imports the startup files from link/current_context/
├── .claude/           static copy: settings.json (permissions) + commands/ (stubs that call link/current_context/procedures/)
├── link/  ──symlink──► ~/Desktop/projects/exercises/yarp/      the dynamic half (git repo #1: exercises)
├── yarp/              THE clone of Timothy's fork (git repo #2). origin = Timothy-Lee-Grant/yarp,
│                      upstream = dotnet/yarp (fetch only; push URL disabled). The ONLY place code changes
└── scratch/           throwaway work outside the fork (quick experiments, publish output). Not versioned anywhere
    └── <issue folder>/  one subfolder per issue
```

The static files never change in place: if they need to change, Timothy re-copies them from `link/static_files/`.
Everything that changes is reached through `link/`.

## 2. What `link/` contains (the `exercises/yarp/` folder)

```
link/  (= ~/Desktop/projects/exercises/yarp/)
├── CLAUDE.md                 desktop Claude's orientation for the YARP folder (the CLI can ignore it)
├── current_context/          THIS configuration: manual, layout, project facts, branches, Timothy, competency,
│                             lecture guide, procedures, git hooks, issue template
├── static_files/             the master copies of the project root's static files + SETUP.md (how to build the project folder)
├── issues/                   one folder per issue (template: current_context/templates/issue/)
│   └── <issue#>_<slug>/
│       ├── README.md             public landing page for maintainers (short)
│       ├── CLAUDE.md             orientation for this issue
│       ├── shared/               the desktop ↔ CLI mailbox for this issue: brief, plan, decisions, mode, STATUS,
│       │                         sessions/, evidence/
│       ├── lectures/             lectures about this issue (001 the change, 002 how to test it, …; audio/)
│       ├── conversation/         desktop's linear conversation log with Timothy
│       ├── sample/               experiments outside the fork (+ sample/evidence/)
│       └── report/               lab report, if there is one
├── shared/                   project-level mailbox: notes between desktop and CLI not tied to one issue
├── yarp_scouting/            searches for YARP issues worth working on: shortlists and candidate notes
├── yarp_concepts/            long-lived concept lectures on YARP (reading + audio/)
└── 1764_websocket_idle_timeout/   the first YARP issue (docs, legacy layout). Leave it where it is: its files are
                              linked from a public docs PR
```

## 3. Which repo versions what

| Thing | Lives in | Versioned by | Pushed by |
|---|---|---|---|
| The code change | `yarp/` (the clone) | git repo #2 (the fork) | Timothy approves each push (settings ask) |
| Plans, evidence, reports, lectures, this config | `link/…` (= `exercises/yarp/…`) | git repo #1 (`exercises`, public on GitHub as `Timothy-Lee-Grant/playground`) | Timothy commits `exercises` when convenient. The CLI doesn't commit or push `exercises` unless he asks |
| Static files in the project root | project root | not versioned there; master copies in `link/static_files/` | — |
| `scratch/` | project root | nothing | — |

`exercises` is **public**: never write secrets, tokens, or candid evaluations of Timothy anywhere under `link/`.

## 4. Path cheat sheet (from the project root)

| You want | Path |
|---|---|
| An issue's plan | `link/issues/<f>/shared/plan.md` |
| An issue's status / session reports / evidence | `link/issues/<f>/shared/STATUS.md`, `…/sessions/`, `…/evidence/` |
| An issue's lectures | `link/issues/<f>/lectures/` |
| The branch registry | `link/current_context/04-branches.md` |
| Run all YARP tests | `cd yarp && ./test.sh` · one project: `./.dotnet/dotnet test test/ReverseProxy.Tests/` (see `03-yarp-project.md`) |
| Git on the clone without `cd` | `git -C yarp <command>` |

## 5. Things that look odd but are on purpose

- **The project root isn't a git repo.** It sits above the clone so our `CLAUDE.md` and `.claude/` can never be
  committed into a YARP pull request.
- **The startup imports in `CLAUDE.md` use the real absolute path** (`~/Desktop/projects/exercises/yarp/…`), not
  `link/…`. Relative `@` imports through a symlink didn't resolve in an earlier setup (iot, 2026-09-30). The `link`
  is for you to navigate; the imports don't depend on it.
- **`.claude/settings.json` lists `exercises/yarp` as an additional directory**, so writes through `link/` are
  allowed without a prompt each time.
- **Git hooks come from `link/current_context/git-hooks/`** via `git config core.hooksPath`, so they update
  whenever this folder does. Don't change `core.hooksPath` (denied).
