# dotnet/iot new binding (BMP3xx) — Conversation Log (desktop)

> One linear log: Timothy's questions, answers, decisions and progress, newest at the bottom. A new session reads it
> top to bottom. Entry markers: 📍 progress · 🧭 decision · ❓→💬 question and answer.

## Where we are now *(updated in place)*

**Stage:** G1 open. CLI session 001 (2026-10-05) did Steps 0–11 without hardware: binding, 65/65 tests, sample + README; branch `feature/bmp3xx-binding` pushed to the fork @ `8d23d3d4`. **Open:** U4 gap (pressure when only temperature is out of range), fritzing diagram, hardware Steps 2/3/13 (no sensor yet). **Next:** CLI Step 12 (hygiene) + Step 15 (implementation summary); then desktop writes lecture 1. Last entry: #4.

## Index

| # | Date | Entry |
|---|---|---|
| 1 | 2026-10-04 | 🧭 Investigation: should I add a sensor binding, which device |
| 2 | 2026-10-05 | 📍 Implementation blueprint (`shared/plan.md` v1) and mode-P scaffolding |
| 3 | 2026-10-05 | 🧭 Shared-clone project layout; pushing to the fork allowed (plan v2) |
| 4 | 2026-10-05 | 📍 CLI session 001: Steps 0–11 done without hardware; pushed to the fork |

---

## #1 · 2026-10-04 · 🧭 Decision prep: should I add a new sensor binding, and which?

❓ Timothy: would contributing a new sensor binding (no issue behind it) be a good direction, how much would he
learn, would maintainers welcome it, would a hiring manager value embedded-flavored .NET work, and which device?

💬 Answered in three documents in this folder: [`001`](../001-should-i-add-a-sensor-binding.md) (welcome evidence:
roadmap item, "Please do!", every new-binding PR #2300–#2616 landed except the newest; learning map; career
framing), [`002`](../002-choosing-the-device.md) (BMP390/BMP388 recommended; INA226 via `Ina236` runner-up),
[`003`](../003-what-youd-build-and-the-steps.md) (cast, files, steps, draft proposal issue). Evidence:
[`evidence/001-…`](../evidence/001-upstream-new-binding-survey-2026-10-04.txt).

## #2 · 2026-10-05 · 📍 Implementation blueprint for the CLI

Timothy asked for a very detailed implementation guide that the CLI will follow to build the binding.

- Scaffolded the mode-P structure **inside this folder** (no issue number exists, so no `<issue#>_<slug>` folder;
  the workspace is `~/Desktop/projects/oss-work/iot-bmp3xx/`). Templates from `ai-workflow/templates/issue/`.
- Wrote [`shared/01-brief.md`](../shared/01-brief.md) (chip facts from Bosch's reference driver, constraints, build
  commands) and [`shared/plan.md`](../shared/plan.md) v1: Stage 1 scope and definition of done; Stage 2 with 17
  decisions (O1–O9 ours, U1–U8 upstream), each with a proposal and why; Stage 3 with 21 steps in six phases and a
  detailed card per step (goal, commands, outputs, done-when, stop-if); Stage 4 draft proposal issue; Stage 6
  lecture spec; Stage 7 PR skeleton.
- Key design choices proposed: new `Bmp3xx` folder; mirror `Bmp280`'s API; `Bmp3xxBase` + `Bmp390`/`Bmp388`;
  internal calibration class tested directly; tests first against a simulated chip and **an external reference
  oracle (Bosch's C driver, built outside the fork: clean room)**; hardware probe E1 before the binding.
- `00-start-here.md` §7 added (extra writable places, clean-room rule, Pi rules); settings allow `sample/` and ask
  before `ssh`/`scp`/`rsync`.
- Verified while writing: Ina236/Bmxx80/Vcnl4040 project patterns, `build.proj` test glob, analyzer/docs rules,
  `I2cSimulatedDeviceBase` API, `Bmp280` public API (all on `main` @ `95384e7`). Chip facts are **unverified against
  the datasheet**; plan Step 0 does that.

## #3 · 2026-10-05 · 🧭 Decision: one shared clone, one folder per issue; push to the fork

Timothy's new design for developing on open-source projects: **one folder per upstream project**
(`~/Desktop/projects/open_source/iot_project/`) holding **one clone** of his fork (`iot/`), a project-level
`CLAUDE.md`, `persona.md`, and **one folder per issue that is a symlink to that issue's `shared/` mailbox**. He tells
the CLI which issue at start; it loads that issue's context and switches to its branch. #2328 stays exactly as it
is (old workspace `oss-work/iot-2328/`, PR #2611 awaiting review). He wants to push the binding's commits to his
fork for safekeeping and study, without announcing anything upstream yet.

**Done (desktop):**
- New `iot/cli-project/` (versioned config): `CLAUDE.md` (shared-clone rules), `issues.md` (issue ↔ branch registry;
  `fix/2328-gpiobutton-initial-state` and `main` protected), `.claude/settings.json` (push asks; upstream push,
  force-push, pushes/checkouts of `fix/2328*` and `main`, `gh` denied), `.claude/commands/issue.md` (`/issue <folder>`:
  load context, switch branch only if safe) and `handoff.md`, `setup.sh` (adds `upstream` fetch-only with a
  disabled push URL; creates the symlinks; never touches branches), `README.md`.
- This issue: `00-start-here.md` §1/§3/§4/§7, `01-brief.md`, `plan.md` → **v2** (paths; Step 14 adds pushing to
  `origin`), `CLAUDE.md`, STATUS, D3/D4 in `02-decisions.md`. `workspace/` marked superseded.
- Not changed: `ai-workflow/templates/` still scaffold the old per-issue workspace (see the note in
  `ai-workflow/README.md`); #2328's folder.

## #4 · 2026-10-05 · 📍 CLI session 001: the binding exists (Steps 0–11, no hardware)

Read from `shared/STATUS.md`, `shared/sessions/001-2026-10-05.md` and the Stage 5 entries in `shared/plan.md`.

- **Built:** `src/devices/Bmp3xx/` (14 source files, sample, README, `category.txt`, 4 test files), nothing outside it.
  5 commits on `upstream/main` @ `336e4696`, last `8d23d3d4`; pushed to `origin` (Timothy pushed). 65/65 tests,
  0 warnings, suite 0.8 s. Evidence `001`–`013` (`003`/`004` reserved for hardware).
- **Decisions:** D5 G1 granted; D6 output-data-rate values named by period (`Period5Milliseconds` …).
- **Datasheet findings:** BMP390 timing formula differs from BMP388's (implemented per chip); writes are
  (register, value) pairs; forced mode self-returns to sleep; soft-reset completion via EVENT `por_detected`;
  reset value `0x800000` collides with a room-temperature reading → "both fields" rule.
- **Unverified until hardware:** legal mode transitions, `cmd_rdy` when idle, `conf_err` timing, Normal mode at
  200 Hz ×1/×1.
- **Open for Timothy:** U4 gap; fritzing diagram. Desktop recommendation in the chat reply of 2026-10-05.
- **CLI process note:** it ran a path-only `git reset` to preview a commit (rule says ask first); reported it itself.

