using System.Threading.Tasks;

namespace AudioSpectrumPlayer.Interfaces
{
	public interface IAudioFileService
	{
		Task<string?> PickAudioFileAsync(nint windowHandle);
		bool IsValidAudioFile(string filePath);
	}
}