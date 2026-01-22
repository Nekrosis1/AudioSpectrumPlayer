using AudioSpectrumPlayer.Avalonia.Interfaces;
using NAudio.Wave;
using Serilog;
using System;
using System.Threading.Tasks;
using System.Timers;

namespace AudioSpectrumPlayer.Avalonia.Services
{
	/// <summary>
	/// Cross-platform audio playback service using NAudio.
	/// Uses WaveOutEvent for audio output and AudioFileReader for file loading.
	/// </summary>
	public class AudioPlayerService : IAudioPlayerService
	{
		private WaveOutEvent? _waveOut;
		private AudioFileReader? _audioFile;
		private Timer? _positionTimer;
		private bool _disposed;

		public TimeSpan CurrentPosition => _audioFile?.CurrentTime ?? TimeSpan.Zero;
		public TimeSpan TotalDuration => _audioFile?.TotalTime ?? TimeSpan.Zero;

		public float Volume
		{
			get => _waveOut?.Volume ?? 1.0f;
			set
			{
				if (_waveOut != null)
				{
					_waveOut.Volume = Math.Clamp(value, 0f, 1f);
				}
			}
		}

		public bool IsPlaying => _waveOut?.PlaybackState == PlaybackState.Playing;

		public event EventHandler<TimeSpan>? PositionChanged;
		public event EventHandler<TimeSpan>? DurationChanged;
		public event EventHandler<bool>? PlaybackStateChanged;
		public event EventHandler<string>? MediaFailed;
		public event EventHandler? MediaOpened;
		public event EventHandler? MediaEnded;

		public AudioPlayerService()
		{
			InitializePositionTimer();
		}

		private void InitializePositionTimer()
		{
			_positionTimer = new Timer(250); // Update every 250ms
			_positionTimer.Elapsed += OnPositionTimerElapsed;
			_positionTimer.AutoReset = true;
		}

		private void OnPositionTimerElapsed(object? sender, ElapsedEventArgs e)
		{
			if (_audioFile != null && IsPlaying)
			{
				PositionChanged?.Invoke(this, CurrentPosition);
			}
		}

		public async Task LoadAsync(string filePath)
		{
			try
			{
				Log.Information("Loading audio file: {FilePath}", filePath);

				// Dispose previous resources
				DisposeAudioResources();

				// Create new audio file reader
				_audioFile = new AudioFileReader(filePath);

				// Create output device
				_waveOut = new WaveOutEvent();
				_waveOut.Init(_audioFile);

				// Subscribe to playback stopped event
				_waveOut.PlaybackStopped += OnPlaybackStopped;

				// Notify listeners
				DurationChanged?.Invoke(this, TotalDuration);
				MediaOpened?.Invoke(this, EventArgs.Empty);

				Log.Information("Audio file loaded successfully. Duration: {Duration}", TotalDuration);
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to load audio file: {FilePath}", filePath);
				MediaFailed?.Invoke(this, ex.Message);
			}

			await Task.CompletedTask;
		}

		private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
		{
			if (e.Exception != null)
			{
				Log.Error(e.Exception, "Playback error occurred");
				MediaFailed?.Invoke(this, e.Exception.Message);
			}
			else if (_audioFile != null && _audioFile.CurrentTime >= _audioFile.TotalTime - TimeSpan.FromMilliseconds(100))
			{
				// Reached end of file
				Log.Information("Playback ended");
				MediaEnded?.Invoke(this, EventArgs.Empty);
			}

			_positionTimer?.Stop();
			PlaybackStateChanged?.Invoke(this, false);
		}

		public void Play()
		{
			if (_waveOut == null || _audioFile == null)
			{
				Log.Warning("Cannot play: No audio loaded");
				return;
			}

			try
			{
				// If we're at the end, restart from beginning
				if (_audioFile.CurrentTime >= _audioFile.TotalTime - TimeSpan.FromMilliseconds(100))
				{
					_audioFile.CurrentTime = TimeSpan.Zero;
				}

				_waveOut.Play();
				_positionTimer?.Start();
				PlaybackStateChanged?.Invoke(this, true);
				Log.Information("Playback started");
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to start playback");
				MediaFailed?.Invoke(this, ex.Message);
			}
		}

		public void Pause()
		{
			if (_waveOut == null)
			{
				Log.Warning("Cannot pause: No audio loaded");
				return;
			}

			try
			{
				_waveOut.Pause();
				_positionTimer?.Stop();
				PlaybackStateChanged?.Invoke(this, false);
				Log.Information("Playback paused");
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to pause playback");
			}
		}

		public void Stop()
		{
			if (_waveOut == null || _audioFile == null)
			{
				Log.Warning("Cannot stop: No audio loaded");
				return;
			}

			try
			{
				_waveOut.Stop();
				_audioFile.CurrentTime = TimeSpan.Zero;
				_positionTimer?.Stop();
				PositionChanged?.Invoke(this, TimeSpan.Zero);
				PlaybackStateChanged?.Invoke(this, false);
				Log.Information("Playback stopped");
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to stop playback");
			}
		}

		public void Seek(TimeSpan position)
		{
			if (_audioFile == null)
			{
				Log.Warning("Cannot seek: No audio loaded");
				return;
			}

			try
			{
				// Clamp position to valid range
				var clampedPosition = TimeSpan.FromTicks(
					Math.Clamp(position.Ticks, 0, _audioFile.TotalTime.Ticks));

				_audioFile.CurrentTime = clampedPosition;
				PositionChanged?.Invoke(this, clampedPosition);
				Log.Debug("Seeked to position: {Position}", clampedPosition);
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to seek to position: {Position}", position);
			}
		}

		private void DisposeAudioResources()
		{
			if (_waveOut != null)
			{
				_waveOut.PlaybackStopped -= OnPlaybackStopped;
				_waveOut.Stop();
				_waveOut.Dispose();
				_waveOut = null;
			}

			if (_audioFile != null)
			{
				_audioFile.Dispose();
				_audioFile = null;
			}
		}

		public void Dispose()
		{
			if (_disposed) return;

			_positionTimer?.Stop();
			_positionTimer?.Dispose();
			_positionTimer = null;

			DisposeAudioResources();

			_disposed = true;
			Log.Debug("AudioPlayerService disposed");
		}
	}
}
