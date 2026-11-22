using AudioSpectrumPlayer.Models;
using System;

namespace AudioSpectrumPlayer.Interfaces
{
	public interface IPcmSource
	{
		int SampleRate { get; }
		TimeSpan Duration { get; }

		/// <summary>
		/// Returns <paramref name="count"/> mono samples starting at <paramref name="time"/>.
		/// Zero-padded where the range lies before 0 or past the end of the audio.
		/// </summary>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is 0 or negative.</exception>
		PcmBlock GetSamples(TimeSpan time, int count);
	}
}
