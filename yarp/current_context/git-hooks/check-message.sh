#!/usr/bin/env bash
# Shared check used by commit-msg and pre-push: could this commit message link to or notify anything on GitHub?
# Blocks: issue/PR references (#123, owner/repo#123, GH-123), GitHub URLs, @mentions, Co-authored-by trailers.
# Prints the offending lines and exits 1 if so. Lines starting with '#' are git's editor comments and are ignored.
msg_file="$1"
pattern='(^|[^A-Za-z0-9_&])#[0-9]+|[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+#[0-9]+|GH-[0-9]+|github\.com/|(^|[^A-Za-z0-9_.])@[A-Za-z0-9][A-Za-z0-9-]*|^co-authored-by:'
hits=$(grep -v '^#' "$msg_file" | grep -niE "$pattern" || true)
if [ -n "$hits" ]; then
  echo "Blocked: this commit message could link to or notify something on GitHub:" >&2
  echo "$hits" | sed 's/^/    /' >&2
  echo "Remove issue/PR numbers (#N), GitHub URLs, @mentions and Co-authored-by lines." >&2
  echo "If Timothy has approved this exact message, he makes the commit himself (git commit --no-verify)." >&2
  exit 1
fi
