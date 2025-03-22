using Microsoft.UI.Xaml;
using Serilog;
using Serilog.Events;
using System;
using System.IO;
using Windows.Storage;

namespace AudioSpectrumPlayer
{
	/// <summary>
	/// Provides application-specific behavior to supplement the default Application class.
	/// </summary>
	public partial class App : Application
	{
		private Window m_window = default!;
		private static LogDisplay? _logDisplay = new LogDisplay();
		public App()
		{
			var loggerConfig = new LoggerConfiguration()
					.MinimumLevel.Debug()
					.Enrich.WithThreadId()
					.WriteTo.Debug(restrictedToMinimumLevel: LogEventLevel.Warning,
					outputTemplate: "[{Level:u3}] ({ThreadId}) {Message:lj}{NewLine}{Exception}")
					.WriteTo.File(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "log-.txt"),
						rollingInterval: RollingInterval.Day,
						outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] ({ThreadId}) {Message:lj}{NewLine}{Exception}");

			Log.Logger = loggerConfig.CreateLogger();
			Log.Information("Application starting");
			Log.Warning($"Logs Written to {Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "log-.txt")}");

			AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
			{
				Log.Fatal("Unhandled AppDomain exception");
				if (args.ExceptionObject is Exception ex)
				{
					Log.Error(ex, "AppDomain.UnhandledException");
				}
				else
				{
					Log.Error($"Unknown exception type: {args.ExceptionObject?.GetType().ToString() ?? "null"}");
				}
			};

			// For UI thread exceptions in WinUI apps
			Current.UnhandledException += (sender, args) =>
			{
				Log.Fatal($"Unhandled UI exception: {args.Message}");
				Log.Fatal(args.Exception, "Application.UnhandledException");

				// Prevent the app from terminating if we can handle the exception
				// args.Handled = true; // Uncomment if you want to try to recover
				Log.CloseAndFlush();
			};


			this.InitializeComponent();
			Log.Debug("Application components initialized");
		}

		protected override void OnLaunched(LaunchActivatedEventArgs args)
		{
			m_window = new MainWindow();
			m_window.Activate();

			ProcessCommandLineArgs();
		}

		private async void ProcessCommandLineArgs()
		{
			try
			{
				Log.Debug("Trying to get args");
				string[] launchArgs = Environment.GetCommandLineArgs();

				for (int i = 0; i < launchArgs.Length; i++)
				{
					Log.Debug($"Arg[{i}]: {launchArgs[i]}");
				}

				if (launchArgs.Length > 1)
				{
					string filePath = launchArgs[1];

					try
					{
						// Try to get the file using WinRT APIs
						StorageFile file = await StorageFile.GetFileFromPathAsync(filePath);
						Log.Debug($"Found file to open: {filePath}");
						if (m_window is MainWindow mainWindow)
						{
							await mainWindow.LoadAudioFileAsync(filePath);
						}
						else
						{
							Log.Fatal("Main window is not available or not a MainWindow instance");
						}
					}
					catch (UnauthorizedAccessException)
					{
						Log.Error($"Access denied to file: {filePath}");
					}
					catch (FileNotFoundException)
					{
						Log.Error($"File does not exist: {filePath}");
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error($"Error processing command line arguments: {ex.Message}");
			}
		}
	}
}
