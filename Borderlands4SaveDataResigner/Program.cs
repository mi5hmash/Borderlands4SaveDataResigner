using System.Text.RegularExpressions;
using Borderlands4SaveDataResigner.Helpers;
using Borderlands4SaveDataResigner.Infrastructure;
using Borderlands4SaveDataResigner.Logger;
using Borderlands4SaveDataResigner.Logger.Models;
using Borderlands4SaveDataResigner.Logger.Providers;

#region SETUP

// CONSTANTS
const string breakLine = "---";

// App Specific CONSTANTS
const string saveFileExtension = ".sav";
const string yamlFileExtension = ".yaml";

// Initialize APP_INFO
var appInfo = new MyAppInfo("Borderlands4SaveDataResigner");

// Create DIRECTORIES
var directories = new Directories();
directories.CreateAll();

// Initialize LOGGER
var logger = new SimpleLogger
{
    LoggedAppName = appInfo.Name,
    LoggedAppVersion = new Version(MyAppInfo.Version)
};
// Configure ConsoleLogProvider
var consoleLogProvider = new ConsoleLogProvider();
logger.AddProvider(consoleLogProvider);
// Configure FileLogProvider
var fileLogProvider = new FileLogProvider(MyAppInfo.RootPath, 2);
fileLogProvider.CreateLogFile();
logger.AddProvider(fileLogProvider);
// Add event handler for unhandled exceptions
AppDomain.CurrentDomain.UnhandledException += (_, e) =>
{
    if (e.ExceptionObject is not Exception exception) return;
    var logEntry = new LogEntry(SimpleLogger.LogSeverity.Critical, $"Unhandled Exception: {exception}");
    fileLogProvider.Log(logEntry);
    fileLogProvider.Flush();
};
// Flush log providers on process exit
AppDomain.CurrentDomain.ProcessExit += (_, _)
    => logger.Flush();

// Print HEADER
ConsoleHelper.PrintHeader(appInfo, breakLine);

// Say HELLO
ConsoleHelper.SayHello(breakLine);
Console.WriteLine("So, you want to hear another story, huh?");

// Get ARGUMENTS from command line
#if DEBUG
// For debugging purposes, you can manually set the arguments...
if (args.Length < 1)
{
    // ...below
    const string localArgs = "-m TEST";

    var matches = new Regex("""[\"].+?[\"]|[\'].+?[\']|[\`].+?[\`]|[^ ]+""").Matches(localArgs);
    args = matches.Select(m => m.Value.Trim('"')).ToArray();
}
#endif
var arguments = ConsoleHelper.ReadArguments(args);
#if DEBUG
// Write the arguments to the console for debugging purposes
ConsoleHelper.WriteArguments(arguments);
Console.WriteLine(breakLine);
#endif

#endregion

#region MAIN

// Optional argument: isVerbose
var isVerbose = arguments.ContainsKey("-v");

// Get list of FILES to PROCESS
arguments.TryGetValue("-p", out var inputRootPath);
string[] filesToProcess;
if (!Directory.Exists(inputRootPath)) 
    throw new DirectoryNotFoundException($"The provided path '{inputRootPath}' is not a valid directory or does not exist.");
// Get MODE
arguments.TryGetValue("-m", out var mode);
switch (mode)
{
    case "decrypt" or "d":
        // USAGE: -m d -p "FILE_PATH" -u 76561197960265729
        DecryptAll();
        break;
    case "encrypt" or "e":
        // USAGE: -m e -p "FILE_PATH" -u 76561197960265730
        EncryptAll();
        break;
    case "resign" or "r":
        // USAGE: -m r -p "FILE_PATH" -uI 76561197960265729 -uO 76561197960265730
        ResignAll();
        break;
    default:
        throw new ArgumentException($"Unknown mode '{mode}'.", nameof(mode));
}

// EXIT the application
Console.WriteLine(breakLine); // print a break line
ConsoleHelper.SayGoodbye(breakLine);
#if DEBUG
ConsoleHelper.PressAnyKeyToExit();
#else
if (isVerbose) ConsoleHelper.PressAnyKeyToExit();
#endif

