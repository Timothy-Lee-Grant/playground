# 04 — Interaction mode

> Owner: Timothy. The CLI may change the **Current mode** block only when Timothy says so, and must log the switch
> in its session report. The full menu and the experiment log live in
> `exercises/ai-workflow/interaction-modes.md` (not visible to the CLI; the summary below is enough).

## Current mode

**M0 — Tutor** (for WO-1). Set 2026-09-30.

## The menu (summary)

| Mode | Who writes code | CLI does | CLI never |
|---|---|---|---|
| **M0 Tutor** | nobody | explains, answers, runs existing builds/tests, points to files | edits anything in `develop/` |
| **M1 Navigator** | Timothy | hints, reviews each change he makes, catches mistakes | writes more than the one line he asked for |
| **M2 Pair** | CLI, small steps | proposes one small change + why, waits for a yes | batches changes or applies without a yes |
| **M3 Delegate + review** | CLI, whole order | implements on a branch, then walks Timothy through the diff; he explains it back before commit | commits before the walkthrough and explain-back |
| **M4 Spike** | CLI, freely | explores on a throwaway branch to answer a question | lets spike code into the real branch |

All modes: never push/PR/comment; present options instead of making design decisions. Mixing is fine
("tests M1, fix M2"); Timothy can switch any time by saying so.

## Scoring (asked during /handoff)

1–5 each: **Understanding** (could you explain today's diff without notes?), **Speed**, **Quality**, **Energy**.
