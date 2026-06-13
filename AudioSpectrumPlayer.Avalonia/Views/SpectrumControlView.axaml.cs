using AudioSpectrumPlayer.Avalonia.Services;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Threading;
using Serilog;
using System;

namespace AudioSpectrumPlayer.Avalonia.Views;

public partial class SpectrumControlView : UserControl
{
	private float[] _currentSpectrumData = [];
	private double _canvasWidth;
	private double _canvasHeight;

	public SpectrumControlView()
	{
		InitializeComponent();

		SpectrumGenerationService spectrumGenerationService = App.GetRequiredService<SpectrumGenerationService>();
		spectrumGenerationService.SpectrumDataUpdated += OnSpectrumDataUpdated;
	}

	private void OnSpectrumDataUpdated(object? sender, float[] spectrumData)
	{
		Dispatcher.UIThread.Post(() => UpdateSpectrumVisualization(spectrumData));
	}

	private void OnCanvasSizeChanged(object? sender, SizeChangedEventArgs e)
	{
		_canvasWidth = e.NewSize.Width;
		_canvasHeight = e.NewSize.Height;

		if (_currentSpectrumData.Length > 0)
		{
			UpdateSpectrumVisualization(_currentSpectrumData);
		}
	}

	private void UpdateSpectrumVisualization(float[] spectrumData)
	{
		if (_canvasWidth <= 0 || _canvasHeight <= 0 || spectrumData.Length == 0)
			return;

		try
		{
			_currentSpectrumData = spectrumData;

			Point[] linePoints = CreateSpectrumPoints(spectrumData);
			Point[] fillPoints = CreateFillPoints(linePoints);

			Points line = [.. linePoints];
			spectrumLine.Points = line;

			Points fill = [.. fillPoints];
			spectrumFill.Points = fill;
		}
		catch (Exception ex)
		{
			Log.Error(ex, "Error updating spectrum visualization");
		}
	}

	private Point[] CreateSpectrumPoints(float[] spectrumData)
	{
		Point[] points = new Point[spectrumData.Length];

		for (int i = 0; i < spectrumData.Length; i++)
		{
			double x = LogarithmicScale(i, spectrumData.Length, _canvasWidth);
			double y = _canvasHeight - (spectrumData[i] * (_canvasHeight - 20));
			points[i] = new Point(x, Math.Max(0, y));
		}

		return points;
	}

	private Point[] CreateFillPoints(Point[] linePoints)
	{
		Point[] fillPoints = new Point[linePoints.Length + 2];
		fillPoints[0] = new Point(0, _canvasHeight);
		Array.Copy(linePoints, 0, fillPoints, 1, linePoints.Length);
		fillPoints[^1] = new Point(_canvasWidth, _canvasHeight);
		return fillPoints;
	}

	private static double LogarithmicScale(int index, int totalPoints, double width)
	{
		if (totalPoints <= 1) return 0;

		double minFreq = Math.Log10(20);
		double maxFreq = Math.Log10(20000);

		double normalizedIndex = (double)index / (totalPoints - 1);
		double logFreq = minFreq + (normalizedIndex * (maxFreq - minFreq));

		double position = (logFreq - minFreq) / (maxFreq - minFreq);
		return position * width;
	}

	public void ClearVisualization()
	{
		Dispatcher.UIThread.Post(() =>
		{
			spectrumLine.Points = [];
			spectrumFill.Points = [];
		});
	}
}
