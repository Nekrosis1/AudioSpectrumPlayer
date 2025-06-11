using NAudio.Wave;
using System;

namespace AudioSpectrumPlayer.Services
{
	public class SpectrumGenerationService
	{
		public float[] GetAudioChunkAtPosition(AudioFileReader audioFile, TimeSpan position, int bufferSize = 1024)
		{
			var sampleProvider = audioFile.ToSampleProvider().ToMono();

			// Calculate the sample offset based on the time position
			long sampleOffset = (long)(position.TotalSeconds * sampleProvider.WaveFormat.SampleRate);

			// Seek to the desired position
			audioFile.CurrentTime = position;

			float[] buffer = new float[bufferSize];
			int samplesRead = sampleProvider.Read(buffer, 0, bufferSize);

			// Pad with zeros if we didn't get enough samples
			if (samplesRead < bufferSize)
			{
				Array.Clear(buffer, samplesRead, bufferSize - samplesRead);
			}
			return buffer;
		}



	}
}
