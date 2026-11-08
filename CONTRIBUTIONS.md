# Contributions: the single source of truth

> **What this is:** the one place that says what I've contributed upstream, what I did in each contribution, and
> where everything stands. Every other file that mentions a contribution's status (root `README.md`, `<repo>/CLAUDE.md`,
> issue `CLAUDE.md` files, `open_source_persona.md` §10.1, scouting trackers) should agree with this file. If they
> disagree, **this file wins** and the other file is drift (see §5).
>
> **Also the hand-off file for other AI projects** (resume, interview prep): give them this file first. Each row links
> to a deeper recap where one exists.
>
> **Last verified against GitHub:** 2026-11-07 (every PR's state, merge date and merger checked through the GitHub API).
> Re-check any time with `python3 scripts/check-contributions.py`.

---

## 1. At a glance

| | |
|---|---|
| Upstream PRs opened | **5** (dotnet/iot ×3, dotnet/yarp ×1, dotnet/AspNetCore.Docs ×1) |
| Merged | **5 of 5** |
| Open PRs | none |
| Maintainers who merged my work | MihaZupan (YARP, ×2), pgrawehr (dotnet/iot, ×2), raffaeler (dotnet/iot) |
| Period | 2026-09-28 (first PR) → 2026-11-05 (latest merge) |
| Work in progress | none (next issue not picked yet; candidates in `scouting/003-issue_shortlist_october_2026.md`) |

---

## 2. Upstream contributions

Dates are when the PR was opened and merged (GitHub, UTC). Size is GitHub's count for the merged PR.

| # | Merged | Repo | PR · issue | Type | Size | Merged by | Workspace · recap |
|---|---|---|---|---|---|---|---|
| C1 | 2026-10-07 | dotnet/AspNetCore.Docs | [PR #37747](https://github.com/dotnet/AspNetCore.Docs/pull/37747) · contributes to [yarp#1764](https://github.com/dotnet/yarp/issues/1764) | Docs (backed by a reproduction) | 1 file, +8 / −1 | MihaZupan | [`yarp/1764_websocket_idle_timeout/`](yarp/1764_websocket_idle_timeout/) · lab report `implementations/001-*` · *no recap yet* |
| C2 | 2026-10-08 | dotnet/iot | [PR #2611](https://github.com/dotnet/iot/pull/2611) · fixes [#2328](https://github.com/dotnet/iot/issues/2328) | Bug fix + tests (first code PR) | 3 files, +98 / −0 | pgrawehr | [`iot/2328_gpiobutton-ispressed-not-initialized/`](iot/2328_gpiobutton-ispressed-not-initialized/) · *no recap yet* |
| C3 | 2026-10-22 | dotnet/iot | [PR #2627](https://github.com/dotnet/iot/pull/2627) · no issue (self-proposed) | New feature: Bmp3xx device binding | 24 files, ~2,800 lines, 15 commits | raffaeler (approved by raffaeler + pgrawehr) | [`iot/new-device-binding/`](iot/new-device-binding/) · [recap](iot/new-device-binding/report/001-contribution-recap.md) |
| C4 | 2026-10-26 | dotnet/yarp | [PR #3063](https://github.com/dotnet/yarp/pull/3063) · contributes to [#275](https://github.com/dotnet/yarp/issues/275) | Test refactor (remove Autofac) | 11 files, +96 / −176 | MihaZupan | [`yarp/issues/275_remove-autofac-from-tests/`](yarp/issues/275_remove-autofac-from-tests/) · [recap](yarp/issues/275_remove-autofac-from-tests/report/001-contribution-recap.md) |
| C5 | 2026-11-05 | dotnet/iot | [PR #2631](https://github.com/dotnet/iot/pull/2631) · follow-up to #2627 | Bug fixes + CI flaky-test fix | 5 files, +123 / −13, 6 commits | pgrawehr | [`iot/bmp3xx-followup/`](iot/bmp3xx-followup/) · *no recap yet* |

Opened: C1 2026-09-28 · C2 2026-10-03 · C3 2026-10-18 · C4 2026-10-23 · C5 2026-10-23.

---

## 3. What I did in each (interview-ready summaries)

### C1 · YARP WebSockets docs (AspNetCore.Docs #37747)

- **Problem:** YARP silently closes an idle WebSocket after its `ActivityTimeout` (100 s by default), and the WebSockets
  docs page didn't say so. Reported in yarp#1764 (open since 2022) and hit again by a user in yarp#2615.
- **What I did:** built a 3-process reproduction (client → YARP → echo server), measured the abort at 100.1 s, showed
  that a 30 s keep-alive fixes it, and found that a .NET `ClientWebSocket` survives by default (it sends a Pong every
  30 s) while browsers don't. That correction changed what the docs needed to say. Then wrote the docs paragraph.
- **Shows:** reverse proxies and WebSockets, evidence before claims, catching my own wrong assumption with data,
  contributing docs across repos (fix lives in AspNetCore.Docs, issue in YARP).

### C2 · `GpioButton.IsPressed` initial state (dotnet/iot #2611)

- **Problem:** `GpioButton.IsPressed` was wrong if the button was already held when the program started, because
  the state was only updated on edge events.
- **What I did:** wrote failing unit tests with a mocked GPIO driver (red), fixed the initialization from the pin
  level (green), coordinated with a parallel maintainer PR (#2608) by merging `main` and re-verifying, passed CI on
  3 OSes. Signed the .NET Foundation CLA.
- **Shows:** edge-triggered vs. level state, test-first bug fixing, working alongside maintainers' in-flight work.
  Maintainer: "Looks good, thanks. Bonus points for adding tests!"

### C3 · Bmp3xx binding for BMP390/BMP388 (dotnet/iot #2627)

- **Problem:** no driver for Bosch's current-generation pressure sensors (the library had the older BMP280).
- **What I did:** checked that maintainers accept new bindings, chose the device, then wrote a register-level I2C
  driver from the datasheet: calibration, compensation formulas, every setting, sample app, README, and 89 unit tests
  against a simulated chip. Bench-tested on a Raspberry Pi 4, which found a bug the simulator couldn't (the chip ignores
  a mode change mid-measurement). One review round, 3 findings fixed with regression tests. Recorded a demo video
  ([YouTube](https://www.youtube.com/watch?v=p0bjJdOGpkM)).
- **Shows:** my embedded background (datasheets, I2C, bench testing) combined with .NET library design, testability
  and CI. Deep recap: [`iot/new-device-binding/report/001-contribution-recap.md`](iot/new-device-binding/report/001-contribution-recap.md).

### C4 · Remove Autofac from YARP's tests (dotnet/yarp #3063)

- **Problem:** a `help wanted` issue from 2020: seven test classes built their subject through Autofac's `AutoMock`,
  hiding each class's real dependencies.
- **What I did:** proposed an Autofac-only first step on the issue, rewrote the seven classes to use explicit
  constructor injection with Moq mocks, deleted the base class and two packages (net −80 lines). Proved it was a pure
  refactor: equal test counts (3,978 in the main project), 0-warning Release build, and break-it checks per class.
  Merged in 4 days with no changes requested.
- **Shows:** dependency injection, test design, safe refactoring in a large unfamiliar codebase.
  Deep recap: [`yarp/issues/275_remove-autofac-from-tests/report/001-contribution-recap.md`](yarp/issues/275_remove-autofac-from-tests/report/001-contribution-recap.md).

### C5 · Bmp3xx follow-up fixes (dotnet/iot #2631)

- **Problem:** a second automated review of the merged binding left three findings, and my simulator tests were
  timing-flaky on macOS CI (red on `main` too, affecting other contributors).
- **What I did:** fixed two binding bugs (a repeated `SetPowerMode` dropped the pending measurement time; a stale
  `conf_err` flag made a valid request throw) and one simulator bug, each test-first; verified on the Pi; then made
  the flaky tests deterministic by injecting a `TimeProvider` with a manually advanced clock. 95 tests, 0 warnings.
- **Shows:** owning my code after merge, fixing a CI problem I caused quickly and openly, deterministic time in tests.

---

## 4. Open threads (not PRs)

| Item | State (checked 2026-11-07) | Next action | Owner |
|---|---|---|---|
| [yarp#1764](https://github.com/dotnet/yarp/issues/1764) | **Still open**, although the docs fix (C1) merged 2026-10-07. Last activity 2026-09-28; cross-repo PRs don't auto-close issues | Comment that #37747 is merged and ask whether it can be closed | Timothy |
| [yarp#275](https://github.com/dotnet/yarp/issues/275) | Open for the Moq half; Autofac half done (C4) | Decide whether to take the Moq follow-up | Timothy |
| [iot#2403](https://github.com/dotnet/iot/issues/2403), [iot#2600](https://github.com/dotnet/iot/issues/2600) | Parked 2026-09-29 (needs a major release / largely resolved by others). Studied only, nothing posted | None; kept as learning records | — |
| Next issue | Not picked. Shortlist `scouting/003` (2026-10-22) | Re-check the shortlist and pick | Timothy + desktop |

---

## 5. Drift log

Places where another file disagreed with GitHub or with this file. Add a row whenever a check finds one; tick it when fixed.

| Found | File | Says | Reality | Fixed |
|---|---|---|---|---|
| 2026-11-07 | `README.md` issues table, `yarp/1764_*/CLAUDE.md`, `yarp/current_context/03`, `05`, memory notes | PR #37747 open, awaiting review | Merged 2026-10-07 by MihaZupan || ✅ 2026-11-07 |
| 2026-11-07 | `README.md`, `open_source_persona.md` §10.1, `iot/CLAUDE.md` | (C5 / PR #2631 absent) | Opened 2026-10-23, merged 2026-11-05 by pgrawehr || ✅ 2026-11-07 |
| 2026-11-07 | `README.md` issues table | No rows for the Bmp3xx binding (C3) or follow-up (C5) | Both merged || ✅ 2026-11-07 |
| 2026-11-07 | `README.md` #2328 row, `iot/CLAUDE.md` #2328 row | "merged" *and* "awaiting maintainer reply, PR planned" in the same cell | Merged 2026-10-08 || ✅ 2026-11-07 |
| 2026-11-07 | `iot/2328_*/CLAUDE.md` Status | "Upstream comment not posted yet. Fork not cloned yet" (2026-09-30) | Done and merged || ✅ 2026-11-07 |
| 2026-11-07 | `iot/new-device-binding/CLAUDE.md` Status, conversation "Where we are now", `iot/CLAUDE.md` row | Stuck at 2026-10-05 ("waiting for G1") | Merged 2026-10-22 (recap is current) || ✅ 2026-11-07 |
| 2026-11-07 | `iot/new-device-binding/shared/STATUS.md` | PR in review round 1, 4 commits unpushed (2026-10-19) | Merged; CLI status never rewritten after || ✅ 2026-11-07 |
| 2026-11-07 | `iot/bmp3xx-followup/shared/STATUS.md`, conversation log (8 lines) | PR #2631 open, CI result unseen (2026-10-22) | Merged 2026-11-05 || ✅ 2026-11-07 |
| 2026-11-07 | `yarp/issues/275_*/CLAUDE.md` Status | "waiting for G1… PR waits for iot#2328" (2026-10-01) | Merged 2026-10-26 (STATUS.md and recap are current) || ✅ 2026-11-07 |
| 2026-11-07 | `open_source_persona.md` header, §7.2, §10.2 | Last updated 2026-09-27; exposure map lacks C1/C3/C4/C5; study log has 2 rows | 5 merged PRs || ✅ 2026-11-07 |
| 2026-11-07 | `open_source_persona.md` §10.1 | C4 dated 2026-10-22 | Opened 2026-10-23 UTC (2026-10-22 Pacific); now one convention: the table uses merge dates || ✅ 2026-11-07 |
| 2026-11-07 | `scouting/003` §6 tracker | YARP #3063 "in review" (2026-10-22) | Merged || ✅ 2026-11-07 |
| 2026-11-07 | Repo housekeeping | `yarp/issues/1234_*`, `1235_*` placeholders, `yarp/_to_delete/`, `Claude outputs/` (duplicate of `iot/new-device-binding/001-*`) | Leftovers | |

---

## 6. Keeping this current

**When to update this file (and only then the others):**

1. A PR is opened, gets a review, is merged or closed → update §1–§2 the same day.
2. A comment is posted on an upstream issue → §4.
3. Before handing context to another AI project (resume, interview prep) → run the check script first.

**The check:** `python3 scripts/check-contributions.py` lists every upstream PR authored by `Timothy-Lee-Grant`
(outside his own repos) with its live state, and flags any PR this file doesn't mention or whose row doesn't say
merged/open/closed to match. Add a §5 row for anything it finds in other files.

**Order of truth:** GitHub → this file → `open_source_persona.md` §10.1 → issue `CLAUDE.md` / `STATUS.md` → `README.md`.
