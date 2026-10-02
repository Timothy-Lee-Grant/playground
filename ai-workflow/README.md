# AI workflow: desktop Claude + Claude Code CLI on one issue

> **What this is:** how two AI sessions that never talk to each other, and never remember anything between
> sessions, stay aligned on one upstream issue. **The files are the only memory.** This doc defines which files
> exist, who writes each one, and when.
>
> **Agreed:** 2026-09-30 (Timothy + desktop Claude, scouting 002). **Status:** v1, expected to change. The *way* of
> working with the AI is an experiment; see [`interaction-modes.md`](interaction-modes.md).

---

## 1. The cast

| Character | Where it runs | Its job | What it can see |
|---|---|---|---|
| **Timothy** | everywhere | Decides. Writes code when the mode says so. Commits, pushes, posts upstream | everything |
| **Desktop Claude** ("the planner") | Claude desktop app, `exercises` folder connected | Talks things through with Timothy, teaches, researches upstream, writes the brief, decisions and work orders; reads the CLI's reports | the whole `exercises` repo |
| **CLI Claude** ("the developer") | Claude Code in the terminal, started in the issue's workspace root | Works on the fork with Timothy; runs builds and tests; writes status and session reports | the workspace root: `develop/` (the fork) + `shared/` |
| **`shared/`** ("the mailbox") | a folder inside the issue folder in `exercises`, **symlinked** into the CLI workspace | The only channel between the two Claudes | both |

---

## 2. The layout

```
~/Desktop/projects/exercises/                         git repo #1: the workbench (desktop Claude sees this)
└── <repo>/<issue#>_<slug>/
    ├── README.md, CLAUDE.md, conversation/, sample/, report/     (v2 issue layout, root CLAUDE.md §2)
    ├── shared/                  ◄── the mailbox: real files live HERE
    │   ├── 00-start-here.md         desktop  → CLI   how to operate (auto-loaded)
    │   ├── 01-brief.md              desktop  → CLI   the issue, evidence, constraints (auto-loaded)
    │   ├── 02-decisions.md          both, append-only; Timothy is always the decider
    │   ├── 03-next.md               desktop  → CLI   the current work order (auto-loaded; superseded by plan.md in mode P)
    │   ├── plan.md                  desktop body + everyone's entries: the living plan in mode P (auto-loaded)
    │   ├── 04-interaction-mode.md   Timothy  → CLI   how to work together right now (auto-loaded)
    │   ├── STATUS.md                CLI      → desktop  one-screen state, rewritten each session
    │   ├── sessions/NNN-date.md     CLI      → desktop  one report per CLI session (/handoff)
    │   └── evidence/                CLI      → both     saved test/build output
    └── workspace/               the CLI workspace's config, kept here so it's versioned
        ├── CLAUDE.md                root instructions: imports shared/ files
        ├── .claude/settings.json    permissions (no push, no gh, ask before commit)
        ├── .claude/commands/handoff.md   the /handoff command
        └── setup.sh                 creates the workspace below (run once)

~/Desktop/projects/oss-work/<repo>-<issue#>/          the CLI workspace root (NOT a git repo)
├── CLAUDE.md           → symlink to workspace/CLAUDE.md
├── .claude/            → settings.json and commands/ are symlinks into workspace/.claude/
├── shared              → symlink to the issue's shared/
└── develop/<repo>/     git repo #2: Timothy's fork (the only place code changes)
```

**Why the workspace root sits above the fork:** Claude Code auto-loads `CLAUDE.md` from the folder it starts in.
Keeping ours one level above the fork means it can never be committed into an upstream PR.

**Why symlinks, not copies:** a copy drifts, and reports would have to be pasted back by hand. With a symlink there
is exactly one copy, it lives in `exercises`, and it's versioned by committing `exercises` as usual.
Limit: symlinks only work on one machine. If the CLI runs on the Linux desktop, sync `exercises` with git instead.

---

## 3. One writer per file

Two writers on one file eventually overwrite each other, so every file has a single owner.

| File | Owner | Others may |
|---|---|---|
| `00-start-here.md`, `01-brief.md`, `03-next.md` | Desktop Claude | CLI: read only. If something's wrong, say so in the session report |
| `plan.md` (mode P) | body: desktop (versioned) · entries: append-only, by author | CLI: Stage 5 entries and CHANGE REQUESTs only |
| `04-interaction-mode.md` | Timothy (desktop edits it for him) | CLI: may change the **Current mode** block only when Timothy says so in the session, and logs it |
| `02-decisions.md` | append-only, anyone | nobody edits an old entry; corrections are new entries |
| `STATUS.md`, `sessions/`, `evidence/` | CLI Claude | desktop: read only |
| `workspace/*` | Desktop Claude | CLI: read only |

---

## 4. The loop

```
 ┌─ DESKTOP (Timothy + planner) ───────┐            ┌─ CLI (Timothy + developer) ─────────┐
 │ talk, learn, decide                 │            │ `claude` in the workspace root:     │
 │ write brief / decisions / next      ├──────────► │   auto-loads 00, 01, 03, 04         │
 │ set the interaction mode            │  shared/   │   reads STATUS + decisions          │
 │                                     │            │   says what it understands; waits   │
 │ "read the latest handoff"           │ ◄──────────┤ works the order in the chosen mode  │
 │ file learning notes (private/)      │            │ `/handoff` → session report, STATUS │
 └─────────────────────────────────────┘            └─────────────────────────────────────┘
```

1. **Desktop:** discuss, decide, write/refresh `03-next.md` (one work order: goal, done-when, not-doing).
2. **CLI:** start `claude` in the workspace root. It confirms the state and the mode, then works with Timothy.
3. **CLI:** end with `/handoff`. It writes `sessions/NNN-YYYY-MM-DD.md` and rewrites `STATUS.md`.
4. **Desktop:** Timothy says "read the latest handoff". Desktop reads it straight from `exercises` (no pasting),
   files the learning observations into `private/`, records the mode experiment in
   [`interaction-modes.md`](interaction-modes.md) §4, and writes the next work order.
5. **Timothy:** commits `exercises` whenever convenient (the mailbox is versioned there), and pushes the fork when
   *he* decides.

---

## 5. Setting up a new issue

1. Desktop creates the issue folder with `shared/` and `workspace/` filled in (copy the latest issue's
   `workspace/` and `00-start-here.md`, then change names and paths).
2. Timothy forks the upstream repo on GitHub (web UI).
3. Timothy runs `bash <issue folder>/workspace/setup.sh`. It creates the workspace, the symlinks and the clone.
4. `cd ~/Desktop/projects/oss-work/<repo>-<issue#> && claude`.

If Claude Code ever ignores a symlinked `settings.json` or command, copy that one file instead and note it here.

---

## 6. Changelog

| Date | Change |
|---|---|
| 2026-09-30 | v1: layout, ownership, loop, `/handoff`; first used for iot#2328 and yarp#275 |
| 2026-09-30 | `@` imports in `workspace/CLAUDE.md` must be **absolute** (`@~/...`): relative ones don't resolve through the symlink (found in iot#2328 session 001). Added fallback "read them yourself"; chunked explanations; optional scores |
| 2026-10-01 | `/handoff` no longer asks Timothy for scores or feedback; the CLI records them only if volunteered, plus its own observations |
| 2026-09-30 | **Mode P** (plan-driven, Timothy observes) + `shared/plan.md` with gates G1–G3 and deviation rules (`00-start-here.md` §6). First used on iot#2328. M0–M4 kept for later |
