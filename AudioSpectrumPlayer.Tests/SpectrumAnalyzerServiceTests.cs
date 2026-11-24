using AudioSpectrumPlayer.Models;
using AudioSpectrumPlayer.Services;
using AudioSpectrumPlayer.Tests.Helpers;

namespace AudioSpectrumPlayer.Tests
{
	public class SpectrumAnalyzerServiceTests
	{
		private const int N = SpectrumAnalyzerService.FftSize;
		private const int BinCount = (N / 2) + 1;

		// A sine between two bins reads up to ~1.42 dB low with Hann (scalloping), never high.
		// Allowed range: true level −1.5 dB … +0.1 dB.
		private const double FullScaleMin = -1.5;
		private const double FullScaleMax = 0.1;
		private const double HalfScaleMin = -7.5;
		private const double HalfScaleMax = -5.9;
		private const double QuarterScaleMin = -13.5;
		private const double QuarterScaleMax = -11.9;

		private static Spectrum Analyze(float[] samples, int sampleRate)
		{
			return new SpectrumAnalyzerService().Analyze(SignalGenerator.Block(samples, sampleRate));
		}

		private static Spectrum AnalyzeFile(string fileName)
		{
			PcmData data = new WavDecoder().Decode(TestFiles.Get(fileName));
			PcmBlock block = new PcmBuffer(data).GetSamples(TimeSpan.FromSeconds(0.25), N);
			return new SpectrumAnalyzerService().Analyze(block);
		}

		/// <summary>Index of the loudest bin in [<paramref name="from"/>, <paramref name="to"/>).</summary>
		private static int HighestBin(Spectrum spectrum, int from = 0, int to = BinCount)
		{
			int highest = from;
			for (int k = from + 1; k < to; k++)
			{
				if (spectrum.MagnitudesDb[k] > spectrum.MagnitudesDb[highest])
				{
					highest = k;
				}
			}
			return highest;
		}

		[Fact]
		public void Sine1k_FullScale_PeaksAtBin186_Near0Db()
		{
			Spectrum spectrum = Analyze(SignalGenerator.Sine(1000, 1.0, 44100, N), 44100);

			int peak = HighestBin(spectrum);

			Assert.Equal(186, peak);
			Assert.InRange((double)spectrum.MagnitudesDb[peak], FullScaleMin, FullScaleMax);
		}

		[Fact]
		public void Sine1k_HalfScale_PeaksAtBin186_NearMinus6Db()
		{
			Spectrum spectrum = Analyze(SignalGenerator.Sine(1000, 0.5, 44100, N), 44100);

			int peak = HighestBin(spectrum);

			Assert.Equal(186, peak);
			Assert.InRange((double)spectrum.MagnitudesDb[peak], HalfScaleMin, HalfScaleMax);
		}

		[Fact]
		public void SineExactlyOnBin200_Reads0Db_AndLeaksOnlyIntoNeighbours()
		{
			double frequency = 200.0 * 44100 / N;
			Spectrum spectrum = Analyze(SignalGenerator.Sine(frequency, 1.0, 44100, N), 44100);

			float peakDb = spectrum.MagnitudesDb[200];

			Assert.Equal(0.0, peakDb, 0.1);
			for (int k = 0; k < BinCount; k++)
			{
				if (Math.Abs(k - 200) >= 3)
				{
					Assert.True(spectrum.MagnitudesDb[k] <= (peakDb - 80),
						$"Bin {k} is {spectrum.MagnitudesDb[k]} dB, less than 80 dB below the peak ({peakDb} dB).");
				}
			}
		}

		[Fact]
		public void TwoSines_1kAnd5k_PeakAtBins186And929_RestBelowMinus60Db()
		{
			float[] samples = SignalGenerator.Add(
				SignalGenerator.Sine(1000, 0.5, 44100, N),
				SignalGenerator.Sine(5000, 0.5, 44100, N));
			Spectrum spectrum = Analyze(samples, 44100);

			// Regions split halfway between the two peaks.
			int lowPeak = HighestBin(spectrum, 0, 557);
			int highPeak = HighestBin(spectrum, 557, BinCount);

			Assert.Equal(186, lowPeak);
			Assert.Equal(929, highPeak);
			Assert.InRange((double)spectrum.MagnitudesDb[lowPeak], HalfScaleMin, HalfScaleMax);
			Assert.InRange((double)spectrum.MagnitudesDb[highPeak], HalfScaleMin, HalfScaleMax);
			for (int k = 0; k < BinCount; k++)
			{
				if ((Math.Abs(k - 186) > 10) && (Math.Abs(k - 929) > 10))
				{
					Assert.True(spectrum.MagnitudesDb[k] < -60,
						$"Bin {k} is {spectrum.MagnitudesDb[k]} dB, expected below -60 dB.");
				}
			}
		}

		[Fact]
		public void Sine1k_At48k_PeaksAtBin171_UsingTheSampleRate()
		{
			Spectrum spectrum = Analyze(SignalGenerator.Sine(1000, 1.0, 48000, N), 48000);

			int peak = HighestBin(spectrum);

			Assert.Equal(171, peak);
			Assert.Equal(1001.95, spectrum.FrequencyOf(171), 0.01);
			Assert.InRange((double)spectrum.MagnitudesDb[peak], FullScaleMin, FullScaleMax);
		}

