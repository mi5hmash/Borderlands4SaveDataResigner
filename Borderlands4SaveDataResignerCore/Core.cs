using Borderlands4SaveDataResignerCore.Deencryption;
using Borderlands4SaveDataResignerCore.Infrastructure;
using Mi5hmasH.Logger;
using Mi5hmasH.Progress;
using static Borderlands4SaveDataResignerCore.Deencryption.Bl4DeencryptorHelper;

namespace Borderlands4SaveDataResignerCore;

public class Core(SimpleLogger logger, ProgressReporter progressReporter)
{
    /// <summary>
    /// Creates a new ParallelOptions instance configured with the specified cancellation token and an optimal degree of parallelism for the current environment.
    /// </summary>
    /// <param name="cts">The CancellationTokenSource whose token will be used to support cancellation of parallel operations.</param>
    /// <returns>A ParallelOptions object initialized with the provided cancellation token and a maximum degree of parallelism based on the number of available processors.</returns>
    private static ParallelOptions GetParallelOptions(CancellationTokenSource cts) 
        => new()
        {
            CancellationToken = cts.Token,
            MaxDegreeOfParallelism = Math.Max(Environment.ProcessorCount - 1, 1)
        };

    /// <summary>
    /// Marks the progress reporting as complete by reporting 100% progress.
    /// </summary>
    /// <param name="progressTracker">The progress tracker used to report progress.</param>
    /// <param name="errorCounter">The error counter used to report errors.</param>
    private void LogAllTasksCompleted(ProgressTracker progressTracker, ErrorCounter errorCounter)
        => logger.LogInfo($"{progressTracker} All tasks completed. {errorCounter}");
    
    /// <summary>
    /// Asynchronously decrypts all files in the specified input directory for the given user.
    /// </summary>
    /// <param name="inputDir">The path to the directory containing the encrypted files to decrypt.</param>
    /// <param name="userId">The identifier of the user whose files are to be decrypted.</param>
    /// <param name="cts">A CancellationTokenSource that can be used to cancel the decryption operation.</param>
    /// <returns>A task that represents the asynchronous decryption operation.</returns>
    public async Task DecryptFilesAsync(string inputDir, string userId, CancellationTokenSource cts)
        => await Task.Run(() => DecryptFiles(inputDir, userId, cts));

    /// <summary>
    /// Decrypts all encrypted files in the specified input directory using the private key derived from the provided user ID, and saves the decrypted files to a new output directory.
    /// </summary>
    /// <param name="inputDir">The path to the directory containing the encrypted files to decrypt. Only files matching the expected encrypted file extension are processed.</param>
    /// <param name="userId">The user identifier used to calculate the private key for decryption.</param>
    /// <param name="cts">A CancellationTokenSource used to cancel the decryption operation. If cancellation is requested, the method will stop processing remaining files.</param>
    public void DecryptFiles(string inputDir, string userId, CancellationTokenSource cts)
    {
        // GET FILES TO PROCESS
        string[] filesToProcess;
        try { filesToProcess = SaveDataFileIo.GetSaveFiles(inputDir); }
        catch (Exception ex)
        {
            logger.LogWarning(ex.Message);
            return;
        }
        // INITIALIZE PROGRESS TRACKER
        var progressTracker = new ProgressTracker(filesToProcess.Length);
        var errorCounter = new ErrorCounter(logger);
        // DECRYPT
        logger.LogInfo($"Decrypting [{progressTracker.Total}] files...");
        // Create a new folder in OUTPUT directory
        var outputDir = Directories.GetNewOutputDirectory("decrypted").AddUserIdAndSuffix(userId);
        Directory.CreateDirectory(outputDir);
        // Setup parallel options
        var po = GetParallelOptions(cts);
        // Process files in parallel
        try
        {
            var key = CalculatePrivateKey(userId);
            Parallel.For((long)0, filesToProcess.Length, po, 
                () => new Bl4Decryptor(key), // per-thread init
                (ctr, _, localDecryptor) =>
                {
                    while (true)
                    {
                        var fileName = Path.GetFileName(filesToProcess[ctr]);
                        var group = $"Task {ctr}";
                    
                        // Try to read file data
                        byte[] inputData;
                        try { inputData = File.ReadAllBytes(filesToProcess[ctr]); }
                        catch (Exception ex)
                        {
                            errorCounter.AddError($"{progressTracker} Failed to read the [{fileName}] file: {ex}", group);
                            break;
                        }
                        // Try to decrypt file data
                        byte[] outputData;
                        try
                        {
                            logger.LogInfo($"{progressTracker} Decrypting the [{fileName}] file...", group);
                            outputData = localDecryptor.DecryptData(inputData);
                        }
                        catch (Exception ex)
                        {
                            errorCounter.AddError($"{progressTracker} Failed to process the [{fileName}] file data: {ex}", group);
                            break;
                        }
                        // Try to save the decrypted file data
                        try
                        {
                            var outputFilePath = Path.Combine(outputDir, Path.ChangeExtension(fileName, SaveDataFileIo.YamlFileExtension));
                            File.WriteAllBytes(outputFilePath, outputData);
                        }
                        catch (Exception ex)
                        {
                            errorCounter.AddError($"{progressTracker} Failed to save the file: {ex}", group);
                            break;
                        }
                        logger.LogInfo($"{progressTracker} Decrypted the [{fileName}] file.", group);
                        break;
                    }
                    progressTracker.Increment();
                    progressReporter.Report(progressTracker.Percentage);
                    return localDecryptor;
                }, 
                localDecryptor => localDecryptor.Dispose()
            );
            LogAllTasksCompleted(progressTracker, errorCounter);
        }
        catch (OperationCanceledException ex)
        {
            errorCounter.AddWarning(ex.Message);
        }
        finally
        {
            progressReporter.Complete();
        }
    }

