#!/usr/bin/env bash
# Shared check used by commit-msg and pre-push: does a commit message contain anything GitHub could turn into a
# cross-reference or notification? Prints the offending lines and exits 1 if so.
#   - issue/PR references: #123, owner/repo#123, GH-123
#   - GitHub URLs (github.com/...)
#   - @mentions (@someone)
#   (Closing keywords like "Fixes" only act when followed by a reference, which is already caught.)
# Lines starting with '#' are git's own comments in the editor template and are ignored.
msg_file="$1"
pattern='(^|[^A-Za-z0-9_&])#[0-9]+|[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+#[0-9]+|GH-[0-9]+|github\.com/|(^|[^A-Za-z0-9_.])@[A-Za-z0-9][A-Za-z0-9-]*'
hits=$(grep -v '^#' "$msg_file" | grep -niE "$pattern" || true)
if [ -n "$hits" ]; then
  echo "Blocked: this commit message could link to or notify something on GitHub:" >&2
  echo "$hits" | sed 's/^/    /' >&2
  echo "Remove issue/PR numbers (#N), GitHub URLs and @mentions. (Timothy only: --no-verify overrides.)" >&2
  exit 1
fi
