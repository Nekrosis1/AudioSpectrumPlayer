using Avalonia.Controls;
using System.Threading.Tasks;

namespace AudioSpectrumPlayer.Avalonia.Interfaces
{
	public interface IAudioFileService
	{
		Task<string?> PickAudioFileAsync(Window window);
		bool IsValidAudioFile(string filePath);
	}
}