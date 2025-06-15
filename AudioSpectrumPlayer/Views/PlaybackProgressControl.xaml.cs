using AudioSpectrumPlayer.Interfaces;
using AudioSpectrumPlayer.ViewModels;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Serilog;
using System;

namespace AudioSpectrumPlayer.Views
{
	public partial class PlaybackProgressControl : UserControl
	{
		private MainWindowViewModel? _currentViewModel;
		private IAudioStateService? _audioStateService;

		public PlaybackProgressControl()
		{
			InitializeComponent();
			DataContextChanged += PlaybackProgressControl_DataContextChanged;
			Log.Debug("PlaybackProgressControl INIT");
		}

		private void PlaybackProgressControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
		{
			if (_audioStateService != null)
			{
				_audioStateService.PositionChanged -= AudioStateService_PositionChanged;
			}

			if (args.NewValue is MainWindowViewModel viewModel)
			{
				_audioStateService = App.GetRequiredService<IAudioStateService>();
				_currentViewModel = viewModel;
				_audioStateService.PositionChanged += AudioStateService_PositionChanged;
				_audioStateService.TotalDurationChanged += AudioStateService_TotalDurationChanged;
				//UpdateProgressUI();
			}
			else
			{
				_currentViewModel = null;
				_audioStateService = null;
			}
		}

		private void AudioStateService_PositionChanged(object? sender, TimeSpan position)
		{
			DispatcherQueue.GetForCurrentThread()?.TryEnqueue(() =>
			{
				UpdateProgressUI();
			});
		}

		private void AudioStateService_TotalDurationChanged(object? sender, TimeSpan duration)
		{
			// Dispatch to UI thread
			DispatcherQueue.GetForCurrentThread()?.TryEnqueue(() =>
			{
				UpdateProgressUI();
			});
		}

		private void ProgressSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
		{
			SeekToPosition(e.NewValue / 100.0);
		}

		private void SeekToPosition(double percentage)
		{
			_currentViewModel?.SeekToPosition(percentage);
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

