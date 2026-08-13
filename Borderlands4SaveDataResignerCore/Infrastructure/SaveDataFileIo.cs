using static Borderlands4SaveDataResignerCore.Deencryption.Bl4DeencryptorHelper;

namespace Borderlands4SaveDataResignerCore.Infrastructure;

public static class SaveDataFileIo
{
    /// <summary>
    /// Gets all files with the save data file extension from the input directory.
    /// </summary>
    /// <param name="inputDir">The path to the directory containing the files to process.</param>
    /// <returns>An array of file paths matching the save data file extension.</returns>
    /// <exception cref="FileNotFoundException">Thrown when no files are found in the specified directory.</exception>
    public static string[] GetSaveFiles(string inputDir)
    {
        var filesToProcess = Directory.GetFiles(inputDir, $"*{SaveFileExtension}", SearchOption.TopDirectoryOnly);
        return filesToProcess.Length == 0
            ? throw new FileNotFoundException("No files found to process.")
            : filesToProcess;
    }
    
    /// <summary>
    /// Gets all files with the YAML file extension from the input directory.
    /// </summary>
    /// <param name="inputDir">The path to the directory containing the files to process.</param>
    /// <returns>An array of file paths matching the YAML file extension.</returns>
    /// <exception cref="FileNotFoundException">Thrown when no files are found in the specified directory.</exception>
    public static string[] GetYamlFiles(string inputDir)
    {
        var filesToProcess = Directory.GetFiles(inputDir, $"*{YamlFileExtension}", SearchOption.TopDirectoryOnly);
        return filesToProcess.Length == 0
            ? throw new FileNotFoundException("No files found to process.")
            : filesToProcess;
    }

    /// <summary>
    /// Gets the first save data file found in the specified input path, which can be either a file or a directory.
    /// </summary>
    /// <param name="inputPath">The path to the file or directory to search for save data files.</param>
    /// <returns>The path to the first save data file found.</returns>
    /// <exception cref="DirectoryNotFoundException">Thrown when the input path is not a valid file or directory.</exception>
    /// <exception cref="FileNotFoundException">Thrown when no save data files are found in the specified directory.</exception>
    public static string GetSaveFile(string inputPath)
    {
        if (File.Exists(inputPath))
            return inputPath;
        if (!Directory.Exists(inputPath))
            throw new DirectoryNotFoundException($"The provided path '{inputPath}' is not a valid file or directory.");
        var filesToProcess = Directory.GetFiles(inputPath, $"*{SaveFileExtension}", SearchOption.TopDirectoryOnly);
        if (filesToProcess.Length == 0)
            throw new FileNotFoundException($"No '{SaveFileExtension}' files found in the provided directory '{inputPath}'.");
        return filesToProcess.FirstOrDefault() ?? string.Empty;
    }
}