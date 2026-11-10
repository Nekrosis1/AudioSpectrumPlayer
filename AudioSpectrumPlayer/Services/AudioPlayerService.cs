using AudioSpectrumPlayer.Interfaces;
using LibVLCSharp.Shared;
using Serilog;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace AudioSpectrumPlayer.Services
{
	/// <inheritdoc cref="IAudioPlayerService"/>
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
		// _mediaPlayer.Time stays 0 while stopped; report parked seek so relative seeks build on it.
		_pendingSeekMs is long pendingMs
			? TimeSpan.FromMilliseconds(pendingMs)
			: TimeSpan.FromMilliseconds(_mediaPlayer.Time < 0 ? 0 : _mediaPlayer.Time);

		/// <inheritdoc/>
		public TimeSpan TotalDuration
		{
			get
			{
				// _mediaPlayer.Length is -1 until playback starts.
				long ms = _mediaPlayer.Length;
				if (ms <= 0 && _currentMedia is not null)
				{
					ms = _currentMedia.Duration;
				}
				return TimeSpan.FromMilliseconds(ms < 0 ? 0 : ms);
			}
		}

		// Own source of truth: libvlc drops a volume set while no audio output exists (stopped).
		private int _volumePercent = 100;

		/// <inheritdoc/>
		public float Volume
		{
			get => _volumePercent / 100f;
			set
			{
				_volumePercent = Math.Clamp((int)Math.Round(value * 100f), 0, 100);
				_mediaPlayer.Volume = _volumePercent;
			}
		}

		public bool IsPlaying => _mediaPlayer.IsPlaying;

		public bool HasMedia => _currentMedia is not null;

		/// <inheritdoc/>
		public string? CurrentFilePath { get; private set; }

		/// <inheritdoc/>
		public event EventHandler<TimeSpan>? PositionChanged;
		/// <inheritdoc/>
		public event EventHandler<TimeSpan>? DurationChanged;
		/// <inheritdoc/>
		public event EventHandler<bool>? PlaybackStateChanged;
		/// <inheritdoc/>
		public event EventHandler<string>? MediaFailed;
		/// <inheritdoc/>
		public event EventHandler? MediaOpened;
		/// <inheritdoc/>
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

		private static void EnsureCoreInitialized()
		{
			if (s_coreInitialized) return;
			lock (s_initLock)
			{
				if (s_coreInitialized) return;
				RegisterLinuxSonameResolver();
				Core.Initialize();
				s_coreInitialized = true;
				Log.Debug("LibVLCSharp Core initialized");
			}
		}

		// LibVLCSharp imports bare "libvlc", i.e. the unversioned libvlc.so symlink, which
		// Debian/Ubuntu/Fedora ship only in -dev packages. Versioned sonames are always there.
		private static void RegisterLinuxSonameResolver()
		{
			if (!OperatingSystem.IsLinux()) return;

			NativeLibrary.SetDllImportResolver(typeof(LibVLC).Assembly, (name, assembly, searchPath) =>
			{
				string[] candidates = name switch
				{
					"libvlc" => ["libvlc.so.5", "libvlc.so"],
					"libvlccore" => ["libvlccore.so.9", "libvlccore.so"],
					_ => []
				};

				foreach (string candidate in candidates)
				{
					if (NativeLibrary.TryLoad(candidate, assembly, searchPath, out IntPtr handle))
					{
						Log.Debug("Resolved native library {Name} to {Candidate}", name, candidate);
						return handle;
					}
				}

				return IntPtr.Zero;
			});
		}

		private void OnTimeChanged(object? sender, MediaPlayerTimeChangedEventArgs e)
		{
			PositionChanged?.Invoke(this, TimeSpan.FromMilliseconds(e.Time));
		}

		private void OnLengthChanged(object? sender, MediaPlayerLengthChangedEventArgs e)
		{
			DurationChanged?.Invoke(this, TimeSpan.FromMilliseconds(e.Length));
		}

		private void OnPlaying(object? sender, EventArgs e)
		{
			Log.Debug("Playback started");

			// Runs on libvlc's event thread; calling back into libvlc inline deadlocks.
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

		// Audio output usually doesn't exist yet when Playing fires, so a volume set is silently dropped.
		// _volumePercent re-read each pass so a change made meanwhile wins.
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
				Thread.Sleep(25);
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

		/// <inheritdoc/>
		public async Task LoadAsync(string filePath)
		{
			_mediaPlayer.Stop();
			_pendingSeekMs = null;
			_currentMedia?.Dispose();
			_currentMedia = null;
			CurrentFilePath = null;

			Media media = new(_libVlc, new Uri(filePath));
			try
			{
				MediaParsedStatus parseStatus = await media.Parse(MediaParseOptions.ParseLocal);

				if (parseStatus != MediaParsedStatus.Done)
				{
					throw new InvalidOperationException($"The file could not be read (parse status: {parseStatus}).");
				}

				// Parse succeeds on files without audio (e.g. renamed text file, video-only container).
				if (!media.Tracks.Any(t => t.TrackType == TrackType.Audio))
				{
					throw new InvalidOperationException("No audio track was found in the file.");
				}
			}
			catch
			{
				media.Dispose();
				throw;
			}

			_currentMedia = media;
			_mediaPlayer.Media = _currentMedia;
			CurrentFilePath = filePath;

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

		/// <inheritdoc/>
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

				if (_mediaPlayer.State is VLCState.Playing or VLCState.Paused)
				{
					_mediaPlayer.Time = targetMs;
					_pendingSeekMs = null;
					// No TimeChanged tick follows while paused.
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

			try { _mediaPlayer.Stop(); } catch { }

			_mediaPlayer.Dispose();
			_currentMedia?.Dispose();
			_libVlc.Dispose();

			_disposed = true;
			Log.Debug("AudioPlayerService disposed");
		}
	}
}
