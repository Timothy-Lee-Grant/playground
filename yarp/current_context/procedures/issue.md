# Procedure: load an issue (`/issue <folder>`)

> Run by the `/issue` command, or whenever Timothy names the issue to work on. `<f>` = the issue folder name, e.g.
> `275_remove-autofac-from-tests`. All paths from the project root.

1. **Find it.** The folder must exist at `link/issues/<f>/` **and** have a row in `link/current_context/04-branches.md`
   (Active). If the argument is empty, partial or unknown: list the Active rows (folder + one line each) and ask.
   A partial match ("275") is fine if it's unambiguous; say which folder you picked.
2. **Load the context**, in this order:
   1. `link/issues/<f>/shared/00-start-here.md` (anything specific to this issue; this manual wins on git/layout)
   2. `link/issues/<f>/shared/01-brief.md`
   3. `link/issues/<f>/shared/04-interaction-mode.md` (the mode; usually P)
   4. `link/issues/<f>/shared/STATUS.md`, then `02-decisions.md`
   5. the newest file in `link/issues/<f>/shared/sessions/` (if any)
   6. `link/issues/<f>/shared/plan.md`: Stages 1–3 in full, then the newest entries in the Stage 3 discussion and
      Stage 5. Note whether **G1** is granted (a `[Timothy]` entry) and which step is next
   7. `link/current_context/03-yarp-project.md` and `06-competency-yarp.md`
3. **Check the clone** (read-only): `git -C yarp status -sb`, `git -C yarp branch --show-current`,
   `git -C yarp log -1 --oneline`, `git -C yarp remote -v` (upstream push URL must be the disabled placeholder),
   `git -C yarp config core.hooksPath`.
4. **Get onto the right branch** (manual §4). Use the issue's row in `04-branches.md`:
   - on it already → nothing;
   - uncommitted changes anywhere → **stop**, show `git -C yarp status --short`, ask;
   - exists locally → `git -C yarp switch <branch>`;
   - only on origin → `git -C yarp fetch origin && git -C yarp switch --track origin/<branch>`;
   - row says "not created yet": create it **only if G1 is granted** (`git -C yarp fetch upstream && git -C yarp switch -c <branch> <base>`),
     then update the row (Status, date). Before G1, stay on `main` and say the branch will be created when work starts;
   - row says it exists but it's on neither local nor origin → stop; tell Timothy the registry and reality disagree.
   After any switch: note that the next build must be `--no-incremental`.
5. **Report in 3–6 lines:** the issue in one line; plan stage, gate state and next step; branch + last commit; mode;
   anything abnormal. Then:
   - **G1 not granted:** offer to walk him through the plan, or answer questions. No code.
   - **G1 granted, steps remaining:** say "Ready to run Steps N–M autonomously" and **wait for his go**.
   - **Run finished:** offer `/lecture change`, `/lecture testing`, or `/handoff`.
6. From here, the issue's `plan.md` is your source of work and the manual §5 governs the run.

One issue per session. To change issue: `/handoff` for this one, then `/issue <other>`.
