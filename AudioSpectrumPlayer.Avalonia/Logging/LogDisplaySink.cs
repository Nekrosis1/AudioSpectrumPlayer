using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;
using System;
using System.IO;

namespace AudioSpectrumPlayer.Avalonia.Logging;

public class LogDisplaySink(ITextFormatter formatter) : ILogEventSink
{
	public static event EventHandler<string>? LogReceived;

	public void Emit(LogEvent logEvent)
	{
		using StringWriter writer = new();
		formatter.Format(logEvent, writer);
		LogReceived?.Invoke(this, writer.ToString());
	}
}

public static class LogDisplaySinkExtensions
{
	public static LoggerConfiguration LogDisplay(
		this LoggerSinkConfiguration sinkConfiguration,
		string outputTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
		LogEventLevel restrictedToMinimumLevel = LogEventLevel.Verbose,
		IFormatProvider? formatProvider = null)
	{
		MessageTemplateTextFormatter formatter = new MessageTemplateTextFormatter(outputTemplate, formatProvider);
		return sinkConfiguration.Sink(new LogDisplaySink(formatter), restrictedToMinimumLevel);
	}
}
