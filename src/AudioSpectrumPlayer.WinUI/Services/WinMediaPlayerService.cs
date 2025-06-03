using AudioSpectrumPlayer.Core.Services;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Serilog;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace AudioSpectrumPlayer.Services
{
	internal class WinMediaPlayerService : IMediaPlayerService
	{
		private MediaPlayer _mediaPlayer = null!;
		private DispatcherTimer _playbackTimer = null!;

		public event EventHandler<TimeSpan>? PositionChanged;
		public event EventHandler<TimeSpan>? DurationChanged;

		public double Volume
		{
			get => _mediaPlayer?.Volume ?? 1.0;
			set
			{
				if (_mediaPlayer != null)
				{
					_mediaPlayer.Volume = value;
					Log.Debug($"Volume changed to {(int)(value * 100)}%");
				}
			}
		}

		public TimeSpan CurrentPosition => _mediaPlayer?.PlaybackSession?.Position ?? TimeSpan.Zero;
		public TimeSpan TotalDuration => _mediaPlayer?.PlaybackSession?.NaturalDuration ?? TimeSpan.Zero;

		public WinMediaPlayerService()
		{
			InitializeMediaPlayer();
		}

		private void InitializeMediaPlayer()
		{
			try
			{
				Log.Debug("Initializing MediaPlayer");

				_mediaPlayer = new MediaPlayer();

				_playbackTimer = new DispatcherTimer
				{
					Interval = TimeSpan.FromMilliseconds(100) // More responsive updates
				};
				_playbackTimer.Tick += PlaybackTimer_Tick;

				_mediaPlayer.MediaOpened += (sender, args) =>
				{
					try
					{
						Log.Debug("Media opened successfully");
						DurationChanged?.Invoke(this, TotalDuration);
						_playbackTimer.Start();
					}
					catch (Exception ex)
					{
						Log.Error(ex, "MediaOpened event");
					}
				};

				_mediaPlayer.MediaFailed += (sender, args) =>
				{
					try
					{
						Log.Error($"Media failed to load: {args.Error}");
					}
					catch (Exception ex)
					{
						Log.Error(ex, "MediaFailed event");
					}
				};

				_mediaPlayer.PlaybackSession.PlaybackStateChanged += (sender, args) =>
				{
					try
					{
						Log.Debug($"Playback state changed to: {sender.PlaybackState}");
					}
					catch (Exception ex)
					{
						Log.Error(ex, "PlaybackStateChanged event");
					}
				};

				Log.Debug("MediaPlayer initialized successfully");
			}
			catch (Exception ex)
			{
				Log.Error(ex, "InitializeMediaPlayer");
			}
		}

		private void PlaybackTimer_Tick(object? sender, object e)
		{
			try
			{
				if (_mediaPlayer?.PlaybackSession != null &&
					_mediaPlayer.PlaybackSession.NaturalDuration.TotalMilliseconds > 0)
				{
					PositionChanged?.Invoke(this, CurrentPosition);
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex, "PlaybackTimer_Tick");
			}
		}

		public async Task<bool> LoadAudioFileAsync(string filePath)
		{
			try
			{
				Log.Information($"Loading audio file: {filePath}");

				if (!File.Exists(filePath))
				{
					Log.Error($"Error: File not found: {filePath}");
					return false;
				}

				Uri uri = new(filePath);
				MediaSource mediaSource = MediaSource.CreateFromUri(uri);

				var dispatcherQueue = DispatcherQueue.GetForCurrentThread();
				if (dispatcherQueue != null)
				{
					dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
					{
						try
						{
							_mediaPlayer.Source = mediaSource;
							Log.Information("Media source set successfully");
						}
						catch (Exception ex)
						{
							Log.Error(ex, "Setting media source on dispatcher");
						}
					});
				}

				return true;
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Load Audio File failed");
				return false;
			}
		}

		public void Play()
		{
			if (_mediaPlayer?.Source != null)
			{
				Log.Information("Playing audio");
				_mediaPlayer.Play();
				_playbackTimer.Start();
			}
		}

		public void Pause()
		{
			if (_mediaPlayer?.Source != null)
			{
				Log.Information("Pausing audio");
				_mediaPlayer.Pause();
				_playbackTimer.Stop();
			}
		}

		public void Stop()
		{
			if (_mediaPlayer?.Source != null)
			{
				Log.Information("Stopping audio");
				_mediaPlayer.Position = TimeSpan.Zero;
				_mediaPlayer.Pause();
				_playbackTimer.Stop();
			}
		}

		public void SeekToPosition(double percentage)
		{
			try
			{
				if (_mediaPlayer?.PlaybackSession != null &&
					_mediaPlayer.PlaybackSession.CanSeek &&
					_mediaPlayer.PlaybackSession.NaturalDuration.TotalMilliseconds > 0)
				{
					TimeSpan newPosition = TimeSpan.FromMilliseconds(
						percentage * _mediaPlayer.PlaybackSession.NaturalDuration.TotalMilliseconds);

					_mediaPlayer.PlaybackSession.Position = newPosition;
					Log.Debug($"Seeked to position: {FormatTimeSpan(newPosition)}");
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex, "SeekToPosition");
			}
		}

		private static string FormatTimeSpan(TimeSpan timeSpan)
		{
			return timeSpan.Hours > 0
				? $"{timeSpan.Hours:00}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}"
				: $"{timeSpan.Minutes:00}:{timeSpan.Seconds:00}";
		}
	}
}
