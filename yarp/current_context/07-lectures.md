# 07 · How to write lectures for Timothy

> Read before writing any lecture (`/lecture` runs `procedures/lecture.md`, which points here). Also read
> `05-timothy.md` §3 (how to explain) and `06-competency-yarp.md` (what he already knows). Owner: desktop; the CLI
> proposes changes.

## 1. When, and which kind

Lectures are written **when Timothy asks** (after an autonomous run, or any time). Don't generate them unasked:
unread lectures pile up, and the measure is lectures he actually reads and teaches back, not lectures produced.
At the end of a run, *offer* them in one line.

| Kind | Command | What it answers | Where it goes |
|---|---|---|---|
| **The change** | `/lecture change` | "What was done, why, and how do I know it's right?" | `link/issues/<f>/lectures/NNN-the-change-end-to-end.md` |
| **Testing it yourself** | `/lecture testing` | "How do I run, break and verify this on my own machine before a PR?" | `link/issues/<f>/lectures/NNN-testing-it-yourself.md` |
| **Concept** | `/lecture concept <topic>` | A concept the change depends on that he doesn't have yet (check `06`) | Issue-specific: `link/issues/<f>/lectures/`; reusable across YARP: `link/yarp_concepts/` (next number; add to its README) |
| **Audio** | `/lecture audio <topic>` | Same content, to *listen to* (NotebookLM podcast). Only when he explicitly asks | `…/lectures/audio/NNN-audio-<topic>.md` + `NNN-audio-<topic>.prompt.md` |

Numbering: `NNN` = next free number in that folder (001, 002, …). Audio has its own numbering under `audio/`.

## 2. Rules for every reading lecture

1. **Facts from the record, not memory.** Build from the issue's `plan.md` (Stage 5 *why* entries), `evidence/`,
   the diff (`git -C yarp diff upstream/main...<branch>`) and the code itself. Quote real file paths, type names
   and line ranges at the commit you name in the header. Label anything you haven't run **unverified**.
2. **Header:** title; the issue; branch and commit; date; prerequisites (link concept lectures); and a
   **"how to read this" line with a fast path** (e.g. "15-minute path: §0, §1, §3, checklist").
3. **§0 is something to run** (5–10 minutes, exact commands, expected output) before any explanation.
4. **Purpose first, one-line rule first** in every section, then the detail (`05` §3 rules 1–2).
5. **A cast of named characters** with real type names, consistent with names he already knows (`06` §3).
6. **Tables and ASCII diagrams**; code excerpts short and annotated; compact syntax expanded the first time.
7. **Common mistakes / traps** section; **what this does NOT prove or do** section.
8. **Ends with a teach-back checklist**: 5–10 ideas he should be able to say back, in plain sentences.
9. **Length:** as short as the job allows. Aim for ~300–600 lines; split rather than exceed ~800.
10. After writing: add the lecture to the reading ledger in `06-competency-yarp.md` §4 as "Generated", and to the
    issue's `CLAUDE.md` "Where things are" if it lists lectures. Never mark it read.
11. **Never edit a lecture he has said he read.** Corrections go in chat; improvements go into the next one.

## 3. Spec: "The change, end to end"

| § | Content |
|---|---|
| 0 | **Watch it work:** check out the branch, run the relevant tests green; then break the product code one test guards, see it go red, undo. Exact commands |
| 1 | **The problem**, shown in the real code before the change (what the issue complains about, and why it matters) |
| 2 | **The cast**: every class/test/helper involved, real name + character name + job + who it talks to |
| 3 | **The change, file by file**: before → after, line by line where it matters, each with its *why* and the alternatives rejected (from Stage 5) |
| 4 | **The pattern once in depth**, then the rest as a table (what was special in each file) |
| 5 | **The tests**: object graph (real vs fake) → arrange/act/assert → each test, red → green, what it proves and what it doesn't |
| 6 | **The traps** met on the way and how each was handled (from deviations and CHANGE REQUESTs) |
| 7 | **The upstream side**: the draft comment or PR description sentence by sentence, what it commits him to, what each likely maintainer answer would mean |
| 8 | **Small calls left for him**, if any |
| 9 | Teach-back checklist |

## 4. Spec: "Testing it yourself" (before the PR)

Purpose: Timothy can **independently** confirm the change works and is ready, with no AI in the loop, and knows
what each check is evidence of. It's a procedure lecture: every step is a command, its expected output, and what
it proves.

| § | Content |
|---|---|
| 0 | **What "ready for a PR" means** for this change, as a checklist (the done-when) |
| 1 | **Set up the shell**: clone state, branch, `source activate.sh` / `./.dotnet/dotnet --version`, why the repo-local SDK |
| 2 | **Build from clean** (`--no-incremental` or `./build.sh`) and what warnings mean here |
| 3 | **Run the tests that matter**, narrowest first: one method → one class → the test project → `./test.sh`. Expected counts (from evidence), how long each takes, how to read a failure |
| 4 | **Prove the tests can fail:** for each important test, the one-line product-code break that should turn it red; run; undo. (A test that can't go red proves nothing) |
| 5 | **Compare with the baseline**: same test count as `main`? Same skips? How to run `main` side by side (`git worktree` or switch + rebuild) |
| 6 | **Review the diff yourself**: `git -C yarp diff --stat upstream/main...HEAD`, then file by file; checklist: only intended files, no debug leftovers, no formatting churn, commit messages clean (no `#123`, links, `@`) |
| 7 | **What CI will run** (`azure-pipelines-pr.yml`) that you can't fully run locally (other OSes, other target frameworks), and what you can do instead |
| 8 | **Troubleshooting**: the errors met during the run (from session reports) and their fixes; "if X, then Y" recipes |
| 9 | **The go / no-go**: the final checklist to tick before telling desktop/CLI "G3 is open" |
| 10 | Teach-back checklist |

Write every command so it can be pasted from the project root or from `yarp/` (say which). Give expected output
from the real evidence files, with their names.

## 5. Audio lectures (NotebookLM)

Only when he explicitly asks for something to *listen to*. The reader is a pair of AI podcast hosts; the listener
can't see anything.

- Plain headings + spoken-style paragraphs. **No** tables, diagrams, emoji, checkboxes, or code blocks longer than
  a line. Describe code in words ("the plus-equals operator"); spell awkward identifiers once.
- Narrative arc: the problem → the characters → one event traveling through the system step by step → why it's
  designed this way → mistakes → recap. Consistent personified names.
- Explicit sentences the hosts will pick up: "The key idea is …", "A common misconception is … Actually, …".
- A spoken recap at the end of each part and at the end (the same 5–10 ideas as a teach-back checklist).
- Say aloud what's verified and what isn't. Never "picture" or "imagine".
- Length: 2,000–4,000 words for one episode; if more is needed, split into parts and write one customization
  prompt per episode in the `.prompt.md` file (which he pastes into NotebookLM's "Customize" box; it's not a source).
