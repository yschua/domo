using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Display;

namespace domo.Data;

public class LogViewerSink : ILogEventSink
{
    private readonly LogViewer _logViewer;
    private readonly MessageTemplateTextFormatter _formatter;

    public LogViewerSink(LogViewer logViewer, string outputTemplate)
    {
        _logViewer = logViewer;
        _formatter = new MessageTemplateTextFormatter(outputTemplate);
    }

    public void Emit(LogEvent logEvent)
    {
        using var writer = new StringWriter();
        _formatter.Format(logEvent, writer);
        _logViewer.Publish(writer.ToString());
    }
}

public static class LogViewerSinkExtensions
{
    public static LoggerConfiguration LogViewerSink(
        this LoggerSinkConfiguration loggerSinkConfiguration,
        LogViewer logViewer,
        string outputTemplate)
    {
        return loggerSinkConfiguration.Sink(new LogViewerSink(logViewer, outputTemplate));
    }
}