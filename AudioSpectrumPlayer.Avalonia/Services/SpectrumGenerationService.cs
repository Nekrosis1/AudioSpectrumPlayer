using AudioSpectrumPlayer.Avalonia.Interfaces;
using MathNet.Numerics.IntegralTransforms;
using Serilog;
using System;
using System.Numerics;

namespace AudioSpectrumPlayer.Avalonia.Services
{
	/// <summary>
	/// FFT processing for spectrum analysis. The FFT pipeline (Hamming window +
	/// log-frequency binning) is intact and ready to use; the PCM input side is
	/// currently a silent stub because the previous NAudio source was removed
	/// during the libvlc migration. A future pass will tap libvlc's audio
	/// callbacks to feed real PCM into <see cref="GetAudioChunkAtPosition"/>.
	/// </summary>
	public class SpectrumGenerationService : IDisposable
	{
		private readonly IAudioStateService _audioStateService;

		private const int FFT_SIZE = 1024; // Must be power of 2
		private const int SPECTRUM_BARS = 64; // Number of frequency bars to display

		public event EventHandler<float[]>? SpectrumDataUpdated;

		public SpectrumGenerationService(IAudioStateService audioStateService)
		{
			_audioStateService = audioStateService;
			_audioStateService.FileLoaded += OnFileLoaded;
		}

		private void OnFileLoaded(object? sender, string filePath)
		{
			Log.Debug("SpectrumGenerationService received FileLoaded for: {FilePath} (no PCM tap wired yet)", filePath);
		}

		// Silent until a libvlc PCM tap feeds real audio in a later pass.
		public float[] GetAudioChunkAtPosition(TimeSpan position, int bufferSize = 1024)
		{
			return new float[bufferSize];
		}

		public void UpdateSpectrumData()
		{
			if (_audioStateService.CurrentFilePath is null) return;

			float[] pcmData = GetAudioChunkAtPosition(_audioStateService.CurrentPosition);
			float[] spectrumData = GenerateSpectrumData(pcmData);
			SpectrumDataUpdated?.Invoke(this, spectrumData);
		}

		private static float[] GenerateSpectrumData(float[] pcmData)
		{
			try
			{
				// The FFT needs exactly FFT_SIZE samples. If the caller handed us
				// fewer (e.g. end of file) or more, force the array to the right
				// length. Array.Resize pads with zeros when growing, which reads as
				// silence — harmless for the transform.
				if (pcmData.Length != FFT_SIZE)
				{
					Array.Resize(ref pcmData, FFT_SIZE);
					Log.Warning("PCM Array had bad size");
				}

				// The FFT works on complex numbers. Our audio samples are real
				// (just amplitudes), so each becomes a Complex with the sample as
				// the real part and 0 as the imaginary part.
				var complexData = new Complex[FFT_SIZE];
				for (int i = 0; i < FFT_SIZE; i++)
				{
					// Apply a Hamming window to reduce "spectral leakage".
					// Why this is needed: the FFT assumes the chunk of audio
					// repeats forever, looping end-to-start. But our chunk almost
					// never starts and ends at the same amplitude, so that imagined
					// loop has a sharp discontinuity at the seam. A sharp edge
					// contains energy at MANY frequencies, which smears false
					// content across the whole spectrum (the "leakage").
					// The window fixes this by tapering the chunk's volume down to
					// near-zero at both ends, so the seam is smooth. The cosine
					// curve below is the Hamming shape: ~0.08 at the edges, 1.0 in
					// the middle. We multiply each sample by its window value.
					double windowValue = 0.54 - 0.46 * Math.Cos(2.0 * Math.PI * i / (FFT_SIZE - 1));
					complexData[i] = new Complex(pcmData[i] * windowValue, 0);
				}

				// The actual transform: time-domain samples in, frequency-domain
				// bins out. After this, complexData[k] tells us "how much of
				// frequency k is present", encoded as a complex number whose
				// Magnitude is the strength. FourierOptions.Matlab uses the same
				// scaling/sign conventions as MATLAB — just a normalization choice,
				// consistent so our dB math below behaves predictably.
				Fourier.Forward(complexData, FourierOptions.Matlab);

				// Collapse the raw FFT bins down to the handful of bars we draw.
				return ConvertToSpectrumBars(complexData);
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Error generating spectrum data");
				return new float[SPECTRUM_BARS]; // Return empty spectrum on error
			}
		}

