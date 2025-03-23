using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Serilog;
using Serilog.Events;
using System;
using System.IO;
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
			PlaybackProgress.PositionChanged += PlaybackProgress_PositionChanged;
			MonitorWindowLifetime();
			Title = "Audio Player";


			var loggerConfig = new LoggerConfiguration()
				.MinimumLevel.Debug()
				.Enrich.WithThreadId()
				.WriteTo.Debug(restrictedToMinimumLevel: LogEventLevel.Warning,
					outputTemplate: "[{Level:u3}] ({ThreadId}) {Message:lj}{NewLine}{Exception}")
				.WriteTo.LogDisplay(LogDisplay, restrictedToMinimumLevel: LogEventLevel.Warning,
					outputTemplate: "[{Level:u3}] ({ThreadId}) {Message:lj}{NewLine}{Exception}")
				.WriteTo.File(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "log-.txt"),
					rollingInterval: RollingInterval.Day,
					outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] ({ThreadId}) {Message:lj}{NewLine}{Exception}");

			Log.Logger = loggerConfig.CreateLogger();




			Log.Debug("Application started");
		}

		private void InitializeMediaPlayer()
		{
			try
			{
				Log.Debug("Initializing MediaPlayer");

				mediaPlayer = new MediaPlayer();
				mediaPlayer.MediaOpened += (sender, args) =>
				{
					try
					{
						Log.Debug("Media opened successfully");
						SetPlaybackTimer();

					}
					catch (Exception ex)
					{
						Log.Error(ex, "MediaOpened event");
					}
				};

				mediaPlayer.MediaFailed += (sender, args) =>
				{
					try
					{
						Log.Error($"Media failed to load: {args.Error}");
					}
					catch (Exception ex)
					{
						Log.Error(ex, "MediaFailed event");
					}
				};

				mediaPlayer.PlaybackSession.PlaybackStateChanged += (sender, args) =>
				{
					try
					{
						Log.Debug($"Playback state changed to: {sender.PlaybackState}");
					}
					catch (Exception ex)
					{
						Log.Error(ex, "PlaybackStateChanged event");
					}
				};

				playbackTimer = new DispatcherTimer
				{
					Interval = TimeSpan.FromMilliseconds(1000)
				};
				playbackTimer.Tick += PlaybackTimer_Tick;
				VolumeControl.VolumeChanged += VolumeControl_VolumeChanged;

				Log.Debug("MediaPlayer initialized successfully");
			}
			catch (Exception ex)
			{
				Log.Error(ex, "InitializeMediaPlayer");
			}
		}

		private void MonitorWindowLifetime()
		{
			try
			{
				var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);

				Log.Debug($"Window handle: {windowHandle}");

				// Register for window messages using Win32 interop
				// This requires adding a reference to a Win32 message hook library or using PInvoke

				AppWindow.Changed += (sender, args) =>
				{
					try
					{
						if (sender != null)
						{
							Log.Debug($"Window state changed: {args}");
						}
					}
					catch (Exception ex)
					{
						Log.Error(ex, "Window state change event");
					}
				};

				this.Closed += (s, e) => Log.Debug("Window explicitly closed");

				Log.Debug("Window lifetime monitoring initialized");
			}
			catch (Exception ex)
			{
				Log.Error(ex, "MonitorWindowLifetime");
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
				Log.Information($"File selected: {file.Path}");
				await LoadAudioFileAsync(file.Path);
			}
			else
			{
				Log.Warning("File selection canceled or failed");
			}
		}

		private void PlayButton_Click(object sender, RoutedEventArgs e)
		{
			if (mediaPlayer.Source != null)
			{
				Log.Information("Playing audio");
				mediaPlayer.Play();

			}
			else
			{
				Log.Debug("Cannot play: No audio file loaded");
			}
		}

		private void PauseButton_Click(object sender, RoutedEventArgs e)
		{
			if (mediaPlayer.Source != null)
			{
				Log.Information("Pausing audio");
				mediaPlayer.Pause();
				playbackTimer.Stop();
			}
			else
			{
				Log.Debug("Cannot pause: No audio file loaded");
			}
		}

		private void StopButton_Click(object sender, RoutedEventArgs e)
		{
			if (mediaPlayer.Source != null)
			{
				Log.Information("Stopping audio");
				mediaPlayer.Position = TimeSpan.Zero;
				mediaPlayer.Pause();
				playbackTimer.Stop();

				PlaybackProgress.Reset();
			}
			else
			{
				Log.Debug("Cannot stop: No audio file loaded");
			}
		}

		private void ClearLogButton_Click(object sender, RoutedEventArgs e)
		{
			LogDisplay.Clear();
			LogDisplay.Log("Log cleared");
		}
		#endregion

		private void VolumeControl_VolumeChanged(object? sender, double volume)
		{
			try
			{
				if (mediaPlayer != null)
				{
					mediaPlayer.Volume = volume;
					// This log is called a lot, only enable when needed
					//Log.Information($"Volume changed to {(int)(volume * 100)}%");
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex, "VolumeControl_VolumeChanged");
			}
		}

		public async Task LoadAudioFileAsync(string filePath)
		{
			try
			{
				Log.Information($"Loading audio file: {filePath}");

				if (!System.IO.File.Exists(filePath))
				{
					Log.Error($"Error: File not found: {filePath}");
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
						playbackTimer.Start();
						Title = $"Audio Spectrum Player - {System.IO.Path.GetFileName(filePath)}";
						Log.Information("Media source set successfully");
					}
					catch (Exception ex)
					{
						Log.Error(ex, "Setting media source on dispatcher");
					}
					await Task.CompletedTask; // only for the IDE to be happy
				});
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Load Audio File failed");
			}
			await Task.CompletedTask; // only for the IDE to be happy
		}

		#region Progress Bar
		private void PlaybackProgress_PositionChanged(object? sender, double e)
		{
			try
			{
				if (mediaPlayer?.PlaybackSession != null &&
					mediaPlayer.PlaybackSession.CanSeek &&
					mediaPlayer.PlaybackSession.NaturalDuration.TotalMilliseconds > 0)
				{
					TimeSpan newPosition = TimeSpan.FromMilliseconds(
						e * mediaPlayer.PlaybackSession.NaturalDuration.TotalMilliseconds);

					mediaPlayer.PlaybackSession.Position = newPosition;
					// This log is called a lot, only enable when needed
					//Log.Information($"Playback Position changed: {FormatTimeSpan(newPosition)}");

					PlaybackProgress.CurrentPosition = newPosition;
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex, "PlaybackProgress_PositionChanged");
			}
		}

		private void SetPlaybackTimer()
		{
			if (mediaPlayer.Source != null)
			{
				PlaybackProgress.CurrentPosition = TimeSpan.Zero;
				PlaybackProgress.TotalDuration = mediaPlayer.PlaybackSession.NaturalDuration;
				if (mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
				{
					playbackTimer.Start();
				}
				Log.Debug($"Progress Bar Initialized");
			}
		}

		private void PlaybackTimer_Tick(object? sender, object e)
		{
			try
			{
				if (mediaPlayer?.PlaybackSession != null &&
					mediaPlayer.PlaybackSession.NaturalDuration.TotalMilliseconds > 0)
				{
					PlaybackProgress.CurrentPosition = mediaPlayer.PlaybackSession.Position;
					PlaybackProgress.TotalDuration = mediaPlayer.PlaybackSession.NaturalDuration;
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex, "PlaybackTimer_Tick");
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
