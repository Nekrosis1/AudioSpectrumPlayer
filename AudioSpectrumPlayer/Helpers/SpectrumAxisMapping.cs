using System;

namespace AudioSpectrumPlayer.Helpers
{
	public static class SpectrumAxisMapping
	{
		public const double MinFrequencyHz = 10.0;
		public const double MaxFrequencyHz = 24000.0;

		public const double MaxDb = 0.0;
		public const double MinDb = -72.0;

		private static readonly double LogFrequencySpan = Math.Log10(MaxFrequencyHz / MinFrequencyHz);

		/// <summary>Logarithmic: <see cref="MinFrequencyHz"/> at x = 0, <see cref="MaxFrequencyHz"/> at x = width.</summary>
		public static double FrequencyToX(double hz, double width)
		{
			return width * Math.Log10(hz / MinFrequencyHz) / LogFrequencySpan;
		}

		/// <summary>Linear: <see cref="MaxDb"/> at y = 0 (top), <see cref="MinDb"/> at y = height (bottom).</summary>
		public static double DbToY(double db, double height)
		{
			return height * (MaxDb - db) / (MaxDb - MinDb);
		}
	}
}
