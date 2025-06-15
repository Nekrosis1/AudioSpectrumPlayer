using AudioSpectrumPlayer.Interfaces;
using AudioSpectrumPlayer.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Serilog;
using System;

namespace AudioSpectrumPlayer.Views
{
	public partial class PlaybackProgressControl : UserControl
	{
		private readonly DispatcherQueue _uiDispatcher;
		public MainWindowViewModel ViewModel => (DataContext as MainWindowViewModel)!;
		private IAudioStateService? _audioStateService;

		public PlaybackProgressControl()
		{
			InitializeComponent();
			_audioStateService = App.GetRequiredService<IAudioStateService>();
			_audioStateService.PositionChanged += AudioStateService_PositionChanged;
			_audioStateService.TotalDurationChanged += AudioStateService_TotalDurationChanged;
			_uiDispatcher = DispatcherQueue;
			UpdateProgressUI();
			Log.Debug("PlaybackProgressControl INIT");
		}

		private void AudioStateService_PositionChanged(object? sender, TimeSpan position)
		{
			_uiDispatcher.TryEnqueue(() =>
			{
				UpdateProgressUI();
			});
		}

		private void AudioStateService_TotalDurationChanged(object? sender, TimeSpan duration)
		{
			_uiDispatcher.TryEnqueue(() =>
			{
				Log.Debug($"Total duration changed: {FormatTimeSpan(duration)}");
				UpdateProgressUI();
			});
		}

		private void ProgressSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
		{
			SeekToPosition(e.NewValue / 100.0);
		}

		private void SeekToPosition(double percentage)
		{
			ViewModel?.SeekToPosition(percentage);
		}

		public void UpdateProgressUI()
		{
			if (_audioStateService == null) return;

			var currentPosition = _audioStateService.CurrentPosition;
			var totalDuration = _audioStateService.TotalDuration;

			if (totalDuration.TotalMilliseconds > 0)
			{
				double progress = (currentPosition.TotalMilliseconds / totalDuration.TotalMilliseconds) * 100;
				progressSlider.Value = progress;

				string currentTime = FormatTimeSpan(currentPosition);
				string totalTime = FormatTimeSpan(totalDuration);
				timeDisplay.Text = $"{currentTime} / {totalTime}";
			}
			else
			{
				progressSlider.Value = 0;
				timeDisplay.Text = "00:00 / 00:00";
			}
		}

		private static string FormatTimeSpan(TimeSpan timeSpan)
		{
			return timeSpan.Hours > 0
				? $"{timeSpan.Hours:00}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}"
				: $"{timeSpan.Minutes:00}:{timeSpan.Seconds:00}";
		}

		public void Reset()
		{
			progressSlider.Value = 0;
			timeDisplay.Text = "00:00 / 00:00";
		}
	}
}

