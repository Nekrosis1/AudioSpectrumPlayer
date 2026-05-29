using AudioSpectrumPlayer.Avalonia.Interfaces;
using AudioSpectrumPlayer.Avalonia.ViewModels;
using Avalonia.Controls;
using Avalonia.Threading;
using Serilog;
using System;

namespace AudioSpectrumPlayer.Avalonia.Views;

public partial class PlaybackProgressControl : UserControl
{
	private readonly IAudioStateService _audioStateService;
	private bool _suppressSeek;

	public MainWindowViewModel? ViewModel => DataContext as MainWindowViewModel;

	public PlaybackProgressControl()
	{
		InitializeComponent();
		_audioStateService = App.GetRequiredService<IAudioStateService>();
		_audioStateService.PositionChanged += OnPositionChanged;
		_audioStateService.TotalDurationChanged += OnTotalDurationChanged;

		progressSlider.ValueChanged += OnSliderValueChanged;

		UpdateProgressUI();
		Log.Debug("PlaybackProgressControl INIT");
	}

	private void OnPositionChanged(object? sender, TimeSpan position)
	{
		Dispatcher.UIThread.Post(UpdateProgressUI);
	}

	private void OnTotalDurationChanged(object? sender, TimeSpan duration)
	{
		Dispatcher.UIThread.Post(() =>
		{
			Log.Debug($"Total duration changed: {FormatTimeSpan(duration)}");
			UpdateProgressUI();
		});
	}

	private void OnSliderValueChanged(object? sender, global::Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
	{
		if (_suppressSeek) return;
		ViewModel?.SeekToPosition(e.NewValue / 100.0);
	}

	private void UpdateProgressUI()
	{
		var currentPosition = _audioStateService.CurrentPosition;
		var totalDuration = _audioStateService.TotalDuration;

		_suppressSeek = true;
		try
		{
			if (totalDuration.TotalMilliseconds > 0)
			{
				progressSlider.Value = (currentPosition.TotalMilliseconds / totalDuration.TotalMilliseconds) * 100;
				timeDisplay.Text = $"{FormatTimeSpan(currentPosition)} / {FormatTimeSpan(totalDuration)}";
			}
			else
			{
				progressSlider.Value = 0;
				timeDisplay.Text = "00:00 / 00:00";
			}
		}
		finally
		{
			_suppressSeek = false;
		}
	}

	private static string FormatTimeSpan(TimeSpan timeSpan)
	{
		return timeSpan.Hours > 0
			? $"{timeSpan.Hours:00}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}"
			: $"{timeSpan.Minutes:00}:{timeSpan.Seconds:00}";
	}
}
