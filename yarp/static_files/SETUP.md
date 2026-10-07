# Setting up the YARP CLI project folder (you run these; nothing here runs by itself)

> **What you're building:** one folder (the "project root") where you start `claude`. It holds two static files
> copied from here, **one symlink** (`link`) to `exercises/yarp/`, the clone of your fork, and a scratch folder.
> About 10 commands. Every one is explained; nothing is deleted or changed outside the new folder, except one
> optional line in your shell profile (step 8).

## The shape you'll end up with

```
$PROJECT/                                  e.g. /Volumes/DevSSD/open_source/yarp_project   (not a git repo)
├── CLAUDE.md          ← copied from exercises/yarp/static_files/CLAUDE.md
├── .claude/           ← copied: settings.json + commands/ (issue, handoff, lecture, new-issue)
├── link  ──symlink──► ~/Desktop/projects/exercises/yarp/       (current_context/, issues/, shared/, yarp_scouting/, …)
├── yarp/              ← git clone of your fork (origin = Timothy-Lee-Grant/yarp, upstream = dotnet/yarp fetch-only)
└── scratch/           ← throwaway work, not versioned
```

Why each piece: `exercises/yarp/current_context/02-layout.md`.

---

## 0. Before you start: two checks

**The SSD's format.** It must be **APFS** (or Mac OS Extended), not exFAT: exFAT can't hold symlinks or the
"executable" flag that git hooks need. Check it:

```bash
diskutil info /Volumes/<SSD name> | grep -i "file system"
```

If it says ExFAT, reformat it as APFS in Disk Utility first (this erases the SSD).

**Your old YARP clone.** It's at `~/Desktop/projects/oss-work/yarp-275/develop/yarp`. As of 2026-10-02 it had no
unpushed work (the `remove-autofac-275` branch was created but empty). Confirm nothing new was added since:

```bash
git -C ~/Desktop/projects/oss-work/yarp-275/develop/yarp status --short
git -C ~/Desktop/projects/oss-work/yarp-275/develop/yarp log --branches --not --remotes --oneline
```

Both empty → nothing to rescue; the new clone replaces it (leave the old folder alone for now; delete it later if
you want the disk space back). Anything listed → stop and tell Claude before going on.

## 1. Name the two locations (only for this terminal window)

```bash
EXERCISES="$HOME/Desktop/projects/exercises"
PROJECT="/Volumes/<SSD name>/open_source/yarp_project"      # ← put the real SSD name; quotes matter if it has spaces
```

These are just shell variables so the next commands are copy-paste. They disappear when you close the window.

## 2. Create the project folder and copy the static files

```bash
mkdir -p "$PROJECT/.claude/commands" "$PROJECT/scratch"
cp "$EXERCISES/yarp/static_files/CLAUDE.md"               "$PROJECT/CLAUDE.md"
cp "$EXERCISES/yarp/static_files/.claude/settings.json"   "$PROJECT/.claude/settings.json"
cp "$EXERCISES/yarp/static_files/.claude/commands/"*.md   "$PROJECT/.claude/commands/"
```

(This `SETUP.md` is deliberately not copied: it's instructions for you, not for Claude.)

## 3. The one symlink

```bash
ln -s "$EXERCISES/yarp" "$PROJECT/link"
ls "$PROJECT/link/"          # should list current_context, issues, shared, yarp_scouting, …
```

`ln -s TARGET NAME` creates a pointer called `link` whose contents are the real `exercises/yarp/` folder. Edits
through `link/` are edits to `exercises`.

## 4. Clone your fork

```bash
git clone git@github.com:Timothy-Lee-Grant/yarp.git "$PROJECT/yarp"
```

(SSH, like your previous clone. If it asks about a host key or fails on authentication, use
`https://github.com/Timothy-Lee-Grant/yarp.git` instead.) `origin` now points at your fork.

## 5. Add `upstream` (dotnet/yarp) as fetch-only

```bash
git -C "$PROJECT/yarp" remote add upstream https://github.com/dotnet/yarp.git
git -C "$PROJECT/yarp" remote set-url --push upstream PUSH_DISABLED_use_origin_your_fork
git -C "$PROJECT/yarp" fetch upstream
git -C "$PROJECT/yarp" remote -v
```

Expected: `origin` fetch + push = your fork; `upstream` fetch = dotnet/yarp, push = `PUSH_DISABLED_…`. A push to
`upstream` now fails at the URL, before any hook or permission rule is even needed.

## 6. Point git at the shared hooks

```bash
git -C "$PROJECT/yarp" config core.hooksPath "$PROJECT/link/current_context/git-hooks"
git -C "$PROJECT/yarp" config core.hooksPath          # prints the path back
"$PROJECT/link/current_context/git-hooks/active-branches.sh"   # prints: remove-autofac-275
```

From now on git runs `commit-msg`, `pre-commit` and `pre-push` from `current_context/git-hooks/`: no `#123`,
GitHub links, `@mentions` or `Co-authored-by` in commit messages; commits only on branches registered as Active in
`04-branches.md`; pushes only to `origin`. They update whenever `current_context` does. You can always override
for a single command you've decided on with `--no-verify` (the CLI can't).

## 7. Make sure `main` matches upstream

```bash
git -C "$PROJECT/yarp" switch main
git -C "$PROJECT/yarp" merge --ff-only upstream/main
git -C "$PROJECT/yarp" status -sb                       # "## main...origin/main" (maybe "[behind N]" vs your fork: fine)
```

## 8. Optional: keep the NuGet cache on the SSD too

The YARP SDK goes inside the clone (`yarp/.dotnet/`, so already on the SSD), but downloaded packages go to
`~/.nuget/packages` on the Mac's internal disk by default (several GB). To move them:

```bash
mkdir -p "/Volumes/<SSD name>/nuget-packages"
echo 'export NUGET_PACKAGES="/Volumes/<SSD name>/nuget-packages"' >> ~/.zshrc
source ~/.zshrc
```

Skip this if you'd rather not touch your shell profile; nothing else depends on it. (If the SSD isn't plugged in,
.NET builds in any project will fail to find packages while this line is active.)

## 9. Check the whole thing

```bash
ls -la "$PROJECT"                       # CLAUDE.md, .claude/, link -> …/exercises/yarp, scratch/, yarp/
ls "$PROJECT/.claude/commands"          # handoff.md issue.md lecture.md new-issue.md
git -C "$PROJECT/yarp" branch --show-current     # main
```

## 10. Start it

```bash
cd "$PROJECT" && claude
```

What should happen: it checks the clone, greets you with the Active issues (#275), and **waits**. Then type
`/issue 275_remove-autofac-from-tests` (or just "let's work on 275"). Good first test: ask it "which context files
did you load at startup?" It should name the four files from `current_context/` (manual, layout, branches,
Timothy). If it says it had to read them itself, tell desktop Claude: the import path needs fixing.

---

## Later: when something changes

| Change | What you do |
|---|---|
| Anything in `current_context/` (rules, branches, persona, procedures, hooks) | Nothing. It's live through `link` |
| The static `CLAUDE.md`, `settings.json` or a command stub changes in `static_files/` | Re-run the `cp` lines in step 2 (desktop will tell you when) |
| You move `exercises` to another folder | Update the `link` symlink (`ln -sfn NEWPATH "$PROJECT/link"`), and the two absolute paths: the `@~/Desktop/projects/exercises/…` lines in `CLAUDE.md` and `additionalDirectories` in `.claude/settings.json` (fix them in `static_files/` and re-copy) |
| You want a fresh start | The whole setup is inside `$PROJECT`; `exercises` is untouched apart from what sessions write into it |
