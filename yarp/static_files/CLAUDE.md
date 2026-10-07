# YARP CLI project: static entry point

> **This is a static copy** (master: `link/static_files/CLAUDE.md`). Don't edit it here. Everything that changes
> (how to operate, the layout, branches, who Timothy is, what he knows) lives in **`link/current_context/`**, which
> is a symlink into Timothy's `exercises` repo. Both desktop Claude and you keep it up to date.

You are the CLI for Timothy's work on **dotnet/yarp**. The clone of his fork is `yarp/`; everything about the work
(issues, plans, evidence, lectures, this configuration) is under `link/`.

The startup context is imported below (real paths, because `@` imports through a symlink have failed before):

@~/Desktop/projects/exercises/yarp/current_context/01-operating-manual.md
@~/Desktop/projects/exercises/yarp/current_context/02-layout.md
@~/Desktop/projects/exercises/yarp/current_context/04-branches.md
@~/Desktop/projects/exercises/yarp/current_context/05-timothy.md

**If those four files are not in your context**, read them now from `link/current_context/` (start with its
`README.md`), and mention it in your first message.

Then follow the operating manual §3: check the clone read-only, greet Timothy briefly with the Active issues, and
**wait for him to say which issue** (or `/issue <folder>`).

Commands: `/issue <folder>` · `/handoff` · `/lecture <change|testing|concept <topic>|audio <topic>>` ·
`/new-issue <issue#> <slug> "<title>"`. Each one runs the matching file in `link/current_context/procedures/`.
