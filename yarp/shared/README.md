# shared/: the project-level mailbox (desktop ↔ CLI)

Each issue has its own mailbox (`issues/<f>/shared/`). This folder is for things that belong to **the YARP project
as a whole** and to no single issue:

| Put here | Example | Name |
|---|---|---|
| A proposal to change `current_context/` (rules, procedures, hooks) that needs Timothy's decision | "the pre-commit hook blocks merge commits on main; allow?" | `NNN-proposal-<topic>.md` |
| A question one session leaves for the other, not tied to an issue | "is the SSD path final?" | `NNN-question-<topic>.md` |
| Notes from a session that worked on the setup itself (not on an issue) | first run of the new layout | `NNN-session-YYYY-MM-DD.md` |

Rules: append-only per file (new information = a new dated section); say who wrote it (`[CLI — date]`,
`[Desktop — date]`); when something here is settled, record the outcome in the file and, if it changed a rule, in
`current_context/README.md`'s changelog. The CLI reaches this folder as `link/shared/`.
