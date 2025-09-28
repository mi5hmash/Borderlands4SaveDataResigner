using System.Diagnostics;

namespace Borderlands4SaveDataResignerCore.Infrastructure;

/// <summary>
/// Provides utility methods and properties for managing application directory paths, including the root and output directories.
/// </summary>
public static class Directories
{
    public static string RootPath => AppDomain.CurrentDomain.BaseDirectory;

    public static string Output { get; } = Path.Combine(RootPath, "_OUTPUT");

    public static void CreateOutput() => Directory.CreateDirectory(Output);

    /// <summary>
    /// Creates all required output resources for the application.
    /// </summary>
    public static void CreateAll()
    {
        CreateOutput();
    }

    /// <summary>
    /// Generates a new output directory path using the current date and time, combined with the specified action name.
    /// </summary>
    /// <param name="action">The name of the action to include in the output directory path.</param>
    /// <returns>A string representing the full path of the new output directory, formatted with the current date, time, and the specified action.</returns>
    public static string GetNewOutputDirectory(string action) 
        => Path.Combine(Output, $"{DateTime.Now:yyyy-MM-dd_HHmmssfff}_{action}");

    /// <summary>
    /// Creates the output folder structure in the specified directory for the given user, mirroring the parent directories of the input files to be processed.
    /// </summary>
    /// <param name="inputRootPath">The root path of the input directory tree. Used to determine the relative structure of folders to replicate in the output directory.</param>
    /// <param name="outputDirectory">The base directory where the output folder structure will be created.</param>
    /// <param name="filesToProcess">An array of file paths representing the files to process. The parent directories of these files are used to construct the output folder structure.</param>
    /// <param name="userId">The identifier for the user. The output folder structure will be created under a subdirectory named after this user.</param>
    public static void CreateOutputFolderStructure(string inputRootPath, string outputDirectory, string[] filesToProcess, string userId)
    {
        var uniqueParentDirectories = filesToProcess
            .Select(Path.GetDirectoryName)
            .Where(dir => dir != null)
            .Distinct()
            .Select(dir => dir?.Replace(inputRootPath, Path.Combine(outputDirectory, userId)))
            .ToArray();
        foreach (var dir in uniqueParentDirectories)
        {
            if (dir == null) continue;
            Directory.CreateDirectory(dir);
        }
    }

    /// <summary>
    /// Opens the specified directory in the system's default file explorer, if the directory exists.
    /// </summary>
    /// <param name="path">The full path of the directory to open.</param>
    public static void OpenDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        string? openCmd = null;
        string? args = null;

        if (Directory.Exists(path)) 
        {
            if (OperatingSystem.IsWindows()) 
            {
                openCmd = "explorer.exe";
                args = $"\"{path}\"";
            }
            else if (OperatingSystem.IsMacOS()) 
            {
                openCmd = "open";
                args = $"\"{path}\"";
            }
            else if (OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD()) 
            {
                openCmd = "xdg-open";
                args = $"\"{path}\"";
            }
        }

        if (openCmd != null && args != null)
            Process.Start(new ProcessStartInfo
            {
                FileName = openCmd,
                Arguments = args,
                UseShellExecute = false
            });
    }
}