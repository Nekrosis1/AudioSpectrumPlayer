using AudioSpectrumPlayer.Avalonia.Interfaces;
using AudioSpectrumPlayer.Avalonia.Services;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Serilog;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AudioSpectrumPlayer.Avalonia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
	private readonly IAudioPlayerService _audioPlayerService;
	private readonly IAudioFileService _audioFileService;
	private readonly IAudioStateService _audioStateService;
	private readonly SpectrumVisualizationService _spectrumVisualizationService;

	[ObservableProperty]
	private TimeSpan _currentPosition;

	[ObservableProperty]
	private TimeSpan _totalDuration;

	[ObservableProperty]
	private string _windowTitle = "Audio Spectrum Player";

	[ObservableProperty]
	private double _volume = 1.0;

	[ObservableProperty]
	private bool _isLogVisible;

	[ObservableProperty]
	private bool _isPlaying;

	public MainWindowViewModel(
		IAudioPlayerService audioPlayerService,
		IAudioFileService audioFileService,
		IAudioStateService audioStateService,
		SpectrumVisualizationService spectrumVisualizationService)
	{
		_audioPlayerService = audioPlayerService;
		_audioFileService = audioFileService;
		_audioStateService = audioStateService;
		_spectrumVisualizationService = spectrumVisualizationService;

		SubscribeToPlayerEvents();
	}

	private void SubscribeToPlayerEvents()
	{
		_audioPlayerService.PositionChanged += OnPositionChanged;
		_audioPlayerService.DurationChanged += OnDurationChanged;
		_audioPlayerService.PlaybackStateChanged += OnPlaybackStateChanged;
		_audioPlayerService.MediaOpened += OnMediaOpened;
		_audioPlayerService.MediaFailed += OnMediaFailed;
		_audioPlayerService.MediaEnded += OnMediaEnded;
	}

	private void OnPositionChanged(object? sender, TimeSpan position)
	{
		Dispatcher.UIThread.Post(() =>
		{
			CurrentPosition = position;
			_audioStateService.UpdateCurrentPosition(position);
		});
	}

	private void OnDurationChanged(object? sender, TimeSpan duration)
	{
		Dispatcher.UIThread.Post(() =>
		{
			TotalDuration = duration;
			_audioStateService.UpdateTotalDuration(duration);
		});
	}

	private void OnPlaybackStateChanged(object? sender, bool isPlaying)
	{
		Dispatcher.UIThread.Post(() =>
		{
			IsPlaying = isPlaying;
			_audioStateService.UpdatePlaybackState(isPlaying);
		});
	}

	private void OnMediaOpened(object? sender, EventArgs e)
	{
		Log.Debug("Media opened successfully");
	}

	private void OnMediaFailed(object? sender, string error)
	{
		Log.Error("Media failed to load: {Error}", error);
	}

	private void OnMediaEnded(object? sender, EventArgs e)
	{
		Log.Information("Media playback ended");
		Dispatcher.UIThread.Post(() =>
		{
			IsPlaying = false;
			_audioStateService.UpdatePlaybackState(false);
			_spectrumVisualizationService.StopVisualization();
		});
	}

	public void Play()
	{
		_audioPlayerService.Play();
		_spectrumVisualizationService.StartVisualization();
	}

	public void Pause()
	{
		_audioPlayerService.Pause();
		_spectrumVisualizationService.StopVisualization();
	}

	public void Stop()
	{
		_audioPlayerService.Stop();
		_spectrumVisualizationService.StopVisualization();
	}

	public void TogglePlayPause()
	{
		try
		{
			if (!_audioPlayerService.IsPlaying && _audioPlayerService.TotalDuration == TimeSpan.Zero)
			{
				Log.Warning("No media loaded, cannot toggle playback");
				return;
			}

			if (IsPlaying)
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
		_audioPlayerService.Volume = (float)value;
		Log.Debug("Volume changed to {Volume}%", (int)(value * 100));
	}

	public async Task<bool> SelectAndLoadAudioFileAsync(Window window)
	{
		try
		{
			string? filePath = await _audioFileService.PickAudioFileAsync(window);

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
			Log.Information("Loading audio file: {FilePath}", filePath);

			if (!File.Exists(filePath))
			{
				Log.Error("Error: File not found: {FilePath}", filePath);
				return;
			}

			await _audioStateService.LoadFileAsync(filePath);
			await _audioPlayerService.LoadAsync(filePath);

			Dispatcher.UIThread.Post(() =>
			{
				WindowTitle = $"{Path.GetFileName(filePath)} - Audio Spectrum Player";
				Log.Information("Media source set successfully");
			});
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Load Audio File failed");
		}
	}

	public void SeekToPosition(double percentage)
	{
		try
		{
			if (_audioPlayerService.TotalDuration.TotalMilliseconds > 0)
			{
				var newPosition = TimeSpan.FromMilliseconds(
					percentage * _audioPlayerService.TotalDuration.TotalMilliseconds);

				_audioPlayerService.Seek(newPosition);
				Log.Debug("Seeked to position: {Position}", FormatTimeSpan(newPosition));
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
		Log.Debug("Log visibility toggled to: {IsLogVisible}", IsLogVisible);
	}

	private static string FormatTimeSpan(TimeSpan timeSpan)
	{
		return timeSpan.Hours > 0
			? $"{timeSpan.Hours:00}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}"
			: $"{timeSpan.Minutes:00}:{timeSpan.Seconds:00}";
	}
}