    /// <summary>
    /// Asynchronously encrypts all files in the specified directory for the given user.
    /// </summary>
    /// <param name="inputDir">The path to the directory containing files to encrypt.</param>
    /// <param name="userId">The identifier of the user for whom the files will be encrypted.</param>
    /// <param name="cts">A CancellationTokenSource that can be used to cancel the encryption operation.</param>
    /// <returns>A task that represents the asynchronous encryption operation.</returns>
    public async Task EncryptFilesAsync(string inputDir, string userId, CancellationTokenSource cts)
        => await Task.Run(() => EncryptFiles(inputDir, userId, cts));

    /// <summary>
    /// Encrypts all YAML files in the specified input directory using the private key derived from the provided user ID.
    /// The encrypted files are saved to a new output directory with updated file extensions.
    /// </summary>
    /// <param name="inputDir">The path to the directory containing the YAML files to encrypt.</param>
    /// <param name="userId">The user identifier used to derive the encryption private key.</param>
    /// <param name="cts">A CancellationTokenSource that can be used to cancel the encryption operation before completion.</param>
    public void EncryptFiles(string inputDir, string userId, CancellationTokenSource cts)
    {
        // GET FILES TO PROCESS
        string[] filesToProcess;
        try { filesToProcess = SaveDataFileIo.GetYamlFiles(inputDir); }
        catch (Exception ex)
        {
            logger.LogWarning(ex.Message);
            return;
        }
        // INITIALIZE PROGRESS TRACKER
        var progressTracker = new ProgressTracker(filesToProcess.Length);
        var errorCounter = new ErrorCounter(logger);
        // ENCRYPT
        logger.LogInfo($"Encrypting [{progressTracker.Total}] files...");
        // Create a new folder in OUTPUT directory
        var outputDir = Directories.GetNewOutputDirectory("encrypted").AddUserIdAndSuffix(userId);
        Directory.CreateDirectory(outputDir);
        // Setup parallel options
        var po = GetParallelOptions(cts);
        // Process files in parallel
        try
        {
            var key = CalculatePrivateKey(userId);
            Parallel.For((long)0, filesToProcess.Length, po, 
                () => new Bl4Encryptor(key), // per-thread init
                (ctr, _, localEncryptor) =>
            {
                while (true)
                {
                    var fileName = Path.GetFileName(filesToProcess[ctr]);
                    var group = $"Task {ctr}";
                    
                    // Try to read file data
                    byte[] inputData;
                    try { inputData = File.ReadAllBytes(filesToProcess[ctr]); }
                    catch (Exception ex)
                    {
                        errorCounter.AddError($"{progressTracker} Failed to read the [{fileName}] file: {ex}", group);
                        break;
                    }
                    // Try to encrypt file data
                    byte[] outputData;
                    try
                    {
                        logger.LogInfo($"{progressTracker} Encrypting the [{fileName}] file...", group);
                        outputData = localEncryptor.EncryptData(inputData);
                    }
                    catch (Exception ex)
                    {
                        errorCounter.AddError($"{progressTracker} Failed to process the [{fileName}] file data: {ex}", group);
                        break;
                    }
                    // Try to save the encrypted file data
                    try
                    {
                        var outputFilePath = Path.Combine(outputDir, Path.ChangeExtension(fileName, SaveDataFileIo.SaveFileExtension));
                        File.WriteAllBytes(outputFilePath, outputData);
                    }
                    catch (Exception ex)
                    {
                        errorCounter.AddError($"{progressTracker} Failed to save the file: {ex}", group);
                        break;
                    }
                    logger.LogInfo($"{progressTracker} Encrypted the [{fileName}] file.", group);
                    break;
                }
                progressTracker.Increment();
                progressReporter.Report(progressTracker.Percentage);
                return localEncryptor;
            }, 
                localEncryptor => localEncryptor.Dispose()
            );
            LogAllTasksCompleted(progressTracker, errorCounter);
        }
        catch (OperationCanceledException ex)
        {
            errorCounter.AddWarning(ex.Message);
        }
        finally
        {
            progressReporter.Complete();
        }
    }

