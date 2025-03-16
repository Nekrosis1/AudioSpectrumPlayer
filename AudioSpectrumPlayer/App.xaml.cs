using Microsoft.UI.Xaml;
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
		public App()
		{
			FileLogger.Initialize();
			FileLogger.Log("Application starting");

			AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
			{
				FileLogger.Log("CRITICAL: Unhandled AppDomain exception");
				if (args.ExceptionObject is Exception ex)
				{
					FileLogger.LogException(ex, "AppDomain.UnhandledException");
				}
				else
				{
					FileLogger.Log($"Unknown exception type: {args.ExceptionObject?.GetType().ToString() ?? "null"}");
				}
			};

			// For UI thread exceptions in WinUI apps
			Current.UnhandledException += (sender, args) =>
			{
				FileLogger.Log($"CRITICAL: Unhandled UI exception: {args.Message}");
				FileLogger.LogException(args.Exception, "Application.UnhandledException");

				// Prevent the app from terminating if we can handle the exception
				// args.Handled = true; // Uncomment if you want to try to recover
			};


			this.InitializeComponent();
			FileLogger.Log("Application components initialized");
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
				FileLogger.Log("Trying to get args");
				string[] launchArgs = Environment.GetCommandLineArgs();

				for (int i = 0; i < launchArgs.Length; i++)
				{
					FileLogger.Log($"Arg[{i}]: {launchArgs[i]}");
				}

				if (launchArgs.Length > 1)
				{
					string filePath = launchArgs[1];

					try
					{
						// Try to get the file using WinRT APIs
						StorageFile file = await StorageFile.GetFileFromPathAsync(filePath);
						FileLogger.Log($"Found file to open: {filePath}");
						if (m_window is MainWindow mainWindow)
						{
							await mainWindow.LoadAudioFileAsync(filePath);
						}
						else
						{
							FileLogger.Log("ERROR: Main window is not available or not a MainWindow instance");
						}
					}
					catch (UnauthorizedAccessException)
					{
						FileLogger.Log($"Access denied to file: {filePath}");
					}
					catch (FileNotFoundException)
					{
						FileLogger.Log($"File does not exist: {filePath}");
					}
				}
			}
			catch (Exception ex)
			{
				FileLogger.Log($"Error processing command line arguments: {ex.Message}");
			}
		}
	}
}
