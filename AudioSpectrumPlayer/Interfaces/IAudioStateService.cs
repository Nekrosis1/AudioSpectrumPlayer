using System;
using System.Threading.Tasks;

namespace AudioSpectrumPlayer.Interfaces
{
	public interface IAudioStateService
	{
		string? CurrentFilePath { get; }
		TimeSpan CurrentPosition { get; }
		bool IsPlaying { get; }
		TimeSpan TotalDuration { get; }

		event EventHandler<string>? FileLoaded;
		event EventHandler<bool>? PlaybackStateChanged;
		event EventHandler<TimeSpan>? PositionChanged;

		Task LoadFileAsync(string filePath);
		void UpdateDuration(TimeSpan duration);
		void UpdatePlaybackState(bool isPlaying);
		void UpdatePosition(TimeSpan position);
	}
}