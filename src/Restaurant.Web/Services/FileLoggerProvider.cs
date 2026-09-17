using Microsoft.Extensions.Logging;

namespace Restaurant.Web.Services;

public class FileLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(categoryName);
    }

    public void Dispose() { }
}

public class FileLogger : ILogger
{
    private readonly string _categoryName;

    public FileLogger(string categoryName)
    {
        _categoryName = categoryName;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;
        
        var message = formatter(state, exception);
        var log = $"[{DateTime.UtcNow}] [{logLevel}] {_categoryName}: {message}\n";
        if (exception != null)
        {
            log += exception.ToString() + "\n";
        }
        
        try
        {
            System.IO.File.AppendAllText("blazor_errors.log", log);
        }
        catch { }
    }
}
