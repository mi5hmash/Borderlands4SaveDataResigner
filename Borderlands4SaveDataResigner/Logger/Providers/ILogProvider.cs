using Borderlands4SaveDataResigner.Logger.Models;

namespace Borderlands4SaveDataResigner.Logger.Providers;

/// <summary>
/// Defines a provider for logging messages and events.
/// </summary>
public interface ILogProvider
{
    void Log(LogEntry entry);
    Task LogAsync(LogEntry entry);
    void Flush();
    Task FlushAsync();
}