using System.Collections.Concurrent;
using System.Globalization;
using Borderlands4SaveDataResigner.Logger.Models;
using CsvHelper;

namespace Borderlands4SaveDataResigner.Logger.Providers;

/// <summary>
/// Provides functionality for logging messages to files with support for log rotation and buffering.
/// </summary>
/// <param name="logsRootDirectory">The root directory where log files will be created and managed.</param>
/// <param name="maxLogFiles">The maximum number of log files to retain in the directory. Older log files will be deleted when this limit is exceeded.</param>
public class FileLogProvider(string logsRootDirectory, int maxLogFiles = 3) : ILogProvider
{
    /// <summary>
    /// Represents a thread-safe queue for storing log entries.
    /// </summary>
    private readonly ConcurrentQueue<LogEntry> _logQueue = new();

    /// <summary>
    /// A max number of log files that can be stored simultaneously.
    /// </summary>
    public int MaxLogFiles { get; } = maxLogFiles;

    /// <summary>
    /// A path where the log files should be stored.
    /// </summary>
    public string LogsRootDirectory { get; } = logsRootDirectory;

    /// <summary>
    /// Maximum buffer size in bytes.
    /// </summary>
    public int MaxBufferSize { get; set; } = 4 * 1024 * 1024;

    /// <summary>
    /// Represents the size of the buffer in use.
    /// </summary>
    private int _currentBufferSize;

    /// <summary>
    /// A name of the current log file.
    /// </summary>
    public string CurrentLogFileName { get; private set; } = null!;

    /// <summary>
    /// Sets a Log File Name with its extension.
    /// </summary>
    /// <returns></returns>
    private void SetCurrentLogFileName()
        => CurrentLogFileName = $"{LogFileNamePrefix}_{DateTime.Now:yyyyMMddHHmmssfff}{LogFileExtension}";

    /// <summary>
    /// Gets a path of the current Log File. 
    /// </summary>
    /// <returns></returns>
    public string GetCurrentLogFilePath()
        => Path.Combine(LogsRootDirectory, CurrentLogFileName);
    
    /// <summary>
    /// A prefix used in log file naming.
    /// </summary>
    public string LogFileNamePrefix { get; set; } = "log";

    /// <summary>
    /// An extension of a log file.
    /// </summary>
    public string LogFileExtension { get; set; } = ".log";

    /// <summary>
    /// Enqueues a log entry into the log queue.
    /// </summary>
    /// <param name="entry">The log entry containing the log level, timestamp, and message to be logged.</param>
    public void Log(LogEntry entry)
    {
        // Enqueue the log entry to the queue
        _logQueue.Enqueue(entry);

        // Check if the buffer size exceeds the maximum limit
        _currentBufferSize += entry.GetSize();
        if (_currentBufferSize <= MaxBufferSize) return;

        // Flush the log queue to the file
        Flush();
        _currentBufferSize = 0;
    }

    /// <summary>
    /// Asynchronously enqueues a log entry into the log queue.
    /// </summary>
    /// <param name="entry">The log entry containing the log level, timestamp, and message to be logged.</param>
    public async Task LogAsync(LogEntry entry)
    {
        _logQueue.Enqueue(entry);
        _currentBufferSize += entry.GetSize();

        if (_currentBufferSize > MaxBufferSize)
        {
            await FlushAsync();
            _currentBufferSize = 0;
        }
    }
    
    /// <summary>
    /// Writes all pending log entries from the queue to the current log file.
    /// </summary>
    public void Flush()
    {
        if (_logQueue.IsEmpty) return;

        var logFilePath = GetCurrentLogFilePath();

        using var writer = new StreamWriter(logFilePath, append: true);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        while (_logQueue.TryDequeue(out var entry))
        {
            csv.WriteRecord(entry);
            csv.NextRecord();
        }
    }

    /// <summary>
    /// Asynchronously writes all pending log entries from the queue to the current log file.
    /// </summary>
    public async Task FlushAsync()
    {
        if (_logQueue.IsEmpty) return;
        
        var logFilePath = GetCurrentLogFilePath();

        await using var fileStream = new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite, 4096, FileOptions.Asynchronous);
        await using var writer = new StreamWriter(fileStream);
        await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        while (_logQueue.TryDequeue(out var entry))
        {
            csv.WriteRecord(entry);
            await csv.NextRecordAsync();
        }
    }

    /// <summary>
    /// Tries to safely delete file located under the given <paramref name="filePath"/>.
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns>True if file has been successfully deleted.</returns>
    public static bool SafelyDeleteFile(string filePath)
    {
        try { File.Delete(filePath); }
        catch { /* ignored */ }
        return !Directory.Exists(filePath);
    }

    /// <summary>
    /// Tries to safely delete many files located under the given <paramref name="filePaths"/>.
    /// </summary>
    /// <param name="filePaths"></param>
    /// <returns></returns>
    private static bool SafelyDeleteFiles(IEnumerable<string> filePaths)
        => filePaths.Aggregate(true, (current, file) => SafelyDeleteFile(file) && current);

    /// <summary>
    /// Retrieves a list of log file paths from the specified logs directory.
    /// </summary>
    /// <returns></returns>
    public List<string> GetLogFileList()
        => Directory.GetFiles(LogsRootDirectory, $"*{LogFileExtension}", SearchOption.TopDirectoryOnly)
            .Where(filePath => Path.GetFileName(filePath).StartsWith(LogFileNamePrefix, StringComparison.OrdinalIgnoreCase))
            .OrderDescending().ToList();

    /// <summary>
    /// Creates a new log file.
    /// </summary>
    public void CreateLogFile()
    {
        // get all log files
        var logFiles = GetLogFileList();

        // delete the oldest log file(s) if the logs limit is reached
        if (MaxLogFiles > 0)
        {
            if (logFiles.Count == MaxLogFiles)
                _ = SafelyDeleteFile(logFiles.Last());
            else if (logFiles.Count > MaxLogFiles)
                _ = SafelyDeleteFiles(logFiles.TakeLast(logFiles.Count - MaxLogFiles + 1));
        }

        // update the path to the current log
        SetCurrentLogFileName();

        // append header to the log file
        var logFilePath = GetCurrentLogFilePath();
        using var writer = new StreamWriter(logFilePath);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        csv.WriteHeader<LogEntry>();
        csv.NextRecord();
    }
}
