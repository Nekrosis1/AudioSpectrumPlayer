//using System;

//namespace AudioSpectrumPlayer
//{
//	public static class Logger
//	{
//		private static LogDisplay? _logDisplay;
//		// simple on off for the loggers
//		private static bool _isFileLoggerActive = false;
//		private static bool _isDisplayLoggerActive = true;

//		public static void Initialize(LogDisplay logDisplay)
//		{
//			_logDisplay = logDisplay;
//		}

//		public static void Log(string message)
//		{
//			if (_isFileLoggerActive)
//			{
//				FileLogger.Log(message);
//			}
//			if (_isDisplayLoggerActive)
//			{
//				_logDisplay?.Log(message);
//			}
//		}

//		public static void LogException(Exception ex, string context = "")
//		{
//			if (_isFileLoggerActive)
//			{
//				FileLogger.LogException(ex, context);
//			}
//		}

//		public static void Clear()
//		{
//			_logDisplay?.Clear();
//		}
//	}
//}
