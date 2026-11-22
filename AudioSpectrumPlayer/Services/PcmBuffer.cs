using AudioSpectrumPlayer.Interfaces;
using AudioSpectrumPlayer.Models;
using System;

namespace AudioSpectrumPlayer.Services
{
	/// <summary>In-memory <see cref="IPcmSource"/> over fully decoded <see cref="PcmData"/>.</summary>
	public class PcmBuffer : IPcmSource
	{
		private readonly float[] _samples;
		private readonly int _channelCount;
		private readonly long _frameCount;

		public PcmBuffer(PcmData data)
		{
			ArgumentNullException.ThrowIfNull(data);
			ArgumentNullException.ThrowIfNull(data.InterleavedSamples, nameof(data));
			if (data.SampleRate <= 0)
			{
				throw new ArgumentException($"Sample rate must be positive, was {data.SampleRate}.", nameof(data));
			}
			if (data.ChannelCount <= 0)
			{
				throw new ArgumentException($"Channel count must be positive, was {data.ChannelCount}.", nameof(data));
			}
			if (data.InterleavedSamples.Length % data.ChannelCount != 0)
			{
				throw new ArgumentException(
					$"Sample count {data.InterleavedSamples.Length} is not a multiple of the channel count {data.ChannelCount}.",
					nameof(data));
			}

			_samples = data.InterleavedSamples;
			_channelCount = data.ChannelCount;
			_frameCount = data.InterleavedSamples.Length / data.ChannelCount;
			SampleRate = data.SampleRate;
		}

		public int SampleRate { get; }

		public TimeSpan Duration => TimeSpan.FromTicks(_frameCount * TimeSpan.TicksPerSecond / SampleRate);

		public PcmBlock GetSamples(TimeSpan time, int count)
		{
			ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

			// Int128: ticks * sample rate overflows long for times beyond ~242 days at 44.1 kHz.
			Int128 startFrame = FloorDiv((Int128)time.Ticks * SampleRate, TimeSpan.TicksPerSecond);
			Int128 endFrame = startFrame + count;

			Int128 firstAvailable = Int128.Max(startFrame, 0);
			Int128 endAvailable = Int128.Min(endFrame, _frameCount);

			float[] mono = new float[count];
			if (firstAvailable < endAvailable)
			{
				// Both fit in int/long: the overlap lies inside [startFrame, startFrame + count)
				// and inside [0, _frameCount).
				int outputOffset = (int)(firstAvailable - startFrame);
				int frames = (int)(endAvailable - firstAvailable);
				long frame = (long)firstAvailable;
				MixToMono(frame, frames, mono.AsSpan(outputOffset, frames));
			}

			return new PcmBlock(mono, SampleRate, time);
		}

		private void MixToMono(long firstFrame, int frameCount, Span<float> destination)
		{
			if (_channelCount == 1)
			{
				_samples.AsSpan((int)firstFrame, frameCount).CopyTo(destination);
				return;
			}

			long sampleIndex = firstFrame * _channelCount;
			for (int i = 0; i < frameCount; i++)
			{
				float sum = 0f;
				for (int channel = 0; channel < _channelCount; channel++)
				{
					sum += _samples[sampleIndex++];
				}
				destination[i] = sum / _channelCount;
			}
		}

		/// <summary>Integer division rounding toward negative infinity, for a positive divisor.</summary>
		private static Int128 FloorDiv(Int128 dividend, long divisor)
		{
			Int128 quotient = dividend / divisor;
			if (dividend < 0 && quotient * divisor != dividend)
			{
				quotient--;
			}
			return quotient;
		}
	}
}
