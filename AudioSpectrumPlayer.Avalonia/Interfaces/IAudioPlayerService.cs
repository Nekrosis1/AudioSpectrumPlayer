using System;
using System.Threading.Tasks;

namespace AudioSpectrumPlayer.Avalonia.Interfaces
{
	/// <summary>
	/// Cross-platform audio playback service backed by libvlc (LibVLCSharp).
	/// Replaces Windows.Media.Playback.MediaPlayer from WinUI3.
	/// </summary>
	public interface IAudioPlayerService : IDisposable
	{
		// Properties
		TimeSpan CurrentPosition { get; }
		TimeSpan TotalDuration { get; }
		float Volume { get; set; }
		bool IsPlaying { get; }

		/// <summary>
		/// True once a media file has been loaded, regardless of whether playback
		/// has started. Note: <see cref="TotalDuration"/> cannot be used as a
		/// "is something loaded" check, because libvlc only knows the player's
		/// length after playback begins.
		/// </summary>
		bool HasMedia { get; }

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
