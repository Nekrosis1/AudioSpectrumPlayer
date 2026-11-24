using AudioSpectrumPlayer.Models;
using System;

namespace AudioSpectrumPlayer.Interfaces
{
	/// <summary>Turns a slice of mono PCM into a <see cref="Spectrum"/> in dBFS.</summary>
	public interface ISpectrumAnalyzerService
	{
		/// <summary>
		/// Analyzes <paramref name="block"/>. Each call returns a new <see cref="Spectrum.MagnitudesDb"/> array.
		/// </summary>
		/// <exception cref="ArgumentException">
		/// The block's length isn't the analyzer's FFT size, its sample rate isn't positive,
		/// or it contains NaN or infinite samples.
		/// </exception>
		Spectrum Analyze(PcmBlock block);
	}
}
