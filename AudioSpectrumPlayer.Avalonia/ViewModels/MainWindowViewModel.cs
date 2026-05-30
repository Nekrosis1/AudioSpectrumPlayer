using AudioSpectrumPlayer.Avalonia.Interfaces;
using AudioSpectrumPlayer.Avalonia.Services;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

	/// <summary>
	/// Child view model for the log panel. Exposed so the menu's "Clear Log"
	/// can bind to <c>LogPanel.ClearLogCommand</c> while the menu's DataContext
	/// remains this view model. (Named LogPanel, not Log, to avoid shadowing
	/// Serilog's static <c>Log</c> used throughout this class.)
	/// </summary>
	public LogViewModel LogPanel { get; }

	[ObservableProperty]
	private TimeSpan _currentPosition;

	[ObservableProperty]
	private TimeSpan _totalDuration;

	/// <summary>Title shown when no file is loaded (or after a failed load).</summary>
	private const string DefaultWindowTitle = "Audio Spectrum Player";

	[ObservableProperty]
	private string _windowTitle = DefaultWindowTitle;

	[ObservableProperty]
	private double _volume = 1.0;

	[ObservableProperty]
	private bool _isLogVisible;

	[ObservableProperty]
	private bool _isPlaying;

	/// <summary>Whether the error element is currently shown.</summary>
	[ObservableProperty]
	private bool _isErrorVisible;

	/// <summary>Text shown in the error element when <see cref="IsErrorVisible"/> is true.</summary>
	[ObservableProperty]
	private string? _errorMessage;

	public MainWindowViewModel(
		IAudioPlayerService audioPlayerService,
		IAudioFileService audioFileService,
		IAudioStateService audioStateService,
		SpectrumVisualizationService spectrumVisualizationService,
		LogViewModel logViewModel)
	{
		_audioPlayerService = audioPlayerService;
		_audioFileService = audioFileService;
		_audioStateService = audioStateService;
		_spectrumVisualizationService = spectrumVisualizationService;
		LogPanel = logViewModel;

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

	// Fires for errors that happen during playback (libvlc EncounteredError), on a
	// background thread. Load-time failures don't come through here — they throw out
	// of LoadAudioFileAsync instead.
	private void OnMediaFailed(object? sender, string error)
	{
		Log.Error("Media failed during playback: {Error}", error);
		ShowError($"Playback error: {error}");
	}

	/// <summary>
	/// Displays a user-facing error message. Safe to call from any thread — marshals to
	/// the UI thread. (The log panel is a developer aid; this is what the end user sees.)
	/// </summary>
	private void ShowError(string message)
	{
		Dispatcher.UIThread.Post(() =>
		{
			ErrorMessage = message;
			IsErrorVisible = true;
		});
	}

	private void ClearError()
	{
		Dispatcher.UIThread.Post(() =>
		{
			IsErrorVisible = false;
			ErrorMessage = null;
		});
	}

	[RelayCommand]
	private void DismissError() => ClearError();

	/// <summary>
	/// Returns the playback UI to an empty state before a new load attempt. Called up
	/// front (not just on failure) so a failed — or any future non-success — load never
	/// leaves the previous file's title, duration, or progress on screen. A successful
	/// load re-populates these afterwards.
	/// </summary>
	private void ResetPlaybackState()
	{
		_audioPlayerService.Stop();
		_spectrumVisualizationService.StopVisualization();

		Dispatcher.UIThread.Post(() =>
		{
			IsPlaying = false;
			CurrentPosition = TimeSpan.Zero;
			TotalDuration = TimeSpan.Zero;
			WindowTitle = DefaultWindowTitle;

			_audioStateService.UpdatePlaybackState(false);
			_audioStateService.UpdateCurrentPosition(TimeSpan.Zero);
			_audioStateService.UpdateTotalDuration(TimeSpan.Zero);
		});
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
		Log.Information("Play");
		_audioPlayerService.Play();
		_spectrumVisualizationService.StartVisualization();
	}

	public void Pause()
	{
		Log.Information("Pause");
		_audioPlayerService.Pause();
		_spectrumVisualizationService.StopVisualization();
	}

	[RelayCommand]
	public void Stop()
	{
		Log.Information("Stop");
		_audioPlayerService.Stop();
		_spectrumVisualizationService.StopVisualization();
	}

	[RelayCommand]
	public void TogglePlayPause()
	{
		try
		{
			if (!_audioPlayerService.HasMedia)
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

	// Single point every volume change flows through — keyboard commands and the
	// VolumeControl drag both set the Volume property, which lands here.
	partial void OnVolumeChanged(double value)
	{
		_audioPlayerService.Volume = (float)value;
		Log.Debug("Volume changed to {Volume}%", (int)Math.Round(value * 100));
	}

	/// <summary>Volume change per +/- key press.</summary>
	private const double VolumeStep = 0.05;

	/// <summary>How far the seek-forward/backward shortcuts jump.</summary>
	private static readonly TimeSpan SeekStep = TimeSpan.FromSeconds(10);

	// Round to 2 decimals each step so we don't accumulate floating
	// point drift.
	[RelayCommand]
	private void VolumeUp()
	{
		Volume = Math.Clamp(Math.Round(Volume + VolumeStep, 2), 0.0, 1.0);
		Log.Information("Volume {Volume}%", (int)Math.Round(Volume * 100));
	}

	[RelayCommand]
	private void VolumeDown()
	{
		Volume = Math.Clamp(Math.Round(Volume - VolumeStep, 2), 0.0, 1.0);
		Log.Information("Volume {Volume}%", (int)Math.Round(Volume * 100));
	}

	[RelayCommand]
	private void SeekForward() => SeekRelative(SeekStep);

	[RelayCommand]
	private void SeekBackward() => SeekRelative(-SeekStep);

	private void SeekRelative(TimeSpan delta)
	{
		try
		{
			var total = _audioPlayerService.TotalDuration;

			// libvlc only knows the duration once playback has begun; until then
			// there is nothing meaningful to seek within.
			if (total.TotalMilliseconds <= 0)
			{
				return;
			}

			var target = _audioPlayerService.CurrentPosition + delta;
			if (target < TimeSpan.Zero) target = TimeSpan.Zero;
			if (target > total) target = total;

			_audioPlayerService.Seek(target);
			Log.Information("Seek {Delta}s -> {Position}", delta.TotalSeconds, FormatTimeSpan(target));
		}
		catch (Exception ex)
		{
			Log.Error(ex, "SeekRelative");
		}
	}

	[RelayCommand]
	private async Task OpenFileAsync()
	{
		try
		{
			string? filePath = await _audioFileService.PickAudioFileAsync();

			if (!string.IsNullOrEmpty(filePath))
			{
				await LoadAudioFileAsync(filePath);
			}
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Error selecting and loading audio file");
		}
	}

	public async Task LoadAudioFileAsync(string filePath)
	{
		try
		{
			Log.Information("Loading audio file: {FilePath}", filePath);
			ClearError();
			ResetPlaybackState();

			if (!File.Exists(filePath))
			{
				throw new FileNotFoundException("The file does not exist.", filePath);
			}

			await _audioStateService.LoadFileAsync(filePath);
			await _audioPlayerService.LoadAsync(filePath);

			// Only reached when the load genuinely succeeded — LoadAsync throws otherwise.
			Dispatcher.UIThread.Post(() =>
			{
				WindowTitle = $"{Path.GetFileName(filePath)} - Audio Spectrum Player";
				Log.Information("Media source set successfully");
			});
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Load Audio File failed: {FilePath}", filePath);
			ShowError($"Could not open \"{Path.GetFileName(filePath)}\".\n{ex.Message}");
		}
	}

	public void SeekToPosition(double percentage)
	{
		try
		{
			if (_audioPlayerService.TotalDuration.TotalMilliseconds > 0)
			{
				TimeSpan newPosition = TimeSpan.FromMilliseconds(
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

	[RelayCommand]
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
