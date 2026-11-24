namespace AudioSpectrumPlayer.Tests.Helpers
{
	internal static class SignalStats
	{
		public static float Peak(float[] samples)
		{
			float peak = 0f;
			foreach (float s in samples)
			{
				peak = Math.Max(peak, Math.Abs(s));
			}
			return peak;
		}

		public static double Rms(float[] samples)
		{
			double sumOfSquares = 0;
			foreach (float s in samples)
			{
				sumOfSquares += (double)s * s;
			}
			return Math.Sqrt(sumOfSquares / samples.Length);
		}

		/// <summary>
		/// Counts changes of <c>sample &gt;= 0</c> between neighbouring samples.
		/// Not <c>a * b &lt; 0</c>: that misses every crossing that lands exactly on a 0 sample,
		/// which is common in the integer test files.
		/// </summary>
		public static int ZeroCrossings(float[] samples)
		{
			int crossings = 0;
			for (int i = 1; i < samples.Length; i++)
			{
				if ((samples[i - 1] >= 0) != (samples[i] >= 0))
				{
					crossings++;
				}
			}
			return crossings;
		}
	}
}
