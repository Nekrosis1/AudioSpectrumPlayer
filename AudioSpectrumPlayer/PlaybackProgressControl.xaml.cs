using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System;
using System.Diagnostics;

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

		private void ProgressSlider_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			Debug.WriteLine($"PointerPressed");
			_isDragging = true;
		}

		private void ProgressSlider_PointerReleased(object sender, PointerRoutedEventArgs e)
		{
			double newPosition = progressSlider.Value / 100.0;
			Debug.WriteLine($"PointerReleased");
			_isDragging = false;
			PositionChanged?.Invoke(this, newPosition);
		}

		private void ProgressSlider_DragStarting(UIElement sender, DragStartingEventArgs args)
		{
			Debug.WriteLine($"DragStarting");
		}

		private void ProgressSlider_DragEnter(object sender, DragEventArgs e)
		{
			Debug.WriteLine($"DragEnter");
		}


		private void ProgressSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
		{
			//Debug.WriteLine($"Is Dragging: {_isDragging}");
			PositionChanged?.Invoke(this, e.NewValue / 100.0);
			//if (_isDragging)
			//{
			//}
		}

		public void UpdateProgressUI()
		{
			if (TotalDuration.TotalMilliseconds > 0)
			{
				double progress = (CurrentPosition.TotalMilliseconds / TotalDuration.TotalMilliseconds) * 100;
				progressSlider.Value = progress;

				string currentTime = FormatTimeSpan(CurrentPosition);
				string totalTime = FormatTimeSpan(TotalDuration);
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
			CurrentPosition = TimeSpan.Zero;
			UpdateProgressUI();
		}


	}
}

