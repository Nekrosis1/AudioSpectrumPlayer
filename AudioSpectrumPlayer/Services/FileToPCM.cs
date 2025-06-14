//using NAudio.Wave;
//using System;

//namespace AudioSpectrumPlayer.Services
//{
//	public class FileToPCM
//	{
//		public float[] GetPCMData(string filePath, int sampleCount)
//		{
//			using var audioFile = new AudioFileReader(filePath);

//			// Convert to mono, 44.1kHz if needed
//			var sampleProvider = audioFile.ToSampleProvider();
//			if (sampleProvider.WaveFormat.Channels > 1)
//			{
//				sampleProvider = sampleProvider.ToMono();
//			}

//			// Read samples
//			float[] buffer = new float[sampleCount];
//			int samplesRead = sampleProvider.Read(buffer, 0, sampleCount);

//			// Return only the samples that were actually read
//			if (samplesRead < sampleCount)
//			{
//				Array.Resize(ref buffer, samplesRead);
//			}

//			return buffer;
//		}
//	}
//}

