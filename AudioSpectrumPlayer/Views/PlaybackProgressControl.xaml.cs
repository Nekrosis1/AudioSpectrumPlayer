using AudioSpectrumPlayer.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Serilog;
using System;
using System.ComponentModel;

namespace AudioSpectrumPlayer.Views
{
	public partial class PlaybackProgressControl : UserControl
	{
		private AudioPlayerViewModel? _currentViewModel;

		public PlaybackProgressControl()
		{
			this.InitializeComponent();
			DataContextChanged += PlaybackProgressControl_DataContextChanged;
			Log.Debug("PlaybackProgressControl INIT");
		}

		private void PlaybackProgressControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
		{
			// Unsubscribe from previous ViewModel
			if (_currentViewModel != null)
			{
				_currentViewModel.PropertyChanged -= ViewModel_PropertyChanged;
			}

			// Subscribe to new ViewModel
			if (args.NewValue is AudioPlayerViewModel viewModel)
			{
				_currentViewModel = viewModel;
				viewModel.PropertyChanged += ViewModel_PropertyChanged;
				UpdateProgressUI(); // Initial update
			}
			else
			{
				_currentViewModel = null;
			}
		}

		private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(AudioPlayerViewModel.CurrentPosition) ||
				e.PropertyName == nameof(AudioPlayerViewModel.TotalDuration))
			{
				UpdateProgressUI();
			}
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
			if (_currentViewModel == null) return;

			var currentPosition = _currentViewModel.CurrentPosition;
			var totalDuration = _currentViewModel.TotalDuration;

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

