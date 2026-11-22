using System;

namespace AudioSpectrumPlayer.Models
{
	/// <summary>
	/// A slice of audio mixed to mono, as floats in -1.0 .. +1.0.
	/// <see cref="StartTime"/> is the time the slice was requested at.
	/// </summary>
	public record PcmBlock(float[] Samples, int SampleRate, TimeSpan StartTime);
}
