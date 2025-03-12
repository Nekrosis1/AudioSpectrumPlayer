using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace AudioSpectrumPlayer
{
	/// <summary>
	/// An empty window that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class MainWindow : Window
	{
		private MediaPlayer mediaPlayer = null!;
		private DispatcherTimer playbackTimer = null!;
		private string? currentFilePath;
		public MainWindow()
		{
			this.InitializeComponent();
			InitializeMediaPlayer();
			MonitorWindowLifetime();
			Title = "Audio Player";

			LogViewer.Log("Application started");
			LogViewer.Log($"Current time: {DateTime.Now}");

			// Log command line arguments
			string[] args = Environment.GetCommandLineArgs();
			LogViewer.Log($"Command line args count: {args.Length}");
			for (int i = 0; i < args.Length; i++)
			{
				LogViewer.Log($"Arg[{i}]: {args[i]}");
			}
		}

		private void InitializeMediaPlayer()
		{
			try
			{
				FileLogger.Log("Initializing MediaPlayer");

				mediaPlayer = new MediaPlayer();
				mediaPlayer.MediaOpened += (sender, args) =>
				{
					try
					{
						FileLogger.Log("Media opened successfully");
						SetPlaybackTimer();
						LogViewer?.Log($"Progress Bar Initialized");
					}
					catch (Exception ex)
					{
						FileLogger.LogException(ex, "MediaOpened event");
					}
				};

				mediaPlayer.MediaFailed += (sender, args) =>
				{
					try
					{
						FileLogger.Log($"Media failed to load: {args.Error}");
					}
					catch (Exception ex)
					{
						FileLogger.LogException(ex, "MediaFailed event");
					}
				};

				mediaPlayer.PlaybackSession.PlaybackStateChanged += (sender, args) =>
				{
					try
					{
						FileLogger.Log($"Playback state changed to: {sender.PlaybackState}");
					}
					catch (Exception ex)
					{
						FileLogger.LogException(ex, "PlaybackStateChanged event");
					}
				};

				playbackTimer = new DispatcherTimer
				{
					Interval = TimeSpan.FromMilliseconds(500)
				};
				playbackTimer.Tick += PlaybackTimer_Tick;

				FileLogger.Log("MediaPlayer initialized successfully");
			}
			catch (Exception ex)
			{
				FileLogger.LogException(ex, "InitializeMediaPlayer");
			}
		}

		private void MonitorWindowLifetime()
		{
			try
			{
				var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);

				FileLogger.Log($"Window handle: {windowHandle}");

				// Register for window messages using Win32 interop
				// This requires adding a reference to a Win32 message hook library or using PInvoke

				AppWindow.Changed += (sender, args) =>
				{
					try
					{
						if (sender != null)
						{
							FileLogger.Log($"Window state changed: {args}");
						}
					}
					catch (Exception ex)
					{
						FileLogger.LogException(ex, "Window state change event");
					}
				};

				this.Closed += (s, e) => FileLogger.Log("Window explicitly closed");

				FileLogger.Log("Window lifetime monitoring initialized");
			}
			catch (Exception ex)
			{
				FileLogger.LogException(ex, "MonitorWindowLifetime");
			}
		}

		#region UI Buttons
		private async void OpenFileButton_Click(object sender, RoutedEventArgs e)
		{
			var picker = new FileOpenPicker();

			nint hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
			WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

			picker.FileTypeFilter.Add(".mp3");
			picker.FileTypeFilter.Add(".mpeg");
			picker.FileTypeFilter.Add(".wav");
			picker.FileTypeFilter.Add(".m4a");
			picker.FileTypeFilter.Add(".wma");
			picker.FileTypeFilter.Add(".aac");
			picker.FileTypeFilter.Add(".flac");
			picker.FileTypeFilter.Add(".ogg");
			picker.FileTypeFilter.Add(".aiff");

			StorageFile file = await picker.PickSingleFileAsync();
			if (file != null)
			{
				LogViewer.Log($"File selected: {file.Path}");
				await LoadAudioFileAsync(file.Path);
			}
			else
			{
				LogViewer.Log("File selection canceled or failed");
			}
		}

		private void PlayButton_Click(object sender, RoutedEventArgs e)
		{
			if (mediaPlayer.Source != null)
			{
				LogViewer.Log("Playing audio");
				mediaPlayer.Play();
				playbackTimer.Start();
			}
			else
			{
				LogViewer.Log("Cannot play: No audio file loaded");
			}
		}

		private void PauseButton_Click(object sender, RoutedEventArgs e)
		{
			if (mediaPlayer.Source != null)
			{
				LogViewer.Log("Pausing audio");
				mediaPlayer.Pause();
				playbackTimer.Stop();
			}
			else
			{
				LogViewer.Log("Cannot pause: No audio file loaded");
			}
		}

		private void StopButton_Click(object sender, RoutedEventArgs e)
		{
			if (mediaPlayer.Source != null)
			{
				LogViewer.Log("Stopping audio");
				mediaPlayer.Position = TimeSpan.Zero;
				mediaPlayer.Pause();
				playbackTimer.Stop();

				PlaybackProgressBar.Value = 0;
				TimeDisplay.Text = $"00:00 / {FormatTimeSpan(mediaPlayer.PlaybackSession.NaturalDuration)}";
			}
			else
			{
				LogViewer.Log("Cannot stop: No audio file loaded");
			}
		}

		private void ClearLogButton_Click(object sender, RoutedEventArgs e)
		{
			LogViewer.Clear();
			LogViewer.Log("Log cleared");
		}
		#endregion

		public async Task LoadAudioFileAsync(string filePath)
		{
			try
			{
				FileLogger.Log($"Loading audio file: {filePath}");

				if (!System.IO.File.Exists(filePath))
				{
					FileLogger.Log($"Error: File does not exist: {filePath}");
					LogViewer?.Log($"Error: File not found: {filePath}");
					return;
				}
				currentFilePath = filePath;
				Uri uri = new(filePath);
				MediaSource mediaSource = MediaSource.CreateFromUri(uri);

				DispatcherQueue.GetForCurrentThread()?.TryEnqueue(DispatcherQueuePriority.Normal, async () =>
				{
					try
					{
						mediaPlayer.Source = mediaSource;

						Title = $"Audio Spectrum Player - {System.IO.Path.GetFileName(filePath)}";
						LogViewer?.Log($"Audio file loaded: {System.IO.Path.GetFileName(filePath)}");
						FileLogger.Log("Media source set successfully");
					}
					catch (Exception ex)
					{
						FileLogger.LogException(ex, "Setting media source on dispatcher");
					}
					await Task.CompletedTask; // only for the IDE to be happy
				});
			}
			catch (Exception ex)
			{
				FileLogger.LogException(ex, "LoadAudioFileFromPathDirectlyAsync");
			}
			await Task.CompletedTask; // only for the IDE to be happy
		}

		#region Progress Bar
		private void SetPlaybackTimer()
		{
			if (mediaPlayer.Source != null)
			{
				PlaybackProgressBar.Value = 0;
				TimeDisplay.Text = $"00:00 / {FormatTimeSpan(mediaPlayer.PlaybackSession.NaturalDuration)}";
			}
		}

		private void PlaybackTimer_Tick(object? sender, object e)
		{
			try
			{
				if (mediaPlayer?.PlaybackSession != null &&
					mediaPlayer.PlaybackSession.NaturalDuration.TotalMilliseconds > 0)
				{
					double progress = (mediaPlayer.PlaybackSession.Position.TotalMilliseconds /
									   mediaPlayer.PlaybackSession.NaturalDuration.TotalMilliseconds) * 100;
					PlaybackProgressBar.Value = progress;

					string currentTime = FormatTimeSpan(mediaPlayer.PlaybackSession.Position);
					string totalTime = FormatTimeSpan(mediaPlayer.PlaybackSession.NaturalDuration);
					TimeDisplay.Text = $"{currentTime} / {totalTime}";
				}
			}
			catch (Exception ex)
			{
				FileLogger.LogException(ex, "PlaybackTimer_Tick");
			}
		}
		private static string FormatTimeSpan(TimeSpan timeSpan)
		{
			return timeSpan.Hours > 0
				? $"{timeSpan.Hours:00}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}"
				: $"{timeSpan.Minutes:00}:{timeSpan.Seconds:00}";
		}
		#endregion
	}
}
