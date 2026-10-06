# dotnet/iot new binding (BMP3xx) — Conversation Log (desktop)

> One linear log: Timothy's questions, answers, decisions and progress, newest at the bottom. A new session reads it
> top to bottom. Entry markers: 📍 progress · 🧭 decision · ❓→💬 question and answer.

## Where we are now *(updated in place)*

**Stage:** plan v1 written (2026-10-05); **waiting for G1.** Last entry: #2. Open decisions: everything in `shared/plan.md` Stage 2 (Timothy may change any before G1); when to start upstream activity (#2611 still open). Next: Timothy reviews the plan and grants G1; buys the sensor; runs `workspace/setup.sh`.

## Index

| # | Date | Entry |
|---|---|---|
| 1 | 2026-10-04 | 🧭 Investigation: should I add a sensor binding, which device |
| 2 | 2026-10-05 | 📍 Implementation blueprint (`shared/plan.md` v1) and mode-P scaffolding |

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

