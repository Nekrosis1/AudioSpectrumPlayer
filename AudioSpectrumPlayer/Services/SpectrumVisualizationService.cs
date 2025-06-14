using AudioSpectrumPlayer.Interfaces;
using Serilog;
using System.Windows.Media;

namespace AudioSpectrumPlayer.Services
{
	public class SpectrumVisualizationService(SpectrumGenerationService _spectrumGenerationService, IAudioStateService _audioStateService)
	{
		private bool _isVisualizationActive = false;

		public void StartVisualization()
		{
			_isVisualizationActive = true;
			CompositionTarget.Rendering += OnRendering;
		}

		public void StopVisualization()
		{
			_isVisualizationActive = false;
			CompositionTarget.Rendering -= OnRendering;
		}

		private void OnRendering(object sender, object e)
		{
			if (!_isVisualizationActive || !_audioStateService.IsPlaying)
				return;

			UpdateSpectrum();
		}

		private void UpdateSpectrum()
		{
			_spectrumGenerationService.UpdatePCMData();
			Log.Information("Spectrum data updated.");
			//var currentPosition = _audioPlayerViewModel!.CurrentPosition;
			//Console.WriteLine($"Current Position: {currentPosition.TotalSeconds} seconds");
			//var pcmData = _spectrumGenerationService.GetAudioChunkAtPosition(_audioFile, currentPosition);
			//var frequencies = AnalyzeFrequencies(pcmData);

			//// Update your visualization UI
			//UpdateVisualizationUI(frequencies);
		}
	}
}