return;

#endregion

#region MODES

void DecryptAll()
{
    arguments.TryGetValue("-u", out var userId);
    if (string.IsNullOrEmpty(userId))
        throw new ArgumentException("Input User ID is missing.", nameof(userId));
    filesToProcess = Directory.GetFiles(inputRootPath, $"*{saveFileExtension}", SearchOption.TopDirectoryOnly);
    if (filesToProcess.Length == 0) return;
    logger.LogInfo($"Decrypting [{filesToProcess.Length}] files...");
    // Calculate the private key based on the provided user ID
    var privateKey = Bl4Deencryptor.CalculatePrivateKey(userId);
    // DECRYPT
    // Create a new folder in OUTPUT directory
    var outputDir = GetNewOutputDirectory(directories.Output, "decrypted");
    // Crate the folder structure in the newly created output directory
    CreateOutputFolderStructure(inputRootPath, outputDir, filesToProcess, userId);
    // Setup parallel options
    CancellationTokenSource cts = new();
    var po = GetParallelOptions(cts);
    // Process files in parallel
    Parallel.For((long)0, filesToProcess.Length, po, (ctr, _) =>
    {
        var fileName = Path.GetFileName(filesToProcess[ctr]);
        var group = $"Task {ctr}";
        ReadOnlySpan<byte> inputDataSpan = File.ReadAllBytes(filesToProcess[ctr]);
        logger.LogInfo($"Decrypting [{fileName}] file...", group);
        byte[] outputDataSpan;
        try
        {
            outputDataSpan = Bl4Deencryptor.DecryptData(inputDataSpan, privateKey);
        }
        catch (Exception e)
        {
            logger.LogError($"Failed to decrypt the [{fileName}] file: {e}", group);
            return; // Skip to the next file
        }
        // Save the decrypted data to the output directory, preserving the folder structure and change the file extension to .yaml
        var outputFilePath = filesToProcess[ctr].Replace(inputRootPath, Path.Combine(outputDir, userId));
        outputFilePath = Path.ChangeExtension(outputFilePath, yamlFileExtension);
        File.WriteAllBytes(outputFilePath, outputDataSpan);
        logger.LogInfo($"Decrypted [{fileName}] file.", group);
    });
}

void EncryptAll()
{
    arguments.TryGetValue("-u", out var userId);
    if (string.IsNullOrEmpty(userId))
        throw new ArgumentException("Output User ID is missing.", nameof(userId));
    filesToProcess = Directory.GetFiles(inputRootPath, $"*{yamlFileExtension}", SearchOption.TopDirectoryOnly);
    if (filesToProcess.Length == 0) return;
    logger.LogInfo($"Encrypting [{filesToProcess.Length}] files...");
    // Calculate the private key based on the provided user ID
    var privateKey = Bl4Deencryptor.CalculatePrivateKey(userId);
    // ENCRYPT
    // Create a new folder in OUTPUT directory
    var outputDir = GetNewOutputDirectory(directories.Output, "encrypted");
    // Crate the folder structure in the newly created output directory
    CreateOutputFolderStructure(inputRootPath, outputDir, filesToProcess, userId);
    // Setup parallel options
    CancellationTokenSource cts = new();
    var po = GetParallelOptions(cts);
    // Process files in parallel
    Parallel.For((long)0, filesToProcess.Length, po, (ctr, _) =>
    {
        var fileName = Path.GetFileName(filesToProcess[ctr]);
        var group = $"Task {ctr}";
        ReadOnlySpan<byte> inputDataSpan = File.ReadAllBytes(filesToProcess[ctr]);
        logger.LogInfo($"Encrypting [{fileName}] file...", group);
        byte[] outputDataSpan;
        try
        {
            outputDataSpan = Bl4Deencryptor.EncryptData(inputDataSpan, privateKey);
        }
        catch (Exception e)
        {
            logger.LogError($"Failed to encrypt the [{fileName}] file: {e}", group);
            return; // Skip to the next file
        }
        // Save the encrypted data to the output directory, preserving the folder structure and change the file extension to .sav
        var outputFilePath = filesToProcess[ctr].Replace(inputRootPath, Path.Combine(outputDir, userId));
        outputFilePath = Path.ChangeExtension(outputFilePath, saveFileExtension);
        File.WriteAllBytes(outputFilePath, outputDataSpan);
        logger.LogInfo($"Encrypted [{fileName}] file.", group);
    });
}

