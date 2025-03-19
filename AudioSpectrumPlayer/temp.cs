//using Microsoft.UI.Xaml;
//using Microsoft.UI.Xaml.Controls;
//using Microsoft.UI.Xaml.Controls.Primitives;
//using Microsoft.UI.Xaml.Input;
//using System;

//namespace AudioSpectrumPlayer
//{
//	public partial class PlaybackProgressControl : UserControl
//	{
//		private bool isDragging = false;
//		private double dragValue = 0;

//		public event EventHandler<double>? PositionChanged;

//		public static readonly DependencyProperty CurrentPositionProperty =
//			DependencyProperty.Register(
//				nameof(CurrentPosition),
//				typeof(TimeSpan),
//				typeof(PlaybackProgressControl),
//				new PropertyMetadata(TimeSpan.Zero, OnCurrentPositionChanged));

//		public static readonly DependencyProperty TotalDurationProperty =
//			DependencyProperty.Register(
//				nameof(TotalDuration),
//				typeof(TimeSpan),
//				typeof(PlaybackProgressControl),
//				new PropertyMetadata(TimeSpan.Zero, OnTotalDurationChanged));

//		public TimeSpan CurrentPosition
//		{
//			get => (TimeSpan)GetValue(CurrentPositionProperty);
//			set => SetValue(CurrentPositionProperty, value);
//		}

//		public TimeSpan TotalDuration
//		{
//			get => (TimeSpan)GetValue(TotalDurationProperty);
//			set => SetValue(TotalDurationProperty, value);
//		}

//		private static void OnCurrentPositionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
//		{
//			if (d is PlaybackProgressControl control && !control.isDragging)
//			{
//				control.UpdateProgressUI();
//			}
//		}

//		private static void OnTotalDurationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
//		{
//			if (d is PlaybackProgressControl control)
//			{
//				control.UpdateProgressUI();
//			}
//		}

//		public PlaybackProgressControl()
//		{
//			this.InitializeComponent();
//		}

//		private void ProgressContainer_PointerPressed(object sender, PointerRoutedEventArgs e)
//		{
//			var point = e.GetCurrentPoint((UIElement)sender);
//			var position = point.Position.X;
//			var total = ((Grid)sender).ActualWidth;
//			var percentage = (position / total) * 100.0;

//			isDragging = true;
//			dragValue = percentage;
//			UpdateThumbPosition(percentage);

//			PositionChanged?.Invoke(this, percentage / 100.0);
//			isDragging = false;
//		}

//		private void SeekThumb_DragStarted(object sender, DragStartedEventArgs e)
//		{
//			isDragging = true;
//			dragValue = progressBar.Value;
//		}

//		private void SeekThumb_DragDelta(object sender, DragDeltaEventArgs e)
//		{
//			if (isDragging)
//			{
//				double horizontalChange = e.HorizontalChange;
//				double progressBarWidth = progressBar.ActualWidth;

//				double changePercentage = (horizontalChange / progressBarWidth) * 100.0;
//				double newValue = dragValue + changePercentage;

//				// Clamp between 0 and 100
//				newValue = Math.Max(0, Math.Min(100, newValue));
//				dragValue = newValue;

//				UpdateThumbPosition(newValue);
//			}
//		}

//		private void SeekThumb_DragCompleted(object sender, DragCompletedEventArgs e)
//		{
//			if (isDragging)
//			{
//				PositionChanged?.Invoke(this, dragValue / 100.0);
//				isDragging = false;
//			}
//		}

//		private void UpdateProgressUI()
//		{
//			if (TotalDuration.TotalMilliseconds > 0)
//			{
//				double progress = (CurrentPosition.TotalMilliseconds / TotalDuration.TotalMilliseconds) * 100;
//				progressBar.Value = progress;
//				UpdateThumbPosition(progress);

//				string currentTime = FormatTimeSpan(CurrentPosition);
//				string totalTime = FormatTimeSpan(TotalDuration);
//				timeDisplay.Text = $"{currentTime} / {totalTime}";
//			}
//			else
//			{
//				progressBar.Value = 0;
//				UpdateThumbPosition(0);
//				timeDisplay.Text = "00:00 / 00:00";
//			}
//		}

//		private void UpdateThumbPosition(double percentage)
//		{
//			percentage = Math.Max(0, Math.Min(100, percentage));
//			double position = (percentage / 100.0) * progressBar.ActualWidth;
//			Canvas.SetLeft(seekThumb, position);
//		}

//		private static string FormatTimeSpan(TimeSpan timeSpan)
//		{
//			return timeSpan.Hours > 0
//				? $"{timeSpan.Hours:00}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}"
//				: $"{timeSpan.Minutes:00}:{timeSpan.Seconds:00}";
//		}

//		public void Reset()
//		{
//			CurrentPosition = TimeSpan.Zero;
//			UpdateProgressUI();
//		}
//	}
//}