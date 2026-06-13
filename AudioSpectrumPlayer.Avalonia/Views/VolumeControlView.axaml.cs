using AudioSpectrumPlayer.Avalonia.ViewModels;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Input;
using System;
using System.ComponentModel;

namespace AudioSpectrumPlayer.Avalonia.Views;

public partial class VolumeControlView : UserControl
{
	private const double TriangleWidth = 150;
	private const double TriangleHeight = 30;

	private bool _isDragging;
	private MainWindowViewModel? _currentViewModel;

	public MainWindowViewModel? ViewModel => DataContext as MainWindowViewModel;

	public VolumeControlView()
	{
		InitializeComponent();

		volumeBackground.Points =
		[
			new Point(0, TriangleHeight),
			new Point(TriangleWidth, 0),
			new Point(TriangleWidth, TriangleHeight),
		];

		DataContextChanged += OnDataContextChanged;
	}

	private void OnDataContextChanged(object? sender, EventArgs e)
	{
		if (_currentViewModel != null)
		{
			_currentViewModel.PropertyChanged -= OnViewModelPropertyChanged;
		}

		if (DataContext is MainWindowViewModel viewModel)
		{
			_currentViewModel = viewModel;
			viewModel.PropertyChanged += OnViewModelPropertyChanged;
			UpdateVolumeIndicator();
		}
		else
		{
			_currentViewModel = null;
		}
	}

	private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
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

		volumeIndicator.Points =
		[
			new Point(0, TriangleHeight),
			new Point(volumeWidth, TriangleHeight - volumeHeight),
			new Point(volumeWidth, TriangleHeight),
		];

		volumePercentage.Text = $"{(int)Math.Round(volume * 100)}%";
	}

	private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
	{
		_isDragging = true;
		e.Pointer.Capture(volumeCanvas);
		UpdateVolumeFromPosition(e.GetPosition(volumeCanvas).X);
	}

	private void OnPointerMoved(object? sender, PointerEventArgs e)
	{
		if (_isDragging)
		{
			UpdateVolumeFromPosition(e.GetPosition(volumeCanvas).X);
		}
	}

	private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
	{
		if (_isDragging)
		{
			_isDragging = false;
			e.Pointer.Capture(null);
			UpdateVolumeFromPosition(e.GetPosition(volumeCanvas).X);
		}
	}

	private void OnPointerEntered(object? sender, PointerEventArgs e)
	{
		if (e.GetCurrentPoint(volumeCanvas).Properties.IsLeftButtonPressed)
		{
			_isDragging = true;
			e.Pointer.Capture(volumeCanvas);
			UpdateVolumeFromPosition(e.GetPosition(volumeCanvas).X);
		}
	}

	private void OnPointerExited(object? sender, PointerEventArgs e)
	{
		if (_isDragging)
		{
			_isDragging = false;
			e.Pointer.Capture(null);
		}
	}

	private void UpdateVolumeFromPosition(double x)
	{
		if (ViewModel == null) return;

		double newVolume = Math.Clamp(x / TriangleWidth, 0.0, 1.0);
		// Setting Volume routes through MainWindowViewModel.OnVolumeChanged,
		// which is the single place volume changes are logged (for both mouse
		// and keyboard), so we don't log again here.
		ViewModel.Volume = newVolume;
	}
}
