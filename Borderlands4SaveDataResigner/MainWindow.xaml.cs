using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using Borderlands4SaveDataResigner.Helpers;
using Borderlands4SaveDataResigner.Infrastructure;
using Borderlands4SaveDataResigner.Logger;
using Borderlands4SaveDataResigner.Logger.Models;
using Borderlands4SaveDataResigner.Logger.Providers;
using System.ComponentModel;

namespace Borderlands4SaveDataResigner;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window, INotifyPropertyChanged
{
    private readonly MyAppInfo _appInfo;
    private readonly Directories _directories;
    private readonly SimpleLogger _logger;
    private readonly UiLogProvider _uiLogProvider;
    private CancellationTokenSource? _currentOperation;

    public MainWindow()
    {
        try
        {
            InitializeComponent();
            
            // Initialize app components
            _appInfo = new MyAppInfo("Borderlands4SaveDataResigner");
            _directories = new Directories();
            _directories.CreateAll();
        
            // Initialize logger
            _logger = new SimpleLogger
            {
                LoggedAppName = _appInfo.Name,
                LoggedAppVersion = new Version(MyAppInfo.Version)
            };
        
            // Create custom UI log provider
            _uiLogProvider = new UiLogProvider();
            _uiLogProvider.Initialize(this);
            _logger.AddProvider(_uiLogProvider);
        
            // Create file log provider
            var fileLogProvider = new FileLogProvider(MyAppInfo.RootPath, 2);
            fileLogProvider.CreateLogFile();
            _logger.AddProvider(fileLogProvider);
        
            // Set output path display
            OutputPathTextBlock.Text = $"Output folder: {_directories.Output}";
        
            // Log startup
            _logger.LogInfo("Application started");
        
            DataContext = this;
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Failed to initialize MainWindow: {ex.Message}\n\nStack trace:\n{ex.StackTrace}", "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            throw;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #region Browse Button Handlers

    private void DecryptBrowseInputButton_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select folder containing .sav files to decrypt",
            UseDescriptionForTitle = true
        };
        
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            DecryptInputPath.Text = dialog.SelectedPath;
        }
    }

    private void EncryptBrowseInputButton_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select folder containing .yaml files to encrypt",
            UseDescriptionForTitle = true
        };
        
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            EncryptInputPath.Text = dialog.SelectedPath;
        }
    }

    private void ResignBrowseInputButton_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select folder containing .sav files to resign",
            UseDescriptionForTitle = true
        };
        
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            ResignInputPath.Text = dialog.SelectedPath;
        }
    }

    #endregion

    #region Operation Button Handlers

    private async void DecryptButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentOperation != null)
        {
            ShowWarning("An operation is already in progress. Please wait for it to complete.");
            return;
        }

        try
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(DecryptInputPath.Text))
            {
                ShowError("Please select an input folder containing .sav files.");
                return;
            }

            if (string.IsNullOrWhiteSpace(DecryptUserId.Text))
            {
                ShowError("Please enter a User ID.");
                return;
            }

            if (!Directory.Exists(DecryptInputPath.Text))
            {
                ShowError("The selected input folder does not exist.");
                return;
            }

            await RunDecryptOperation(DecryptInputPath.Text, DecryptUserId.Text);
        }
        catch (Exception ex)
        {
            ShowError($"Decrypt operation failed: {ex.Message}");
            _logger.LogError($"Decrypt operation failed: {ex}");
        }
    }

    private async void EncryptButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentOperation != null)
        {
            ShowWarning("An operation is already in progress. Please wait for it to complete.");
            return;
        }

        try
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(EncryptInputPath.Text))
            {
                ShowError("Please select an input folder containing .yaml files.");
                return;
            }

            if (string.IsNullOrWhiteSpace(EncryptUserId.Text))
            {
                ShowError("Please enter a User ID.");
                return;
            }

            if (!Directory.Exists(EncryptInputPath.Text))
            {
                ShowError("The selected input folder does not exist.");
                return;
            }

            await RunEncryptOperation(EncryptInputPath.Text, EncryptUserId.Text);
        }
        catch (Exception ex)
        {
            ShowError($"Encrypt operation failed: {ex.Message}");
            _logger.LogError($"Encrypt operation failed: {ex}");
        }
    }

    private async void ResignButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentOperation != null)
        {
            ShowWarning("An operation is already in progress. Please wait for it to complete.");
            return;
        }

        try
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(ResignInputPath.Text))
            {
                ShowError("Please select an input folder containing .sav files.");
                return;
            }

            if (string.IsNullOrWhiteSpace(ResignInputUserId.Text))
            {
                ShowError("Please enter the Input User ID.");
                return;
            }

            if (string.IsNullOrWhiteSpace(ResignOutputUserId.Text))
            {
                ShowError("Please enter the Output User ID.");
                return;
            }

            if (!Directory.Exists(ResignInputPath.Text))
            {
                ShowError("The selected input folder does not exist.");
                return;
            }

            await RunResignOperation(ResignInputPath.Text, ResignInputUserId.Text, ResignOutputUserId.Text);
        }
        catch (Exception ex)
        {
            ShowError($"Resign operation failed: {ex.Message}");
            _logger.LogError($"Resign operation failed: {ex}");
        }
    }

    #endregion

    #region Operation Logic

    private async Task RunDecryptOperation(string inputPath, string userId)
    {
        _currentOperation = new CancellationTokenSource();
        SetOperationInProgress(true, "decrypt");
        
        try
        {
            const string saveFileExtension = ".sav";
            const string yamlFileExtension = ".yaml";

            var filesToProcess = Directory.GetFiles(inputPath, $"*{saveFileExtension}", SearchOption.TopDirectoryOnly);
            if (filesToProcess.Length == 0)
            {
                ShowWarning("No .sav files found in the selected folder.");
                return;
            }

            _logger.LogInfo($"Found {filesToProcess.Length} .sav files to decrypt");
            
            // Calculate the private key based on the provided user ID
            var privateKey = Bl4Deencryptor.CalculatePrivateKey(userId);
            
            // Create output directory
            var outputDir = GetNewOutputDirectory(_directories.Output, "decrypted");
            CreateOutputFolderStructure(inputPath, outputDir, filesToProcess, userId);

            DecryptStatusText.Text = "Decrypting files...";
            DecryptProgressBar.Value = 0;
            DecryptProgressBar.Maximum = filesToProcess.Length;

            // Process files
            for (int i = 0; i < filesToProcess.Length; i++)
            {
                if (_currentOperation.Token.IsCancellationRequested)
                    break;

                var fileName = Path.GetFileName(filesToProcess[i]);
                _logger.LogInfo($"Decrypting {fileName}...");
                
                try
                {
                    ReadOnlySpan<byte> inputDataSpan = File.ReadAllBytes(filesToProcess[i]);
                    byte[] outputDataSpan = Bl4Deencryptor.DecryptData(inputDataSpan, privateKey);
                    
                    // Save the decrypted data
                    var outputFilePath = filesToProcess[i].Replace(inputPath, Path.Combine(outputDir, userId));
                    outputFilePath = Path.ChangeExtension(outputFilePath, yamlFileExtension);
                    File.WriteAllBytes(outputFilePath, outputDataSpan);
                    
                    _logger.LogInfo($"Successfully decrypted {fileName}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to decrypt {fileName}: {ex.Message}");
                }

                // Update progress
                DecryptProgressBar.Value = i + 1;
                DecryptProgressText.Text = $"{((i + 1) * 100 / filesToProcess.Length):F0}%";
                DecryptStatusText.Text = $"Processed {i + 1} of {filesToProcess.Length} files";
                
                await Task.Delay(10); // Allow UI to update
            }

            DecryptStatusText.Text = "Decrypt operation completed!";
            ShowSuccess($"Successfully decrypted {filesToProcess.Length} files to:\n{outputDir}");
        }
        finally
        {
            SetOperationInProgress(false, "decrypt");
            _currentOperation?.Dispose();
            _currentOperation = null;
        }
    }

    private async Task RunEncryptOperation(string inputPath, string userId)
    {
        _currentOperation = new CancellationTokenSource();
        SetOperationInProgress(true, "encrypt");
        
        try
        {
            const string yamlFileExtension = ".yaml";
            const string saveFileExtension = ".sav";

            var filesToProcess = Directory.GetFiles(inputPath, $"*{yamlFileExtension}", SearchOption.TopDirectoryOnly);
            if (filesToProcess.Length == 0)
            {
                ShowWarning("No .yaml files found in the selected folder.");
                return;
            }

            _logger.LogInfo($"Found {filesToProcess.Length} .yaml files to encrypt");
            
            // Calculate the private key based on the provided user ID
            var privateKey = Bl4Deencryptor.CalculatePrivateKey(userId);
            
            // Create output directory
            var outputDir = GetNewOutputDirectory(_directories.Output, "encrypted");
            CreateOutputFolderStructure(inputPath, outputDir, filesToProcess, userId);

            EncryptStatusText.Text = "Encrypting files...";
            EncryptProgressBar.Value = 0;
            EncryptProgressBar.Maximum = filesToProcess.Length;

            // Process files
            for (int i = 0; i < filesToProcess.Length; i++)
            {
                if (_currentOperation.Token.IsCancellationRequested)
                    break;

                var fileName = Path.GetFileName(filesToProcess[i]);
                _logger.LogInfo($"Encrypting {fileName}...");
                
                try
                {
                    ReadOnlySpan<byte> inputDataSpan = File.ReadAllBytes(filesToProcess[i]);
                    byte[] outputDataSpan = Bl4Deencryptor.EncryptData(inputDataSpan, privateKey);
                    
                    // Save the encrypted data
                    var outputFilePath = filesToProcess[i].Replace(inputPath, Path.Combine(outputDir, userId));
                    outputFilePath = Path.ChangeExtension(outputFilePath, saveFileExtension);
                    File.WriteAllBytes(outputFilePath, outputDataSpan);
                    
                    _logger.LogInfo($"Successfully encrypted {fileName}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to encrypt {fileName}: {ex.Message}");
                }

                // Update progress
                EncryptProgressBar.Value = i + 1;
                EncryptProgressText.Text = $"{((i + 1) * 100 / filesToProcess.Length):F0}%";
                EncryptStatusText.Text = $"Processed {i + 1} of {filesToProcess.Length} files";
                
                await Task.Delay(10); // Allow UI to update
            }

            EncryptStatusText.Text = "Encrypt operation completed!";
            ShowSuccess($"Successfully encrypted {filesToProcess.Length} files to:\n{outputDir}");
        }
        finally
        {
            SetOperationInProgress(false, "encrypt");
            _currentOperation?.Dispose();
            _currentOperation = null;
        }
    }

    private async Task RunResignOperation(string inputPath, string inputUserId, string outputUserId)
    {
        _currentOperation = new CancellationTokenSource();
        SetOperationInProgress(true, "resign");
        
        try
        {
            const string saveFileExtension = ".sav";

            var filesToProcess = Directory.GetFiles(inputPath, $"*{saveFileExtension}", SearchOption.TopDirectoryOnly);
            if (filesToProcess.Length == 0)
            {
                ShowWarning("No .sav files found in the selected folder.");
                return;
            }

            _logger.LogInfo($"Found {filesToProcess.Length} .sav files to resign");
            
            // Calculate private keys
            var inputPrivateKey = Bl4Deencryptor.CalculatePrivateKey(inputUserId);
            var outputPrivateKey = Bl4Deencryptor.CalculatePrivateKey(outputUserId);
            
            // Create output directory
            var outputDir = GetNewOutputDirectory(_directories.Output, "resigned");
            CreateOutputFolderStructure(inputPath, outputDir, filesToProcess, outputUserId);

            ResignStatusText.Text = "Resigning files...";
            ResignProgressBar.Value = 0;
            ResignProgressBar.Maximum = filesToProcess.Length;

            // Process files
            for (int i = 0; i < filesToProcess.Length; i++)
            {
                if (_currentOperation.Token.IsCancellationRequested)
                    break;

                var fileName = Path.GetFileName(filesToProcess[i]);
                _logger.LogInfo($"Resigning {fileName}...");
                
                try
                {
                    ReadOnlySpan<byte> encryptedDataSpan = File.ReadAllBytes(filesToProcess[i]);
                    
                    // Decrypt with old user's key
                    byte[] decryptedData = Bl4Deencryptor.DecryptData(encryptedDataSpan, inputPrivateKey);
                    
                    // Anonymize the decrypted data
                    var anonymizedData = Bl4Deencryptor.AnonymizeSaveData(decryptedData, Path.GetFileNameWithoutExtension(fileName), _logger, $"Resign-{i}");
                    
                    // Encrypt with new user's key
                    byte[] resignedData = Bl4Deencryptor.EncryptData(anonymizedData, outputPrivateKey);
                    
                    // Save the resigned data
                    var outputFilePath = filesToProcess[i].Replace(inputPath, Path.Combine(outputDir, outputUserId));
                    File.WriteAllBytes(outputFilePath, resignedData);
                    
                    _logger.LogInfo($"Successfully resigned {fileName}");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Failed to resign {fileName}: {ex.Message}");
                }

                // Update progress
                ResignProgressBar.Value = i + 1;
                ResignProgressText.Text = $"{((i + 1) * 100 / filesToProcess.Length):F0}%";
                ResignStatusText.Text = $"Processed {i + 1} of {filesToProcess.Length} files";
                
                await Task.Delay(10); // Allow UI to update
            }

            ResignStatusText.Text = "Resign operation completed!";
            ShowSuccess($"Successfully resigned {filesToProcess.Length} files to:\n{outputDir}");
        }
        finally
        {
            SetOperationInProgress(false, "resign");
            _currentOperation?.Dispose();
            _currentOperation = null;
        }
    }

    #endregion

    #region Helper Methods

    private static string GetNewOutputDirectory(string rootPath, string action) => 
        Path.Combine(rootPath, $"{DateTime.Now:yyyy-MM-dd_HHmmssfff}_{action}");

    private static void CreateOutputFolderStructure(string inputRootPath, string outputDirectory, string[] filesToProcess, string userId)
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

    private static T? FindVisualParent<T>(DependencyObject child) where T : DependencyObject
    {
        var parentObject = VisualTreeHelper.GetParent(child);
        if (parentObject == null) return null;
        if (parentObject is T parent) return parent;
        return FindVisualParent<T>(parentObject);
    }

    private void SetOperationInProgress(bool inProgress, string operation)
    {
        Dispatcher.Invoke(() =>
        {
            switch (operation.ToLower())
            {
                case "decrypt":
                    if (!inProgress)
                    {
                        DecryptProgressText.Text = "100%";
                        DecryptProgressBar.Value = DecryptProgressBar.Maximum;
                    }
                    break;
                case "encrypt":
                    if (!inProgress)
                    {
                        EncryptProgressText.Text = "100%";
                        EncryptProgressBar.Value = EncryptProgressBar.Maximum;
                    }
                    break;
                case "resign":
                    if (!inProgress)
                    {
                        ResignProgressText.Text = "100%";
                        ResignProgressBar.Value = ResignProgressBar.Maximum;
                    }
                    break;
            }
        });
    }

    private void ShowError(string message)
    {
        System.Windows.MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private void ShowWarning(string message)
    {
        System.Windows.MessageBox.Show(message, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void ShowSuccess(string message)
    {
        System.Windows.MessageBox.Show(message, "Success", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    #endregion

    #region UI Log Provider

    public class UiLogProvider : ILogProvider
    {
        private MainWindow? _mainWindow;

        public void Initialize(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
        }

        public void Log(LogEntry logEntry)
        {
            if (_mainWindow?.LogTextBlock == null) return;

            _mainWindow.Dispatcher.Invoke(() =>
            {
                var timestamp = DateTime.Now.ToString("HH:mm:ss");
                var logText = $"[{timestamp}] {logEntry.LogLevel}: {logEntry.Message}\n";
                _mainWindow.LogTextBlock.Text += logText;
                
                // Auto-scroll to bottom
                var scrollViewer = FindVisualParent<ScrollViewer>(_mainWindow.LogTextBlock);
                if (scrollViewer != null)
                {
                    scrollViewer.ScrollToEnd();
                }
            });
        }

        public async Task LogAsync(LogEntry logEntry)
        {
            await Task.Run(() => Log(logEntry));
        }

        public void Flush()
        {
            // No action needed for UI provider
        }

        public async Task FlushAsync()
        {
            await Task.CompletedTask;
        }
    }

    #endregion
}