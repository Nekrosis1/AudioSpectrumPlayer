#!/usr/bin/env bash
#
# Generates the WAV test files into AudioSpectrumPlayer.Tests/TestData/ (gitignored).
# Run once per machine before running the tests, and again to regenerate; existing
# files are overwritten. Needs ffmpeg. Run from anywhere:
#
#   ./scripts/generate-test-audio.sh
#
# Amplitudes are set with aevalsrc, because ffmpeg's `sine` source is fixed at 1/8.
# In aevalsrc, `a|b` gives one expression per channel. -c:a picks the sample format.

set -euo pipefail

cd "$(dirname "$0")/.."

OUT="AudioSpectrumPlayer.Tests/TestData"

if ! command -v ffmpeg >/dev/null; then
	echo "ffmpeg not found"
	exit 1
fi

mkdir -p "$OUT"

# gen <file name> <lavfi source> <codec>
gen() {
	ffmpeg -hide_banner -loglevel error -y -f lavfi -i "$2" -c:a "$3" "$OUT/$1"
	echo "  $1"
}

SINE1K="0.5*sin(2*PI*1000*t)"

echo "Writing to $OUT:"

# Files the tests use (Task01-wavToPCM.md). 1 s, 44100 Hz, amplitude 0.5 unless the name says otherwise.
gen sine1k_s16_mono.wav           "aevalsrc=$SINE1K:s=44100:d=1"           pcm_s16le
gen sine1k_f32_mono.wav           "aevalsrc=$SINE1K:s=44100:d=1"           pcm_f32le
gen sine1k_u8_mono.wav            "aevalsrc=$SINE1K:s=44100:d=1"           pcm_u8
gen sine1k_s24_mono.wav           "aevalsrc=$SINE1K:s=44100:d=1"           pcm_s24le
gen sine1k_s32_mono.wav           "aevalsrc=$SINE1K:s=44100:d=1"           pcm_s32le
gen sine1k_f64_mono.wav           "aevalsrc=$SINE1K:s=44100:d=1"           pcm_f64le
gen sine1k_s16_stereo_L.wav       "aevalsrc=$SINE1K|0:s=44100:d=1"         pcm_s16le
gen sine1k_f32_stereo_L.wav       "aevalsrc=$SINE1K|0:s=44100:d=1"         pcm_f32le
gen sine1k_s16_6ch_L.wav          "aevalsrc=$SINE1K|0|0|0|0|0:s=44100:d=1" pcm_s16le
gen silence_s16_mono.wav          "anullsrc=r=44100:cl=mono:d=1"           pcm_s16le
gen sine1k_s16_mono_48k.wav       "aevalsrc=$SINE1K:s=48000:d=1"           pcm_s16le
gen sine1k_s16_mono_3s.wav        "aevalsrc=$SINE1K:s=44100:d=3"           pcm_s16le
gen sine1k_s16_mono_48k_3s.wav    "aevalsrc=$SINE1K:s=48000:d=3"           pcm_s16le
gen unsupported_adpcm_mono.wav    "aevalsrc=$SINE1K:s=44100:d=1"           adpcm_ms

# For manual testing and bug hunting, not used by the tests.
# 10 s, 44100 Hz, amplitude 0.5, stereo with the same signal on both channels.
SINE200="0.5*sin(2*PI*200*t)"
SQUARE200="0.5*sgn(sin(2*PI*200*t))"
# Linear sweep 100 Hz -> 2000 Hz over 10 s: frequency 100 + 190*t, so the phase is 2*PI*(100*t + 95*t^2).
SWEEP="0.5*sin(2*PI*(100*t+95*t*t))"

gen sine200_f32_stereo_10s.wav         "aevalsrc=$SINE200|$SINE200:s=44100:d=10"     pcm_f32le
gen square200_f32_stereo_10s.wav       "aevalsrc=$SQUARE200|$SQUARE200:s=44100:d=10" pcm_f32le
gen sweep100-2000_f32_stereo_10s.wav   "aevalsrc=$SWEEP|$SWEEP:s=44100:d=10"         pcm_f32le

echo "Done."