    /// <summary>
    /// Represents the local state for the re-signing process, encapsulating the decryption and encryption logic for a specific pair of user keys.
    /// </summary>
    /// <param name="keyInput">The key used for decrypting the original save files.</param>
    /// <param name="keyOutput">The key used for encrypting the re-signed save files.</param>
    private sealed class ResignerLocalState(byte[] keyInput, byte[] keyOutput) : IDisposable
    {
        public Bl4Decryptor Decryptor { get; } = new(keyInput);
        public Bl4Encryptor Encryptor { get; } = new(keyOutput);

        public void Dispose()
        {
            Decryptor.Dispose();
            Encryptor.Dispose();
        }
    }
    
    /// <summary>
    /// Asynchronously re-signs all files in the specified input directory using the provided user identifiers.
    /// </summary>
    /// <param name="inputDir">The path to the directory containing the files to be re-signed.</param>
    /// <param name="userIdInput">The user identifier associated with the original signatures of the files.</param>
    /// <param name="userIdOutput">The user identifier to be used for the new signatures applied to the files.</param>
    /// <param name="cts">A CancellationTokenSource that can be used to cancel the re-signing operation.</param>
    /// <returns>A task that represents the asynchronous resigning operation.</returns>
    public async Task ResignFilesAsync(string inputDir, string userIdInput, string userIdOutput, CancellationTokenSource cts)
        => await Task.Run(() => ResignFiles(inputDir, userIdInput, userIdOutput, cts));

    /// <summary>
    /// Re-signs all save files in the specified directory by decrypting them with the original user ID and re-encrypting them with a new user ID.
    /// </summary>
    /// <param name="inputDir">The path to the directory containing the save files to be resigned.</param>
    /// <param name="userIdInput">The user ID used to decrypt the original save files. Must be a valid identifier for the source user.</param>
    /// <param name="userIdOutput">The user ID used to re-encrypt the save files. Must be a valid identifier for the target user.</param>
    /// <param name="cts">A CancellationTokenSource used to cancel the operation if needed. If cancellation is requested, the process will terminate early.</param>
    public void ResignFiles(string inputDir, string userIdInput, string userIdOutput, CancellationTokenSource cts)
    {
        // GET FILES TO PROCESS
        string[] filesToProcess;
        try { filesToProcess = SaveDataFileIo.GetSaveFiles(inputDir); }
        catch (Exception ex)
        {
            logger.LogWarning(ex.Message);
            return;
        }
        // INITIALIZE PROGRESS TRACKER
        var progressTracker = new ProgressTracker(filesToProcess.Length);
        var errorCounter = new ErrorCounter(logger);
        // RE-SIGN
        logger.LogInfo($"Re-signing [{progressTracker.Total}] files...");
        // Create a new folder in OUTPUT directory
        var outputDir = Directories.GetNewOutputDirectory("resigned").AddUserIdAndSuffix(userIdInput);
        Directory.CreateDirectory(outputDir);
        // Setup parallel options
        var po = GetParallelOptions(cts);
        // Process files in parallel
        try
        {
            var keyInput = CalculatePrivateKey(userIdInput);
            var keyOutput = CalculatePrivateKey(userIdOutput);
            Parallel.For((long)0, filesToProcess.Length, po, 
                () => new ResignerLocalState(keyInput, keyOutput), // per-thread init
                (ctr, _, resignerLocalState) =>
                {
                    while (true)
                    {
                        var fileName = Path.GetFileName(filesToProcess[ctr]);
                        var group = $"Task {ctr}";
                        
                        // Try to read file data
                        byte[] encryptedData;
                        try { encryptedData = File.ReadAllBytes(filesToProcess[ctr]); }
                        catch (Exception ex)
                        {
                            errorCounter.AddError($"{progressTracker} Failed to read the [{fileName}] file: {ex}", group);
                            break;
                        }
                        // Try to decrypt file data
                        byte[] decryptedData;
                        try
                        {
                            logger.LogInfo($"{progressTracker} Decrypting the [{fileName}] file...", group);
                            decryptedData = resignerLocalState.Decryptor.DecryptData(encryptedData);
                        }
                        catch (Exception ex)
                        {
                            errorCounter.AddError($"{progressTracker} Failed to process the [{fileName}] file data: {ex}", group);
                            break;
                        }
                        // Anonymize the decrypted data
                        var decryptedDataWithUpdatedGuids = Bl4SaveDataFileHelper.AnonymizeSaveData(decryptedData, Path.GetFileNameWithoutExtension(fileName), logger, group);
                        // Try to encrypt file data
                        byte[] resignedData;
                        try
                        {
                            logger.LogInfo($"{progressTracker} Encrypting the [{fileName}] file...", group);
                            resignedData = resignerLocalState.Encryptor.EncryptData(decryptedDataWithUpdatedGuids);
                        }
                        catch (Exception ex)
                        {
                            errorCounter.AddError($"{progressTracker} Failed to process the [{fileName}] file data: {ex}", group);
                            break;
                        }
                        // Try to save the re-signed data to the output directory, preserving the folder structure
                        try
                        {
                            var outputFilePath = Path.Combine(outputDir, Path.ChangeExtension(fileName, SaveDataFileIo.SaveFileExtension));
                            File.WriteAllBytes(outputFilePath, resignedData);
                        }
                        catch (Exception ex)
                        {
                            errorCounter.AddError($"{progressTracker} Failed to save the file: {ex}", group);
                            break;
                        }
                        logger.LogInfo($"{progressTracker} Re-signed the [{fileName}] file.", group);
                        break;
                    }
                    progressTracker.Increment();
                    progressReporter.Report(progressTracker.Percentage);
                    return resignerLocalState;
                }, 
                resignerLocalState => resignerLocalState.Dispose()
            );
            LogAllTasksCompleted(progressTracker, errorCounter);
        }
        catch (OperationCanceledException ex)
        {
            errorCounter.AddWarning(ex.Message);
        }
        finally
        {
            progressReporter.Complete();
        }
    }

