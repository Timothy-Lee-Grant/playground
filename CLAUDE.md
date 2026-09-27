# Repo Map

This repo is a playground, not a single project. It's split into areas with different rules — check which
area a file is in before assuming how you're allowed to help.

## `hand_experiments/`

The original part of this repo: hand-written practice implementations of C#/systems concepts (`kafka/`,
`redis/`, `reactive/`, `rabbit_mq/`, `sqlite_interactions/`, `packaging_experiment/`, `call_backs/`, plus
`lectures/` written about them). **Strict no-AI-writes-code rule applies there** — see
`hand_experiments/CLAUDE.md` for the full rules before touching anything in that folder. That file used to be
this repo's root CLAUDE.md; it moved down a level because its rules are specific to that folder, not the whole
repo anymore.

## Everything outside `hand_experiments/`

The new part of the repo: a space to spin up self-contained play projects for whatever idea comes up next.
Unlike `hand_experiments/`, **there is no restriction on AI writing code here** — the point of this half of the
repo is to move fast on an idea, not to practice typing it by hand.

Current experiments:

- `yarp_timeout/` — see `yarp_timeout/CLAUDE.md`.

### Conventions shared across experiment folders

- Each experiment is its own top-level folder with its own `CLAUDE.md` (or `README.md`) describing what it is,
  its structure, and its current status. Don't assume one experiment's rules apply to another.
- Markdown research/notes files follow the numbering convention already established in `hand_experiments/` and
  `yarp_timeout/`: `NNN-title.md`, sequential per folder, oldest first.
- New experiments get a new top-level folder and a line added to the list above — keep this file's experiment
  list current.
