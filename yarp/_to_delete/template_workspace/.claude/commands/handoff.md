---
description: End the session - write the session report and update STATUS.md for desktop Claude
---

Wrap up this session for the planner (desktop Claude), who will read only what you write in `shared/`.

1. **Don't ask Timothy anything.** Record the mode scores (Understanding, Speed, Quality, Energy, 1–5) and his
   view of how the session went **only if he volunteered them during the session**; otherwise write "not given".
   Instead, write your own observations: what he asked, where he engaged, what seemed to land. (Timothy, 2026-10-01:
   he doesn't want to be asked at handoff.)
2. **Write the session report** at `shared/sessions/NNN-YYYY-MM-DD.md`, where NNN is one more than the highest
   existing number (start at 001). Follow `shared/sessions/_TEMPLATE.md` exactly, every section, "none" where empty.
   Include his questions in his words where you can. Mark every discovery verified (with evidence file) or unverified.
3. **Rewrite `shared/STATUS.md`** (same table format): date, stage, work order and whether its "done when" is met,
   mode, branch and last commit (`git -C develop/{{REPO}} log -1 --oneline`), test status with evidence file, upstream
   state if you know it, blockers, what's needed from Timothy or desktop, link to the new report.
4. **Append to `shared/02-decisions.md`** any decision Timothy made this session (never edit old rows).
5. **Mode P:** make sure every step you worked on has its Stage 5 entry in `shared/plan.md` (with the *why*), and
   that any open CHANGE REQUEST is listed under "Needs Timothy / desktop" in STATUS.
6. Don't edit `00`, `01`, `03`, or `04` (except a mode switch Timothy asked for, already logged).
7. Reply to Timothy with 3 lines: what the report says, the single most important thing for desktop, and a reminder
   to commit the `exercises` repo when convenient.

$ARGUMENTS
