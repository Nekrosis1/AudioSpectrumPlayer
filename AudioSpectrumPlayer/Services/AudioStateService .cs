using AudioSpectrumPlayer.Interfaces;
using Serilog;
using System;
using System.Threading.Tasks;

namespace AudioSpectrumPlayer.Services
{
	public class AudioStateService : IAudioStateService
	{
		public string? CurrentFilePath { get; private set; }
		public TimeSpan CurrentPosition { get; private set; }
		public TimeSpan TotalDuration { get; private set; }
		public bool IsPlaying { get; private set; }

		public event EventHandler<string>? FileLoaded;
		public event EventHandler<TimeSpan>? PositionChanged;
		public event EventHandler<bool>? PlaybackStateChanged;

		public async Task LoadFileAsync(string filePath)
		{
			CurrentFilePath = filePath;
			FileLoaded?.Invoke(this, filePath);
			await Task.CompletedTask;
		}

		public void UpdatePosition(TimeSpan position)
		{
			CurrentPosition = position;
			Log.Information($"Audio position updated: {position}");
			PositionChanged?.Invoke(this, position);
		}

		public void UpdatePlaybackState(bool isPlaying)
		{
			IsPlaying = isPlaying;
			PlaybackStateChanged?.Invoke(this, isPlaying);
		}

		public void UpdateDuration(TimeSpan duration)
		{
			TotalDuration = duration;
		}
	}
}