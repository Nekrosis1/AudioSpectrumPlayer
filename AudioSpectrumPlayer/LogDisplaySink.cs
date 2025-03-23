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
	public class LogDisplaySink(LogDisplay logDisplay, ITextFormatter formatter) : ILogEventSink
	{
		private readonly LogDisplay _logDisplay = logDisplay ?? throw new ArgumentNullException(nameof(logDisplay));

		public void Emit(LogEvent logEvent)
		{
			using StringWriter stringWriter = new StringWriter();
			formatter.Format(logEvent, stringWriter);
			_logDisplay.Log(stringWriter.ToString());
		}
	}

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