		private static float[] ConvertToSpectrumBars(Complex[] fftData)
		{
			var spectrumBars = new float[SPECTRUM_BARS];

			// Only use the first half of FFT data (positive frequencies).
			// Why: for real-valued input, the FFT output is symmetric — the second
			// half is a mirror image of the first (the "negative frequencies") and
			// carries no new information. So bins [0 .. FFT_SIZE/2] are all we need.
			int usableFFTSize = FFT_SIZE / 2;

			// Group FFT bins into spectrum bars using logarithmic scaling.
			// Why logarithmic: the FFT spaces its bins LINEARLY in frequency (bin 1,
			// 2, 3 ... each a fixed Hz apart). But human hearing is LOGARITHMIC —
			// the jump from 100→200 Hz sounds like the same musical interval as
			// 1000→2000 Hz (both one octave). If we drew bars linearly, almost
			// everything would cram into the low end. So we carve the bins into
			// bars whose width grows exponentially, giving bass and treble fair
			// visual space — the way a graphic equalizer looks.
			for (int i = 0; i < SPECTRUM_BARS; i++)
			{
				// Calculate frequency range for this bar (logarithmic distribution).
				// 2^(fraction * log2(N)) maps an evenly-spaced bar index onto an
				// exponentially-spaced bin range. As i climbs by 1, the start/end
				// bins jump by an ever-larger amount.
				double startFreq = Math.Pow(2, (double)i / SPECTRUM_BARS * Math.Log2(usableFFTSize));
				double endFreq = Math.Pow(2, (double)(i + 1) / SPECTRUM_BARS * Math.Log2(usableFFTSize));

				int startBin = (int)Math.Floor(startFreq);
				int endBin = (int)Math.Ceiling(endFreq);

				// Ensure we don't go out of bounds.
				startBin = Math.Max(1, Math.Min(startBin, usableFFTSize - 1)); // Skip DC component (bin 0)
				endBin = Math.Max(startBin, Math.Min(endBin, usableFFTSize - 1));
				// Note: bin 0 is the "DC component" — the average signal offset, not
				// an audible frequency — so we start at bin 1 and ignore it.

				// Average the magnitude of FFT bins in this range. .Magnitude is the
				// length of the complex number (sqrt(real² + imag²)) = how strong
				// that frequency is. Averaging across the bar's bins gives one value.
				double magnitude = 0;
				int binCount = endBin - startBin + 1;
				for (int bin = startBin; bin <= endBin; bin++)
				{
					magnitude += fftData[bin].Magnitude;
				}
				magnitude /= binCount;

				// Convert to dB scale and normalize.
				// Why dB (a logarithm again): loudness, like pitch, is perceived
				// logarithmically — raw magnitudes span a huge range and the quiet
				// detail would be invisible on a linear scale. 20*log10(x) is the
				// standard amplitude-to-decibels formula. The + 1e-10 avoids
				// log10(0) = -infinity when a bin is pure silence.
				double dB = 20 * Math.Log10(magnitude + 1e-10);

				// Map the dB value into the 0..1 range the renderer expects.
				// We assume roughly -60 dB (silence-ish) up to 0 dB (loudest):
				// (dB + 60) / 60 puts -60→0.0 and 0→1.0. Tune these two numbers if
				// the bars sit too low or clip at the top for your audio levels.
				double normalizedMagnitude = Math.Max(0, (dB + 60) / 60);

				// Clamp to 1.0 so a louder-than-expected bin can't overflow the bar.
				spectrumBars[i] = (float)Math.Min(1.0, normalizedMagnitude);
			}

			return spectrumBars;
		}

		public void Dispose()
		{
			_audioStateService.FileLoaded -= OnFileLoaded;
		}
	}
}
