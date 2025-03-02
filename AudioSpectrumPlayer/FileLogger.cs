using System;
using System.IO;

namespace AudioSpectrumPlayer
{
    public static class FileLogger
    {
        private static readonly string LogFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AudioSpectrumPlayer",
            "logs",
            $"app_log_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

        private static readonly object LockObject = new object();
        private static bool _initialized = false;

        public static void Initialize()
        {
            if (_initialized) return;

            try
            {
                // Create directory if it doesn't exist
                string directory = Path.GetDirectoryName(LogFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Write initial log entry
                Log($"=== Audio Spectrum Player Log Started ===");
                Log($"Application Version: {typeof(FileLogger).Assembly.GetName().Version}");
                Log($"OS Version: {Environment.OSVersion}");

                _initialized = true;
            }
            catch (Exception ex)
            {
                // Can't really log this error anywhere...
            }
        }

        public static void Log(string message)
        {
            try
            {
                lock (LockObject)
                {
                    string entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
                    File.AppendAllText(LogFilePath, entry + Environment.NewLine);
                }
            }
            catch
            {
                // Silently ignore errors in logging
            }
        }

        public static void LogException(Exception ex, string context = "")
        {
            try
            {
                string message = string.IsNullOrEmpty(context)
                    ? $"EXCEPTION: {ex.Message}"
                    : $"EXCEPTION in {context}: {ex.Message}";

                Log(message);
                Log($"Exception Type: {ex.GetType().FullName}");
                Log($"Stack Trace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    Log($"Inner Exception: {ex.InnerException.Message}");
                    Log($"Inner Stack Trace: {ex.InnerException.StackTrace}");
                }

                Log("=== Exception End ===");
            }
            catch
            {
                // Silently ignore errors in logging
            }
        }
    }
}