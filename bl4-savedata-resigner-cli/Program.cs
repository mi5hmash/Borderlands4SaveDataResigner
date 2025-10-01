using Borderlands4SaveDataResignerCore;
using Borderlands4SaveDataResignerCore.Helpers;
using Mi5hmasH.AppInfo;
using Mi5hmasH.ConsoleHelper;
using Mi5hmasH.Logger;
using Mi5hmasH.Logger.Models;
using Mi5hmasH.Logger.Providers;

#region SETUP

// CONSTANTS
const string breakLine = "---";

// Initialize APP_INFO
var appInfo = new MyAppInfo("bl4-savedata-resigner-cli");

// Initialize LOGGER
var logger = new SimpleLogger
{
    LoggedAppName = appInfo.Name
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
AppDomain.CurrentDomain.ProcessExit += (_, _) => logger.Flush();

//Initialize ProgressReporter
var progressReporter = new ProgressReporter(new Progress<string>(Console.WriteLine), null);

// Initialize CORE
var core = new Core(logger, progressReporter);

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
    args = ConsoleHelper.GetArgs(localArgs);
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

// Show HELP if no arguments are provided or if -h is provided
if (arguments.Count == 0 || arguments.ContainsKey("-h"))
{
    PrintHelp();
    return;
}

// Optional argument: isVerbose
var isVerbose = arguments.ContainsKey("-v");

// Get MODE
arguments.TryGetValue("-m", out var mode);
switch (mode)
{
    case "decrypt" or "d":
        DecryptAll();
        break;
    case "encrypt" or "e":
        EncryptAll();
        break;
    case "resign" or "r":
        ResignAll();
        break;
    case "bruteforce" or "b":
        BruteforceSteamId();
        break;
    default:
        throw new ArgumentException($"Unknown mode: '{mode}'.");
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

#region HELPERS

static void PrintHelp()
{
    const string userIdInput = "76561197960265729";
    const string userIdOutput = "76561197960265730";
    var inputPath = Path.Combine(".", "InputDirectory");
    var gpPath = Path.Combine(inputPath, "1.sav");
    var exeName = Path.Combine(".", Path.GetFileName(Environment.ProcessPath) ?? "ThisExecutableFileName.exe");
    var helpMessage = $"""
                        Usage: {exeName} -m <mode> [options]

                        Modes:
                          -m d  Decrypt SaveData files
                          -m e  Encrypt SaveData files
                          -m r  Re-sign SaveData files
                          -m b  Bruteforce Steam ID for a SaveData file

                        Options:
                          -p <path>     Path to folder containing SaveData files or path to a single SaveData file (used in Bruteforce mode)
                          -u <user_id>  User ID (used in decrypt/encrypt modes)
                          -uI <old_id>  Original User ID (used in re-sign mode)
                          -uO <new_id>  New User ID (used in re-sign mode)
                          -v            Verbose output
                          -h            Show this help message

                        Examples:
                          Decrypt:     {exeName} -m d -p "{inputPath}" -u {userIdInput}
                          Encrypt:     {exeName} -m e -p "{inputPath}" -u {userIdOutput}
                          Re-sign:     {exeName} -m r -p "{inputPath}" -uI {userIdInput} -uO {userIdOutput}
                          Bruteforce:  {exeName} -m b -p "{gpPath}"
                        """;
    Console.WriteLine(helpMessage);
}

string GetValidatedInputRootPath()
{
    arguments.TryGetValue("-p", out var inputRootPath);
    if (File.Exists(inputRootPath)) inputRootPath = Path.GetDirectoryName(inputRootPath);
    return !Directory.Exists(inputRootPath)
        ? throw new DirectoryNotFoundException($"The provided path '{inputRootPath}' is not a valid directory or does not exist.")
        : inputRootPath;
}

#endregion

#region MODES

void DecryptAll()
{
    var cts = new CancellationTokenSource();
    arguments.TryGetValue("-u", out var userId);
    if (string.IsNullOrEmpty(userId))
        throw new ArgumentException("Input User ID is missing.");
    var inputRootPath = GetValidatedInputRootPath();
    core.DecryptFiles(inputRootPath, userId, cts);
    cts.Dispose();
}

void EncryptAll()
{
    var cts = new CancellationTokenSource();
    arguments.TryGetValue("-u", out var userId);
    if (string.IsNullOrEmpty(userId))
        throw new ArgumentException("Output User ID is missing.");
    var inputRootPath = GetValidatedInputRootPath();
    core.EncryptFiles(inputRootPath, userId, cts);
    cts.Dispose();
}

void ResignAll()
{
    var cts = new CancellationTokenSource();
    arguments.TryGetValue("-uI", out var userIdInput);
    if (string.IsNullOrEmpty(userIdInput))
        throw new ArgumentException("Input User ID is missing.");
    arguments.TryGetValue("-uO", out var userIdOutput);
    if (string.IsNullOrEmpty(userIdOutput))
        throw new ArgumentException("Output User ID is missing.");
    var inputRootPath = GetValidatedInputRootPath();
    core.ResignFiles(inputRootPath, userIdInput, userIdOutput, cts);
    cts.Dispose();
}

void BruteforceSteamId()
{
    var cts = new CancellationTokenSource();
    arguments.TryGetValue("-p", out var filePath);
    if (!File.Exists(filePath))
        throw new FileNotFoundException($"The provided path '{filePath}' is not a valid file or does not exist.");
    core.BruteforceSteamId(filePath, cts);
    cts.Dispose();
}

#endregion