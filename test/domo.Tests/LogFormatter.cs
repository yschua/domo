using Divergic.Logging.Xunit;
using Microsoft.Extensions.Logging;
using System.Text;

namespace domo.Tests;

public class LogFormatter : ILogFormatter
{
    public string Format(
        int scopeLevel,
        string categoryName,
        LogLevel logLevel,
        EventId eventId,
        string message,
        Exception exception)
    {
        var builder = new StringBuilder();

        builder.Append($"[{DateTime.Now:s.fff}] ");

        if (!string.IsNullOrEmpty(message))
        {
            builder.Append(message);
        }

        if (exception != null)
        {
            builder.Append($"\n{exception}");
        }

        return builder.ToString();
    }
}

public class TestLoggingConfig : LoggingConfig
{
    public TestLoggingConfig()
    {
        Formatter = new LogFormatter();
    }

    public static TestLoggingConfig Current { get; } = new TestLoggingConfig();
}