void ResignAll()
{
    arguments.TryGetValue("-uI", out var userIdInput);
    if (string.IsNullOrEmpty(userIdInput))
        throw new ArgumentException("Input User ID is missing.", nameof(userIdInput));
    arguments.TryGetValue("-uO", out var userIdOutput);
    if (string.IsNullOrEmpty(userIdOutput))
        throw new ArgumentException("Output User ID is missing.", nameof(userIdOutput));
    filesToProcess = Directory.GetFiles(inputRootPath, $"*{saveFileExtension}", SearchOption.TopDirectoryOnly);
    if (filesToProcess.Length == 0) return;
    logger.LogInfo($"Resigning [{filesToProcess.Length}] files...");
    // Calculate the private key based on the provided user ID
    var privateKey = Bl4Deencryptor.CalculatePrivateKey(userIdInput);
    // RESIGN
    // Create a new folder in OUTPUT directory
    var outputDir = GetNewOutputDirectory(directories.Output, "resigned");
    // Crate the folder structure in the newly created output directory
    CreateOutputFolderStructure(inputRootPath, outputDir, filesToProcess, userIdOutput);
    // Setup parallel options
    CancellationTokenSource cts = new();
    var po = GetParallelOptions(cts);
    // Process files in parallel
    Parallel.For((long)0, filesToProcess.Length, po, (ctr, _) =>
    {
        var fileName = Path.GetFileName(filesToProcess[ctr]);
        var group = $"Task {ctr}";
        ReadOnlySpan<byte> encryptedDataSpan = File.ReadAllBytes(filesToProcess[ctr]);
        // Decrypt the data with the old user's private key
        logger.LogInfo($"Decrypting [{fileName}] file...", group);
        byte[] decryptedData;
        try
        {
            decryptedData = Bl4Deencryptor.DecryptData(encryptedDataSpan, privateKey);
        }
        catch (Exception e)
        {
            logger.LogError($"Failed to decrypt the [{fileName}] file: {e}", group);
            return; // Skip to the next file
        }
        // Update GUIDs in the decrypted data
        var decryptedDataWithUpdatedGuids = Bl4Deencryptor.UpdateGuids(decryptedData, Path.GetFileNameWithoutExtension(fileName), logger, group);
        // Encrypt the updated data with the new user's private key
        logger.LogInfo($"Encrypting [{fileName}] file...", group);
        privateKey = Bl4Deencryptor.CalculatePrivateKey(userIdOutput);
        byte[] resignedData;
        try
        {
            resignedData = Bl4Deencryptor.EncryptData(decryptedDataWithUpdatedGuids, privateKey);
        }
        catch (Exception e)
        {
            logger.LogError($"Failed to encrypt the [{fileName}] file: {e}", group);
            return; // Skip to the next file
        }
        // Save the resigned data to the output directory, preserving the folder structure
        var outputFilePath = filesToProcess[ctr].Replace(inputRootPath, Path.Combine(outputDir, userIdOutput));
        File.WriteAllBytes(outputFilePath, resignedData);
        logger.LogInfo($"Resigned [{fileName}] file.", group);
    });
}

#endregion

#region LOCAL_HELPERS

static string GetNewOutputDirectory(string rootPath, string action) => Path.Combine(rootPath, $"{DateTime.Now:yyyy-MM-dd_HHmmssfff}_{action}");

static void CreateOutputFolderStructure(string inputRootPath, string outputDirectory, string[] filesToProcess, string userId)
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

static ParallelOptions GetParallelOptions(CancellationTokenSource cts) =>
    new()
    {
        CancellationToken = cts.Token,
        MaxDegreeOfParallelism = Math.Max(Environment.ProcessorCount - 1, 1)
    };

#endregion