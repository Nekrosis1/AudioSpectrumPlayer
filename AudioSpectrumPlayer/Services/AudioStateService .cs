using AudioSpectrumPlayer.Interfaces;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Serilog;
using System;
using System.Threading.Tasks;
using Windows.Media.Playback;

namespace AudioSpectrumPlayer.Services
{
	public class AudioStateService : IAudioStateService
	{
		private DispatcherTimer? _playbackTimer;
		private MediaPlayer? _mediaPlayer;
		public string? CurrentFilePath { get; private set; }
		public TimeSpan CurrentPosition { get; private set; }
		public TimeSpan TotalDuration { get; private set; }
		public bool IsPlaybackActive { get; private set; }

		public event EventHandler<string>? FileLoaded;
		public event EventHandler<TimeSpan>? PositionChanged;
		public event EventHandler<TimeSpan>? TotalDurationChanged;
		public event EventHandler<bool>? PlaybackStateChanged;

		public async Task LoadFileAsync(string filePath)
		{
			CurrentFilePath = filePath;
			FileLoaded?.Invoke(this, filePath);
			await Task.CompletedTask;
		}

		public void SetMediaPlayer(MediaPlayer mediaPlayer)
		{
			_mediaPlayer = mediaPlayer;
		}

		public void StartMonitoring()
		{
			DispatcherQueue.GetForCurrentThread()?.TryEnqueue(() =>
			{
				_playbackTimer ??= new DispatcherTimer
				{
					Interval = TimeSpan.FromMilliseconds(250)
				};

				_playbackTimer.Tick += PlaybackTimer_Tick;
				_playbackTimer.Start();
			});
		}

		public void StopMonitoring()
		{
			DispatcherQueue.GetForCurrentThread()?.TryEnqueue(() =>
			{
				_playbackTimer?.Stop();
			});
		}

		private void PlaybackTimer_Tick(object? sender, object e)
		{
			try
			{
				if (_mediaPlayer?.PlaybackSession != null &&
					_mediaPlayer.PlaybackSession.NaturalDuration.TotalMilliseconds > 0)
				{
					UpdateCurrentPosition(_mediaPlayer.PlaybackSession.Position);
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex, "PlaybackTimer_Tick in AudioStateService");
			}
		}

		public void UpdateCurrentPosition(TimeSpan position)
		{
			CurrentPosition = position;
			PositionChanged?.Invoke(this, position);
		}

		public void UpdatePlaybackState(bool isActive)
		{
			IsPlaybackActive = isActive;
			if (isActive == true && _mediaPlayer != null)
			{
				StartMonitoring();
				Log.Information($"Current Position Ticking started");
			}
			else
			{
				StopMonitoring();
				Log.Information($"Current Position Ticking stopped");
			}
			PlaybackStateChanged?.Invoke(this, isActive);
		}

		public void UpdateTotalDuration(TimeSpan duration)
		{
			TotalDuration = duration;
			TotalDurationChanged?.Invoke(this, duration);
		}

		public void Dispose()
		{
			_playbackTimer?.Stop();
			_playbackTimer = null;
			_mediaPlayer = null;
		}
	}
}