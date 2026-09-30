# Interaction modes: ways of working with the CLI (an experiment)

> **Purpose:** find out which ways of working with an AI coding agent give Timothy the best mix of *understanding*,
> *speed*, *quality* and *energy*, and for which kinds of task. There's no single right answer; different tasks
> probably want different modes. This file is the menu and the lab notebook.
>
> **Owner:** Timothy (desktop Claude maintains it). The CLI never edits this file; it reads the current mode from the
> issue's `shared/04-interaction-mode.md` and reports how the session went.

---

## 1. The menu

Ordered from "Timothy does the most" to "the AI does the most". The rule shared by all of them: **nothing reaches
an upstream PR that Timothy can't explain line by line.**

| Mode | Name | Who writes code | The CLI's job | Good for (guess, to be tested) |
|---|---|---|---|---|
| **M0** | Tutor | Nobody (read-only) | Explain code, answer questions, run *existing* builds/tests, point to files. **No edits** | Orientation, reading a codebase, first build |
| **M1** | Navigator | **Timothy** types everything | Hints, reviews each change Timothy makes, catches mistakes, suggests where to look. Edits only a specific line when asked | Code where the learning *is* the typing: tests, small fixes |
| **M2** | Pair | CLI, in small steps | Propose **one** small change at a time with the why; wait for approval; Timothy may ask "explain before applying" | Product-code fixes with design weight |
| **M3** | Delegate + review | CLI, a whole work order | Implement the order on a branch, then walk Timothy through the diff. Timothy explains the diff back before committing | Mechanical or repetitive work (e.g. rewriting 7 test files) |
| **M4** | Spike | CLI, freely | Explore fast on a throwaway branch to answer a question. The code is discarded; only findings are kept | "Does this approach even work?" |

**Mixing is allowed** and expected: e.g. "tests in M1, fix in M2". The current mode can change mid-session when
Timothy says so; the CLI logs the switch in its session report.

---

## 2. What each mode must not do

| Mode | Never |
|---|---|
| M0 | Edit any file in `develop/` |
| M1 | Write more than the one line/snippet Timothy asked for; "helpfully" finish the task |
| M2 | Batch several changes into one proposal; apply without a yes |
| M3 | Commit before Timothy has explained the diff back; skip the walkthrough |
| M4 | Merge spike code into the real branch |
| All | Push, open PRs, comment on GitHub; make a design decision instead of presenting options |

---

## 3. How a session is scored

The CLI's `/handoff` asks Timothy four quick questions (1–5 each, one word is fine) and records them, plus its own
observations:

| Measure | Question | Why |
|---|---|---|
| **Understanding** | "Could you explain today's diff to a reviewer without notes?" | The non-negotiable |
| **Speed** | "Did the session move as fast as you wanted?" | Deadlines exist |
| **Quality** | "Would you be comfortable with a maintainer reading this code?" | |
| **Energy** | "Did this feel good to work in?" | Sustainable pace; motivation drives everything else |

Desktop Claude copies the scores into §4 and, over time, into the learner model (`private/002-learner-model.md`).

---

## 4. Experiment log

One row per CLI session. Newest at the bottom.

| Date | Issue | Work order | Mode(s) | U | S | Q | E | Note (what worked, what didn't) |
|---|---|---|---|---|---|---|---|---|
| | | | | | | | | |

---

## 5. Findings so far

None yet. After ~5 sessions, look for patterns: which mode for which task, where understanding dropped, where
energy dropped.
