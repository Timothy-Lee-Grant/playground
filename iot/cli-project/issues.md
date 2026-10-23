# Issue registry for the iot CLI project

> **Owner: desktop Claude** (Timothy decides; desktop records). **CLI: read-only**; if a row is wrong, say so in
> the session report. Symlinked into the project root as `ISSUES.md`.
>
> One row per piece of work the CLI may touch. The CLI may **only** switch to, commit on, or push a branch listed
> under "Active". Everything else is protected.

## Active

| Folder (in `iot_project/`) | What | Upstream ref | Branch | Base | Remote push | Status (date) |
|---|---|---|---|---|---|---|
| `bmp3xx-followup` | Follow-up to the merged Bmp3xx binding: fix F1–F3 from the second review (simulator busy window, pending time on a repeated Normal request, stale conf_err) | PR 2627 (merged); no issue | `fix/bmp3xx-normal-mode-followup` | `upstream/main` (≥ `4108607`) | `origin` (Timothy's fork) only, with his OK | Set up 2026-10-22; branch not created yet (plan Step 1, after G1) |

## Protected (never check out, commit, rebase, reset or push from this project)

| Branch | Why |
|---|---|
| `feature/bmp3xx-binding` | dotnet/iot PR 2627 **merged** 2026-10-22 (upstream `4108607`). Done; a follow-up gets its own branch from `upstream/main` |
| `main` | Mirrors `upstream/main` only when Timothy asks. Never commit on it |
| `fix/2328-gpiobutton-initial-state` | dotnet/iot#2328, PR #2611 **merged** (upstream `c8e4589f`). Worked only from `~/Desktop/projects/oss-work/iot-2328/` (the old layout, left intact on purpose) |
| anything not listed under "Active" | Ask Timothy |

## Adding an issue (desktop)

1. Create the issue folder in `exercises/iot/<issue#>_<slug>/` (templates: `exercises/ai-workflow/templates/issue/`).
2. Add a row under "Active" (folder name = the exercises folder name; branch; base).
3. Add the issue's real `shared/` path (and `sample/`, if the CLI writes there) to `.claude/settings.json`
   `additionalDirectories`.
4. Timothy re-runs `bash ~/Desktop/projects/exercises/iot/cli-project/setup.sh`: it creates the new symlink.
