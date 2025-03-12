using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

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
				var ex = args.ExceptionObject as Exception;
				FileLogger.Log("CRITICAL: Unhandled AppDomain exception");
				if (ex != null)
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

		private void ProcessCommandLineArgs()
		{
			try
			{
				FileLogger.Log("Trying to get args");
				string[] launchArgs = Environment.GetCommandLineArgs();

				Debug.WriteLine($"Total args: {launchArgs.Length}");
				for (int i = 0; i < launchArgs.Length; i++)
				{
					FileLogger.Log($"Arg[{i}]: {launchArgs[i]}");
					Debug.WriteLine($"Arg[{i}]: {launchArgs[i]}");
				}

				if (launchArgs.Length > 1)
				{
					string filePath = launchArgs[1];
					if (System.IO.File.Exists(filePath))
					{
						FileLogger.Log($"Found file to open: {filePath}");
						Debug.WriteLine($"Found file to open: {filePath}");
						LoadAudioFileAsync(filePath);
					}
					else
					{
						FileLogger.Log($"File does not exist: {filePath}");
						Debug.WriteLine($"File does not exist: {filePath}");
					}
				}
			}
			catch (Exception ex)
			{
				// Log any errors that occur during command line processing
				Debug.WriteLine($"Error processing command line arguments: {ex.Message}");
			}
		}

		private async Task LoadAudioFileAsync(string filePath)
		{
			try
			{
				FileLogger.Log($"Attempting to load file: {filePath}");

				// Instead of using StorageFile directly, pass the file path to the MainWindow
				if (m_window is MainWindow mainWindow)
				{
					FileLogger.Log("Main window available, passing file path to it");
					await mainWindow.LoadAudioFile(filePath);
				}
				else
				{
					FileLogger.Log("ERROR: Main window is not available or not a MainWindow instance");
				}
			}
			catch (Exception ex)
			{
				FileLogger.LogException(ex, "LoadAudioFileAsync");
			}
		}
	}
}
