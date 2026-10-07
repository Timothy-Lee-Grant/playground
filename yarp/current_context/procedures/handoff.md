# Procedure: end a session (`/handoff`)

> Run by the `/handoff` command, or when Timothy says he's wrapping up. `<f>` = the issue loaded this session (if
> unsure, ask). Desktop Claude and future CLI sessions will only know what you write here.

1. **Don't ask Timothy anything** (no scores, no feedback). Record scores or opinions only if he volunteered them
   during the session; otherwise write "not given" and give your own observations.
2. **Session report** at `link/issues/<f>/shared/sessions/NNN-YYYY-MM-DD.md` (NNN = highest existing + 1, from 001),
   following `link/issues/<f>/shared/sessions/_TEMPLATE.md`, every section, "none" where empty. His questions in
   his words where you can. Every discovery marked verified (with evidence file) or unverified.
3. **Rewrite `link/issues/<f>/shared/STATUS.md`** (same table format): date; stage; plan step and whether its
   done-when is met; mode; branch and last commit (`git -C yarp log -1 --oneline`); **pushed or not**
   (`git -C yarp status -sb`); tests with evidence file; upstream state if known; blockers; what's needed from
   Timothy or desktop (open CHANGE REQUESTs, decisions); link to the new report.
4. **Registry:** update the issue's row *Status (date)* in `link/current_context/04-branches.md` (and *On origin?*
   if it was pushed). Add rows for any branch created this session.
5. **Decisions** Timothy made → append to `link/issues/<f>/shared/02-decisions.md` (never edit old rows).
6. **Plan:** every step worked on has its Stage 5 entry with the *why*; open CHANGE REQUESTs are listed in STATUS.
7. **Competency:** if he said he read something, explained something, or asked something revealing, add a dated
   evidence line to `link/current_context/06-competency-yarp.md` (§2 or §4). Evidence only.
8. **Setup feedback:** anything in `current_context/` that was wrong, missing or confusing goes in the report's
   "Feedback on the setup" (and edited only if Timothy said to).
9. **Leave the clone clean** (everything committed), unless Timothy chose to leave work in progress; then say so in
   STATUS, because no other issue can switch branches until it's committed.
10. **Reply in 3 lines:** what the report says; the single most important thing for the next session; a reminder
    that `exercises` has new files to commit when convenient.
