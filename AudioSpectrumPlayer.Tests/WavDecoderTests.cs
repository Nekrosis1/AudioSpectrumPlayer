using AudioSpectrumPlayer.Models;
using AudioSpectrumPlayer.Services;
using AudioSpectrumPlayer.Tests.Helpers;

namespace AudioSpectrumPlayer.Tests
{
	public class WavDecoderTests
	{
		private const double SineAmplitude = 0.5;
		private const double SineRms = 0.5 / 1.4142135623730951; //Math.Sqrt(2)
		private const double LevelTolerance = 0.01;
		private const int ExpectedCrossingsPerSecond = 2000;
		private const int CrossingTolerance = 2;

		private static PcmData Decode(string fileName)
		{
			return new WavDecoder().Decode(TestFiles.Get(fileName));
		}

		private static float[] WholeFileMono(PcmData data)
		{
			int frames = data.InterleavedSamples.Length / data.ChannelCount;
			return new PcmBuffer(data).GetSamples(TimeSpan.Zero, frames).Samples;
		}

		[Theory]
		[InlineData("sine1k_s16_mono.wav", 44100, 1, 1)]
		[InlineData("sine1k_f32_mono.wav", 44100, 1, 1)]
		[InlineData("sine1k_u8_mono.wav", 44100, 1, 1)]
		[InlineData("sine1k_s24_mono.wav", 44100, 1, 1)]
		[InlineData("sine1k_s32_mono.wav", 44100, 1, 1)]
		[InlineData("sine1k_f64_mono.wav", 44100, 1, 1)]
		[InlineData("sine1k_s16_stereo_L.wav", 44100, 2, 1)]
		[InlineData("sine1k_f32_stereo_L.wav", 44100, 2, 1)]
		[InlineData("sine1k_s16_6ch_L.wav", 44100, 6, 1)]
		[InlineData("silence_s16_mono.wav", 44100, 1, 1)]
		[InlineData("sine1k_s16_mono_48k.wav", 48000, 1, 1)]
		[InlineData("sine1k_s16_mono_3s.wav", 44100, 1, 3)]
		[InlineData("sine1k_s16_mono_48k_3s.wav", 48000, 1, 3)]
		public void SupportedFile_Decodes_WithExpectedFormatAndDuration(string fileName, int sampleRate, int channels, int seconds)
		{
			PcmData data = Decode(fileName);
			PcmBuffer buffer = new(data);

			Assert.Equal(sampleRate, data.SampleRate);
			Assert.Equal(channels, data.ChannelCount);
			Assert.Equal(sampleRate, buffer.SampleRate);

			TimeSpan oneSample = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / sampleRate + 1);
			TimeSpan deviation = (buffer.Duration - TimeSpan.FromSeconds(seconds)).Duration();
			Assert.True(deviation <= oneSample, $"Duration {buffer.Duration} is more than one sample off {seconds} s.");
		}

		[Theory]
		[InlineData("sine1k_s16_mono.wav")]
		[InlineData("sine1k_f32_mono.wav")]
		[InlineData("sine1k_u8_mono.wav")]
		[InlineData("sine1k_s24_mono.wav")]
		[InlineData("sine1k_s32_mono.wav")]
		[InlineData("sine1k_f64_mono.wav")]
		public void MonoSine_HasExpectedPeakRmsAndZeroCrossings(string fileName)
		{
			float[] samples = WholeFileMono(Decode(fileName));

			Assert.Equal(SineAmplitude, SignalStats.Peak(samples), LevelTolerance);
			Assert.Equal(SineRms, SignalStats.Rms(samples), LevelTolerance);
			Assert.InRange(SignalStats.ZeroCrossings(samples),
				ExpectedCrossingsPerSecond - CrossingTolerance, ExpectedCrossingsPerSecond + CrossingTolerance);
		}

		[Theory]
		[InlineData("sine1k_s16_mono.wav", 1.0 / 32768)]
		[InlineData("sine1k_u8_mono.wav", 1.0 / 128)]
		[InlineData("sine1k_s24_mono.wav", 1e-6)]
		[InlineData("sine1k_s32_mono.wav", 1e-6)]
		[InlineData("sine1k_f64_mono.wav", 1e-6)]
		public void MonoFormat_MatchesFloatReference_WithinQuantizationStep(string fileName, double tolerance)
		{
			float[] reference = Decode("sine1k_f32_mono.wav").InterleavedSamples;
			float[] samples = Decode(fileName).InterleavedSamples;

			Assert.Equal(reference.Length, samples.Length);
			for (int i = 0; i < reference.Length; i++)
			{
				double difference = Math.Abs(samples[i] - reference[i]);
				Assert.True(difference <= tolerance,
					$"Sample {i}: {samples[i]} vs reference {reference[i]}, difference {difference} > {tolerance}.");
			}
		}

		[Theory]
		[InlineData("sine1k_s16_stereo_L.wav")]
		[InlineData("sine1k_f32_stereo_L.wav")]
		public void StereoLeftOnly_MonoMix_HasHalfPeak(string fileName)
		{
			float[] mono = WholeFileMono(Decode(fileName));

			Assert.Equal(SineAmplitude / 2, SignalStats.Peak(mono), LevelTolerance);
		}

		[Fact]
		public void SixChannelsFirstOnly_MonoMix_HasSixthPeak()
		{
			float[] mono = WholeFileMono(Decode("sine1k_s16_6ch_L.wav"));

			Assert.Equal(SineAmplitude / 6, SignalStats.Peak(mono), LevelTolerance);
		}

		[Fact]
		public void Silence_AllSamplesExactlyZero()
		{
			float[] samples = Decode("silence_s16_mono.wav").InterleavedSamples;

			Assert.NotEmpty(samples);
			Assert.All(samples, s => Assert.Equal(0f, s));
		}

		[Fact]
		public void Sine48k_HasSampleRate48000_AndExpectedZeroCrossings()
		{
			PcmData data = Decode("sine1k_s16_mono_48k.wav");

			Assert.Equal(48000, data.SampleRate);
			Assert.InRange(SignalStats.ZeroCrossings(WholeFileMono(data)),
				ExpectedCrossingsPerSecond - CrossingTolerance, ExpectedCrossingsPerSecond + CrossingTolerance);
		}

		[Fact]
		public void Adpcm_ThrowsNotSupported_NamingFormatTag()
		{
			string path = TestFiles.Get("unsupported_adpcm_mono.wav");

			NotSupportedException exception = Assert.Throws<NotSupportedException>(() => new WavDecoder().Decode(path));
			Assert.Contains("0x0002", exception.Message);
		}


// 		// hack: Look at PCM (as dev).
// 		  [Fact]
//   public void Peek()
//   {
//       PcmData pcm = new WavDecoder().Decode(TestFiles.Get("ramp_n10_f32_mono.wav"));
//       float[] s = pcm.InterleavedSamples;
//       TestContext.Current.TestOutputHelper?.WriteLine(
//           $"{s.Length} samples, {pcm.SampleRate} Hz, {pcm.ChannelCount} ch: " + string.Join(" ", s.Take(30)));
// 		  Console.WriteLine("Are we ramping?");
//   }


	}
}
