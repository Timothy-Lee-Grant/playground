#!/usr/bin/env bash
# Create a new YARP issue folder from the standard template.
# Usage (from anywhere):  bash <path>/current_context/new-issue.sh <issue#> <slug> "<short title>"
#   e.g.  bash link/current_context/new-issue.sh 275 remove-autofac-from-tests "Remove Autofac from tests"
# Creates <yarp folder>/issues/<issue#>_<slug>/ from current_context/templates/issue/. Never overwrites.
# Then: add the issue's branch row to current_context/04-branches.md (procedures/new-issue.md step 3).
set -euo pipefail
[ $# -eq 3 ] || { sed -n 2,6p "$0"; exit 1; }
ISSUE=$1; SLUG=$2; TITLE=$3
HERE="$(cd "$(dirname "$(readlink -f "$0")")" && pwd)"      # .../yarp/current_context
DEST="$(dirname "$HERE")/issues/${ISSUE}_${SLUG}"
[ -e "$DEST" ] && { echo "Already exists: $DEST" >&2; exit 1; }
mkdir -p "$(dirname "$DEST")"
cp -R "$HERE/templates/issue" "$DEST"
DATE=$(TZ=America/Los_Angeles date +%Y-%m-%d)
export ISSUE SLUG TITLE DATE
find "$DEST" -type f ! -name .gitkeep | while read -r f; do
  python3 - "$f" <<'PY'
import os, sys
p = sys.argv[1]; t = open(p).read()
for k in ("ISSUE", "SLUG", "TITLE", "DATE"):
    t = t.replace("{{%s}}" % k, os.environ[k])
open(p, "w").write(t)
PY
done
if grep -rn '{{' "$DEST" >/dev/null; then echo "Warning: unreplaced placeholders:"; grep -rn '{{' "$DEST"; fi
echo "Created $DEST"
echo "Next: register the branch in current_context/04-branches.md (Active, 'not created yet'),"
echo "      then write shared/01-brief.md and shared/plan.md Stages 1-3; Timothy grants G1."
