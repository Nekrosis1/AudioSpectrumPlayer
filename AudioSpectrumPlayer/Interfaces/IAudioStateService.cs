using System;
using System.Threading.Tasks;

namespace AudioSpectrumPlayer.Interfaces
{
	/// <summary>
	/// Shared playback state for the UI; only stores the values reported to it and controls no playback.
	/// </summary>
	public interface IAudioStateService
	{
		string? CurrentFilePath { get; }
		TimeSpan CurrentPosition { get; }
		bool IsPlaybackActive { get; }
		TimeSpan TotalDuration { get; }

		/// <summary>
		/// Raised by <see cref="LoadFileAsync"/> with the file path.
		/// </summary>
		event EventHandler<string>? FileLoaded;

		/// <summary>
		/// Raised by <see cref="UpdateCurrentPosition"/> with the new position.
		/// </summary>
		event EventHandler<TimeSpan>? PositionChanged;

		/// <summary>
		/// Raised by <see cref="UpdateTotalDuration"/> with the new duration.
		/// </summary>
		event EventHandler<TimeSpan>? TotalDurationChanged;

		/// <summary>
		/// Raised by <see cref="UpdatePlaybackState"/> with true when playback became active, false when it stopped.
		/// </summary>
		event EventHandler<bool>? PlaybackStateChanged;

		void StartMonitoring();
		void StopMonitoring();

		/// <summary>
		/// Records <paramref name="filePath"/> as the current file and raises <see cref="FileLoaded"/>; does not load any audio.
		/// </summary>
		Task LoadFileAsync(string filePath);

		void UpdateTotalDuration(TimeSpan duration);
		void UpdatePlaybackState(bool isPlaying);
		void UpdateCurrentPosition(TimeSpan position);
	}
}
