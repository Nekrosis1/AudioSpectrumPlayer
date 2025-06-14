using AudioSpectrumPlayer.Interfaces;
using NAudio.Wave;
using Serilog;
using System;

namespace AudioSpectrumPlayer.Services
{
	public class SpectrumGenerationService : IDisposable
	{
		private readonly IAudioStateService _audioStateService;
		private AudioFileReader? _audioFileReader;
		private ISampleProvider? _sampleProvider;
		private string? _currentFilePath;

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

				// Calculate the sample offset based on the time position
				//long sampleOffset = (long)(position.TotalSeconds * sampleProvider.WaveFormat.SampleRate);

				// Seek to the desired position
				_audioFileReader.CurrentTime = position;

				float[] buffer = new float[bufferSize];
				int samplesRead = _sampleProvider.Read(buffer, 0, bufferSize);

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

		public void UpdatePCMData()
		{
			if (_audioStateService.CurrentFilePath == null) return;

			var currentPosition = _audioStateService.CurrentPosition;
			//Console.WriteLine($"Current Position: {currentPosition.TotalSeconds} seconds");

			var pcmData = GetAudioChunkAtPosition(currentPosition);

		}

		public void Dispose()
		{
			_audioFileReader?.Dispose();
			_audioStateService.FileLoaded -= OnFileLoaded;
		}
	}
}
