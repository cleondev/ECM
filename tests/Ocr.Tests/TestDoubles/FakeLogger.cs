using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Ocr.Tests.TestDoubles;

internal sealed class FakeLogger<T> : ILogger<T>
{
    private static readonly NullScope Scope = new();

    public ConcurrentQueue<LogEntry> Entries { get; } = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => Scope;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);
        Entries.Enqueue(new LogEntry(logLevel, formatter(state, exception), exception));
    }

    internal readonly record struct LogEntry(LogLevel Level, string Message, Exception? Exception);

    private sealed class NullScope : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
