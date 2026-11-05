using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace ServiceRequests.IntegrationTests.Infrastructure;

/// <summary>Test-only: keeps the text of every SQL command EF Core executed, from its own log.</summary>
public sealed class SqlRecorder : ILoggerProvider
{
    private readonly ConcurrentQueue<string> _commands = new();

    public IReadOnlyCollection<string> Commands => _commands;

    public ILogger CreateLogger(string categoryName) => new Recorder(_commands);

    public void Dispose()
    {
    }

    private sealed class Recorder(ConcurrentQueue<string> commands) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (eventId.Id == RelationalEventId.CommandExecuted.Id)
            {
                commands.Enqueue(formatter(state, exception));
            }
        }
    }
}
