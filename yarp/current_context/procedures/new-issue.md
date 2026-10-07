# Procedure: start a new issue (`/new-issue <issue#> <slug> "<title>"`)

> Creates the standard issue folder. Usually after a scouting session picked the issue (`link/yarp_scouting/`).

1. **Check it isn't already there:** `ls link/issues/` and `04-branches.md`. Check the upstream issue is still
   open and unclaimed (no recent "I'll take this" comment, no open PR). You can't use `gh`; ask Timothy to look,
   or read the issue page if a browser tool is available. Record what you found.
2. **Scaffold:** `bash link/current_context/new-issue.sh <issue#> <slug> "<title>"`. It copies
   `link/current_context/templates/issue/` to `link/issues/<issue#>_<slug>/` and fills in the names.
3. **Register the branch:** add a row to `04-branches.md` (Active): folder, branch name
   (`<short-purpose>-<issue#>`), one sentence of purpose, base `upstream/main`, *On origin?* = "not created yet",
   status "folder created (date); briefing + plan next".
4. **Next, if Timothy asks you to** (otherwise desktop does it): write `shared/01-brief.md` (the issue, the cast,
   control flow, constraints, how to build/test) from the code, then `shared/plan.md` Stages 1–3 (direction;
   decisions tagged *ours* / *upstream* with proposals; the step table with proofs). Then stop: **G1 is
   Timothy's.**
5. Reply with the folder path and what's next.
