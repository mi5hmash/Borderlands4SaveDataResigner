using System.Diagnostics.CodeAnalysis;
using CsvHelper.Configuration.Attributes;
using CsvHelper.TypeConversion;

namespace Borderlands4SaveDataResigner.Logger.Models;

/// <summary>
/// Represents a log entry containing information about an event, including its severity, timestamp, message, and optional grouping.
/// </summary>
/// <remarks>This class is used to encapsulate details about a single log event, such as its severity
/// level, associated message, and optional grouping for categorization. It provides constructors for creating log
/// entries with varying levels of detail  and supports equality comparison and stable hash code generation.</remarks>
public class LogEntry
{
    /// <summary>
    /// The timestamp of the log entry.
    /// </summary>
    [TypeConverter(typeof(DateTimeConverter))]
    [Format("yyyy-MM-dd HH:mm:ss.fff")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The severity level for logging messages.
    /// </summary>
    public SimpleLogger.LogSeverity LogLevel { get; set; }

    /// <summary>
    /// The name of the group associated with the entity.
    /// </summary>
    public string? Group { get; set; }

    /// <summary>
    /// The message associated with the current operation.
    /// </summary>
    public string Message { get; set; } = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="LogEntry"/> class with the specified log severity and message.
    /// </summary>
    /// <param name="logLevel">The severity level of the log entry.</param>
    /// <param name="message">The message associated with the log entry. Cannot be null.</param>
    public LogEntry(SimpleLogger.LogSeverity logLevel, string message)
    {
        LogLevel = logLevel;
        Message = message;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LogEntry"/> class with the specified group, log level, and message.
    /// </summary>
    /// <remarks>Use this constructor to create a log entry with specific details, such as grouping
    /// logs by category or specifying the severity level for filtering purposes.</remarks>
    /// <param name="group">The group or category associated with the log entry. This value cannot be null or empty.</param>
    /// <param name="logLevel">The severity level of the log entry, indicating its importance or urgency.</param>
    /// <param name="message">The message content of the log entry. This value cannot be null or empty.</param>
    public LogEntry(string group, SimpleLogger.LogSeverity logLevel, string message)
    {
        Group = group;
        LogLevel = logLevel;
        Message = message;
    }

    /// <summary>
    /// Default constructor for CsvHelper
    /// </summary>
    public LogEntry() { }

    /// <summary>
    /// Calculates the total size of the log entry in bytes.
    /// </summary>
    /// <returns>The total size of the log entry in bytes.</returns>
    public int GetSize()
        => 8 + sizeof(SimpleLogger.LogSeverity) + (Group?.Length ?? 0) + Message.Length;

    public int GetHashCodeStable()
        => HashCode.Combine(Timestamp, LogLevel, Group, Message);

    // This is a workaround to avoid the default GetHashCode() implementation in objects where all fields are mutable.
    private readonly Guid _uniqueId = Guid.NewGuid();
    public override int GetHashCode()
        => _uniqueId.GetHashCode();

    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is LogEntry castedObj && Equals(castedObj);

    public static bool operator ==(LogEntry left, LogEntry right)
        => left.Equals(right);

    public static bool operator !=(LogEntry left, LogEntry right)
        => !(left == right);
}