#!/usr/bin/env bash
# Prints the branch names listed under "## Active" in ../04-branches.md (second column, in backticks), one per line.
here="$(dirname "$(readlink -f "$0")")"
registry="$here/../04-branches.md"
[ -f "$registry" ] || { echo "Branch registry not found: $registry" >&2; exit 2; }
awk '/^## Active/{a=1;next} /^## /{a=0} a && /^\| `/' "$registry" \
  | awk -F'|' '{print $3}' | sed -nE 's/.*`([^`]+)`.*/\1/p'
