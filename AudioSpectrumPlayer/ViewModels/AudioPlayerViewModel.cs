using AudioSpectrumPlayer.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Serilog;
using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Media.Playback;

namespace AudioSpectrumPlayer.ViewModels
{
	public partial class AudioPlayerViewModel : ObservableObject
	{
		private MediaPlayer _mediaPlayer;
		private DispatcherTimer _playbackTimer;
		private string _currentFilePath;
		[ObservableProperty]
		private TimeSpan _currentPosition;

		[ObservableProperty]
		private TimeSpan _totalDuration;
		private double _volume;

		public ICommand PlayCommand { get; }
		public ICommand PauseCommand { get; }
		public ICommand StopCommand { get; }
		public ICommand OpenFileCommand { get; }

		public AudioPlayerViewModel()
		{
			InitializeMediaPlayer();
			//SetupCommands();
		}

		private void InitializeMediaPlayer()
		{
			try
			{
				Log.Debug("Initializing MediaPlayer");

				_mediaPlayer = new MediaPlayer();

				_playbackTimer = new DispatcherTimer
				{
					Interval = TimeSpan.FromMilliseconds(1000)
				};
				_playbackTimer.Tick += PlaybackTimer_Tick;

				_mediaPlayer.MediaOpened += (sender, args) =>
				{
					try
					{
						Log.Debug("Media opened successfully");
						SetPlaybackTimer();

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

				VolumeControl.VolumeChanged += VolumeControl_VolumeChanged;

				Log.Debug("MediaPlayer initialized successfully");
			}
			catch (Exception ex)
			{
				Log.Error(ex, "InitializeMediaPlayer");
			}
		}

		private void SetPlaybackTimer()
		{
			if (_mediaPlayer.Source != null)
			{
				PlaybackProgress.CurrentPosition = TimeSpan.Zero;
				PlaybackProgress.TotalDuration = _mediaPlayer.PlaybackSession.NaturalDuration;
				if (_mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
				{
					_playbackTimer.Start();
				}
				Log.Debug($"Progress Bar Initialized");
			}
		}

		private void PlaybackTimer_Tick(object? sender, object e)
		{
			try
			{
				if (_mediaPlayer?.PlaybackSession != null &&
					_mediaPlayer.PlaybackSession.NaturalDuration.TotalMilliseconds > 0)
				{
					PlaybackProgress.CurrentPosition = _mediaPlayer.PlaybackSession.Position;
					PlaybackProgress.TotalDuration = _mediaPlayer.PlaybackSession.NaturalDuration;
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex, "PlaybackTimer_Tick");
			}
		}

		private async Task LoadAudioFileAsync(string filePath)
		{
			// Move file loading logic from MainWindow
		}

	}
}
