using AudioSpectrumPlayer.Avalonia.ViewModels;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using System;
using System.ComponentModel;

namespace AudioSpectrumPlayer.Avalonia.Views;

public partial class VolumeControlView : UserControl
{
	private bool _isDragging;
	private MainWindowViewModel? _currentViewModel;

	public MainWindowViewModel? ViewModel => DataContext as MainWindowViewModel;

	public VolumeControlView()
	{
		InitializeComponent();

		// Build the polygons from the Canvas's actual size so the visible
		// triangle always fills its layout slot exactly (no dead space, no
		// mismatch between where you click and the volume it maps to).
		volumeCanvas.SizeChanged += OnCanvasSizeChanged;

		DataContextChanged += OnDataContextChanged;
	}

	private void OnCanvasSizeChanged(object? sender, SizeChangedEventArgs e)
	{
		double width = volumeCanvas.Bounds.Width;
		double height = volumeCanvas.Bounds.Height;

		volumeBackground.Points =
		[
			new Point(0, height),
			new Point(width, 0),
			new Point(width, height),
		];

		UpdateVolumeIndicator();
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
		if (e.PropertyName == nameof(MainWindowViewModel.Volume) ||
			e.PropertyName == nameof(MainWindowViewModel.IsMuted))
		{
			UpdateVolumeIndicator();
		}
	}

	private void UpdateVolumeIndicator()
	{
		if (ViewModel == null) return;

		double width = volumeCanvas.Bounds.Width;
		double height = volumeCanvas.Bounds.Height;

		double volume = ViewModel.Volume;
		double volumeWidth = width * volume;
		double volumeHeight = height * volume;

		volumeIndicator.Points =
		[
			new Point(0, height),
			new Point(volumeWidth, height - volumeHeight),
			new Point(volumeWidth, height),
		];

		// Grey the indicator out while muted; the level (Points/text) is preserved.
		volumeIndicator.Fill = ViewModel.IsMuted ? Brushes.Gray : Brushes.DodgerBlue;
		volumePercentage.Opacity = ViewModel.IsMuted ? 0.5 : 1.0;

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

		// Interacting with the slider unmutes (VLC-style); the level set below applies.
		ViewModel.IsMuted = false;

		double newVolume = Math.Clamp(x / volumeCanvas.Bounds.Width, 0.0, 1.0);
		// Setting Volume routes through MainWindowViewModel.OnVolumeChanged,
		// which is the single place volume changes are logged (for both mouse
		// and keyboard), so we don't log again here.
		ViewModel.Volume = newVolume;
	}
}
