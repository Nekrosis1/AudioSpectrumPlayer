using AudioSpectrumPlayer.Interfaces;
using AudioSpectrumPlayer.Models;
using MathNet.Numerics.IntegralTransforms;
using System;
using System.Numerics;

namespace AudioSpectrumPlayer.Services
{
	/// <summary>
	/// Reuses its FFT buffer, so not thread-safe; one instance per consumer.
	/// </summary>
	public class SpectrumAnalyzerService : ISpectrumAnalyzerService
	{

		public const int FftSize = 8192;

		public const int WindowSize = FftSize;

		// Compile-time guard: fails to build (CS0031) if WindowSize > FftSize, since a negative
		// constant can't convert to uint.
		private const uint WindowFitsInFft = FftSize - WindowSize;

		public const WindowFunction Window = WindowFunction.Hann;

		// Floor is below the FFT's own rounding noise (≈ −168 dB), never hides real content.
		public const double DbFloor = -200.0;

		private static readonly double[] WindowValues = CreateWindow(Window, WindowSize);

		// A full-scale sine reads |X[k]| ≈ sum(window) / 2, so dividing by this (and doubling
		// for the mirrored half) puts it at 0 dB regardless of FFT size or window function.
		private static readonly double WindowSum = Sum(WindowValues);

		// Reused across calls: 8192 Complex values are 128 KB, which would land on the Large
		// Object Heap on every frame if allocated per call.
		private readonly Complex[] _fftBuffer = new Complex[FftSize];

		public Spectrum Analyze(PcmBlock block)
		{
			ArgumentNullException.ThrowIfNull(block);
			ArgumentNullException.ThrowIfNull(block.Samples, nameof(block));
			if (block.Samples.Length != WindowSize)
			{
				throw new ArgumentException(
					$"Block must hold exactly {WindowSize} samples, had {block.Samples.Length}.", nameof(block));
			}
			if (block.SampleRate <= 0)
			{
				throw new ArgumentException($"Sample rate must be positive, was {block.SampleRate}.", nameof(block));
			}

			float[] samples = block.Samples;
			for (int i = 0; i < WindowSize; i++)
			{
				// A NaN or ±Inf sample would turn every bin into NaN, which the dB floor can't
				// catch (Math.Max(NaN, floor) is NaN).
				if (!float.IsFinite(samples[i]))
				{
					throw new ArgumentException(
						$"Sample {i} is {samples[i]}; only finite samples can be analyzed.", nameof(block));
				}
				_fftBuffer[i] = new Complex(samples[i] * WindowValues[i], 0);
			}
			Array.Clear(_fftBuffer, WindowSize, FftSize - WindowSize);

			// NoScaling: the normalization below assumes an unscaled forward transform. The
			// default (FourierOptions.Default) divides by √N, ≈ 39 dB too low at N = 8192.
			Fourier.Forward(_fftBuffer, FourierOptions.NoScaling);

			// Bins above Nyquist mirror the ones below it for real input, so they are skipped.
			int nyquistBin = FftSize / 2;
			float[] magnitudesDb = new float[nyquistBin + 1];
			for (int k = 0; k <= nyquistBin; k++)
			{
				// A sine's energy is split between bin k and its mirror, hence factor 2.
				// DC and Nyquist have no mirror.
				double mirrorFactor = (k == 0) || (k == nyquistBin) ? 1.0 : 2.0;
				double amplitude = mirrorFactor * _fftBuffer[k].Magnitude / WindowSum;
				magnitudesDb[k] = (float)Math.Max(20.0 * Math.Log10(amplitude), DbFloor);
			}

			return new Spectrum(magnitudesDb, block.SampleRate, FftSize);
		}

		private static double[] CreateWindow(WindowFunction function, int length)
		{
			double[] window = new double[length];
			switch (function)
			{
				case WindowFunction.Hann:
					for (int n = 0; n < length; n++)
					{
						// Periodic form (divide by N, not N − 1): for a sine exactly on a bin, the
						// leakage then stays inside the two neighbouring bins.
						window[n] = 0.5 - (0.5 * Math.Cos(2.0 * Math.PI * n / length));
					}
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(function), function, "Unknown window function.");
			}
			return window;
		}

		private static double Sum(double[] values)
		{
			double sum = 0;
			foreach (double value in values)
			{
				sum += value;
			}
			return sum;
		}
	}
}
