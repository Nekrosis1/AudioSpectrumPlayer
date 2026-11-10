using System.Threading.Tasks;

namespace AudioSpectrumPlayer.Interfaces
{
	public interface IAudioFileService
	{
		/// <summary>
		/// Shows a file picker filtered to supported audio formats.
		/// </summary>
		/// <returns>Local path of the chosen file, or null if canceled or the picker is unavailable or failed.</returns>
		Task<string?> PickAudioFileAsync();

		/// <summary>
		/// True if the file exists and has a supported audio extension; contents are not checked.
		/// </summary>
		bool IsValidAudioFile(string filePath);
	}
}
