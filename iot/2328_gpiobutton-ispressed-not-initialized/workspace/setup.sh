#!/usr/bin/env bash
# One-time setup of the Claude Code CLI workspace for dotnet/iot#2328.
# Prerequisite: fork https://github.com/dotnet/iot to your GitHub account (web UI) first.
# Safe to re-run: existing links are refreshed, an existing clone is left alone.
set -euo pipefail

ISSUE_DIR="$HOME/Desktop/projects/exercises/iot/2328_gpiobutton-ispressed-not-initialized"
WS="$HOME/Desktop/projects/oss-work/iot-2328"
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

if [ -d "$CLONE/.git" ]; then
  echo "Clone already exists at $CLONE (left alone)."
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
