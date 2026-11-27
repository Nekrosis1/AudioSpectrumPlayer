using AudioSpectrumPlayer.Helpers;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using System;
using System.Globalization;
using System.Linq;

namespace AudioSpectrumPlayer.Controls
{
	/// <summary>Background, grid lines and axis labels of the spectrum.</summary>
	public class SpectrumGridControl : Control
	{
		private static readonly double[] GridFrequenciesHz =
		[
			10, 20, 30, 40, 50, 60, 70, 80, 90,
			100, 200, 300, 400, 500, 600, 700, 800, 900,
			1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 9000,
			10000, 20000,
		];

		private static readonly double[] DecadeFrequenciesHz = [100, 1000, 10000];

		private static readonly double[] LabelledFrequenciesHz =
		[
			20, 30, 40, 60, 80,
			100, 200, 300, 400, 600, 800,
			1000, 2000, 3000, 4000, 6000, 8000,
			10000, 20000,
		];

		private static readonly double[] GridLevelsDb = [-6, -12, -18, -24, -30, -36, -42, -48, -54, -60, -66];

		// The Color swatch from VS Code is lying. This is AARRBBGG, not RRBBGGAA. Don't get bamboozeled
		private static readonly IImmutableSolidColorBrush BackgroundBrush = new ImmutableSolidColorBrush(Color.Parse("#16171b"));
		private static readonly IImmutableSolidColorBrush LineBrush = new ImmutableSolidColorBrush(Color.Parse("#20FFFFFF"));
		private static readonly IImmutableSolidColorBrush DecadeLineBrush = new ImmutableSolidColorBrush(Color.Parse("#50FFFFFF"));

		private const double LabelFontSize = 11;
		private const double LabelGap = 2;

		public static readonly StyledProperty<FontFamily> FontFamilyProperty =
			TextElement.FontFamilyProperty.AddOwner<SpectrumGridControl>();

		static SpectrumGridControl()
		{
			ClipToBoundsProperty.OverrideDefaultValue<SpectrumGridControl>(true);
			AffectsRender<SpectrumGridControl>(FontFamilyProperty);
		}

		/// <summary>Label font; inherited, so the app's default font unless set.</summary>
		public FontFamily FontFamily
		{
			get => GetValue(FontFamilyProperty);
			set => SetValue(FontFamilyProperty, value);
		}

		private TopLevel? _topLevel;

		protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
		{
			base.OnAttachedToVisualTree(e);
			_topLevel = TopLevel.GetTopLevel(this);
			_topLevel?.ScalingChanged += OnScalingChanged;
		}

		protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
		{
			base.OnDetachedFromVisualTree(e);
			_topLevel?.ScalingChanged -= OnScalingChanged;
			_topLevel = null;
		}

		// The snapped line positions depend on the scaling, e.g. after moving to another monitor.
		private void OnScalingChanged(object? sender, EventArgs e)
		{
			InvalidateVisual();
		}

		public override void Render(DrawingContext context)
		{
			double width = Bounds.Width;
			double height = Bounds.Height;

			context.FillRectangle(BackgroundBrush, new Rect(Bounds.Size));

			if (width <= 0 || height <= 0)
			{
				return;
			}

			double scaling = VisualRoot?.RenderScaling ?? 1.0;
			double pixel = 1.0 / scaling;
			ImmutablePen linePen = new(LineBrush, pixel);
			ImmutablePen decadeLinePen = new(DecadeLineBrush, pixel);
			Typeface typeface = new(FontFamily);

			foreach (double hz in GridFrequenciesHz)
			{
				double mappedX = SpectrumAxisMapping.FrequencyToX(hz, width);
				double x = SnapToPixelCenter(mappedX, scaling);
				ImmutablePen pen = DecadeFrequenciesHz.Contains(hz) ? decadeLinePen : linePen;
				context.DrawLine(pen, new Point(x, 0), new Point(x, height));

				if (LabelledFrequenciesHz.Contains(hz))
				{
					FormattedText label = CreateLabel(FormatFrequency(hz), typeface);
					context.DrawText(label, new Point(x + LabelGap, height - label.Height - LabelGap));
				}
			}

			foreach (double db in GridLevelsDb)
			{
				double y = SnapToPixelCenter(SpectrumAxisMapping.DbToY(db, height), scaling);
				context.DrawLine(linePen, new Point(0, y), new Point(width, y));

				FormattedText label = CreateLabel(db.ToString(CultureInfo.InvariantCulture), typeface);
				context.DrawText(label, new Point(LabelGap, y - label.Height));
			}
		}

		/// <summary>
		/// Moves a position to the centre of the device pixel it falls in, so a one-device-pixel
		/// line covers exactly that pixel instead of being smeared across two.
		/// </summary>
		private static double SnapToPixelCenter(double position, double scaling)
		{
			return (Math.Floor(position * scaling) + 0.5) / scaling;
		}

		private static string FormatFrequency(double hz)
		{
			return hz >= 1000
				? (hz / 1000).ToString(CultureInfo.InvariantCulture) + "k"
				: hz.ToString(CultureInfo.InvariantCulture);
		}

		private static FormattedText CreateLabel(string text, Typeface typeface)
		{
			return new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
				typeface, LabelFontSize, LineBrush);
		}
	}
}