		[Fact]
		public void Silence_EveryBinIsTheDbFloor()
		{
			Spectrum spectrum = Analyze(new float[N], 44100);

			Assert.All(spectrum.MagnitudesDb, db => Assert.Equal(SpectrumAnalyzerService.DbFloor, db));
		}

		[Theory]
		[InlineData(0.0, 44100)]
		[InlineData(1000.0, 44100)]
		[InlineData(1000.0, 48000)]
		public void AnyInput_Has4097Bins_FromZeroToNyquist(double frequency, int sampleRate)
		{
			Spectrum spectrum = Analyze(SignalGenerator.Sine(frequency, 1.0, sampleRate, N), sampleRate);

			Assert.Equal(4097, spectrum.MagnitudesDb.Length);
			Assert.Equal(0.0, spectrum.FrequencyOf(0));
			Assert.Equal(sampleRate / 2.0, spectrum.FrequencyOf(4096));
			Assert.Equal(sampleRate, spectrum.SampleRate);
			Assert.Equal(N, spectrum.FftSize);
			Assert.All(spectrum.MagnitudesDb, db => Assert.True(float.IsFinite(db)));
		}

		[Fact]
		public void EachCall_ReturnsANewArray_EarlierResultsStayUnchanged()
		{
			SpectrumAnalyzerService analyzer = new();
			Spectrum first = analyzer.Analyze(SignalGenerator.Block(SignalGenerator.Sine(1000, 1.0, 44100, N), 44100));
			float[] firstCopy = (float[])first.MagnitudesDb.Clone();

			Spectrum second = analyzer.Analyze(SignalGenerator.Block(new float[N], 44100));

			Assert.NotSame(first.MagnitudesDb, second.MagnitudesDb);
			Assert.Equal(firstCopy, first.MagnitudesDb);
		}

		[Theory]
		[InlineData(0)]
		[InlineData(N - 1)]
		[InlineData(N + 1)]
		public void WrongBlockLength_Throws(int length)
		{
			SpectrumAnalyzerService analyzer = new();

			Assert.Throws<ArgumentException>(() => analyzer.Analyze(SignalGenerator.Block(new float[length], 44100)));
		}

		[Theory]
		[InlineData(float.NaN)]
		[InlineData(float.PositiveInfinity)]
		[InlineData(float.NegativeInfinity)]
		public void NonFiniteSample_Throws(float badSample)
		{
			float[] samples = SignalGenerator.Sine(1000, 0.5, 44100, N);
			samples[1234] = badSample;

			Assert.Throws<ArgumentException>(() => new SpectrumAnalyzerService().Analyze(SignalGenerator.Block(samples, 44100)));
		}

		[Theory]
		[InlineData(0)]
		[InlineData(-44100)]
		public void NonPositiveSampleRate_Throws(int sampleRate)
		{
			Assert.Throws<ArgumentException>(() => new SpectrumAnalyzerService().Analyze(SignalGenerator.Block(new float[N], sampleRate)));
		}

		// All WAV test files are amplitude 0.5.

		[Theory]
		[InlineData("sine1k_f32_mono.wav", 186, HalfScaleMin, HalfScaleMax)]
		[InlineData("sine1k_s16_mono_48k.wav", 171, HalfScaleMin, HalfScaleMax)]
		// Mono mix of a silent right channel halves the level: 0.25 → −12 dB.
		[InlineData("sine1k_f32_stereo_L.wav", 186, QuarterScaleMin, QuarterScaleMax)]
		public void WavSine_PeaksAtExpectedBinAndLevel(string fileName, int expectedBin, double minDb, double maxDb)
		{
			Spectrum spectrum = AnalyzeFile(fileName);

			int peak = HighestBin(spectrum);

			Assert.Equal(expectedBin, peak);
			Assert.InRange((double)spectrum.MagnitudesDb[peak], minDb, maxDb);
		}

		[Fact]
		public void WavSine48k_FrequencyOfPeakBin_Is1001_95Hz()
		{
			Spectrum spectrum = AnalyzeFile("sine1k_s16_mono_48k.wav");

			Assert.Equal(48000, spectrum.SampleRate);
			Assert.Equal(1001.95, spectrum.FrequencyOf(171), 0.01);
		}

		[Fact]
		public void WavSilence_EveryBinIsTheDbFloor()
		{
			Spectrum spectrum = AnalyzeFile("silence_s16_mono.wav");

			Assert.All(spectrum.MagnitudesDb, db => Assert.Equal(SpectrumAnalyzerService.DbFloor, db));
		}

		// // hack: Look at the FFT array (as dev).
		// [Fact]
		// public void Peek()
		// {
		// 	PcmData pcm = new WavDecoder().Decode("TestData/sine_bin9.wav");
		// 	PcmBlock block = new PcmBuffer(pcm).GetSamples(TimeSpan.FromSeconds(0.5), SpectrumAnalyzerService.FftSize);
		// 	Spectrum spectrum = new SpectrumAnalyzerService().Analyze(block);

		// 	for (int k = 0; k <= 20; k++)
		// 	{
		// 		float db = spectrum.MagnitudesDb[k];
		// 		Console.WriteLine($"{k,3}  {spectrum.FrequencyOf(k),7:F2} Hz  {db,8:F2} dB  {Math.Pow(10, db / 20),6:F3}");
		// 	}
		// }
	}
}
