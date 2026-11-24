namespace AudioSpectrumPlayer.Models
{
	/// <summary>
	/// Raw analysis result: one level per frequency bin, in dBFS (0 dB = a full-scale sine).
	/// <see cref="MagnitudesDb"/> holds <c>FftSize / 2 + 1</c> bins, from DC (bin 0) to Nyquist.
	/// </summary>
	public record Spectrum(float[] MagnitudesDb, int SampleRate, int FftSize)
	{
		/// <summary>Frequency (Hz) that bin <paramref name="k"/> represents.</summary>
		public double FrequencyOf(int k) => (double)k * SampleRate / FftSize;
	}
}
