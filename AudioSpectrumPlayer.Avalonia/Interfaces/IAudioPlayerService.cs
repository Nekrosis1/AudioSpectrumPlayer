using System;
using System.Threading.Tasks;

namespace AudioSpectrumPlayer.Avalonia.Interfaces
{
	/// <summary>
	/// Cross-platform audio playback service using NAudio.
	/// Replaces Windows.Media.Playback.MediaPlayer from WinUI3.
	/// </summary>
	public interface IAudioPlayerService : IDisposable
	{
		// Properties
		TimeSpan CurrentPosition { get; }
		TimeSpan TotalDuration { get; }
		float Volume { get; set; }
		bool IsPlaying { get; }

		// Events
		event EventHandler<TimeSpan>? PositionChanged;
		event EventHandler<TimeSpan>? DurationChanged;
		event EventHandler<bool>? PlaybackStateChanged;
		event EventHandler<string>? MediaFailed;
		event EventHandler? MediaOpened;
		event EventHandler? MediaEnded;

		// Methods
		Task LoadAsync(string filePath);
		void Play();
		void Pause();
		void Stop();
		void Seek(TimeSpan position);
	}
}
