//using System;
//using System.IO;

//namespace AudioSpectrumPlayer
//{
//	public static class FileLogger
//	{
//		//public static bool IsLoggingEnabled { get; set; } = false;

//		private static readonly string LogFilePath = Path.Combine(
//			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
//			"AudioSpectrumPlayer",
//			"logs",
//			$"app_log_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

//		private static readonly object LockObject = new();
//		private static bool _initialized = false;

//		public static void Initialize()
//		{
//			if (_initialized) return;

//			try
//			{
//				string? directory = Path.GetDirectoryName(LogFilePath);
//				if (string.IsNullOrEmpty(directory))
//				{
//					directory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
//					// TODO: need some way of notifying that I am not in the normal log folder, figure out later
//				}
//				if (!Directory.Exists(directory))
//				{
//					Directory.CreateDirectory(directory);
//				}

//				// Write initial log entry
//				Log($"=== Audio Spectrum Player Log Started ===");
//				Log($"Application Version: {typeof(FileLogger).Assembly.GetName().Version}");
//				Log($"OS Version: {Environment.OSVersion}");

//				_initialized = true;
//			}
//			catch (Exception)
//			{
//				// Can't really log this error anywhere...
//				// TODO: Maybe add a UI notification bubble thing
//			}
//		}

//		public static void Log(string message)
//		{
//			//if (!IsLoggingEnabled) return;
//			try
//			{
//				lock (LockObject)
//				{
//					string entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
//					File.AppendAllText(LogFilePath, entry + Environment.NewLine);
//				}
//			}
//			catch
//			{
//				// Silently ignore errors in logging
//			}
//		}

//		public static void LogException(Exception ex, string context = "")
//		{
//			try
//			{
//				string message = string.IsNullOrEmpty(context)
//					? $"EXCEPTION: {ex.Message}"
//					: $"EXCEPTION in {context}: {ex.Message}";

//				Log(message);
//				Log($"Exception Type: {ex.GetType().FullName}");
//				Log($"Stack Trace: {ex.StackTrace}");

//				if (ex.InnerException != null)
//				{
//					Log($"Inner Exception: {ex.InnerException.Message}");
//					Log($"Inner Stack Trace: {ex.InnerException.StackTrace}");
//				}

//				Log("=== Exception End ===");
//			}
//			catch
//			{
//				// Silently ignore errors in logging
//			}
//		}
//	}
//}