using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Serilog;
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
			//Logger.Log("OnCurrentPositionChanged");
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
			Log.Debug("PlaybackProgressControl INIT");
			//this.DragStarting += ProgressSlider_DragStarting;
			//this.Holding += ProgressSlider_Holding;
			//this.ManipulationStarting += ProgressSlider_ManipulationStarting;
			//this.PointerPressed += ProgressSlider_PointerPressed;
			//this.PointerReleased += ProgressSlider_PointerReleased;

		}

		public void ProgressSlider_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			Log.Debug($"PointerPressed");
			//_isDragging = true;
		}

		public void ProgressSlider_PointerReleased(object sender, PointerRoutedEventArgs e)
		{
			//double newPosition = progressSlider.Value / 100.0;
			Log.Debug($"PointerReleased");
			//_isDragging = false;
			//PositionChanged?.Invoke(this, newPosition);
		}

		public void ProgressSlider_DragStarting(UIElement sender, DragStartingEventArgs args)
		{
			Log.Debug($"DragStarting");
		}

		public void ProgressSlider_DragEnter(object sender, DragEventArgs e)
		{
			Log.Debug($"DragEnter");
		}

		private void ProgressSlider_ManipulationStarting(object sender, ManipulationStartingRoutedEventArgs e)
		{
			Log.Debug($"ManipulationStarting");
		}

		private void ProgressSlider_Holding(object sender, dynamic e)
		{
			Log.Debug($"Holding");
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

