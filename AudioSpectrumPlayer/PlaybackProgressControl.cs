using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using System;

namespace AudioSpectrumPlayer
{
	class PlaybackProgressControl : UserControl
	{
		private Grid mainGrid;
		private ProgressBar progressBar;
		private Thumb seekThumb;
		private TextBlock timeDisplay;
		private bool isDragging = false;
		private double dragValue = 0;

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
			if (d is PlaybackProgressControl control && !control.isDragging)
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
			this.DefaultStyleKey = typeof(PlaybackProgressControl);
			this.InitializeControl();
		}

		private void InitializeControl()
		{
			mainGrid = new Grid();
			mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

			progressBar = new ProgressBar
			{
				Height = 8,
				Margin = new Thickness(0, 0, 0, 8),
				Value = 0,
				Maximum = 100,
				Background = new SolidColorBrush(Windows.UI.Color.FromArgb(50, 128, 128, 128)),
				Foreground = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 120, 215)),
			};
			// remove animations
			//var styleResourceDictionary = new ResourceDictionary();
			var progressBarStyle = new Style(typeof(ProgressBar));
			progressBarStyle.Setters.Add(new Setter(TransitionsProperty, new TransitionCollection()));
			progressBar.Style = progressBarStyle;

			seekThumb = new Thumb
			{
				Width = 16,
				Height = 16,
				Background = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)),
				CornerRadius = new CornerRadius(8),
				Margin = new Thickness(-8, 0, 0, 0),
				VerticalAlignment = VerticalAlignment.Center,
				HorizontalAlignment = HorizontalAlignment.Left,
			};

			// Add pointer events to the grid for clicking anywhere on the progress bar
			var progressContainer = new Grid
			{
				Height = 20,
				Background = new SolidColorBrush(Windows.UI.Color.FromArgb(1, 0, 0, 0))
			};
			progressContainer.PointerPressed += ProgressContainer_PointerPressed;
			progressContainer.Children.Add(progressBar);

			// Position the thumb in a Canvas above the progress bar
			var canvas = new Canvas
			{
				Height = 20
			};
			canvas.Children.Add(seekThumb);

			timeDisplay = new TextBlock
			{
				HorizontalAlignment = HorizontalAlignment.Right,
				Text = "00:00 / 00:00",
				Margin = new Thickness(0, 0, 0, 8)
			};

			// Arrange all elements
			var overlayGrid = new Grid();
			overlayGrid.Children.Add(progressContainer);
			overlayGrid.Children.Add(canvas);

			Grid.SetRow(overlayGrid, 0);
			Grid.SetRow(timeDisplay, 1);

			mainGrid.Children.Add(overlayGrid);
			mainGrid.Children.Add(timeDisplay);

			Content = mainGrid;

			seekThumb.DragStarted += SeekThumb_DragStarted;
			seekThumb.DragDelta += SeekThumb_DragDelta;
			seekThumb.DragCompleted += SeekThumb_DragCompleted;
		}

		private void ProgressContainer_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			var point = e.GetCurrentPoint((UIElement)sender);
			var position = point.Position.X;
			var total = ((Grid)sender).ActualWidth;
			var percentage = (position / total) * 100.0;

			isDragging = true;
			dragValue = percentage;
			UpdateThumbPosition(percentage);

			PositionChanged?.Invoke(this, percentage / 100.0);
			isDragging = false;
		}

		private void SeekThumb_DragStarted(object sender, DragStartedEventArgs e)
		{
			isDragging = true;
			dragValue = progressBar.Value;
		}

		private void SeekThumb_DragDelta(object sender, DragDeltaEventArgs e)
		{
			if (isDragging)
			{
				// Calculate the relative horizontal movement
				double horizontalChange = e.HorizontalChange;
				double progressBarWidth = progressBar.ActualWidth;

				// Calculate the new value as a percentage of the progress bar width
				double changePercentage = (horizontalChange / progressBarWidth) * 100.0;
				double newValue = dragValue + changePercentage;

				// Clamp between 0 and 100
				newValue = Math.Max(0, Math.Min(100, newValue));
				dragValue = newValue;

				// Update UI
				UpdateThumbPosition(newValue);
			}
		}

		private void SeekThumb_DragCompleted(object sender, DragCompletedEventArgs e)
		{
			if (isDragging)
			{
				// Notify about position change
				PositionChanged?.Invoke(this, dragValue / 100.0);
				isDragging = false;
			}
		}

		private void UpdateProgressUI()
		{
			if (TotalDuration.TotalMilliseconds > 0)
			{
				double progress = (CurrentPosition.TotalMilliseconds / TotalDuration.TotalMilliseconds) * 100;
				progressBar.Value = progress;
				UpdateThumbPosition(progress);

				string currentTime = FormatTimeSpan(CurrentPosition);
				string totalTime = FormatTimeSpan(TotalDuration);
				timeDisplay.Text = $"{currentTime} / {totalTime}";
			}
			else
			{
				progressBar.Value = 0;
				UpdateThumbPosition(0);
				timeDisplay.Text = "00:00 / 00:00";
			}
		}

		private void UpdateThumbPosition(double percentage)
		{
			percentage = Math.Max(0, Math.Min(100, percentage));
			double position = (percentage / 100.0) * progressBar.ActualWidth;
			Canvas.SetLeft(seekThumb, position);
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

