using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;

namespace AudioSpectrumPlayer
{
	public partial class PlaybackProgressControl : UserControl
	{
		private bool _isDragging = false;

		public event EventHandler<double>? PositionChanged;

		public static readonly DependencyProperty CurrentPositionProperty =
			DependencyProperty.Register(
				nameof(CurrentPosition),
				typeof(TimeSpan),
				typeof(PlaybackProgressControl),
				new PropertyMetadata(TimeSpan.Zero, OnCurrentPositionChanged));

		public static readonly DependencyProperty TotalDurationProperty =
			DependencyProperty.Register(
				nameof(TotalDuration),
				typeof(TimeSpan),
				typeof(PlaybackProgressControl),
				new PropertyMetadata(TimeSpan.Zero, OnTotalDurationChanged));

		public TimeSpan CurrentPosition
		{
			get => (TimeSpan)GetValue(CurrentPositionProperty);
			set => SetValue(CurrentPositionProperty, value);
		}

		public TimeSpan TotalDuration
		{
			get => (TimeSpan)GetValue(TotalDurationProperty);
			set => SetValue(TotalDurationProperty, value);
		}

		private static void OnCurrentPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is PlaybackProgressControl control && !control._isDragging)
			{
				control.UpdateProgressUI();
			}
		}

		private static void OnTotalDurationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is PlaybackProgressControl control)
			{
				control.UpdateProgressUI();
			}
		}

		public PlaybackProgressControl()
		{
			this.InitializeComponent();
		}

		private void ProgressSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
		{
			if (_isDragging)
			{
				PositionChanged?.Invoke(this, e.NewValue / 100.0);
			}
		}

		public void UpdateProgressUI()
		{
			if (TotalDuration.TotalMilliseconds > 0)
			{
				_isDragging = false;
				double progress = (CurrentPosition.TotalMilliseconds / TotalDuration.TotalMilliseconds) * 100;
				progressSlider.Value = progress;
				_isDragging = true;

				string currentTime = FormatTimeSpan(CurrentPosition);
				string totalTime = FormatTimeSpan(TotalDuration);
				timeDisplay.Text = $"{currentTime} / {totalTime}";
			}
			else
			{
				_isDragging = false;
				progressSlider.Value = 0;
				_isDragging = true;
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
			CurrentPosition = TimeSpan.Zero;
			UpdateProgressUI();
		}
	}
}

