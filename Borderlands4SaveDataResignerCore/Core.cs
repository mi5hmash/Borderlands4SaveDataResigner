using Borderlands4SaveDataResignerCore.Helpers;
using Borderlands4SaveDataResignerCore.Infrastructure;
using Mi5hmasH.Logger;

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
        var filesToProcess = Directory.GetFiles(inputDir, $"*{Bl4Deencryptor.SaveFileExtension}", SearchOption.TopDirectoryOnly);
        if (filesToProcess.Length == 0) return;
        // DECRYPT
        logger.LogInfo($"Decrypting [{filesToProcess.Length}] files...");
        // Calculate the private key based on the provided user ID
        var privateKey = Bl4Deencryptor.CalculatePrivateKey(userId);
        // Create a new folder in OUTPUT directory
        var outputDir = Directories.GetNewOutputDirectory("decrypted").AddUserIdAndSuffix(userId);
        Directory.CreateDirectory(outputDir);
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
                    logger.LogInfo($"[{progress}/{filesToProcess.Length}] Decrypting the [{fileName}] file...", group);
                    var outputDataSpan = Bl4Deencryptor.DecryptData(inputDataSpan, privateKey);
                    // Save the decrypted data to the output directory, preserving the folder structure and change the file extension to .yaml
                    var outputFilePath = Path.Combine(outputDir, Path.ChangeExtension(fileName, Bl4Deencryptor.YamlFileExtension));
                    File.WriteAllBytes(outputFilePath, outputDataSpan);
                    logger.LogInfo($"[{progress}/{filesToProcess.Length}] Decrypted the [{fileName}] file.", group);
                }
                catch (Exception e)
                {
                    logger.LogError($"[{progress}/{filesToProcess.Length}] Failed to decrypt the [{fileName}] file: {e}", group);
                }
                finally
                {
                    Interlocked.Increment(ref progress);
                    progressReporter.Report((int)((double)progress / filesToProcess.Length * 100));
                }
            });
            logger.LogInfo($"[{progress}/{filesToProcess.Length}] All tasks completed.");
        }
        catch (OperationCanceledException e)
        {
            logger.LogWarning(e.Message);
        }
        finally
        {
            // Ensure progress is set to 100% at the end
            progressReporter.Report(100);
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
        var filesToProcess = Directory.GetFiles(inputDir, $"*{Bl4Deencryptor.YamlFileExtension}", SearchOption.TopDirectoryOnly);
        if (filesToProcess.Length == 0) return;
        // ENCRYPT
        logger.LogInfo($"Encrypting [{filesToProcess.Length}] files...");
        // Calculate the private key based on the provided user ID
        var privateKey = Bl4Deencryptor.CalculatePrivateKey(userId);
        // Create a new folder in OUTPUT directory
        var outputDir = Directories.GetNewOutputDirectory("encrypted").AddUserIdAndSuffix(userId);
        Directory.CreateDirectory(outputDir);
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
                    logger.LogInfo($"[{progress}/{filesToProcess.Length}] Encrypting the [{fileName}] file...", group);
                    var outputDataSpan = Bl4Deencryptor.EncryptData(inputDataSpan, privateKey);
                    // Save the encrypted data to the output directory, preserving the folder structure and change the file extension to .sav
                    var outputFilePath = Path.Combine(outputDir, Path.ChangeExtension(fileName, Bl4Deencryptor.SaveFileExtension));
                    File.WriteAllBytes(outputFilePath, outputDataSpan);
                    logger.LogInfo($"[{progress}/{filesToProcess.Length}] Encrypted the [{fileName}] file.", group);
                }
                catch (Exception e)
                {
                    logger.LogError($"[{progress}/{filesToProcess.Length}] Failed to encrypt the [{fileName}] file: {e}", group);
                }
                finally
                {
                    Interlocked.Increment(ref progress);
                    progressReporter.Report((int)((double)progress / filesToProcess.Length * 100));
                }
            });
            logger.LogInfo($"[{progress}/{filesToProcess.Length}] All tasks completed.");
        }
        catch (OperationCanceledException e)
        {
            logger.LogWarning(e.Message);
        }
        finally
        {
            // Ensure progress is set to 100% at the end
            progressReporter.Report(100); 
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
        var filesToProcess = Directory.GetFiles(inputDir, $"*{Bl4Deencryptor.SaveFileExtension}", SearchOption.TopDirectoryOnly);
        if (filesToProcess.Length == 0) return;
        // RE-SIGN
        logger.LogInfo($"Re-signing [{filesToProcess.Length}] files...");
        // Calculate the private key based on the provided user ID
        var privateKey = Bl4Deencryptor.CalculatePrivateKey(userIdInput);
        // Create a new folder in OUTPUT directory
        var outputDir = Directories.GetNewOutputDirectory("resigned").AddUserIdAndSuffix(userIdInput);
        Directory.CreateDirectory(outputDir);
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
                    logger.LogInfo($"[{progress}/{filesToProcess.Length}] Decrypting the [{fileName}] file...", group);
                    byte[] decryptedData;
                    try
                    {
                        decryptedData = Bl4Deencryptor.DecryptData(encryptedDataSpan, privateKey);
                    }
                    catch (Exception e)
                    {
                        logger.LogError($"[{progress}/{filesToProcess.Length}] Failed to decrypt the [{fileName}] file: {e}", group);
                        break; // Skip to the next file
                    }
                    // Anonymize the decrypted data
                    var decryptedDataWithUpdatedGuids = Bl4Deencryptor.AnonymizeSaveData(decryptedData, Path.GetFileNameWithoutExtension(fileName), logger, group);
                    // Encrypt the updated data with the new user's private key
                    logger.LogInfo($"[{progress}/{filesToProcess.Length}] Encrypting the [{fileName}] file...", group);
                    privateKey = Bl4Deencryptor.CalculatePrivateKey(userIdOutput);
                    byte[] resignedData;
                    try
                    {
                        resignedData = Bl4Deencryptor.EncryptData(decryptedDataWithUpdatedGuids, privateKey);
                    }
                    catch (Exception e)
                    {
                        logger.LogError($"[{progress}/{filesToProcess.Length}] Failed to encrypt the [{fileName}] file: {e}", group);
                        break; // Skip to the next file
                    }
                    // Save the re-signed data to the output directory, preserving the folder structure
                    var outputFilePath = Path.Combine(outputDir, fileName);
                    File.WriteAllBytes(outputFilePath, resignedData);
                    logger.LogInfo($"[{progress}/{filesToProcess.Length}] Re-signed the [{fileName}] file.", group);
                    break;
                }
                Interlocked.Increment(ref progress);
                progressReporter.Report((int)((double)progress / filesToProcess.Length * 100));
            });
            logger.LogInfo($"[{progress}/{filesToProcess.Length}] All tasks completed.");
        }
        catch (OperationCanceledException e)
        {
            logger.LogWarning(e.Message);
        }
        finally
        {
            // Ensure progress is set to 100% at the end
            progressReporter.Report(100);
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
    /// <exception cref="DirectoryNotFoundException">Thrown if <paramref name="inputPath"/> does not refer to an existing file or directory.</exception>
    /// <exception cref="FileNotFoundException">Thrown if no save files matching the expected extension are found in the specified directory.</exception>
    public ulong? BruteforceSteamId(string inputPath, CancellationTokenSource cts)
    {
        ulong? result = null;
        try
        {
            string fileToProcess;
            if (File.Exists(inputPath))
                fileToProcess = inputPath;
            else
            {
                if (!Directory.Exists(inputPath))
                    throw new DirectoryNotFoundException($"The provided path '{inputPath}' is not a valid file or directory.");
                var filesToProcess = Directory.GetFiles(inputPath, $"*{Bl4Deencryptor.SaveFileExtension}", SearchOption.TopDirectoryOnly);
                if (filesToProcess.Length == 0)
                    throw new FileNotFoundException($"No '{Bl4Deencryptor.SaveFileExtension}' files found in the provided directory '{inputPath}'.");
                fileToProcess = filesToProcess.FirstOrDefault() ?? string.Empty;
            }
            var fileName = Path.GetFileName(fileToProcess);
            logger.LogInfo("Brute-forcing SteamID...");
            // Setup parallel options
            var po = GetParallelOptions(cts);
            uint lap = 0;
            var inputDataSpan = File.ReadAllBytes(fileToProcess);
            const ulong universeBase = 76561197960265729;
            Parallel.For(0, uint.MaxValue, po, (ctr, state) =>
            {
                var currentSteamId = universeBase + (ulong)ctr;
                if (lap % 10_000_000 == 0)
                {
                    var progress = (double)lap / uint.MaxValue;
                    progressReporter.Report($"[{progress:P2}] Brute-forcing: {fileName}", (int)(progress * 100));
                }
                if (Bl4Deencryptor.BruteforceSteamId(inputDataSpan, currentSteamId))
                {
                    result = currentSteamId;
                    state.Stop();
                }
                Interlocked.Increment(ref lap);
            });
            logger.LogInfo(result is null ? "SteamID not found." : $"Found SteamID: {result}.");
        }
        catch (Exception e)
        {
            logger.LogWarning(e.Message);
        }
        finally
        {
            // Ensure progress is set to 100% at the end
            progressReporter.Report(100);
        }
        return result;
    }
}