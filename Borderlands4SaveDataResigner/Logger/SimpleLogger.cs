using System.Runtime.InteropServices;
using Borderlands4SaveDataResigner.Logger.Models;
using Borderlands4SaveDataResigner.Logger.Providers;

namespace Borderlands4SaveDataResigner.Logger;

public class SimpleLogger
{
    /// <summary>
    /// SimpleLogger version.
    /// </summary>
    private const string Version = "2.0.0";

    /// <summary>
    /// Stores the name of the logged app.
    /// </summary>
    public string LoggedAppName { get; set; } = "App";

    /// <summary>
    /// Stores the version of the logged app.
    /// </summary>
    public Version LoggedAppVersion { get; set; } = new(0, 0, 0, 0);

    /// <summary>
    /// A private lock object used to synchronize access to shared resources.
    /// </summary>
    private readonly Lock _lockObject = new();

    #region LOG_PROVDERS

    /// <summary>
    /// A collection of log providers used to handle logging operations.
    /// </summary>
    private readonly List<ILogProvider> _logProviders = [];

    /// <summary>
    /// Adds a log provider to the collection of log providers.
    /// </summary>
    /// <param name="provider">The log provider to add.</param>
    public void AddProvider(ILogProvider provider)
    {
        var scope = _lockObject.EnterScope();

        _logProviders.Add(provider);
        if (provider is FileLogProvider)
            provider.Log(CreateLogHeader());
        scope.Dispose();
    }

    /// <summary>
    /// Removes all log providers from the collection.
    /// </summary>
    public void ClearProviders()
    {
        var scope = _lockObject.EnterScope();

        _logProviders.Clear();

        scope.Dispose();
    }

    #endregion

    #region OS_PLATFORM

    /// <summary>
    /// Gets the OS platform.
    /// </summary>
    /// <returns></returns>
    private static string GetOsPlatform()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return "Windows";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return "Linux";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD)) return "FreeBSD";
        return RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "MacOS" : "Unknown";
    }

    #endregion

    #region SEVERITY_LEVEL

    /// <summary>
    /// Log severity enumerator.
    /// </summary>
    public enum LogSeverity
    {
        Trace,
        Debug,
        Info,
        Warning,
        Error,
        Critical
    }

    /// <summary>
    /// Minimum severity level of the messages to include in the log.
    /// </summary>
    public LogSeverity MinSeverityLevel { get; set; } = LogSeverity.Trace;

    #endregion

    #region LOG_METHODS

    /// <summary>
    /// Creates a first Log Entry.
    /// </summary>
    /// <returns></returns>
    private LogEntry CreateLogHeader()
        => new(LogSeverity.Info, $"Log created with SimpleLogger v{Version} by Mi5hmasH.\nLogged app: {LoggedAppName} v{LoggedAppVersion} | OSPlatform: {GetOsPlatform()}");

    /// <summary>
    /// Logs the specified log entry to all configured log providers.
    /// </summary>
    /// <param name="severity"></param>
    /// <param name="message"></param>
    /// <param name="group"></param>
    private void Log(LogSeverity severity, string message, string? group = null)
    {
        if ((int)severity < (int)MinSeverityLevel) return;

        var entry = string.IsNullOrEmpty(group)
            ? new LogEntry(severity, message)
            : new LogEntry(group, severity, message);

        var scope = _lockObject.EnterScope();

        _logProviders.ForEach(provider => provider.Log(entry));

        scope.Dispose();
    }

    /// <summary>
    /// Logs a trace-level message, optionally associating it with a specific group.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="group">An optional group identifier to associate with the log entry.</param>
    public void LogTrace(string message, string? group = null)
        => Log(LogSeverity.Trace, message, group);

    /// <summary>
    /// Logs a debug-level message, optionally associating it with a specific group.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="group">An optional group identifier to associate with the log entry.</param>
    public void LogDebug(string message, string? group = null)
        => Log(LogSeverity.Debug, message, group);

    /// <summary>
    /// Logs an info-level message, optionally associating it with a specific group.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="group">An optional group identifier to associate with the log entry.</param>
    public void LogInfo(string message, string? group = null)
        => Log(LogSeverity.Info, message, group);

    /// <summary>
    /// Logs a warning-level message, optionally associating it with a specific group.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="group">An optional group identifier to associate with the log entry.</param>
    public void LogWarning(string message, string? group = null)
        => Log(LogSeverity.Warning, message, group);

    /// <summary>
    /// Logs an error-level message, optionally associating it with a specific group.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="group">An optional group identifier to associate with the log entry.</param>
    public void LogError(string message, string? group = null)
        => Log(LogSeverity.Error, message, group);

    /// <summary>
    /// Logs a critical-level message, optionally associating it with a specific group.
    /// </summary>
    /// <param name="message">The message to log.</param>
    /// <param name="group">An optional group identifier to associate with the log entry.</param>
    public void LogCritical(string message, string? group = null)
        => Log(LogSeverity.Critical, message, group);

    /// <summary>
    /// Flushes all log providers, ensuring that any buffered log entries are written to their respective outputs.
    /// </summary>
    public void Flush()
    {
        var scope = _lockObject.EnterScope();

        _logProviders.ForEach(provider => provider.Flush());

        scope.Dispose();
    }

    /// <summary>
    /// Flushes all log providers asynchronously, ensuring that any buffered log entries are written to their respective outputs.
    /// </summary>
    public async Task FlushAsync()
    {
        foreach (var provider in _logProviders)
            await provider.FlushAsync();
    }

    #endregion
}