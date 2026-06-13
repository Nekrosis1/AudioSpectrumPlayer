using AudioSpectrumPlayer.Avalonia.Interfaces;
using LibVLCSharp.Shared;
using Serilog;
using System;
using System.Linq;
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

		// libvlc ignores a Time change unless the decoder is running, so a seek made
		// while stopped / not-yet-started is parked here and applied when Playing fires.
		private long? _pendingSeekMs;


		public TimeSpan CurrentPosition =>
			// While stopped/not-yet-started, _mediaPlayer.Time stays 0 even though a seek may
			// be parked in _pendingSeekMs. Report the parked target so relative seeks (and the
			// UI) build on the intended spot instead of repeatedly computing from 0.
			_pendingSeekMs is long pendingMs
				? TimeSpan.FromMilliseconds(pendingMs)
				: TimeSpan.FromMilliseconds(_mediaPlayer.Time < 0 ? 0 : _mediaPlayer.Time);

		public TimeSpan TotalDuration
		{
			get
			{
				// _mediaPlayer.Length is -1 until playback actually starts; fall back to the
				// duration libvlc learned while parsing so the value is known right after load
				// (otherwise seeking before the first Play computes against a zero duration).
				long ms = _mediaPlayer.Length;
				if (ms <= 0 && _currentMedia is not null)
				{
					ms = _currentMedia.Duration;
				}
				return TimeSpan.FromMilliseconds(ms < 0 ? 0 : ms);
			}
		}

		// Intended output volume (0–100), our own source of truth. libvlc only honors a
		// volume change while an audio output exists (playing/paused); a set while stopped
		// is dropped, so we re-apply this value when playback (re)starts.
		private int _volumePercent = 100;

		public float Volume
		{
			get => _volumePercent / 100f;
			set
			{
				_volumePercent = Math.Clamp((int)Math.Round(value * 100f), 0, 100);
				// Takes effect immediately while playing/paused (an audio output exists);
				// dropped while stopped, where OnPlaying re-applies it once the output comes up.
				_mediaPlayer.Volume = _volumePercent;
			}
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

		// fire on libvlc background thread and confirm actual state.
		private void OnPlaying(object? sender, EventArgs e)
		{
			Log.Debug("Playback started");

			// Re-apply the parked seek and the intended volume now that the decoder/output
			// are coming up — both are dropped by libvlc while stopped. Done on the thread
			// pool because this event runs on libvlc's own locked thread; calling back into
			// libvlc inline deadlocks (the app freezes).
			long? seekMs = _pendingSeekMs;
			_pendingSeekMs = null;
			Task.Run(() =>
			{
				try
				{
					if (seekMs is long ms)
					{
						_mediaPlayer.Time = ms;
					}
					ApplyIntendedVolumeUntilItSticks();
				}
				catch (Exception ex) { Log.Error(ex, "Failed to apply parked seek/volume"); }
			});
			PlaybackStateChanged?.Invoke(this, true);
		}

		// When Playing fires, libvlc's audio output usually doesn't exist yet, so a single
		// volume set is silently dropped (libvlc_audio_set_volume returns -1, no output).
		// Retry briefly until the output accepts it, verified by reading the value back.
		// Once it sticks we stop: volume does not drift during steady playback. _volumePercent
		// is re-read each pass so a volume change made during this startup window still wins.
		private void ApplyIntendedVolumeUntilItSticks()
		{
			for (int attempt = 0; attempt < 20; attempt++)
			{
				int wanted = _volumePercent;
				_mediaPlayer.Volume = wanted;
				if (_mediaPlayer.Volume == wanted)
				{
					return;
				}
				Thread.Sleep(25); // up to ~500 ms total for the output to appear
			}
			Log.Warning("Volume did not take effect after retries (wanted {Volume})", _volumePercent);
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

		/// <summary>
		/// Loads and validates a media file. Throws on failure (unreadable file, no audio
		/// track) and HasMedia remains false.
		/// The <see cref="MediaFailed"/> event is reserved for
		/// errors that occur later, during playback.
		/// </summary>
		public async Task LoadAsync(string filePath)
		{
			_mediaPlayer.Stop();
			_pendingSeekMs = null;
			_currentMedia?.Dispose();
			_currentMedia = null;

			Media media = new(_libVlc, new Uri(filePath));
			try
			{
				// Parse far enough to learn the duration and track list before we commit to
				// this media. Status tells whether libvlc could actually read the file
				MediaParsedStatus parseStatus = await media.Parse(MediaParseOptions.ParseLocal);

				if (parseStatus != MediaParsedStatus.Done)
				{
					throw new InvalidOperationException($"The file could not be read (parse status: {parseStatus}).");
				}

				// A parse can succeed on a file with no playable audio (e.g. a renamed text
				// file or a video-only container). Require at least one audio track.
				if (!media.Tracks.Any(t => t.TrackType == TrackType.Audio))
				{
					throw new InvalidOperationException("No audio track was found in the file.");
				}
			}
			catch
			{
				// Drop the half-loaded media so HasMedia stays false (otherwise Play() would
				// start a track that produces no sound), then let the caller report the reason.
				media.Dispose();
				throw;
			}

			_currentMedia = media;
			_mediaPlayer.Media = _currentMedia;

			TimeSpan duration = TimeSpan.FromMilliseconds(_currentMedia.Duration);
			if (_currentMedia.Duration > 0)
			{
				DurationChanged?.Invoke(this, duration);
			}

			MediaOpened?.Invoke(this, EventArgs.Empty);
			Log.Information("Audio file loaded successfully. Duration: {Duration:hh\\:mm\\:ss}", duration);
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
				_pendingSeekMs = null;
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
				double maxMs = TotalDuration.TotalMilliseconds > 0
					? TotalDuration.TotalMilliseconds
					: long.MaxValue;
				long targetMs = (long)Math.Clamp(position.TotalMilliseconds, 0, maxMs);

				// Only Playing/Paused decoders honor a Time change. Otherwise park the
				// target for OnPlaying and emit PositionChanged so the UI tracks the spot.
				if (_mediaPlayer.State is VLCState.Playing or VLCState.Paused)
				{
					_mediaPlayer.Time = targetMs;
					_pendingSeekMs = null;
					// Emit the new spot ourselves: while paused the decoder is idle, so no
					// TimeChanged tick follows and the UI would otherwise stay put until play
					// resumes. While playing this just gives immediate feedback before the next tick.
					PositionChanged?.Invoke(this, TimeSpan.FromMilliseconds(targetMs));
					Log.Debug("Seeked to position: {Position} ms", targetMs);
				}
				else
				{
					_pendingSeekMs = targetMs;
					PositionChanged?.Invoke(this, TimeSpan.FromMilliseconds(targetMs));
					Log.Debug("Parked seek until playback: {Position} ms", targetMs);
				}
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
