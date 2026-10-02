#!/usr/bin/env bash
# Scaffold a new issue folder in mode P (the default way of working; see default-workflow.md).
# Usage: bash ai-workflow/new-issue.sh <owner> <repo> <issue#> <slug> "<short title>"
#   e.g. bash ai-workflow/new-issue.sh dotnet yarp 275 remove-autofac-from-tests "Remove Autofac from tests"
# Creates <repo>/<issue#>_<slug>/ from ai-workflow/templates/issue/. Refuses to overwrite an existing folder.
set -euo pipefail
[ $# -eq 5 ] || { sed -n 2,5p "$0"; exit 1; }
OWNER=$1; REPO=$2; ISSUE=$3; SLUG=$4; TITLE=$5
HERE="$(cd "$(dirname "$0")" && pwd)"; ROOT="$(dirname "$HERE")"
DEST="$ROOT/$REPO/${ISSUE}_${SLUG}"
[ -e "$DEST" ] && { echo "Already exists: $DEST" >&2; exit 1; }
mkdir -p "$ROOT/$REPO"
cp -R "$HERE/templates/issue" "$DEST"
DATE=$(TZ=America/Los_Angeles date +%Y-%m-%d)
export OWNER REPO ISSUE SLUG TITLE DATE
find "$DEST" -type f ! -name .gitkeep | while read -r f; do
  python3 - "$f" <<'PY'
import os, sys
p = sys.argv[1]; t = open(p).read()
for k in ("OWNER", "REPO", "ISSUE", "SLUG", "TITLE", "DATE"):
    t = t.replace("{{%s}}" % k, os.environ[k])
open(p, "w").write(t)
PY
done
chmod +x "$DEST/workspace/setup.sh"
if grep -rn '{{' "$DEST" >/dev/null; then echo "Warning: unreplaced placeholders:"; grep -rn '{{' "$DEST"; fi
echo "Created $DEST (mode P)."
echo "Next: desktop writes the briefing and plan.md Stages 1-3; Timothy forks $OWNER/$REPO, then runs:"
echo "  bash \"$DEST/workspace/setup.sh\""
