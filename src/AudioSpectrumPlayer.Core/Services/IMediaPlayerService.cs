namespace AudioSpectrumPlayer.Core.Services
{
	public interface IMediaPlayerService
	{
		Task<bool> LoadAudioFileAsync(string filePath);
		void Play();
		void Pause();
		void Stop();
		void SeekToPosition(double percentage);

		event EventHandler<TimeSpan> PositionChanged;
		event EventHandler<TimeSpan> DurationChanged;

		double Volume { get; set; }
		TimeSpan CurrentPosition { get; }
		TimeSpan TotalDuration { get; }
	}
}
