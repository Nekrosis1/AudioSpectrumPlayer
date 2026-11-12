#!/usr/bin/env bash
#
# Quiet test wrapper for AI agents. Builds the test project (via agent-build.sh), runs the
# tests and prints only build errors, test failures, the count line and one result line,
# so test output doesn't flood an agent's context. Run from anywhere:
#
#   ./scripts/agent-test.sh                          # all tests
#   ./scripts/agent-test.sh -method "*WavDecoder*"   # extra arguments go to the xUnit runner
#
# Exit code: 0 all tests passed, 1 build failed, 3 tests failed, no tests ran,
# or the test run itself failed (crash, bad runner argument).

set -uo pipefail

cd "$(dirname "$0")/.."

PROJECT="AudioSpectrumPlayer.Tests/AudioSpectrumPlayer.Tests.csproj"

if ! ./scripts/agent-build.sh tests; then
	exit 1
fi

# --no-build: agent-build.sh just built it. The project is an xUnit v3 executable, so
# everything after `--` goes to the xUnit runner.
output=$(dotnet run --project "$PROJECT" --no-build -- -noLogo -noColor "$@" 2>&1)
status=$?

# Strip color codes (some runner errors are colored despite -noColor), drop the
# progress lines and the summary header, and keep everything else.
filtered=$(echo "$output" \
	| sed 's/\x1b\[[0-9;]*m//g' \
	| grep -Ev '^ +(Discovering|Discovered|Starting|Finished): |^=== TEST EXECUTION SUMMARY ===$')

MAX_CRASH_LINES=30

# No count line means the run never finished: the test process crashed (e.g. stack overflow,
# Environment.FailFast) or the runner refused an argument. A crash dumps a stack trace of
# hundreds of runtime and xUnit frames; keep only the messages and the frames from our own code.
if [ $status -ne 0 ] && ! echo "$filtered" | grep -q 'Total: '; then
	short=$(echo "$filtered" | awk '!/^ +at / || /^ +at AudioSpectrumPlayer/' | head -n $MAX_CRASH_LINES)
	echo "$short"
	all_lines=$(echo "$filtered" | wc -l)
	short_lines=$(echo "$short" | wc -l)
	if [ "$all_lines" -gt "$short_lines" ]; then
		echo "(test run crashed; output shortened from $all_lines to $short_lines lines)"
	fi
	echo "TESTS FAILED"
	exit 3
fi

if [ -n "$filtered" ]; then
	echo "$filtered"
fi

if [ $status -ne 0 ]; then
	echo "TESTS FAILED"
	exit 3
fi

if echo "$filtered" | grep -Eq 'Total: 0$'; then
	echo "NO TESTS RAN"
	exit 3
fi

echo "TESTS OK"
exit 0
