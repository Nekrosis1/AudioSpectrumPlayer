using AudioSpectrumPlayer.Core.Services;
using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;
using System;
using System.IO;

namespace AudioSpectrumPlayer.Shared.Services
{
	public class LogDisplaySink(ILogDisplayService logDisplayService, ITextFormatter formatter) : ILogEventSink
	{
		private readonly ILogDisplayService _logDisplayService = logDisplayService ?? throw new ArgumentNullException(nameof(logDisplayService));

		public void Emit(LogEvent logEvent)
		{
			using StringWriter stringWriter = new();
			formatter.Format(logEvent, stringWriter);
			_logDisplayService.Log(stringWriter.ToString());
		}
	}

	public static class LogDisplaySinkExtensions
	{
		public static LoggerConfiguration LogDisplay(
			this LoggerSinkConfiguration loggerSinkConfiguration,
			ILogDisplayService logDisplayService,
			 string outputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
			LogEventLevel restrictedToMinimumLevel = LogEventLevel.Verbose,
			IFormatProvider? formatProvider = null)
		{
			MessageTemplateTextFormatter formatter = new MessageTemplateTextFormatter(outputTemplate, formatProvider);
			return loggerSinkConfiguration.Sink(new LogDisplaySink(logDisplayService, formatter), restrictedToMinimumLevel);
		}
	}
}

