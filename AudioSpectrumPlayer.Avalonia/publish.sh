#!/usr/bin/env bash
#
# Produces self-contained, single-file builds of AudioSpectrumPlayer for
# Windows and Linux into ./Release/<rid>. Run from anywhere:
#
#   ./publish.sh              # both platforms
#   ./publish.sh win-x64      # just one
#
# Windows output is standalone: it carries the .NET runtime and libvlc.
# Linux output carries the .NET runtime but links the *system* libvlc, so the
# target machine needs VLC installed (see README notes / future AppImage).

set -euo pipefail

cd "$(dirname "$0")"

PROJECT="AudioSpectrumPlayer.Avalonia.csproj"
OUT_ROOT="Release"
if [ $# -gt 0 ]; then
	RIDS=("$@")
else
	RIDS=(win-x64 linux-x64)
fi

for rid in "${RIDS[@]}"; do
	out="$OUT_ROOT/$rid"
	echo ">>> publishing $rid -> $out"

	rm -rf "$out"

	# IncludeAllContentForSelfExtract is what gets libvlc's plugins/ tree out of
	# the bundle at runtime; without it Core.Initialize() finds libvlc.dll but no
	# plugins, and playback fails with no obvious error.
	dotnet publish "$PROJECT" \
		--configuration Release \
		--runtime "$rid" \
		--self-contained true \
		--output "$out" \
		-p:PublishSingleFile=true \
		-p:IncludeNativeLibrariesForSelfExtract=true \
		-p:IncludeAllContentForSelfExtract=true \
		-p:DebugType=none \
		-p:SatelliteResourceLanguages=en
done

echo
echo "done:"
find "$OUT_ROOT" -maxdepth 2 -type f -not -name "*.pdb" -printf "  %p (%s bytes)\n"
