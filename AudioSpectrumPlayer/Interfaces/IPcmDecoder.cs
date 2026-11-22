using AudioSpectrumPlayer.Models;
using System.IO;

namespace AudioSpectrumPlayer.Interfaces
{
	/// <summary>Decodes one audio format into <see cref="PcmData"/>.</summary>
	public interface IPcmDecoder
	{
		PcmData Decode(string filePath);
		PcmData Decode(Stream stream);
	}
}
