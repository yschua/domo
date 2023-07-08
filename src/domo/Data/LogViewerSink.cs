using Serilog;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace domo.Data;

public class LogViewerSink : ILogEventSink
{
    private readonly LogViewer _logViewer;

    public LogViewerSink(LogViewer logViewer)
    {
        _logViewer = logViewer;
    }

    public void Emit(LogEvent logEvent)
    {
        var level = logEvent.Level.ToString().Substring(0, 3).ToUpper();
        var message = $"[{logEvent.Timestamp} {level}] {logEvent.RenderMessage()}";
        _logViewer.Publish(message);
    }
}

public static class LogViewerSinkExtensions
{
    public static LoggerConfiguration LogViewerSink(
        this LoggerSinkConfiguration loggerSinkConfiguration,
        LogViewer logViewer)
    {
        return loggerSinkConfiguration.Sink(new LogViewerSink(logViewer));
    }
}