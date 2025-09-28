using Borderlands4SaveDataResignerCore.Helpers;
using Borderlands4SaveDataResignerCore.Infrastructure;
using Mi5hmasH.Logger;

namespace Borderlands4SaveDataResignerCore;

public class Core
{
    private readonly SimpleLogger _logger;

    private readonly ProgressReporter _progressReporter;

    private static void CreateDirectories() => Directories.CreateAll();

    public Core(SimpleLogger logger, ProgressReporter progressReporter)
    {
        _logger = logger;
        _progressReporter = progressReporter;
        CreateDirectories();
    }

    private static ParallelOptions GetParallelOptions(CancellationTokenSource cts) 
        => new()
        {
            CancellationToken = cts.Token,
            MaxDegreeOfParallelism = Math.Max(Environment.ProcessorCount - 1, 1)
        };

    public async Task DecryptFilesAsync(string inputDir, string userId, CancellationTokenSource cts)
        => await Task.Run(() => DecryptFiles(inputDir, userId, cts));

    public void DecryptFiles(string inputDir, string userId, CancellationTokenSource cts)
    {
        var filesToProcess = Directory.GetFiles(inputDir, $"*{Bl4Deencryptor.SaveFileExtension}", SearchOption.TopDirectoryOnly);
        if (filesToProcess.Length == 0) return;
        _logger.LogInfo($"Decrypting [{filesToProcess.Length}] files...");
        // Calculate the private key based on the provided user ID
        var privateKey = Bl4Deencryptor.CalculatePrivateKey(userId);
        // DECRYPT
        // Create a new folder in OUTPUT directory
        var outputDir = Directories.GetNewOutputDirectory("decrypted");
        // Crate the folder structure in the newly created output directory
        Directories.CreateOutputFolderStructure(inputDir, outputDir, filesToProcess, userId);
        // Setup parallel options
        var po = GetParallelOptions(cts);
        // Process files in parallel
        var progress = 0;
        try
        {
            Parallel.For((long)0, filesToProcess.Length, po, (ctr, _) =>
            {
                var fileName = Path.GetFileName(filesToProcess[ctr]);
                var group = $"Task {ctr}";
                try
                {
                    ReadOnlySpan<byte> inputDataSpan = File.ReadAllBytes(filesToProcess[ctr]);
                    _logger.LogInfo($"[{progress}/{filesToProcess.Length}] Decrypting [{fileName}] file...", group);
                    var outputDataSpan = Bl4Deencryptor.DecryptData(inputDataSpan, privateKey);
                    // Save the decrypted data to the output directory, preserving the folder structure and change the file extension to .yaml
                    var outputFilePath = filesToProcess[ctr].Replace(inputDir, Path.Combine(outputDir, userId));
                    outputFilePath = Path.ChangeExtension(outputFilePath, Bl4Deencryptor.YamlFileExtension);
                    File.WriteAllBytes(outputFilePath, outputDataSpan);
                    _logger.LogInfo($"[{progress}/{filesToProcess.Length}] Decrypted [{fileName}] file.", group);
                }
                catch (Exception e)
                {
                    _logger.LogError($"[{progress}/{filesToProcess.Length}] Failed to decrypt the [{fileName}] file: {e}", group);
                }
                finally
                {
                    Interlocked.Increment(ref progress);
                    _progressReporter.Report((int)((double)progress / filesToProcess.Length * 100));
                }
            });
            _logger.LogInfo($"[{progress}/{filesToProcess.Length}] All tasks completed.");
        }
        catch (OperationCanceledException e)
        {
            _logger.LogWarning(e.Message);
        }
    }

    public async Task EncryptFilesAsync(string inputDir, string userId, CancellationTokenSource cts)
        => await Task.Run(() => EncryptFiles(inputDir, userId, cts));

