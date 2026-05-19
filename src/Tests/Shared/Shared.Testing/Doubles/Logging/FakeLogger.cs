using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Shared.Testing.Doubles.Logging;

public sealed class FakeLogger : ILogger
{
    private readonly ConcurrentQueue<LogEntry> _entries = new();

    public IReadOnlyCollection<LogEntry> Entries => _entries;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var entry = new LogEntry(
            logLevel,
            eventId,
            formatter(state, exception),
            exception,
            state?.ToString() ?? string.Empty);
        _entries.Enqueue(entry);
    }

    public bool IsEnabled(LogLevel logLevel) => true;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public void Clear() => _entries.Clear();
}

public sealed record LogEntry(
    LogLevel Level,
    EventId EventId,
    string Message,
    Exception? Exception,
    string State);

public sealed class FakeLogger<T> : ILogger<T>
{
    private readonly FakeLogger _inner;

    public FakeLogger(FakeLogger inner) => _inner = inner;

    public IReadOnlyCollection<LogEntry> Entries => _inner.Entries;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        => _inner.Log(logLevel, eventId, state, exception, formatter);

    public bool IsEnabled(LogLevel logLevel) => _inner.IsEnabled(logLevel);

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => _inner.BeginScope(state);

    public void Clear() => _inner.Clear();
}
