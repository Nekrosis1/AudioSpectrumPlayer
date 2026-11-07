#!/usr/bin/env bash
# agent-diff.sh — print all uncommitted work in the current worktree/branch.
#
# Usage:  ./scripts/agent-diff.sh
#
# No arguments. The script re-roots itself via git from the caller's working
# directory, so it always targets the current worktree/branch — a subagent
# spawned inside a worktree gets that worktree's changes automatically.
#
# Output, in order:
#   1. current branch
#   2. git status --porcelain            (every changed/added/deleted/untracked file)
#   3. git diff HEAD                     (tracked files: staged + unstaged, combined)
#   4. untracked files rendered as diffs against empty (so new files appear
#      in the same unified-diff format as everything else)
#
# The point of wrapping this in a script: it is one permission-allowed command
# that yields the complete change set, so read-only agents (e.g. the
# adversarial reviewer) can obtain it without arbitrary shell/git access.

set -u

ROOT="$(git rev-parse --show-toplevel 2>/dev/null)"
if [ -z "$ROOT" ]; then
	echo "ERROR: not inside a git repository." >&2
	exit 1
fi
cd "$ROOT" || exit 1

echo "=== BRANCH ==="
git rev-parse --abbrev-ref HEAD

echo
echo "=== STATUS (git status --porcelain) ==="
git status --porcelain

echo
echo "=== DIFF vs HEAD (tracked files, staged + unstaged) ==="
git diff HEAD

echo
echo "=== UNTRACKED FILES (rendered as diffs against empty) ==="
git ls-files --others --exclude-standard -z | while IFS= read -r -d '' f; do
	git diff --no-index -- /dev/null "$f" || true
done

exit 0
