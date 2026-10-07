# Procedure: write a lecture (`/lecture <kind> [topic]`)

> `<kind>` is `change`, `testing`, `concept <topic>` or `audio <topic>`. Only Timothy starts this. If the kind is
> missing, ask which one (one line, list the four).

1. **Read the rules:** `link/current_context/07-lectures.md` (the spec for this kind), `05-timothy.md` §3–4 and
   `06-competency-yarp.md` (what he already knows; which names he knows).
2. **Gather the record** for the loaded issue `<f>`: `shared/plan.md` (Stages 1–3, every Stage 5 entry and CHANGE
   REQUEST), `shared/evidence/`, the session reports, `shared/02-decisions.md`, and the diff
   (`git -C yarp diff upstream/main...HEAD`; `--stat` first). For `testing`, also the exact commands and outputs
   that worked, and every error met on the way.
3. **Verify as you write.** Re-run anything whose output you quote and don't already have as evidence; save it.
   Check every file path and type name against the code at the commit in the header.
4. **Write it** to the path in `07-lectures.md` §1 (next free number). Short sections, one-line rule first, a fast
   path in the header, teach-back checklist at the end.
5. **Record it:** add a "Generated" row to `06-competency-yarp.md` §4; add it to the issue's `CLAUDE.md` file list
   if there is one.
6. **Reply in 3–4 lines:** the path, how long the fast path takes, and what §0 asks him to run. Don't summarize the
   whole lecture in chat. Don't mark it read.
