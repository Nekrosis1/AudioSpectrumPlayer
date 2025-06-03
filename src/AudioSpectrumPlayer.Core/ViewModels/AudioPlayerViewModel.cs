using AudioSpectrumPlayer.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Serilog;


namespace AudioSpectrumPlayer.Core.ViewModels
{
	public partial class AudioPlayerViewModel : ObservableObject
	{
		private readonly IMediaPlayerService _mediaPlayerService;
		private readonly IAudioFileService _audioFileService;

		[ObservableProperty]
		private string _currentFilePath;
		[ObservableProperty]
		private TimeSpan _currentPosition;
		[ObservableProperty]
		private TimeSpan _totalDuration;
		[ObservableProperty]
		private string _windowTitle = "Audio Player";
		[ObservableProperty]
		private double _volume = 1.0;

		public AudioPlayerViewModel(IAudioFileService audioFileService, IMediaPlayerService mediaPlayerService)
		{
			_mediaPlayerService = mediaPlayerService;
			_audioFileService = audioFileService;
			InitializeEvents();
		}

		private void InitializeEvents()
		{
			_mediaPlayerService.PositionChanged += OnPositionChanged;
			_mediaPlayerService.DurationChanged += OnDurationChanged;
		}

		private void OnPositionChanged(object? sender, TimeSpan position)
		{
			CurrentPosition = position;
		}

		private void OnDurationChanged(object? sender, TimeSpan duration)
		{
			TotalDuration = duration;
		}

		//private void InitializeMediaPlayer()
		//{
		//	try
		//	{
		//		Log.Debug("Initializing MediaPlayer");

		//		_mediaPlayer = new MediaPlayer();

		//		_playbackTimer = new DispatcherTimer
		//		{
		//			Interval = TimeSpan.FromMilliseconds(1000)
		//		};
		//		_playbackTimer.Tick += PlaybackTimer_Tick;

		//		_mediaPlayer.MediaOpened += (sender, args) =>
		//		{
		//			try
		//			{
		//				Log.Debug("Media opened successfully");
		//				InitializePlaybackState();

		//			}
		//			catch (Exception ex)
		//			{
		//				Log.Error(ex, "MediaOpened event");
		//			}
		//		};

		//		_mediaPlayer.MediaFailed += (sender, args) =>
		//		{
		//			try
		//			{
		//				Log.Error($"Media failed to load: {args.Error}");
		//			}
		//			catch (Exception ex)
		//			{
		//				Log.Error(ex, "MediaFailed event");
		//			}
		//		};

		//		_mediaPlayer.PlaybackSession.PlaybackStateChanged += (sender, args) =>
		//		{
		//			try
		//			{
		//				Log.Debug($"Playback state changed to: {sender.PlaybackState}");
		//			}
		//			catch (Exception ex)
		//			{
		//				Log.Error(ex, "PlaybackStateChanged event");
		//			}
		//		};

		//		//VolumeControl.VolumeChanged += VolumeControl_VolumeChanged;

		//		Log.Debug("MediaPlayer initialized successfully");
		//	}
		//	catch (Exception ex)
		//	{
		//		Log.Error(ex, "InitializeMediaPlayer");
		//	}
		//}

		//private void InitializePlaybackState()
		//{
		//	DispatcherQueue.GetForCurrentThread()?.TryEnqueue(() =>
		//	{
		//		if (_mediaPlayer.Source != null)
		//		{
		//			CurrentPosition = TimeSpan.Zero;
		//			TotalDuration = _mediaPlayer.PlaybackSession.NaturalDuration;

		//			if (_mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
		//			{
		//				_playbackTimer.Start();
		//			}
		//			Log.Debug("Progress Bar Initialized");
		//		}
		//	});
		//}

		public async Task<bool> SelectAndLoadAudioFileAsync(nint windowHandle)
		{
			try
			{
				string? filePath = await _audioFileService.PickAudioFileAsync(windowHandle);

				if (!string.IsNullOrEmpty(filePath))
				{
					await LoadAudioFileAsync(filePath);
					return true;
				}

				return false;
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Error selecting and loading audio file");
				return false;
			}
		}

		//private void PlaybackTimer_Tick(object? sender, object e)
		//{
		//	try
		//	{
		//		if (_mediaPlayer?.PlaybackSession != null &&
		//			_mediaPlayer.PlaybackSession.NaturalDuration.TotalMilliseconds > 0)
		//		{
		//			CurrentPosition = _mediaPlayer.PlaybackSession.Position;
		//			TotalDuration = _mediaPlayer.PlaybackSession.NaturalDuration;
		//		}
		//	}
		//	catch (Exception ex)
		//	{
		//		Log.Error(ex, "PlaybackTimer_Tick");
		//	}
		//}

		public void Play() => _mediaPlayerService.Play();
		public void Pause() => _mediaPlayerService.Pause();
		public void Stop() => _mediaPlayerService.Stop();
		public void SeekToPosition(double percentage) => _mediaPlayerService.SeekToPosition(percentage);

		partial void OnVolumeChanged(double value)
		{
			_mediaPlayerService.Volume = value;
		}

		public async Task LoadAudioFileAsync(string filePath)
		{
			try
			{
				bool success = await _mediaPlayerService.LoadAudioFileAsync(filePath);
				if (success)
				{
					_currentFilePath = filePath;
					WindowTitle = $"Audio Spectrum Player - {Path.GetFileName(filePath)}";
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Load Audio File failed");
			}
		}

		//public void SeekToPosition(double percentage)
		//{
		//	try
		//	{
		//		if (_mediaPlayer?.PlaybackSession != null &&
		//			_mediaPlayer.PlaybackSession.CanSeek &&
		//			_mediaPlayer.PlaybackSession.NaturalDuration.TotalMilliseconds > 0)
		//		{
		//			TimeSpan newPosition = TimeSpan.FromMilliseconds(
		//				percentage * _mediaPlayer.PlaybackSession.NaturalDuration.TotalMilliseconds);

		//			_mediaPlayer.PlaybackSession.Position = newPosition;

		//			// Update the property so UI reflects the change immediately
		//			CurrentPosition = newPosition;

		//			Log.Debug($"Seeked to position: {FormatTimeSpan(newPosition)}");
		//		}
		//	}
		//	catch (Exception ex)
		//	{
		//		Log.Error(ex, "SeekToPosition");
		//	}
		//}
		private static string FormatTimeSpan(TimeSpan timeSpan)
		{
			return timeSpan.Hours > 0
				? $"{timeSpan.Hours:00}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}"
				: $"{timeSpan.Minutes:00}:{timeSpan.Seconds:00}";
		}
	}
}
