using System;
using System.Threading.Tasks;

namespace AudioSpectrumPlayer.Interfaces
{
	/// <summary>
	/// Cross-platform audio playback of one loaded file at a time.
	/// </summary>
	/// <remarks>
	/// Events may be raised on a background thread; marshal to the UI thread before touching UI state.
	/// </remarks>
	public interface IAudioPlayerService : IDisposable
	{
		TimeSpan CurrentPosition { get; }

		/// <summary>
		/// Length of loaded media, or <see cref="TimeSpan.Zero"/> if none is loaded or the length is unknown.
		/// </summary>
		TimeSpan TotalDuration { get; }

		/// <summary>
		/// Volume from 0 (mute) to 1 (full).
		/// </summary>
		float Volume { get; set; }

		bool IsPlaying { get; }

		bool HasMedia { get; }

		/// <summary>
		/// Path of the loaded file, or null if nothing is loaded.
		/// </summary>
		string? CurrentFilePath { get; }

		/// <summary>
		/// Raised with the new position during playback and after <see cref="Seek"/> or <see cref="Stop"/>.
		/// </summary>
		event EventHandler<TimeSpan>? PositionChanged;

		/// <summary>
		/// Raised with the total duration once it is known.
		/// </summary>
		event EventHandler<TimeSpan>? DurationChanged;

		/// <summary>
		/// Raised with true when playback starts, false when it pauses or stops.
		/// </summary>
		event EventHandler<bool>? PlaybackStateChanged;

		/// <summary>
		/// Raised with an error message when playback fails; <see cref="LoadAsync"/> throws instead.
		/// </summary>
		event EventHandler<string>? MediaFailed;

		/// <summary>
		/// Raised after <see cref="LoadAsync"/> succeeds.
		/// </summary>
		event EventHandler? MediaOpened;

		/// <summary>
		/// Raised when playback reaches the end of the file.
		/// </summary>
		event EventHandler? MediaEnded;

		/// <summary>
		/// Stops playback and replaces the current media with the given file.
		/// </summary>
		/// <param name="filePath">Absolute path to the audio file.</param>
		/// <exception cref="InvalidOperationException">File is unreadable or has no audio track; no media is loaded afterwards.</exception>
		Task LoadAsync(string filePath);

		void Play();

		void Pause();

		void Stop();

		/// <summary>
		/// Jumps to <paramref name="position"/>, clamped to the media length; while stopped, applied once playback starts.
		/// </summary>
		void Seek(TimeSpan position);
	}
}
