using AudioSpectrumPlayer.Interfaces;
using AudioSpectrumPlayer.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using Serilog;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace AudioSpectrumPlayer.ViewModels
{
	public partial class MainWindowViewModel : ObservableObject
	{
		private MediaPlayer _mediaPlayer = null!;
		private readonly IAudioFileService _audioFileService;
		private readonly IAudioStateService _audioStateService;
		private readonly SpectrumVisualizationService _spectrumVisualizationService;


#pragma warning disable MVVMTK0045 // Using [ObservableProperty] on fields is not AOT compatible for WinRT | I am waiting for the C# feature to be released stable
		[ObservableProperty]
		private TimeSpan _currentPosition;
		[ObservableProperty]
		private TimeSpan _totalDuration;
		[ObservableProperty]
		private string _windowTitle = "Audio Spectrum Player";
		[ObservableProperty]
		private double _volume = 1.0;
		[ObservableProperty]
		private bool _isLogVisible = false;
#pragma warning restore MVVMTK0045 // Using [ObservableProperty] on fields is not AOT compatible for WinRT

		public MainWindowViewModel(IAudioFileService audioFileService, IAudioStateService audioStateService, SpectrumVisualizationService spectrumVisualizationService)
		{
			_audioFileService = audioFileService;
			_audioStateService = audioStateService;
			_spectrumVisualizationService = spectrumVisualizationService;
			InitializeMediaPlayer();
		}

		private void InitializeMediaPlayer()
		{
			try
			{
				Log.Debug("Initializing MediaPlayer");
				_mediaPlayer = new MediaPlayer();
				_audioStateService.SetMediaPlayer(_mediaPlayer);

				_mediaPlayer.MediaOpened += (sender, args) =>
				{
					try
					{
						Log.Debug("Media opened successfully");
						InitializePlaybackState();
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
						if (sender.PlaybackState == MediaPlaybackState.Playing)
						{
							_audioStateService.UpdatePlaybackState(true);
							Log.Debug("AudioPlayer State is Playing");
						}
						else
						{
							_audioStateService.UpdatePlaybackState(false);
							Log.Debug("AudioPlayer State is NOT Playing");
						}
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

		private void InitializePlaybackState()
		{
			if (_mediaPlayer.Source != null && _mediaPlayer.PlaybackSession.NaturalDuration.TotalMilliseconds > 0)
			{
				Log.Information($"Initializing playback state, Duration = {_mediaPlayer.PlaybackSession.NaturalDuration}");
				_audioStateService.UpdateTotalDuration(_mediaPlayer.PlaybackSession.NaturalDuration);
				_audioStateService.UpdateCurrentPosition(TimeSpan.Zero);
				Log.Debug("Progress Bar Initialized");
			}
		}

		public void Play()
		{
			if (_mediaPlayer.Source != null)
			{
				Log.Information("Playing audio");
				_mediaPlayer.Play();
				_audioStateService.UpdatePlaybackState(true);
				//_spectrumVisualizationService.StartVisualization();
			}
		}

		public void Pause()
		{
			if (_mediaPlayer.Source != null)
			{
				Log.Information("Pausing audio");
				_mediaPlayer.Pause();
				_audioStateService.UpdatePlaybackState(false);
				_spectrumVisualizationService.StopVisualization();
			}
		}

		public void Stop()
		{
			if (_mediaPlayer.Source != null)
			{
				Log.Information("Stopping audio");
				_mediaPlayer.Position = TimeSpan.Zero;
				_mediaPlayer.Pause();
				_audioStateService.UpdatePlaybackState(false);
				_audioStateService.UpdateCurrentPosition(TimeSpan.Zero);
				_spectrumVisualizationService.StopVisualization();
			}
		}

		public void TogglePlayPause()
		{
			try
			{
				if (_mediaPlayer.Source == null)
				{
					Log.Warning("No media loaded, cannot toggle playback");
					return;
				}

				if (_audioStateService.IsPlaybackActive)
				{
					Pause();
				}
				else
				{
					Play();
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Error toggling play/pause");
			}
		}

		partial void OnVolumeChanged(double value)
		{
			if (_mediaPlayer != null)
			{
				_mediaPlayer.Volume = value;
				Log.Debug($"Volume changed to {(int)(value * 100)}%");
			}
		}

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

		public async Task LoadAudioFileAsync(string filePath)
		{
			try
			{
				Log.Information($"Loading audio file: {filePath}");

				if (!System.IO.File.Exists(filePath))
				{
					Log.Error($"Error: File not found: {filePath}");
					return;
				}

				Uri uri = new(filePath);
				await _audioStateService.LoadFileAsync(filePath);
				MediaSource mediaSource = MediaSource.CreateFromUri(uri);

				DispatcherQueue.GetForCurrentThread()?.TryEnqueue(DispatcherQueuePriority.Normal, () =>
				{
					try
					{
						_mediaPlayer.Source = mediaSource;
						WindowTitle = $"{Path.GetFileName(filePath)} - Audio Spectrum Player";
						Log.Information("Media source set successfully");
					}
					catch (Exception ex)
					{
						Log.Error(ex, "Setting media source on dispatcher");
					}
				});
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Load Audio File failed");
			}
			await Task.CompletedTask; // only for the IDE to be happy
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

		public void ToggleLogVisibility()
		{
			IsLogVisible = !IsLogVisible;
			Log.Debug($"Log visibility toggled to: {IsLogVisible}");
		}

		private static string FormatTimeSpan(TimeSpan timeSpan)
		{
			return timeSpan.Hours > 0
				? $"{timeSpan.Hours:00}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}"
				: $"{timeSpan.Minutes:00}:{timeSpan.Seconds:00}";
		}
	}
}
