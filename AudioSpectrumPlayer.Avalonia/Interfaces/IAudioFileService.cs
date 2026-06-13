using System.Threading.Tasks;

namespace AudioSpectrumPlayer.Avalonia.Interfaces
{
	public interface IAudioFileService
	{
		Task<string?> PickAudioFileAsync();
		bool IsValidAudioFile(string filePath);
	}
}
