# Issue registry for the iot CLI project

> **Owner: desktop Claude** (Timothy decides; desktop records). **CLI: read-only**; if a row is wrong, say so in
> the session report. Symlinked into the project root as `ISSUES.md`.
>
> One row per piece of work the CLI may touch. The CLI may **only** switch to, commit on, or push a branch listed
> under "Active". Everything else is protected.

## Active

| Folder (in `iot_project/`) | What | Upstream ref | Branch | Base | Remote push | Status (date) |
|---|---|---|---|---|---|---|
| `new-device-binding` | New binding: Bosch BMP390/BMP388 (`src/devices/Bmp3xx/`) | dotnet/iot PR 2627 (opened 2026-10-18; no proposal issue) | `feature/bmp3xx-binding` | `upstream/main` | `origin` (Timothy's fork) only, with his OK | **PR open** @ `fc7d7b57` (79/79 tests, hardware checks pass). In review: CLI may commit review fixes on this branch; pushes on Timothy's word; Timothy posts every reply (2026-10-18) |

## Protected (never check out, commit, rebase, reset or push from this project)

| Branch | Why |
|---|---|
| `main` | Mirrors `upstream/main` only when Timothy asks. Never commit on it |
| `fix/2328-gpiobutton-initial-state` | dotnet/iot#2328, PR #2611 **merged** (upstream `c8e4589f`). Worked only from `~/Desktop/projects/oss-work/iot-2328/` (the old layout, left intact on purpose) |
| anything not listed under "Active" | Ask Timothy |

## Adding an issue (desktop)

1. Create the issue folder in `exercises/iot/<issue#>_<slug>/` (templates: `exercises/ai-workflow/templates/issue/`).
2. Add a row under "Active" (folder name = the exercises folder name; branch; base).
3. Add the issue's real `shared/` path (and `sample/`, if the CLI writes there) to `.claude/settings.json`
   `additionalDirectories`.
4. Timothy re-runs `bash ~/Desktop/projects/exercises/iot/cli-project/setup.sh`: it creates the new symlink.
