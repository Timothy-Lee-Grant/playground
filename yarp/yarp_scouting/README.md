# yarp_scouting/: finding the next YARP issue

Where a scouting session (desktop or CLI) records its search for YARP issues worth working on. Earlier
cross-repo searches are in `../../scouting/` (e.g. `002-code_pr_shortlist_week_of_2026-09-29.md`, which picked #275).

| File | What |
|---|---|
| `NNN-shortlist-YYYY-MM-DD.md` | One search: the query/filters used, candidates with a short assessment each (what it is, size, still open and unclaimed?, what Timothy would learn, build/test feasibility on the Mac), and a recommendation |
| `candidates.md` | Running list of candidates seen, one line each, with status (new / studied / picked / parked / taken by someone else) |

How a pick becomes work: Timothy chooses → `/new-issue <issue#> <slug> "<title>"` (or
`bash current_context/new-issue.sh …`) creates `issues/<issue#>_<slug>/` and the branch row → brief + plan → G1.

Check before recommending: `../current_context/03-yarp-project.md` (build/test constraints, contribution rules:
"comment on the issue if you want to do the fix"; features need design agreement first) and
`../current_context/06-competency-yarp.md` (what would stretch him without drowning him).
