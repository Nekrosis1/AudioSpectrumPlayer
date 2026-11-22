namespace AudioSpectrumPlayer.Models
{
	/// <summary>
	/// A whole decoded file, all channels, as floats in -1.0 .. +1.0.
	/// Samples are interleaved by frame: L R L R … for stereo.
	/// </summary>
	public record PcmData(float[] InterleavedSamples, int SampleRate, int ChannelCount);
}
