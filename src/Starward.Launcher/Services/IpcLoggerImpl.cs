using System;
using dotnetCampus.Ipc.Pipes;
using dotnetCampus.Ipc.Utils.Logging;
using Microsoft.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Starward.Launcher.Services;

internal class IpcLoggerImpl(string name, ILogger<IpcProvider> impl) : IpcLogger(name), ILogger
{
    void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        impl.Log(logLevel, eventId, state, exception, formatter);
    }

    bool ILogger.IsEnabled(LogLevel logLevel)
    {
        return impl.IsEnabled(logLevel);
    }

    IDisposable? ILogger.BeginScope<TState>(TState state)
    {
        return impl.BeginScope(state);
    }
}