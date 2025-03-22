using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;
using System;
using System.IO;

namespace AudioSpectrumPlayer
{
	public class LogDisplaySink : ILogEventSink
	{
		private readonly LogDisplay _logDisplay;
		private readonly ITextFormatter _formatter;

		public LogDisplaySink(LogDisplay logDisplay, ITextFormatter formatter)
		{
			_logDisplay = logDisplay ?? throw new ArgumentNullException(nameof(logDisplay));
			_formatter = formatter;
		}

		public void Emit(LogEvent logEvent)
		{
			using StringWriter stringWriter = new StringWriter();
			_formatter.Format(logEvent, stringWriter);


			//string message = logEvent.RenderMessage(_formatProvider);
			//string logLevel = logEvent.Level.ToString();

			//// Format similar to your other log formats
			//var logMessage = $"[{logLevel.Substring(0, 3)}] {message}";

			//// Include exception if present
			//if (logEvent.Exception != null)
			//{
			//	logMessage += $"{Environment.NewLine}{logEvent.Exception}";
			//}

			_logDisplay.Log(stringWriter.ToString());
		}
	}

	// Extension method to easily add this sink
	public static class LogDisplaySinkExtensions
	{
		public static LoggerConfiguration LogDisplay(
			this LoggerSinkConfiguration loggerSinkConfiguration,
			LogDisplay logDisplay,
			 string outputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
			LogEventLevel restrictedToMinimumLevel = LogEventLevel.Verbose,
			IFormatProvider? formatProvider = null)
		{
			MessageTemplateTextFormatter formatter = new MessageTemplateTextFormatter(outputTemplate, formatProvider);
			return loggerSinkConfiguration.Sink(new LogDisplaySink(logDisplay, formatter), restrictedToMinimumLevel);
		}
	}
}

