namespace AudioSpectrumPlayer.Core.Services
{
	public interface IAudioFileService
	{
		Task<string?> PickAudioFileAsync(nint windowHandle);
		bool IsValidAudioFile(string filePath);
	}
}