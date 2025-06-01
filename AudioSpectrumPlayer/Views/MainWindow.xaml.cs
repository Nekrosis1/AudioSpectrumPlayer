using AudioSpectrumPlayer.ViewModels;
using Microsoft.UI.Xaml;
using Serilog;
using Serilog.Events;
using System;
using System.IO;

namespace AudioSpectrumPlayer.Views
{
	/// <summary>
	/// An empty window that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class MainWindow : Window
	{
		public AudioPlayerViewModel ViewModel { get; }
		public MainWindow(AudioPlayerViewModel viewModel, LogDisplay logDisplay)
		{
			this.InitializeComponent();
			ViewModel = viewModel;
			LogDisplay = logDisplay;
			MonitorWindowLifetime();
			Title = "Audio Player";

			var loggerConfig = new LoggerConfiguration()
				.MinimumLevel.Debug()
				.Enrich.WithThreadId()
				.WriteTo.Debug(restrictedToMinimumLevel: LogEventLevel.Warning,
					outputTemplate: "[{Level:u3}] ({ThreadId}) {Message:lj}{NewLine}{Exception}")
				.WriteTo.LogDisplay(LogDisplay, restrictedToMinimumLevel: LogEventLevel.Debug,
					outputTemplate: "[{Level:u3}] ({ThreadId}) {Message:lj}{NewLine}{Exception}")
				.WriteTo.File(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "log-.txt"),
					rollingInterval: RollingInterval.Day,
					outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] ({ThreadId}) {Message:lj}{NewLine}{Exception}");

			Log.Logger = loggerConfig.CreateLogger();
			Log.Debug("Application started");
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

		private void PlayButton_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.Play();
		}

		private void PauseButton_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.Pause();
		}

		private void StopButton_Click(object sender, RoutedEventArgs e)
		{
			ViewModel.Stop();
		}

		private async void OpenFileButton_Click(object sender, RoutedEventArgs e)
		{
			nint hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
			await ViewModel.SelectAndLoadAudioFileAsync(hwnd);
		}

		private void ClearLogButton_Click(object sender, RoutedEventArgs e)
		{
			LogDisplay.Clear();
			LogDisplay.Log("Log cleared");
		}
		#endregion
	}
}
