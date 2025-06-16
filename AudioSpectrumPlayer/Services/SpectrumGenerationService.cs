using AudioSpectrumPlayer.Interfaces;
using MathNet.Numerics.IntegralTransforms;
using NAudio.Wave;
using Serilog;
using System;
using System.Linq;
using System.Numerics;

namespace AudioSpectrumPlayer.Services
{
	public class SpectrumGenerationService : IDisposable
	{
		private readonly IAudioStateService _audioStateService;
		private AudioFileReader? _audioFileReader;
		private ISampleProvider? _sampleProvider;
		private string? _currentFilePath;

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
			LoadAudioFile(filePath);
		}

		private void LoadAudioFile(string filePath)
		{
			// Dispose of previous resources
			_audioFileReader?.Dispose();

			try
			{
				_audioFileReader = new AudioFileReader(filePath);
				_sampleProvider = _audioFileReader.ToSampleProvider().ToMono();
				_currentFilePath = filePath;
				Log.Information($"SpectrumGenerationService loaded audio file: {filePath}");
			}
			catch (Exception ex)
			{
				// Log error and clean up
				_audioFileReader?.Dispose();
				_audioFileReader = null;
				_sampleProvider = null;
				_currentFilePath = null;
				throw; // or handle gracefully
			}
		}


		public float[] GetAudioChunkAtPosition(TimeSpan position, int bufferSize = 1024)
		{
			if (_audioFileReader == null || _sampleProvider == null)
			{
				return new float[bufferSize];
			}
			try
			{

				//long samplePosition = (long)(position.TotalSeconds * _audioFileReader.WaveFormat.SampleRate);

				// Seek to the desired position
				_audioFileReader.CurrentTime = position;
				float[] buffer = new float[bufferSize];
				int samplesRead = _sampleProvider.Read(buffer, 0, bufferSize);
				int nonZeroCount = buffer.Count(x => Math.Abs(x) > 0.001f);
				Log.Debug($"Samples read: {samplesRead}, Non-zero samples: {nonZeroCount}, Position: {position.TotalSeconds:F2}s");
				if (samplesRead < bufferSize)
				{
					Array.Clear(buffer, samplesRead, bufferSize - samplesRead);
				}
				return buffer;
			}
			catch (Exception)
			{
				// Return silence on error
				return new float[bufferSize];
			}
		}

		public void UpdateSpectrumData()
		{
			if (_audioStateService.CurrentFilePath == null) return;

			TimeSpan currentPosition = _audioStateService.CurrentPosition;
			float[] pcmData = GetAudioChunkAtPosition(currentPosition);

			float[] spectrumData = GenerateSpectrumData(pcmData);

			SpectrumDataUpdated?.Invoke(this, spectrumData);
		}

		private float[] GenerateSpectrumData(float[] pcmData)
		{
			try
			{
				// Ensure we have exactly FFT_SIZE samples
				if (pcmData.Length != FFT_SIZE)
				{
					Array.Resize(ref pcmData, FFT_SIZE);
					Log.Warning("PCM Array had bad size");
				}

				// Convert float array to Complex array for FFT
				var complexData = new Complex[FFT_SIZE];
				for (int i = 0; i < FFT_SIZE; i++)
				{
					// Apply Hamming window to reduce spectral leakage
					double windowValue = 0.54 - 0.46 * Math.Cos(2.0 * Math.PI * i / (FFT_SIZE - 1));
					complexData[i] = new Complex(pcmData[i] * windowValue, 0);
				}

				// Perform FFT
				Fourier.Forward(complexData, FourierOptions.Matlab);

				// Convert to magnitude spectrum and reduce to desired number of bars
				return ConvertToSpectrumBars(complexData);
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Error generating spectrum data");
				return new float[SPECTRUM_BARS]; // Return empty spectrum on error
			}
		}

		private float[] ConvertToSpectrumBars(Complex[] fftData)
		{
			var spectrumBars = new float[SPECTRUM_BARS];

			// Only use the first half of FFT data (positive frequencies)
			int usableFFTSize = FFT_SIZE / 2;

			// Group FFT bins into spectrum bars using logarithmic scaling
			for (int i = 0; i < SPECTRUM_BARS; i++)
			{
				// Calculate frequency range for this bar (logarithmic distribution)
				double startFreq = Math.Pow(2, (double)i / SPECTRUM_BARS * Math.Log2(usableFFTSize));
				double endFreq = Math.Pow(2, (double)(i + 1) / SPECTRUM_BARS * Math.Log2(usableFFTSize));

				int startBin = (int)Math.Floor(startFreq);
				int endBin = (int)Math.Ceiling(endFreq);

				// Ensure we don't go out of bounds
				startBin = Math.Max(1, Math.Min(startBin, usableFFTSize - 1)); // Skip DC component (bin 0)
				endBin = Math.Max(startBin, Math.Min(endBin, usableFFTSize - 1));

				// Average the magnitude of FFT bins in this range
				double magnitude = 0;
				int binCount = endBin - startBin + 1;

				for (int bin = startBin; bin <= endBin; bin++)
				{
					magnitude += fftData[bin].Magnitude;
				}

				magnitude /= binCount;

				// Convert to dB scale and normalize
				double dB = 20 * Math.Log10(magnitude + 1e-10); // Add small value to avoid log(0)

				// Normalize to 0-1 range (adjust these values based on your audio levels)
				double normalizedMagnitude = Math.Max(0, (dB + 60) / 60); // Assuming -60dB to 0dB range

				spectrumBars[i] = (float)Math.Min(1.0, normalizedMagnitude);
			}

			return spectrumBars;
		}


		public void Dispose()
		{
			_audioFileReader?.Dispose();
			_audioStateService.FileLoaded -= OnFileLoaded;
		}
	}
}
