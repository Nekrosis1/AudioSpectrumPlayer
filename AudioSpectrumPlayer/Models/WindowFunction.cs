namespace AudioSpectrumPlayer.Models
{
	/// <summary>
	/// The fade-in/fade-out curve multiplied onto a sliding window of samples before the FFT,
	/// to hide the seam between its end and its start ("leakage").
	/// </summary>
	public enum WindowFunction
	{
		/// <summary>Periodic Hann: <c>w[n] = 0.5 − 0.5 × cos(2π n / N)</c>.</summary>
		Hann,
	}
}
