using AudioSpectrumPlayer.Avalonia.Interfaces;
using LibVLCSharp.Shared;
using Serilog;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AudioSpectrumPlayer.Avalonia.Services
{
	/// <summary>
	/// Cross-platform audio playback service backed by libvlc (via LibVLCSharp).
	/// Replaces the prior NAudio implementation, which depended on Windows
	/// Media Foundation (mfplat.dll) for MP3/FLAC/AAC decoding and therefore
	/// crashed on Linux.
	/// </summary>
	public class AudioPlayerService : IAudioPlayerService
	{
		private static bool s_coreInitialized;
		private static readonly Lock s_initLock = new();

		private readonly LibVLC _libVlc;
		private readonly MediaPlayer _mediaPlayer;
		private Media? _currentMedia;
		private bool _disposed;

		public TimeSpan CurrentPosition =>
			TimeSpan.FromMilliseconds(_mediaPlayer.Time < 0 ? 0 : _mediaPlayer.Time);

		public TimeSpan TotalDuration =>
			TimeSpan.FromMilliseconds(_mediaPlayer.Length < 0 ? 0 : _mediaPlayer.Length);

		public float Volume
		{
			get => _mediaPlayer.Volume / 100f;
			set => _mediaPlayer.Volume = Math.Clamp((int)Math.Round(value * 100f), 0, 100);
		}

		public bool IsPlaying => _mediaPlayer.IsPlaying;

		public bool HasMedia => _currentMedia is not null;

		public event EventHandler<TimeSpan>? PositionChanged;
		public event EventHandler<TimeSpan>? DurationChanged;
		public event EventHandler<bool>? PlaybackStateChanged;
		public event EventHandler<string>? MediaFailed;
		public event EventHandler? MediaOpened;
		public event EventHandler? MediaEnded;

		public AudioPlayerService()
		{
			EnsureCoreInitialized();

			_libVlc = new LibVLC();
			_mediaPlayer = new MediaPlayer(_libVlc);

			_mediaPlayer.TimeChanged += OnTimeChanged;
			_mediaPlayer.LengthChanged += OnLengthChanged;
			_mediaPlayer.Playing += OnPlaying;
			_mediaPlayer.Paused += OnPaused;
			_mediaPlayer.Stopped += OnStopped;
			_mediaPlayer.EndReached += OnEndReached;
			_mediaPlayer.EncounteredError += OnEncounteredError;
		}

		// libvlc requires a one-time native init before any LibVLC instance is created.
		private static void EnsureCoreInitialized()
		{
			if (s_coreInitialized) return;
			lock (s_initLock)
			{
				if (s_coreInitialized) return;
				Core.Initialize();
				s_coreInitialized = true;
				Log.Debug("LibVLCSharp Core initialized");
			}
		}

		private void OnTimeChanged(object? sender, MediaPlayerTimeChangedEventArgs e)
		{
			PositionChanged?.Invoke(this, TimeSpan.FromMilliseconds(e.Time));
		}

		private void OnLengthChanged(object? sender, MediaPlayerLengthChangedEventArgs e)
		{
			DurationChanged?.Invoke(this, TimeSpan.FromMilliseconds(e.Length));
		}

		// These fire on a libvlc background thread and confirm the actual player
		// state. Logged at Debug (file only); the user-facing "intent" logs live
		// in MainWindowViewModel (UI thread), which is where the LogDisplay panel
		// reliably picks them up.
		private void OnPlaying(object? sender, EventArgs e)
		{
			Log.Debug("Playback started");
			PlaybackStateChanged?.Invoke(this, true);
		}

		private void OnPaused(object? sender, EventArgs e)
		{
			Log.Debug("Playback paused");
			PlaybackStateChanged?.Invoke(this, false);
		}

		private void OnStopped(object? sender, EventArgs e)
		{
			Log.Debug("Playback stopped");
			PlaybackStateChanged?.Invoke(this, false);
		}

		private void OnEndReached(object? sender, EventArgs e)
		{
			Log.Information("Playback ended");
			MediaEnded?.Invoke(this, EventArgs.Empty);
		}

		private void OnEncounteredError(object? sender, EventArgs e)
		{
			const string message = "LibVLC encountered an error during playback";
			Log.Error(message);
			MediaFailed?.Invoke(this, message);
		}

		public async Task LoadAsync(string filePath)
		{
			try
			{
				_mediaPlayer.Stop();
				_currentMedia?.Dispose();

				_currentMedia = new Media(_libVlc, new Uri(filePath));

				// Parse synchronously enough to fill in Duration before we report MediaOpened.
				await _currentMedia.Parse(MediaParseOptions.ParseLocal);

				_mediaPlayer.Media = _currentMedia;

				TimeSpan duration = TimeSpan.FromMilliseconds(_currentMedia.Duration);
				if (_currentMedia.Duration > 0)
				{
					DurationChanged?.Invoke(this, duration);
				}

				MediaOpened?.Invoke(this, EventArgs.Empty);
				Log.Information("Audio file loaded successfully. Duration: {Duration:hh\\:mm\\:ss}", duration);
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to load audio file: {FilePath}", filePath);
				MediaFailed?.Invoke(this, ex.Message);
			}
		}

		public void Play()
		{
			if (_currentMedia is null)
			{
				Log.Warning("Cannot play: no audio loaded");
				return;
			}

			try
			{
				_mediaPlayer.Play();
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to start playback");
				MediaFailed?.Invoke(this, ex.Message);
			}
		}

		public void Pause()
		{
			try
			{
				_mediaPlayer.Pause();
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to pause playback");
			}
		}

		public void Stop()
		{
			try
			{
				_mediaPlayer.Stop();
				PositionChanged?.Invoke(this, TimeSpan.Zero);
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to stop playback");
			}
		}

		public void Seek(TimeSpan position)
		{
			if (_currentMedia is null)
			{
				Log.Warning("Cannot seek: no audio loaded");
				return;
			}

			try
			{
				long targetMs = (long)Math.Clamp(
					position.TotalMilliseconds,
					0,
					_mediaPlayer.Length > 0 ? _mediaPlayer.Length : long.MaxValue);

				_mediaPlayer.Time = targetMs;
				Log.Debug("Seeked to position: {Position} ms", targetMs);
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to seek to position: {Position}", position);
			}
		}

		public void Dispose()
		{
			if (_disposed) return;

			try { _mediaPlayer.Stop(); } catch { /* tearing down anyway */ }

			_mediaPlayer.Dispose();
			_currentMedia?.Dispose();
			_libVlc.Dispose();

			_disposed = true;
			Log.Debug("AudioPlayerService disposed");
		}
	}
}
