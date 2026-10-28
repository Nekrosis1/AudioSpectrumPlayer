#!/usr/bin/env bash
#
# Quiet build wrapper for AI agents. Builds only the projects listed below and
# prints nothing but errors and one result line per project, so build output
# doesn't flood an agent's context. Run from anywhere:
#
#   ./scripts/agent-build.sh AudioSpectrumPlayer     # by name
#   ./scripts/agent-build.sh asp                     # by alias
#   ./scripts/agent-build.sh asp other               # several, in order
#
# Exit code: 0 all builds succeeded, 1 a build failed, 2 usage error.

set -uo pipefail

cd "$(dirname "$0")/.."

# One entry per project: "<name> [aliases...]|<path to .csproj, relative to repo root>"
PROJECTS=(
	"AudioSpectrumPlayer asp|AudioSpectrumPlayer/AudioSpectrumPlayer.csproj"
)

usage() {
	echo "usage: $0 <project> [project...]"
	echo "projects (name, then aliases):"
	for entry in "${PROJECTS[@]}"; do
		echo "  ${entry%%|*}"
	done
}

resolve() {
	local wanted="$1" entry name
	for entry in "${PROJECTS[@]}"; do
		for name in ${entry%%|*}; do
			if [ "$name" = "$wanted" ]; then
				echo "${entry##*|}"
				return 0
			fi
		done
	done
	return 1
}

if [ $# -eq 0 ]; then
	usage
	exit 2
fi

# Resolve everything first, so a typo doesn't fail halfway through.
paths=()
for arg in "$@"; do
	if ! path=$(resolve "$arg"); then
		echo "unknown project: $arg"
		usage
		exit 2
	fi
	paths+=("$path")
done

failed=0
for path in "${paths[@]}"; do
	label="$(basename "$path" .csproj)"

	# -v:q + ErrorsOnly drops everything but errors; NoSummary stops MSBuild from
	# repeating them at the end. awk removes the duplicates that remain.
	output=$(dotnet build "$path" \
		-nologo \
		-tl:off \
		-v:q \
		-clp:"ErrorsOnly;NoSummary" \
		-p:GenerateFullPaths=true 2>&1)
	status=$?

	if [ $status -eq 0 ]; then
		echo "BUILD OK: $label"
	else
		echo "$output" | awk 'NF && !seen[$0]++'
		echo "BUILD FAILED: $label"
		failed=1
	fi
done

exit $failed
