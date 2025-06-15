using AudioSpectrumPlayer.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.ComponentModel;

namespace AudioSpectrumPlayer.Views
{
	public sealed partial class VolumeControl : UserControl
	{
		private bool _isDragging = false;
		private MainWindowViewModel? _currentViewModel;
		private const double TriangleWidth = 150;
		private const double TriangleHeight = 30;
		public MainWindowViewModel? ViewModel => DataContext as MainWindowViewModel;

		public VolumeControl()
		{
			InitializeComponent();
			DataContextChanged += VolumeControl_DataContextChanged;

			volumeBackground.Points.Clear();
			volumeBackground.Points.Add(new Windows.Foundation.Point(0, TriangleHeight));
			volumeBackground.Points.Add(new Windows.Foundation.Point(TriangleWidth, 0));
			volumeBackground.Points.Add(new Windows.Foundation.Point(TriangleWidth, TriangleHeight));
		}

		private void VolumeControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
		{
			if (_currentViewModel != null)
			{
				_currentViewModel.PropertyChanged -= ViewModel_PropertyChanged;
			}
			if (args.NewValue is MainWindowViewModel viewModel)
			{
				_currentViewModel = viewModel;
				viewModel.PropertyChanged += ViewModel_PropertyChanged;
				UpdateVolumeIndicator();
			}
			else
			{
				_currentViewModel = null;
			}
		}

		private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(MainWindowViewModel.Volume))
			{
				UpdateVolumeIndicator();
			}
		}

		private void UpdateVolumeIndicator()
		{
			if (ViewModel == null) return;

			double volume = ViewModel.Volume;
			double volumeWidth = TriangleWidth * volume;
			double volumeHeight = TriangleHeight * volume;

			volumeIndicator.Points.Clear();
			volumeIndicator.Points.Add(new Windows.Foundation.Point(0, TriangleHeight));
			volumeIndicator.Points.Add(new Windows.Foundation.Point(volumeWidth, TriangleHeight - volumeHeight));
			volumeIndicator.Points.Add(new Windows.Foundation.Point(volumeWidth, TriangleHeight));
		}


		private void VolumeCanvas_PointerPressed(object sender, PointerRoutedEventArgs e)
		{
			_isDragging = true;
			volumeCanvas.CapturePointer(e.Pointer);
			UpdateVolumeFromPosition(e.GetCurrentPoint(volumeCanvas).Position.X);
		}

		private void VolumeCanvas_PointerMoved(object sender, PointerRoutedEventArgs e)
		{
			if (_isDragging)
			{
				UpdateVolumeFromPosition(e.GetCurrentPoint(volumeCanvas).Position.X);
			}
		}

		private void VolumeCanvas_PointerReleased(object sender, PointerRoutedEventArgs e)
		{
			if (_isDragging)
			{
				_isDragging = false;
				volumeCanvas.ReleasePointerCapture(e.Pointer);
				UpdateVolumeFromPosition(e.GetCurrentPoint(volumeCanvas).Position.X);
			}
		}

		private void VolumeCanvas_PointerEntered(object sender, PointerRoutedEventArgs e)
		{
			// If entering with button pressed, start a new drag operation
			if (e.GetCurrentPoint(volumeCanvas).Properties.IsLeftButtonPressed)
			{
				_isDragging = true;
				volumeCanvas.CapturePointer(e.Pointer);
				UpdateVolumeFromPosition(e.GetCurrentPoint(volumeCanvas).Position.X);
			}
		}

		private void VolumeCanvas_PointerExited(object sender, PointerRoutedEventArgs e)
		{
			if (_isDragging)
			{
				_isDragging = false;
				volumeCanvas.ReleasePointerCapture(e.Pointer);
			}
		}

		private void UpdateVolumeFromPosition(double x)
		{
			if (ViewModel == null) return;

			double newVolume = Math.Clamp(x / TriangleWidth, 0.0, 1.0);
			ViewModel.Volume = newVolume;
		}

		public string GetVolumeText(double volume)
		{
			return $"{(int)(volume * 100)}%";
		}
	}
}