    /// <summary>
    /// Attempts to discover a valid Steam ID by processing the specified input file asynchronously.
    /// </summary>
    /// <param name="inputFile">The path to the input file containing data to be analyzed for Steam ID discovery.</param>
    /// <param name="cts">A CancellationTokenSource used to cancel the operation if needed. If cancellation is requested, the method will terminate early.</param>
    /// <returns>A nullable unsigned long representing the discovered Steam ID if found; otherwise, null.</returns>
    public async Task<ulong?> BruteforceSteamIdAsync(string inputFile, CancellationTokenSource cts)
        => await Task.Run(() => BruteforceSteamId(inputFile, cts));

    /// <summary>
    /// Attempts to discover the SteamID associated with a save file by performing a brute-force search over possible SteamID values.
    /// </summary>
    /// <param name="inputPath">The path to the save file or a directory containing save files. If a directory is provided, the method searches for the first file matching the expected save file extension.</param>
    /// <param name="cts">A CancellationTokenSource used to cancel the brute-force operation. Pass a token to allow the operation to be interrupted.</param>
    /// <returns>The discovered SteamID as an unsigned 64-bit integer if found; otherwise, null.</returns>
    public ulong? BruteforceSteamId(string inputPath, CancellationTokenSource cts)
    {
        ulong? result = null;
        // GET FILE TO PROCESS AND READ IT
        string fileToProcess;
        byte[] encryptedData;
        try
        {
            fileToProcess = SaveDataFileIo.GetSaveFile(inputPath);
            encryptedData = File.ReadAllBytes(fileToProcess);
        }
        catch (Exception ex)
        {
            logger.LogError(ex.Message);
            return result;
        }
        // INITIALIZE PROGRESS TRACKER
        var progressTracker = new ProgressTracker(100);
        var errorCounter = new ErrorCounter(logger);
        // BRUTE-FORCE
        const uint step = uint.MaxValue / 100;
        var fileName = Path.GetFileName(fileToProcess);
        // Setup parallel options
        var po = GetParallelOptions(cts);
        // Brute-force data in parallel
        try
        {
            Parallel.For(0, uint.MaxValue, po,
                () => new Bl4BruteforceSteamId(76561197960265729), // per-thread init
                (ctr, state, localBruteforcer) =>
                {
                    while (true)
                    {
                        if (ctr % step == 0)
                        {
                            progressReporter.Report($"{progressTracker} Brute-forcing SteamID of {fileName}...", progressTracker.Percentage);
                            progressTracker.Increment();
                        }
                        if (!localBruteforcer.TryAccountId(encryptedData, (uint)ctr)) return localBruteforcer;
                        var found = localBruteforcer.GetUnmaskedSteamIdFromKey();
                        result = found;
                        state.Stop();
                        return localBruteforcer;
                    }
                },
                localBruteforcer => localBruteforcer.Dispose()
            );
            if (result is null) 
                throw new Exception("SteamID not found.");
            
            logger.LogInfo($"Found SteamID: {result}.");
        }
        catch (Exception ex)
        {
            errorCounter.AddWarning(ex.Message);
        }
        finally
        {
            progressReporter.Complete();
        }
        return result;
    }
}