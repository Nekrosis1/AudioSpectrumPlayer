using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Serilog;
using System;
using Windows.Foundation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace AudioSpectrumPlayer.Views
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class SpectrumControl : Page
	{
		private readonly DispatcherQueue _dispatcherQueue;
		private float[] _currentSpectrumData = Array.Empty<float>();
		private double _canvasWidth;
		private double _canvasHeight;

		// Smoothing for visual appeal
		//private float[] _smoothedSpectrumData = Array.Empty<float>();
		//private const float SmoothingFactor = 0.3f;

		public SpectrumControl()
		{
			InitializeComponent();
			_dispatcherQueue = DispatcherQueue;
			Loaded += OnLoaded;
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			// Subscribe to spectrum updates from the service
			var spectrumGenerationService = App.GetRequiredService<Services.SpectrumGenerationService>();
			spectrumGenerationService.SpectrumDataUpdated += OnSpectrumDataUpdated;
		}

		private void OnSpectrumDataUpdated(object? sender, float[] spectrumData)
		{
			_dispatcherQueue?.TryEnqueue(() =>
			{
				UpdateSpectrumVisualization(spectrumData);
			});
		}

		private void SpectrumCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			_canvasWidth = e.NewSize.Width;
			_canvasHeight = e.NewSize.Height;

			// Update frequency labels positioning
			//Canvas.SetLeft(lowFreqLabel, 5);
			//Canvas.SetTop(lowFreqLabel, _canvasHeight - 20);

			//Canvas.SetLeft(highFreqLabel, _canvasWidth - 40);
			//Canvas.SetTop(highFreqLabel, _canvasHeight - 20);

			// Redraw with current data if available
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

				// Initialize smoothed data array if needed
				//if (_smoothedSpectrumData.Length != spectrumData.Length)
				//{
				//	_smoothedSpectrumData = new float[spectrumData.Length];
				//}

				// Apply smoothing to reduce flickering
				//ApplySmoothing(spectrumData);

				// Create points for the spectrum curve
				var linePoints = CreateSpectrumPoints(spectrumData);
				//var linePoints = CreateSpectrumPoints(_smoothedSpectrumData);
				var fillPoints = CreateFillPoints(linePoints);

				// Update the polyline for the spectrum curve
				spectrumLine.Points.Clear();
				foreach (var point in linePoints)
				{
					spectrumLine.Points.Add(point);
				}

				// Update the polygon for the filled area
				spectrumFill.Points.Clear();
				foreach (var point in fillPoints)
				{
					spectrumFill.Points.Add(point);
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Error updating spectrum visualization");
			}
		}

		//private void ApplySmoothing(float[] newData)
		//{
		//	for (int i = 0; i < newData.Length && i < _smoothedSpectrumData.Length; i++)
		//	{
		//		_smoothedSpectrumData[i] = _smoothedSpectrumData[i] * (1 - SmoothingFactor) +
		//								  newData[i] * SmoothingFactor;
		//	}
		//}

		private Point[] CreateSpectrumPoints(float[] spectrumData)
		{
			var points = new Point[spectrumData.Length];

			for (int i = 0; i < spectrumData.Length; i++)
			{
				// Logarithmic X positioning (20Hz to 20kHz)
				double x = LogarithmicScale(i, spectrumData.Length, _canvasWidth);

				// Linear Y positioning (inverted because Canvas Y=0 is at top)
				double y = _canvasHeight - (spectrumData[i] * (_canvasHeight - 20)); // Leave 20px margin at bottom

				points[i] = new Point(x, Math.Max(0, y));
			}

			return points;
		}

		private Point[] CreateFillPoints(Point[] linePoints)
		{
			// Create points for the filled polygon
			var fillPoints = new Point[linePoints.Length + 2];

			// Start at bottom-left
			fillPoints[0] = new Point(0, _canvasHeight);

			// Copy all line points
			Array.Copy(linePoints, 0, fillPoints, 1, linePoints.Length);

			// End at bottom-right
			fillPoints[fillPoints.Length - 1] = new Point(_canvasWidth, _canvasHeight);

			return fillPoints;
		}

		private static double LogarithmicScale(int index, int totalPoints, double width)
		{
			if (totalPoints <= 1) return 0;

			// Map index to logarithmic frequency scale (20Hz to 20kHz)
			double minFreq = Math.Log10(20);      // log10(20Hz)
			double maxFreq = Math.Log10(20000);   // log10(20kHz)

			double normalizedIndex = (double)index / (totalPoints - 1);
			double logFreq = minFreq + (normalizedIndex * (maxFreq - minFreq));

			// Convert back to linear scale for positioning
			double position = (logFreq - minFreq) / (maxFreq - minFreq);

			return position * width;
		}

		public void ClearVisualization()
		{
			_dispatcherQueue?.TryEnqueue(() =>
			{
				spectrumLine.Points.Clear();
				spectrumFill.Points.Clear();
				//Array.Clear(_smoothedSpectrumData);
			});
		}
	}
}
