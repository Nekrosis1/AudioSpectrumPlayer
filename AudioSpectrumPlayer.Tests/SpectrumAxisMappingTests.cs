using AudioSpectrumPlayer.Helpers;

namespace AudioSpectrumPlayer.Tests
{
	public class SpectrumAxisMappingTests
	{
		private const double Width = 1000;
		private const double Height = 720;
		private const int Precision = 9;

		[Fact]
		public void FrequencyToX_MinFrequency_IsLeftEdge()
		{
			Assert.Equal(0, SpectrumAxisMapping.FrequencyToX(10, Width), Precision);
		}

		[Fact]
		public void FrequencyToX_MaxFrequency_IsRightEdge()
		{
			Assert.Equal(Width, SpectrumAxisMapping.FrequencyToX(24000, Width), Precision);
		}

		[Fact]
		public void FrequencyToX_1k_IsLogPosition()
		{
			double expected = Width * Math.Log10(100) / Math.Log10(2400);

			double x = SpectrumAxisMapping.FrequencyToX(1000, Width);

			Assert.Equal(expected, x, Precision);
			Assert.Equal(0.5917, x / Width, 4);
		}

		[Fact]
		public void FrequencyToX_EqualRatios_GiveEqualDistances()
		{
			double low = SpectrumAxisMapping.FrequencyToX(200, Width) - SpectrumAxisMapping.FrequencyToX(100, Width);
			double high = SpectrumAxisMapping.FrequencyToX(2000, Width) - SpectrumAxisMapping.FrequencyToX(1000, Width);

			Assert.Equal(low, high, Precision);
		}

		[Fact]
		public void DbToY_0Db_IsTopEdge()
		{
			Assert.Equal(0, SpectrumAxisMapping.DbToY(0, Height), Precision);
		}

		[Fact]
		public void DbToY_Minus72Db_IsBottomEdge()
		{
			Assert.Equal(Height, SpectrumAxisMapping.DbToY(-72, Height), Precision);
		}

		[Fact]
		public void DbToY_Minus36Db_IsMiddle()
		{
			Assert.Equal(Height / 2, SpectrumAxisMapping.DbToY(-36, Height), Precision);
		}
	}
}
