using AudioSpectrumPlayer.Models;
using AudioSpectrumPlayer.Services;
using AudioSpectrumPlayer.Tests.Helpers;

namespace AudioSpectrumPlayer.Tests
{
	public class PcmBufferTests
	{
		private static PcmData DecodeMono44k()
		{
			return new WavDecoder().Decode(TestFiles.Get("sine1k_s16_mono.wav"));
		}

		[Fact]
		public void SliceInTheMiddle_ReturnsMatchingSamples()
		{
			PcmData data = DecodeMono44k();
			PcmBuffer buffer = new(data);
			TimeSpan time = TimeSpan.FromSeconds(0.5);

			PcmBlock block = buffer.GetSamples(time, 100);

			Assert.Equal(data.InterleavedSamples[22050..22150], block.Samples);
			Assert.Equal(44100, block.SampleRate);
			Assert.Equal(time, block.StartTime);
		}

		[Fact]
		public void SlicePastTheEnd_IsZeroPaddedAfterTheLastSample()
		{
			PcmData data = DecodeMono44k();
			PcmBuffer buffer = new(data);

			// 0.99 s = sample 43659; 441 samples remain before the end at 44100.
			PcmBlock block = buffer.GetSamples(TimeSpan.FromMilliseconds(990), 500);

			Assert.Equal(500, block.Samples.Length);
			Assert.Equal(data.InterleavedSamples[43659..44100], block.Samples[..441]);
			Assert.All(block.Samples[441..], s => Assert.Equal(0f, s));
		}

		[Fact]
		public void SliceBeforeZero_IsZeroPaddedBeforeTheFirstSample()
		{
			PcmData data = DecodeMono44k();
			PcmBuffer buffer = new(data);
			TimeSpan time = TimeSpan.FromMilliseconds(-10);

			// -10 ms = 441 samples before the start.
			PcmBlock block = buffer.GetSamples(time, 1000);

			Assert.All(block.Samples[..441], s => Assert.Equal(0f, s));
			Assert.Equal(data.InterleavedSamples[..559], block.Samples[441..]);
			Assert.Equal(time, block.StartTime);
		}

		[Fact]
		public void WholeFile_ReturnsEverySample_WithoutPadding()
		{
			PcmData data = DecodeMono44k();
			PcmBuffer buffer = new(data);

			PcmBlock block = buffer.GetSamples(TimeSpan.Zero, data.InterleavedSamples.Length);

			Assert.Equal(data.InterleavedSamples, block.Samples);
		}

		[Theory]
		[InlineData("sine1k_s16_mono_3s.wav", 44100)]
		[InlineData("sine1k_s16_mono_48k_3s.wav", 48000)]
		public void OneSecond_StartsAtSampleRateIndex(string fileName, int expectedIndex)
		{
			PcmData data = new WavDecoder().Decode(TestFiles.Get(fileName));
			PcmBuffer buffer = new(data);

			PcmBlock block = buffer.GetSamples(TimeSpan.FromSeconds(1), 256);

			Assert.Equal(data.InterleavedSamples[expectedIndex..(expectedIndex + 256)], block.Samples);
		}

		[Fact]
		public void Stereo_MixesToAverageOfChannels()
		{
			PcmData data = new([0.5f, -0.25f, 1f, 1f, -1f, 0f], 10, 2);

			PcmBlock block = new PcmBuffer(data).GetSamples(TimeSpan.Zero, 3);

			Assert.Equal([0.125f, 1f, -0.5f], block.Samples);
		}

		[Fact]
		public void ThreeChannels_MixesToAverageOfChannels()
		{
			PcmData data = new([0.3f, 0.6f, 0.9f, -0.3f, 0f, 0f], 10, 3);

			PcmBlock block = new PcmBuffer(data).GetSamples(TimeSpan.Zero, 2);

			Assert.Equal(0.6f, block.Samples[0], 1e-6f);
			Assert.Equal(-0.1f, block.Samples[1], 1e-6f);
		}

		[Fact]
		public void TimeBetweenSamples_FloorsToTheEarlierSample()
		{
			// 10 Hz: one sample per 100 ms. 150 ms lies between samples 1 and 2.
			PcmData data = new([1f, 2f, 3f, 4f], 10, 1);

			PcmBlock block = new PcmBuffer(data).GetSamples(TimeSpan.FromMilliseconds(150), 2);

			Assert.Equal([2f, 3f], block.Samples);
		}

		[Fact]
		public void NegativeTimeBetweenSamples_FloorsTowardNegativeInfinity()
		{
			// -1 tick lies between samples -1 and 0; truncation would wrongly give 0.
			PcmData data = new([1f, 2f, 3f], 10, 1);

			PcmBlock block = new PcmBuffer(data).GetSamples(TimeSpan.FromTicks(-1), 3);

			Assert.Equal([0f, 1f, 2f], block.Samples);
		}

		[Fact]
		public void RangeEntirelyOutsideTheData_IsAllZeros()
		{
			PcmBuffer buffer = new(new PcmData([1f, 2f, 3f], 10, 1));

			Assert.Equal([0f, 0f], buffer.GetSamples(TimeSpan.FromSeconds(-5), 2).Samples);
			Assert.Equal([0f, 0f], buffer.GetSamples(TimeSpan.FromSeconds(5), 2).Samples);
			Assert.Equal([0f, 0f], buffer.GetSamples(TimeSpan.MaxValue, 2).Samples);
			Assert.Equal([0f, 0f], buffer.GetSamples(TimeSpan.MinValue, 2).Samples);
		}

		[Fact]
		public void Duration_IsFrameCountOverSampleRate()
		{
			PcmBuffer buffer = new(new PcmData(new float[2 * 25], 10, 2));

			Assert.Equal(TimeSpan.FromSeconds(2.5), buffer.Duration);
		}

		[Theory]
		[InlineData(0)]
		[InlineData(-1)]
		public void NonPositiveCount_Throws(int count)
		{
			PcmBuffer buffer = new(new PcmData([1f], 10, 1));

			Assert.Throws<ArgumentOutOfRangeException>(() => buffer.GetSamples(TimeSpan.Zero, count));
		}

		[Fact]
		public void SampleCountNotMultipleOfChannels_Throws()
		{
			Assert.Throws<ArgumentException>(() => new PcmBuffer(new PcmData([1f, 2f, 3f], 10, 2)));
		}
	}
}
