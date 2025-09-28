using Borderlands4SaveDataResignerCore;
using Borderlands4SaveDataResignerCore.Helpers;
using Borderlands4SaveDataResignerCore.Infrastructure;
using Borderlands4SaveDataResignerWpf.Helpers;
using Borderlands4SaveDataResignerWpf.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mi5hmasH.AppSettings;
using Mi5hmasH.AppSettings.Encryption;
using Mi5hmasH.AppSettings.Flavors;
using Mi5hmasH.GameLaunchers.Helpers;
using Mi5hmasH.Logger;
using Mi5hmasH.Logger.Models;
using Mi5hmasH.Logger.Providers;
using Microsoft.Win32;
using System.Collections.Specialized;
using System.IO;
using System.Media;
using Mi5hmasH.AppInfo;

namespace Borderlands4SaveDataResignerWpf.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    #region APP_INFO
    [ObservableProperty] private MyAppInfo _myAppInfo = new("Borderlands 4 - SaveData Resigner");
    [ObservableProperty] private string _appVersion = $"v{MyAppInfo.Version}";
    [RelayCommand] private static void VisitAuthorsGithub() => "https://github.com/mi5hmash".OpenUrl();
    #endregion
    
    #region UI_STATE
    [ObservableProperty] private bool _isBusy;
    [ObservableProperty] private bool _isAbortAllowed;
    #endregion

    #region PROGRESS_REPORTER
    [ObservableProperty] private int _progressValue;
    [ObservableProperty] private string _progressText = "Loading...";
    private readonly ProgressReporter _progressReporter;
    #endregion
    
    #region INPUT_FOLDER_PATH
    [ObservableProperty] private string _inputFolderPath = MyAppInfo.RootPath;

    public void OnFileDrop(string operationType, StringCollection filePaths)
    {
        if (filePaths.Count == 0) return;
        if (operationType == "GetInputPath") InputFolderPath = filePaths[0] ?? string.Empty;
    }

    partial void OnInputFolderPathChanged(string value)
    {
        if (Directory.Exists(value)) return;
        if (File.Exists(value))
        {
            _inputFolderPath = Path.GetDirectoryName(value) ?? string.Empty;
            _progressReporter.Report("Input Folder Path is valid.");
            return;
        }
        _progressReporter.Report("Invalid Input Folder Path!");
        _inputFolderPath = string.Empty;
    }

    [RelayCommand]
    private void SelectInputFolderPath()
    {
        OpenFileDialog openFileDialog = new()
        {
            InitialDirectory = InputFolderPath,
            Filter = "SaveData Files (*.sav;*.yaml)|*.sav;*.yaml",
            FilterIndex = 1
        };
        if (openFileDialog.ShowDialog() == true) InputFolderPath = openFileDialog.FileName;
    }
    #endregion

    #region OUTPUT_FOLDER_PATH
    [RelayCommand]
    private static void OpenOutputDirectory()
        => Directories.OpenDirectory(Directories.Output);
    #endregion

    #region USER_ID
    [ObservableProperty] private string _userIdInput;
    [ObservableProperty] private string _userIdOutput;
    
    [RelayCommand]
    private void SwapUserIds()
    {
        (UserIdInput, UserIdOutput) = (UserIdOutput, UserIdInput);
        _progressReporter.Report("User IDs has been swapped.");
    }
    #endregion

    private CancellationTokenSource _cts = new();
    private readonly Core _core;
    private readonly AppSettingsManager<MyAppSettings, Json> _appSettingsManager;
    [ObservableProperty] private SuperUserManager _superUserManager;

    public MainWindowViewModel()
    {
        // Initialize ProgressReporter
        _progressReporter = new ProgressReporter(
            new Progress<string>(s => ProgressText = s),
            new Progress<int>(i => ProgressValue = i)
        );
        // Initialize Logger
        var logger = new SimpleLogger
        {
            LoggedAppName = MyAppInfo.Name
        };
        // Configure StatusBarLogProvider
        var statusBarLogProvider = new StatusBarLogProvider(_progressReporter.Report);
        logger.AddProvider(statusBarLogProvider);
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
        // Initialize _core
        _core = new Core(logger, _progressReporter);
        // Initialize SuperUserManager
        SuperUserManager = new SuperUserManager(_progressReporter);
        // Load AppSettings
        var myAppSettings = new MyAppSettings();
        _appSettingsManager = new AppSettingsManager<MyAppSettings, Json>(myAppSettings, MyAppInfo.RootPath);
        _appSettingsManager.SetEncryptor(new AesCrypto("OUV0k8JXQafInOjo1p8e8v3WYKqOtTdWXL4/o8QnVtM="));
        try { _appSettingsManager.Load(); }
        catch { 
            // ignore
        }
        // Apply loaded settings
        UserIdInput = _appSettingsManager.Settings.UserIdInput;
        UserIdOutput = _appSettingsManager.Settings.UserIdOutput;
        SuperUserManager.IsSuperUser = _appSettingsManager.Settings.IsSu;
        AppDomain.CurrentDomain.ProcessExit += (_, _) => SaveAppSettings();
        // Finalize setup
        _progressReporter.Report(100);
        _progressReporter.Report("Ready");
    }

    private void SaveAppSettings()
    {
        _appSettingsManager.Settings.UserIdInput = UserIdInput;
        _appSettingsManager.Settings.UserIdOutput = UserIdOutput;
        _appSettingsManager.Settings.IsSu = SuperUserManager.IsSuperUser;
        _appSettingsManager.Save();
    }

    #region ACTIONS

    [RelayCommand]
    public void AbortAction()
    {
        if (!IsAbortAllowed || !IsBusy) return;
        _cts.Cancel();
    }

    private async Task PerformAction(Func<Task> function, bool canBeAborted = false)
    {
        if (IsBusy) return;
        IsBusy = true;
        if (canBeAborted) IsAbortAllowed = true;
        try
        {
            await function();
        }
        finally
        {
            // play sound
            if (_cts.IsCancellationRequested) 
                SystemSounds.Beep.Play();
            else
            {
                using var sp = new SoundPlayer(Properties.Resources.typewriter_machine);
                sp.Play();
            }
            // reset flags
            if (canBeAborted) IsAbortAllowed = false;
            IsBusy = false;
        }
    }
    
    [RelayCommand]
    private async Task DecryptAllAsync()
    {
        _cts = new CancellationTokenSource();
        await PerformAction(() => _core.DecryptFilesAsync(InputFolderPath, UserIdInput, _cts), true);
        _cts.Dispose();
    }

    [RelayCommand]
    private async Task EncryptAllAsync()
    {
        _cts = new CancellationTokenSource();
        await PerformAction(() => _core.EncryptFilesAsync(InputFolderPath, UserIdOutput, _cts), true);
        _cts.Dispose();
    }

    [RelayCommand]
    private async Task ResignAllAsync()
    {
        _cts = new CancellationTokenSource();
        await PerformAction(() => _core.ResignFilesAsync(InputFolderPath, UserIdInput, UserIdOutput, _cts), true);
        _cts.Dispose();
    }

    [RelayCommand]
    private async Task BruteforceSteamId()
    {
        if (IsBusy) return;
        IsBusy = true;
        IsAbortAllowed = true;
        _cts = new CancellationTokenSource();
        var result = await _core.BruteforceSteamIdAsync(InputFolderPath, _cts);
        if (result != null) UserIdInput = result.ToString() ?? string.Empty;
        SystemSounds.Beep.Play();
        _cts.Dispose();
        IsAbortAllowed = false;
        IsBusy = false;
    }

#if DEBUG
    [RelayCommand]
    private async Task Test()
    {
        _cts = new CancellationTokenSource();
        await PerformAction(() => HugeTask(_cts), true);
        _cts.Dispose();
    } 

    private async Task HugeTask(CancellationTokenSource cts)
    {
        const int totalSteps = 10;
        _progressReporter.Report("Huge task started.");
        for (var i = 1; i <= totalSteps; i++)
        {
            await Task.Delay(1000);
            if (cts.IsCancellationRequested) 
            {
                _progressReporter.Report("Huge task aborted!");
                return;
            }
            _progressReporter.Report($"Processing step {i}/{totalSteps}...");
        }
        _progressReporter.Report("Huge task completed!");
    }
#endif
    #endregion
}