using Microsoft.Extensions.Logging;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Infrastructure.Hosting.Common.UnitTests.Recording;

public class MockLogger : ILogger
{
    private readonly List<LogItem> _items = new();

    public IReadOnlyList<LogItem> Items => _items;

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull
    {
        return null;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        _items.Add(new LogItem
            { Level = logLevel, Exception = exception, Message = formatter(state, exception) });
    }

    public void Reset()
    {
        _items.Clear();
    }
}

public class LogItem
{
    public Exception? Exception { get; set; }

    public LogLevel Level { get; set; }

    public string? Message { get; set; }
}