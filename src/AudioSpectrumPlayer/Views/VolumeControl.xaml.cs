using AudioSpectrumPlayer.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Serilog;
using System;
using System.ComponentModel;
using Windows.Foundation;

namespace AudioSpectrumPlayer.Views
{
	public sealed partial class VolumeControl : UserControl
	{
		private bool _isDragging = false;
		private AudioPlayerViewModel? _currentViewModel; // Track current ViewModel

		public VolumeControl()
		{
			this.InitializeComponent();
			SizeChanged += VolumeControl_SizeChanged;
			DataContextChanged += VolumeControl_DataContextChanged;
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

		private void VolumeControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
		{
			// Unsubscribe from the previous ViewModel if it exists
			if (_currentViewModel != null)
			{
				_currentViewModel.PropertyChanged -= ViewModel_PropertyChanged;
			}

			// Subscribe to the new ViewModel
			if (args.NewValue is AudioPlayerViewModel viewModel)
			{
				_currentViewModel = viewModel;
				viewModel.PropertyChanged += ViewModel_PropertyChanged;
				UpdateVolumeUI(); // Initial update
			}
			else
			{
				_currentViewModel = null;
			}
		}

		private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(AudioPlayerViewModel.Volume))
			{
				UpdateVolumeUI();
			}
		}


		private void UpdateVolumeUI()
		{
			double currentVolume = GetCurrentVolume();

			double width = volumeCanvas.ActualWidth;
			double height = volumeCanvas.ActualHeight;

			if (width <= 0 || height <= 0)
			{
				width = 180;
				height = 32;
			}

			double volumeX = width * currentVolume;
			double volumeY = height - (height * currentVolume);

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

			int percentage = (int)(currentVolume * 100);
			volumePercentage.Text = $"{percentage}%";
		}

		private void VolumeCanvas_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			_isDragging = true;
			volumeCanvas.CapturePointer(e.Pointer);
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

						SetVolumeInViewModel(Math.Clamp(newVolume, 0, 1));

						// End the dragging operation
						_isDragging = false;
						volumeCanvas.ReleasePointerCapture(e.Pointer);
						Log.Debug("Volume update Clamped PointerMoved");
					}
					else
					{
						// Normal update within bounds
						UpdateVolumeFromPointerPosition(position);
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

				SetVolumeInViewModel(Math.Clamp(newVolume, 0, 1));
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
			double currentVolume = GetCurrentVolume();
			if (Math.Abs(currentVolume - newVolume) > 0.001)
			{
				SetVolumeInViewModel(newVolume);
			}
		}

		private double GetCurrentVolume()
		{
			if (DataContext is AudioPlayerViewModel viewModel)
			{
				return viewModel.Volume;
			}
			return 1.0; // Default value
		}

		private void SetVolumeInViewModel(double volume)
		{
			if (DataContext is AudioPlayerViewModel viewModel)
			{
				viewModel.Volume = volume;
			}
		}
	}
}