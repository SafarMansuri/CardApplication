using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace CardApplicationAPI.Tests.Infrastructure;

public sealed class TestErrorLog : ILoggerProvider
{
    private readonly ConcurrentQueue<string> errors = new();
    public string Details => string.Join(Environment.NewLine, errors);
    public ILogger CreateLogger(string categoryName) => new ErrorLogger(errors);
    public void Dispose() { }

    private sealed class ErrorLogger(ConcurrentQueue<string> errors) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Error;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel >= LogLevel.Error && exception != null)
                errors.Enqueue(exception.ToString());
        }
    }
}