    public void EncryptFiles(string inputDir, string userId, CancellationTokenSource cts)
    {
        var filesToProcess = Directory.GetFiles(inputDir, $"*{Bl4Deencryptor.YamlFileExtension}", SearchOption.TopDirectoryOnly);
        if (filesToProcess.Length == 0) return;
        _logger.LogInfo($"Encrypting [{filesToProcess.Length}] files...");
        // Calculate the private key based on the provided user ID
        var privateKey = Bl4Deencryptor.CalculatePrivateKey(userId);
        // ENCRYPT
        // Create a new folder in OUTPUT directory
        var outputDir = Directories.GetNewOutputDirectory("encrypted");
        // Crate the folder structure in the newly created output directory
        Directories.CreateOutputFolderStructure(inputDir, outputDir, filesToProcess, userId);
        // Setup parallel options
        var po = GetParallelOptions(cts);
        // Process files in parallel
        var progress = 0;
        try
        {
            Parallel.For((long)0, filesToProcess.Length, po, (ctr, _) =>
            {
                var fileName = Path.GetFileName(filesToProcess[ctr]);
                var group = $"Task {ctr}";
                try
                {
                    ReadOnlySpan<byte> inputDataSpan = File.ReadAllBytes(filesToProcess[ctr]);
                    _logger.LogInfo($"[{progress}/{filesToProcess.Length}] Encrypting [{fileName}] file...", group);
                    var outputDataSpan = Bl4Deencryptor.EncryptData(inputDataSpan, privateKey);
                    // Save the encrypted data to the output directory, preserving the folder structure and change the file extension to .sav
                    var outputFilePath = filesToProcess[ctr].Replace(inputDir, Path.Combine(outputDir, userId));
                    outputFilePath = Path.ChangeExtension(outputFilePath, Bl4Deencryptor.SaveFileExtension);
                    File.WriteAllBytes(outputFilePath, outputDataSpan);
                    _logger.LogInfo($"[{progress}/{filesToProcess.Length}] Encrypted [{fileName}] file.", group);
                }
                catch (Exception e)
                {
                    _logger.LogError(
                        $"[{progress}/{filesToProcess.Length}] Failed to encrypt the [{fileName}] file: {e}", group);
                }
                finally
                {
                    Interlocked.Increment(ref progress);
                    _progressReporter.Report((int)((double)progress / filesToProcess.Length * 100));
                }
            });
            _logger.LogInfo($"[{progress}/{filesToProcess.Length}] All tasks completed.");
        }
        catch (OperationCanceledException e)
        {
            _logger.LogWarning(e.Message);
        }
    }

    public async Task ResignFilesAsync(string inputDir, string userIdInput, string userIdOutput, CancellationTokenSource cts)
        => await Task.Run(() => ResignFiles(inputDir, userIdInput, userIdOutput, cts));

    public void ResignFiles(string inputDir, string userIdInput, string userIdOutput, CancellationTokenSource cts)
    {
        var filesToProcess = Directory.GetFiles(inputDir, $"*{Bl4Deencryptor.SaveFileExtension}", SearchOption.TopDirectoryOnly);
        if (filesToProcess.Length == 0) return;
        _logger.LogInfo($"Resigning [{filesToProcess.Length}] files...");
        // Calculate the private key based on the provided user ID
        var privateKey = Bl4Deencryptor.CalculatePrivateKey(userIdInput);
        // RESIGN
        // Create a new folder in OUTPUT directory
        var outputDir = Directories.GetNewOutputDirectory("resigned");
        // Crate the folder structure in the newly created output directory
        Directories.CreateOutputFolderStructure(inputDir, outputDir, filesToProcess, userIdOutput);
        // Setup parallel options
        var po = GetParallelOptions(cts);
        // Process files in parallel
        var progress = 0;
        try
        {
            Parallel.For((long)0, filesToProcess.Length, po, (ctr, _) =>
            {
                while (true)
                {
                    var fileName = Path.GetFileName(filesToProcess[ctr]);
                    var group = $"Task {ctr}";
                    ReadOnlySpan<byte> encryptedDataSpan = File.ReadAllBytes(filesToProcess[ctr]);
                    // Decrypt the data with the old user's private key
                    _logger.LogInfo($"[{progress}/{filesToProcess.Length}] Decrypting [{fileName}] file...", group);
                    byte[] decryptedData;
                    try
                    {
                        decryptedData = Bl4Deencryptor.DecryptData(encryptedDataSpan, privateKey);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError($"[{progress}/{filesToProcess.Length}] Failed to decrypt the [{fileName}] file: {e}", group);
                        break; // Skip to the next file
                    }
                    // Anonymize the decrypted data
                    var decryptedDataWithUpdatedGuids = Bl4Deencryptor.AnonymizeSaveData(decryptedData, Path.GetFileNameWithoutExtension(fileName), _logger, group);
                    // Encrypt the updated data with the new user's private key
                    _logger.LogInfo($"[{progress}/{filesToProcess.Length}] Encrypting [{fileName}] file...", group);
                    privateKey = Bl4Deencryptor.CalculatePrivateKey(userIdOutput);
                    byte[] resignedData;
                    try
                    {
                        resignedData = Bl4Deencryptor.EncryptData(decryptedDataWithUpdatedGuids, privateKey);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError($"[{progress}/{filesToProcess.Length}] Failed to encrypt the [{fileName}] file: {e}", group);
                        break; // Skip to the next file
                    }
                    // Save the resigned data to the output directory, preserving the folder structure
                    var outputFilePath = filesToProcess[ctr].Replace(inputDir, Path.Combine(outputDir, userIdOutput));
                    File.WriteAllBytes(outputFilePath, resignedData);
                    _logger.LogInfo($"[{progress}/{filesToProcess.Length}] Resigned [{fileName}] file.", group);
                    break;
                }
                Interlocked.Increment(ref progress);
                _progressReporter.Report((int)((double)progress / filesToProcess.Length * 100));
            });
            _logger.LogInfo($"[{progress}/{filesToProcess.Length}] All tasks completed.");
        }
        catch (OperationCanceledException e)
        {
            _logger.LogWarning(e.Message);
        }
    }
}