using Borderlands4SaveDataResigner.Logger.Models;

namespace Borderlands4SaveDataResigner.Logger.Providers;

/// <summary>
/// Provides a logging implementation that writes log messages to the console.
/// </summary>
public class ConsoleLogProvider : ILogProvider
{
    /// <summary>
    /// Logs a message with the specified log entry details.
    /// </summary>
    /// <param name="entry"></param>
    public void Log(LogEntry entry)
    {
        Console.WriteLine(string.IsNullOrEmpty(entry.Group)
            ? $"[{entry.LogLevel}] {entry.Timestamp}: {entry.Message}"
            : $"[{entry.LogLevel}] {entry.Timestamp}: [Group: {entry.Group}] {entry.Message}");
    }

    public async Task LogAsync(LogEntry entry)
    {
        Log(entry);
        await Task.CompletedTask;
    }

    public void Flush()
    {
        // No action needed for console logging, as it is immediate.
    }

    public Task FlushAsync()
    {
        // No action needed for console logging, as it is immediate.
        return Task.CompletedTask;
    }
}