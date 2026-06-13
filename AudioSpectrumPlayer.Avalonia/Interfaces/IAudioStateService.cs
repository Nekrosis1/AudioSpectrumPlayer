using System;
using System.Threading.Tasks;

namespace AudioSpectrumPlayer.Avalonia.Interfaces
{
	public interface IAudioStateService
	{
		string? CurrentFilePath { get; }
		TimeSpan CurrentPosition { get; }
		bool IsPlaybackActive { get; }
		TimeSpan TotalDuration { get; }

		event EventHandler<string>? FileLoaded;
		event EventHandler<TimeSpan>? PositionChanged;
		event EventHandler<TimeSpan>? TotalDurationChanged;
		event EventHandler<bool>? PlaybackStateChanged;

		void StartMonitoring();
		void StopMonitoring();
		Task LoadFileAsync(string filePath);
		void UpdateTotalDuration(TimeSpan duration);
		void UpdatePlaybackState(bool isPlaying);
		void UpdateCurrentPosition(TimeSpan position);
	}
}