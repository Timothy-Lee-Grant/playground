# STATUS

> Owner: CLI. Rewritten in place by `/handoff` every session. Desktop reads this first.

| | |
|---|---|
| **Updated** | 2026-09-30 (session 001, CLI) |
| **Stage** | Setup checked; fork cloned and building. WO-1 partly done: baseline run and code tour done. |
| **Work order** | WO-1: "done when" **not met**. Steps 4 (PR #2608 diff) and 5 (MockableGpioDriver) are left, plus Timothy's own-words explanation. |
| **Mode** | M0 |
| **Branch / last commit** | `main` @ `1eb0b2f6 Make OneWire sysfs paths instance-level and configurable via constructors (#2603)`, clean |
| **Tests** | Button tests 8/8 pass (SDK 10.0.302, net8.0): `evidence/001-baseline-button-tests.txt` |
| **Upstream** | Not re-checked this session. Last known: comment on #2328 not posted; PR #2608 open. |
| **Blockers** | Workspace `CLAUDE.md` `@shared/...` imports don't load (symlink + relative paths, likely). Workaround: the CLI reads the files by hand. |
| **Needs Timothy / desktop** | Fix the imports (absolute paths, see report §9). Consider adding "read before/after callback registration" to P1 (report §6). |
| **Last session report** | [`sessions/001-2026-09-30.md`](sessions/001-2026-09-30.md) |
