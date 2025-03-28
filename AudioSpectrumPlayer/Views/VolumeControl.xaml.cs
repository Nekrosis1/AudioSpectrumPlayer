using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Serilog;
using System;
using Windows.Foundation;

namespace AudioSpectrumPlayer.Views
{
	public sealed partial class VolumeControl : UserControl
	{
		private bool _isDragging = false;
		private double _currentVolume = 1.0;

		public event EventHandler<double>? VolumeChanged;

		public static readonly DependencyProperty VolumeProperty =
			DependencyProperty.Register(
				nameof(Volume),
				typeof(double),
				typeof(VolumeControl),
				new PropertyMetadata(1.0, OnVolumeChanged));

		public double Volume
		{
			get => (double)GetValue(VolumeProperty);
			set => SetValue(VolumeProperty, value);
		}

		private static void OnVolumeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is VolumeControl control)
			{
				control._currentVolume = (double)e.NewValue;
				control.UpdateVolumeUI();
			}
		}

		public VolumeControl()
		{
			this.InitializeComponent();

			_currentVolume = 1.0;
			UpdateVolumeUI();

			// Make sure the canvas and paths resize properly
			SizeChanged += VolumeControl_SizeChanged;
		}

		private void VolumeControl_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			UpdatePathGeometries();
			UpdateVolumeUI();
		}

		private void UpdatePathGeometries()
		{
			double width = volumeCanvas.ActualWidth;
			double height = volumeCanvas.ActualHeight;

			if (width <= 0 || height <= 0)
			{
				width = 180;
				height = 32;
			}

			volumeBackground.Data = new PathGeometry()
			{
				Figures =
				{
					new PathFigure()
					{
						StartPoint = new Point(0, height - 2),
						Segments =
						{
							new LineSegment() { Point = new Point(width, 0) },
							new LineSegment() { Point = new Point(width, height - 2) }
						},
						IsClosed = true
					}
				}
			};

			hitArea.Width = width;
			hitArea.Height = height;
		}

		private void UpdateVolumeUI()
		{
			double width = volumeCanvas.ActualWidth;
			double height = volumeCanvas.ActualHeight;

			if (width <= 0 || height <= 0)
			{
				width = 180;
				height = 32;
			}

			double volumeX = width * _currentVolume;
			double volumeY = height - (height * _currentVolume);

			PathGeometry pathGeometry = new PathGeometry();
			PathFigure figure = new PathFigure
			{
				StartPoint = new Point(0, height - 2),
				IsClosed = true
			};

			figure.Segments.Add(new LineSegment { Point = new Point(volumeX, volumeY) });
			figure.Segments.Add(new LineSegment { Point = new Point(volumeX, height - 2) });

			pathGeometry.Figures.Add(figure);
			volumeIndicator.Data = pathGeometry;

			int percentage = (int)(_currentVolume * 100);
			volumePercentage.Text = $"{percentage}%";
		}

		private void VolumeCanvas_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			_isDragging = true;
			//volumeCanvas.CapturePointer(e.Pointer);
			UpdateVolumeFromPointerPosition(e.GetCurrentPoint(volumeCanvas).Position);
		}

		private void VolumeCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
		{
			if (_isDragging)
			{
				{
					Point position = e.GetCurrentPoint(volumeCanvas).Position;

					//Check if pointer is outside the control bounds
					if (position.X < 0 || position.X > volumeCanvas.ActualWidth ||
						position.Y < 0 || position.Y > volumeCanvas.ActualHeight)
					{
						// Clamp the value to valid range
						double clampedX = Math.Clamp(position.X, 0, volumeCanvas.ActualWidth);
						double newVolume = clampedX / volumeCanvas.ActualWidth;

						// Update volume with clamped value
						_currentVolume = Math.Clamp(newVolume, 0, 1);
						Volume = _currentVolume;
						UpdateVolumeUI();
						VolumeChanged?.Invoke(this, _currentVolume);

						// End the dragging operation
						_isDragging = false;
						volumeCanvas.ReleasePointerCapture(e.Pointer);
						Log.Debug("Volume update Clamped PointerMoved");
					}
					else
					{
						// Normal update within bounds
						UpdateVolumeFromPointerPosition(position);
						Log.Warning("Volume update PointerMoved");
					}
				}
			}
		}

		private void VolumeCanvas_PointerReleased(object sender, PointerRoutedEventArgs e)
		{
			if (_isDragging)
			{
				_isDragging = false;
				volumeCanvas.ReleasePointerCapture(e.Pointer);

				// Get final position and update volume
				Point position = e.GetCurrentPoint(volumeCanvas).Position;
				double clampedX = Math.Clamp(position.X, 0, volumeCanvas.ActualWidth);
				double newVolume = clampedX / volumeCanvas.ActualWidth;

				_currentVolume = Math.Clamp(newVolume, 0, 1);
				Volume = _currentVolume;
				UpdateVolumeUI();
				VolumeChanged?.Invoke(this, _currentVolume);
				Log.Debug("Volume update Clamped PointerReleased");
			}
		}

		private void VolumeCanvas_PointerExited(object sender, PointerRoutedEventArgs e)
		{
			if (_isDragging)
			{
				_isDragging = false;
				volumeCanvas.ReleasePointerCapture(e.Pointer);
				Log.Debug("Volume update PointerExited");
			}
		}

		private void UpdateVolumeFromPointerPosition(Point position)
		{
			double width = volumeCanvas.ActualWidth;
			if (width <= 0) width = 180; // Default if not yet rendered

			// Constrain to valid range
			double newVolume = Math.Clamp(position.X / width, 0, 1);

			// Only update if volume has changed
			if (Math.Abs(_currentVolume - newVolume) > 0.001)
			{
				_currentVolume = newVolume;
				Volume = newVolume;
				UpdateVolumeUI();
				VolumeChanged?.Invoke(this, newVolume);
			}
		}
	}
}