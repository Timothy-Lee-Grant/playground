#!/usr/bin/env bash
# One-time setup of the Claude Code CLI workspace for dotnet/iot new binding (BMP3xx).
# Prerequisite: fork https://github.com/dotnet/iot to your GitHub account (web UI) first.
# Safe to re-run: existing links are refreshed, an existing clone is left alone.
set -euo pipefail

ISSUE_DIR="$HOME/Desktop/projects/exercises/iot/new-device-binding"
WS="$HOME/Desktop/projects/oss-work/iot-bmp3xx"
FORK_URL="https://github.com/Timothy-Lee-Grant/iot.git"
UPSTREAM_URL="https://github.com/dotnet/iot.git"
CLONE="$WS/develop/iot"

echo "Workspace: $WS"
mkdir -p "$WS/develop" "$WS/.claude/commands"

# Symlinks: one real copy, living in exercises (versioned there)
ln -sfn "$ISSUE_DIR/shared"                          "$WS/shared"
ln -sfn "$ISSUE_DIR/workspace/CLAUDE.md"             "$WS/CLAUDE.md"
ln -sfn "$ISSUE_DIR/workspace/.claude/settings.json" "$WS/.claude/settings.json"
ln -sfn "$ISSUE_DIR/workspace/.claude/commands/handoff.md" "$WS/.claude/commands/handoff.md"
echo "Links created."

# An existing clone is never touched here: the CLI audits it in plan Step 0 (read-only) and proposes how to
# bring it to the standard state, keeping any old work on a backup branch.
OTHER=$(find "$WS/develop" -mindepth 2 -maxdepth 2 -name .git 2>/dev/null | grep -v "^$CLONE/.git$" || true)
if [ -d "$CLONE/.git" ]; then
  echo "Existing clone found at $CLONE (left alone). The CLI audits its state in plan Step 0."
elif [ -n "$OTHER" ]; then
  echo "Found an existing git repo under $WS/develop, but not at $CLONE:" >&2
  echo "$OTHER" | sed 's#/.git$##' >&2
  echo "Not cloning a second copy. Rename that folder to $CLONE (mv), then re-run." >&2
  exit 1
else
  if ! git ls-remote "$FORK_URL" >/dev/null 2>&1; then
    echo "Can't reach $FORK_URL. Fork dotnet/iot on GitHub first, then re-run." >&2
    exit 1
  fi
  # blob:none = partial clone: full history, file contents fetched on demand (saves disk on the Mac)
  git clone --filter=blob:none "$FORK_URL" "$CLONE"
  git -C "$CLONE" remote add upstream "$UPSTREAM_URL"
  git -C "$CLONE" fetch upstream
  echo "Cloned. origin = your fork, upstream = dotnet/iot."
fi

echo
echo "Check:"
ls -la "$WS" "$WS/.claude" "$WS/.claude/commands"
echo
echo "Next:  cd \"$WS\" && claude"
