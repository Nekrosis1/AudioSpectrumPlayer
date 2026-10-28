using System.Threading.Tasks;

namespace AudioSpectrumPlayer.Interfaces
{
	public interface IAudioFileService
	{
		Task<string?> PickAudioFileAsync();
		bool IsValidAudioFile(string filePath);
	}
}
