using AudioSpectrumPlayer.Core.Services;
using AudioSpectrumPlayer.Core.ViewModels;
using AudioSpectrumPlayer.Services;
using AudioSpectrumPlayer.Shared.Services;
using AudioSpectrumPlayer.WinUI.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

		private Window? m_window;
		private static LogDisplay? _logDisplay;
		private IHost? _host;

		public App()
		{
			ConfigureServices();
			ConfigureLogging();

			this.InitializeComponent();
			Log.Debug("Application components initialized");
		}

		private static void ConfigureLogging()
		{
			_logDisplay = new LogDisplay();

			var loggerConfig = new LoggerConfiguration()
				.MinimumLevel.Debug()
				.Enrich.WithThreadId()
				.WriteTo.Debug(restrictedToMinimumLevel: LogEventLevel.Information,
					outputTemplate: "[{Level:u3}] ({ThreadId}) {Message:lj}{NewLine}{Exception}")
				.WriteTo.LogDisplay(_logDisplay, restrictedToMinimumLevel: LogEventLevel.Debug,
					outputTemplate: "[{Level:u3}] ({ThreadId}) {Message:lj}{NewLine}{Exception}")
				.WriteTo.File(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "log-.txt"),
					rollingInterval: RollingInterval.Day,
					outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] ({ThreadId}) {Message:lj}{NewLine}{Exception}");

			Log.Logger = loggerConfig.CreateLogger();
			Log.Information("Application starting");
			Log.Information($"Logs Written to {Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "log-[Date].txt")}");

			SetupExceptionHandling();
		}

		private void ConfigureServices()
		{
			var hostBuilder = Host.CreateDefaultBuilder()
				.ConfigureServices(services =>
				{
					// Services
					services.AddSingleton<IAudioFileService, AudioFileService>();
					services.AddSingleton<IMediaPlayerService, WinMediaPlayerService>();


					// ViewModels
					services.AddSingleton<AudioPlayerViewModel>();
					services.AddSingleton<LogViewModel>();

					// Views
					services.AddSingleton<MainWindow>();
					services.AddSingleton<LogDisplay>(); // Add this
					services.AddSingleton<ILogDisplayService>(provider => provider.GetRequiredService<LogDisplay>()); // Add this
				});

			_host = hostBuilder.Build();
		}

		private static void SetupExceptionHandling()
		{
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

			Current.UnhandledException += (sender, args) =>
			{
				Log.Fatal($"Unhandled UI exception: {args.Message}");
				Log.Fatal(args.Exception, "Application.UnhandledException");
				Log.CloseAndFlush();
			};
		}

		protected override void OnLaunched(LaunchActivatedEventArgs args)
		{
			m_window = _host?.Services.GetRequiredService<MainWindow>();
			m_window?.Activate();

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
						StorageFile file = await StorageFile.GetFileFromPathAsync(filePath);
						Log.Debug($"Found file to open: {filePath}");

						if (m_window is MainWindow mainWindow)
						{
							var audioFileService = _host?.Services.GetRequiredService<IAudioFileService>();
							if (audioFileService?.IsValidAudioFile(filePath) == true)
							{
								await mainWindow.ViewModel.LoadAudioFileAsync(filePath);
							}
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

		public static T? GetService<T>() where T : class
		{
			return ((App)Current)._host?.Services.GetService<T>();
		}

		public static T GetRequiredService<T>() where T : class
		{
			return ((App)Current)._host?.Services.GetRequiredService<T>()
				?? throw new InvalidOperationException($"Service {typeof(T).Name} not found");
		}
	}
}
