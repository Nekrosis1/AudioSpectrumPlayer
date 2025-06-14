using AudioSpectrumPlayer.Interfaces;
using AudioSpectrumPlayer.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Serilog;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace AudioSpectrumPlayer.ViewModels
{
	public partial class AudioPlayerViewModel : ObservableObject
	{
		private MediaPlayer _mediaPlayer = null!;
		private DispatcherTimer _playbackTimer = null!;
		private readonly IAudioFileService _audioFileService;
		private readonly IAudioStateService _audioStateService;
		private readonly SpectrumVisualizationService _spectrumVisualizationService;
		public bool IsPlaying { get; set; }

#pragma warning disable MVVMTK0045 // Using [ObservableProperty] on fields is not AOT compatible for WinRT | I am waiting for the C# feature to be released stable
		[ObservableProperty]
		private string? _currentFilePath;
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

		public AudioPlayerViewModel(IAudioFileService audioFileService, IAudioStateService audioStateService, SpectrumVisualizationService spectrumVisualizationService)
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
						InitializePlaybackState();
						_audioStateService.UpdateDuration(TotalDuration);

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
							IsPlaying = true;
							Log.Debug("AudioPlayer State is Playing");
						}
						else
						{
							IsPlaying = false;
							Log.Debug("AudioPlayer State is NOT Playing");
						}
						Log.Debug($"Playback state changed to: {sender.PlaybackState}");
					}
					catch (Exception ex)
					{
						Log.Error(ex, "PlaybackStateChanged event");
					}
				};

				//VolumeControl.VolumeChanged += VolumeControl_VolumeChanged;

				Log.Debug("MediaPlayer initialized successfully");
			}
			catch (Exception ex)
			{
				Log.Error(ex, "InitializeMediaPlayer");
			}
		}

		private void InitializePlaybackState()
		{
			DispatcherQueue.GetForCurrentThread()?.TryEnqueue(() =>
			{
				if (_mediaPlayer.Source != null)
				{
					CurrentPosition = TimeSpan.Zero;
					TotalDuration = _mediaPlayer.PlaybackSession.NaturalDuration;

					if (_mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
					{
						_playbackTimer.Start();
					}
					Log.Debug("Progress Bar Initialized");
				}
			});
		}

		private void PlaybackTimer_Tick(object? sender, object e)
		{
			try
			{
				if (_mediaPlayer?.PlaybackSession != null &&
					_mediaPlayer.PlaybackSession.NaturalDuration.TotalMilliseconds > 0)
				{
					CurrentPosition = _mediaPlayer.PlaybackSession.Position;
					_audioStateService.UpdatePosition(CurrentPosition);

				}
			}
			catch (Exception ex)
			{
				Log.Error(ex, "PlaybackTimer_Tick");
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
				_playbackTimer.Stop();
				_audioStateService.UpdatePlaybackState(false);
				//_spectrumVisualizationService.StartVisualization();
			}
		}

		public void Stop()
		{
			if (_mediaPlayer.Source != null)
			{
				Log.Information("Stopping audio");
				_mediaPlayer.Position = TimeSpan.Zero;
				_mediaPlayer.Pause();
				_playbackTimer.Stop();
				_audioStateService.UpdatePlaybackState(false);
				CurrentPosition = TimeSpan.Zero;
				_spectrumVisualizationService.StartVisualization();
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
				CurrentFilePath = filePath;

				Uri uri = new(filePath);
				await _audioStateService.LoadFileAsync(filePath);
				MediaSource mediaSource = MediaSource.CreateFromUri(uri);

				DispatcherQueue.GetForCurrentThread()?.TryEnqueue(DispatcherQueuePriority.Normal, () =>
				{
					try
					{
						_mediaPlayer.Source = mediaSource;
						_playbackTimer.Start();
						WindowTitle = $"{Path.GetFileName(filePath)} - Audio Spectrum Player";
						Log.Information("Media source set successfully");
					}
					catch (Exception ex)
					{
						Log.Error(ex, "Setting media source on dispatcher");
					}
					//await Task.CompletedTask; // only for the IDE to be happy
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

					// Update the property so UI reflects the change immediately
					CurrentPosition = newPosition;

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
