using AudioSpectrumPlayer.Avalonia.Interfaces;
using Avalonia.Threading;
using Serilog;
using System;
using System.Threading.Tasks;

namespace AudioSpectrumPlayer.Avalonia.Services
{
	public class AudioStateService : IAudioStateService
	{
		private DispatcherTimer? _playbackTimer;
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

		public void StartMonitoring()
		{
			Dispatcher.UIThread.Post(() =>
			{
				if (_playbackTimer == null)
				{
					_playbackTimer = new DispatcherTimer
					{
						Interval = TimeSpan.FromMilliseconds(250)
					};
					_playbackTimer.Tick += PlaybackTimer_Tick;
				}

				_playbackTimer.Start();
			});
		}

		public void StopMonitoring()
		{
			Dispatcher.UIThread.Post(() =>
			{
				_playbackTimer?.Stop();
			});
		}

		private void PlaybackTimer_Tick(object? sender, EventArgs e)
		{
			try
			{
				// The ViewModel or AudioPlayerService will call UpdateCurrentPosition
				// This timer just ensures regular position updates are requested
				// We don't update position here directly anymore
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
			if (isActive == true)
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
		}
	}
}