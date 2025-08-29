using AudioSpectrumPlayer.Interfaces;
using Serilog;
using System.Linq;

namespace AudioSpectrumPlayer.Services
{
	public class SpectrumVisualizationService
	{
		private readonly SpectrumGenerationService _spectrumGenerationService;
		private readonly IAudioStateService _audioStateService;
		private bool _isVisualizationActive = false;
		private float[] _currentSpectrumData = new float[64];

		public SpectrumVisualizationService(SpectrumGenerationService spectrumGenerationService, IAudioStateService audioStateService)
		{
			_spectrumGenerationService = spectrumGenerationService;
			_audioStateService = audioStateService;
			_spectrumGenerationService.SpectrumDataUpdated += OnSpectrumDataUpdated;

		}

		private void OnSpectrumDataUpdated(object? sender, float[] spectrumData)
		{
			_currentSpectrumData = spectrumData;
			// Here you can trigger UI updates or store data for rendering
			Log.Debug($"Spectrum updated - Max value: {spectrumData.Max():F3}");

		}

		public void StartVisualization()
		{
			_isVisualizationActive = true;
		}

		public void StopVisualization()
		{
			_isVisualizationActive = false;
		}

		private void UpdateSpectrum()
		{
			_spectrumGenerationService.UpdateSpectrumData();
			Log.Information("Spectrum data updated.");

			var currentPosition = _audioStateService!.CurrentPosition;
			Console.WriteLine($"Current Position: {currentPosition.TotalSeconds} seconds");
			var pcmData = _spectrumGenerationService.GetAudioChunkAtPosition(currentPosition);
			//var frequencies = AnalyzeFrequencies(pcmData);

			//// Update your visualization UI
			//UpdateVisualizationUI(frequencies);
		}
	}
}
