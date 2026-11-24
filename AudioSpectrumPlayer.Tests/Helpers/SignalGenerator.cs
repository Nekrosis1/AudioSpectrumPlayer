using AudioSpectrumPlayer.Models;

namespace AudioSpectrumPlayer.Tests.Helpers
{
	internal static class SignalGenerator
	{
		public static float[] Sine(double frequency, double amplitude, int sampleRate, int count)
		{
			float[] samples = new float[count];
			for (int n = 0; n < count; n++)
			{
				samples[n] = (float)(amplitude * Math.Sin(2.0 * Math.PI * frequency * n / sampleRate));
			}
			return samples;
		}

		public static float[] Add(float[] a, float[] b)
		{
			float[] sum = new float[a.Length];
			for (int n = 0; n < a.Length; n++)
			{
				sum[n] = a[n] + b[n];
			}
			return sum;
		}

		public static PcmBlock Block(float[] samples, int sampleRate)
		{
			return new PcmBlock(samples, sampleRate, TimeSpan.Zero);
		}
	}
}
