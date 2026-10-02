# {{OWNER}}/{{REPO}}#{{ISSUE}}: {{TITLE}} (desktop orientation)

## Read this first

This issue uses the v2 layout (root [`../../CLAUDE.md`](../../CLAUDE.md) §2), the AI workflow
([`../../ai-workflow/README.md`](../../ai-workflow/README.md)) and **mode P, the default sequence**
([`../../ai-workflow/default-workflow.md`](../../ai-workflow/default-workflow.md)). Development happens in Claude
Code (the CLI) on a fork; the two sessions talk only through [`shared/`](shared/).

For a new **desktop** session, read in this order:

1. Root [`../../CLAUDE.md`](../../CLAUDE.md), [`../../persona.md`](../../persona.md), [`../CLAUDE.md`](../CLAUDE.md).
2. This file, then [`shared/plan.md`](shared/plan.md) (where are we in the sequence? which gate is next?).
3. [`shared/STATUS.md`](shared/STATUS.md) and the newest file in [`shared/sessions/`](shared/sessions/).
4. [`conversation/001-conversation-log.md`](conversation/001-conversation-log.md), top to bottom.

## What this is

## Where things are

| Path | Contents | Owner |
|---|---|---|
| `conversation/` | Desktop conversation log (Timothy ↔ desktop Claude) | desktop |
| `shared/00–04`, `shared/plan.md` | Operating agreement, brief, decisions, mode, the living plan | desktop / Timothy |
| `shared/STATUS.md`, `shared/sessions/`, `shared/evidence/` | CLI state, session reports, test output | CLI |
| `workspace/` | The CLI workspace's `CLAUDE.md`, settings, `/handoff` command, `setup.sh` | desktop |
| `lectures/` | Lectures for this issue (001: the change end to end); `lectures/audio/` for NotebookLM versions | desktop |
| `sample/`, `report/` | Experiments outside the fork; lab report | either / desktop |

**CLI workspace:** `~/Desktop/projects/oss-work/{{REPO}}-{{ISSUE}}/` (fork at `develop/{{REPO}}`, origin
`Timothy-Lee-Grant/{{REPO}}`, upstream `{{OWNER}}/{{REPO}}`).

## Status

Set up {{DATE}}. Mode P. Next: desktop writes `plan.md` Stages 1–3; Timothy grants G1.

## Desktop duties after each CLI session

1. Read `shared/STATUS.md`, the newest session report and the new Stage 5 entries in `plan.md`.
2. File its "Timothy's learning" section into `private/002-learner-model.md` §7.
3. Add a row to `../../ai-workflow/interaction-modes.md` §4.
4. Record any decisions Timothy made in the conversation log; write the next plan revision or lecture.
