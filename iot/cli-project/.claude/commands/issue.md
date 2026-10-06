---
description: Start work on one issue - load its context from its shared/ folder and switch to its branch if safe
---

Start a session on the issue folder named: **$ARGUMENTS**

1. **Find it.** Read `ISSUES.md`. If `$ARGUMENTS` is empty or isn't a folder listed under "Active", show the Active
   rows and ask Timothy which one. Never work on a folder that isn't listed.
2. **Load the context**, in this order (paths relative to the project root; `<f>` = the folder):
   `<f>/00-start-here.md` (your operating agreement for this issue), `<f>/01-brief.md`, `<f>/04-interaction-mode.md`,
   `<f>/STATUS.md`, `<f>/02-decisions.md`, the newest file in `<f>/sessions/` (if any), then `<f>/plan.md`
   (mode P: Stage 1–3 in full, then the newest entries in Stages 3.4 and 5). If `00` says something that
   contradicts the root `CLAUDE.md` about the clone or branches, the root `CLAUDE.md` wins; note it in your report.
3. **Check the clone** (read-only): `git -C iot remote -v`, `git -C iot status --porcelain`,
   `git -C iot branch --show-current`, `git -C iot log -1 --oneline`.
   - `upstream`'s push URL must not be a real URL. If it is, stop and tell Timothy (setup should have disabled it).
4. **Switch to the issue's branch** (from the `ISSUES.md` row), only if it's safe:
   - Already on it → nothing to do.
   - Uncommitted changes on another branch → **stop**; show `git -C iot status --short` and ask Timothy.
   - Local branch exists → `git -C iot switch <branch>`.
   - Only `origin/<branch>` exists → `git -C iot fetch origin` then `git -C iot switch --track origin/<branch>`.
   - Doesn't exist yet → **don't create it here.** Creating it is a plan step (it happens after G1); say so.
   - Never switch to a protected branch, whatever the reason.
5. **Report to Timothy in 3–6 lines:** the issue, the plan stage and next step, which gate is open, the branch and
   last commit, the mode. Then **wait for his go-ahead.**
