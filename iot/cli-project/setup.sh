#!/usr/bin/env bash
# One-time (re-runnable) setup of the iot CLI project: one clone, one folder per issue.
#   bash ~/Desktop/projects/exercises/iot/cli-project/setup.sh
# Prerequisite: the fork is already cloned at $PROJECT/iot (git clone git@github.com:Timothy-Lee-Grant/iot.git).
# Safe to re-run: links are refreshed; the clone's branches and files are never touched.
set -euo pipefail

EXERCISES="$HOME/Desktop/projects/exercises"
CFG="$EXERCISES/iot/cli-project"
PROJECT="$HOME/Desktop/projects/open_source/iot_project"
CLONE="$PROJECT/iot"
UPSTREAM_URL="https://github.com/dotnet/iot.git"

[ -d "$CLONE/.git" ] || { echo "No clone at $CLONE. Clone your fork there first." >&2; exit 1; }

# 1. Remotes: origin = your fork; upstream = dotnet/iot, fetch only (push URL deliberately invalid)
echo "== remotes"
git -C "$CLONE" remote get-url origin
if git -C "$CLONE" remote get-url upstream >/dev/null 2>&1; then
  git -C "$CLONE" remote set-url upstream "$UPSTREAM_URL"
else
  git -C "$CLONE" remote add upstream "$UPSTREAM_URL"
fi
git -C "$CLONE" remote set-url --push upstream "PUSH_DISABLED_use_origin_your_fork"
git -C "$CLONE" fetch upstream --quiet
git -C "$CLONE" fetch origin --quiet
git -C "$CLONE" remote -v

# 2. Project-level files (symlinks: the real copies live in exercises, versioned there)
echo "== links"
mkdir -p "$PROJECT/.claude/commands" "$PROJECT/scratch"
ln -sfn "$CFG/CLAUDE.md"                    "$PROJECT/CLAUDE.md"
ln -sfn "$CFG/issues.md"                    "$PROJECT/ISSUES.md"
ln -sfn "$EXERCISES/persona.md"             "$PROJECT/persona.md"
ln -sfn "$CFG/.claude/settings.json"        "$PROJECT/.claude/settings.json"
ln -sfn "$CFG/.claude/commands/issue.md"    "$PROJECT/.claude/commands/issue.md"
ln -sfn "$CFG/.claude/commands/handoff.md"  "$PROJECT/.claude/commands/handoff.md"

# 3. One link per active issue: <folder> -> exercises/iot/<folder>/shared  (folders read from issues.md)
awk '/^## Active/{a=1;next} /^## /{a=0} a && /^\| `/{print}' "$CFG/issues.md" \
  | sed -E 's/^\| `([^`]+)`.*/\1/' | while read -r f; do
    target="$EXERCISES/iot/$f/shared"
    if [ -d "$target" ]; then
      if [ -e "$PROJECT/$f" ] && [ ! -L "$PROJECT/$f" ]; then
        echo "  $PROJECT/$f exists and is not a link; left alone" >&2
      else
        ln -sfn "$target" "$PROJECT/$f"; mkdir -p "$PROJECT/scratch/$f"; echo "  $f -> $target"
      fi
    else
      echo "  missing $target (row in issues.md but no shared/ folder)" >&2
    fi
  done

echo "== check"
ls -la "$PROJECT" "$PROJECT/.claude/commands"
echo "Current branch: $(git -C "$CLONE" branch --show-current)   (should be main; the CLI switches per issue)"
echo
echo "Next:  cd \"$PROJECT\" && claude      then:  /issue new-device-binding"
