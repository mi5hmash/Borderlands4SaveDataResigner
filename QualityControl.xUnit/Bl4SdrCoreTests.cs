using Borderlands4SaveDataResignerCore;
using Borderlands4SaveDataResignerCore.Deencryption;
using Mi5hmasH.Logger;
using Mi5hmasH.Progress;
using static Borderlands4SaveDataResignerCore.Deencryption.Bl4DeencryptorHelper;

namespace QualityControl.xUnit;

public sealed class Bl4SdrCoreTests : IDisposable
{
    private readonly Core _core;
    private readonly ITestOutputHelper _output;

    public Bl4SdrCoreTests(ITestOutputHelper output)
    {
        _output = output;
        _output.WriteLine("SETUP");

        // Setup
        var logger = new SimpleLogger();
        var progressReporter = new ProgressReporter(null, null);
        _core = new Core(logger, progressReporter);
    }

    public void Dispose()
    {
        _output.WriteLine("CLEANUP");
    }
    
    [Fact]
    public async Task DecryptFilesAsync_DoesNotThrow_WhenNoFiles()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var testResult = true;

        // Act
        try
        {
            await _core.DecryptFilesAsync(tempDir, "userId", cts);
        }
        catch
        {
            testResult = false;
        }
        Directory.Delete(tempDir);

        // Assert
        Assert.True(testResult);
    }

    [Fact]
    public async Task EncryptFilesAsync_DoesNotThrow_WhenNoFiles()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var testResult = true;

        // Act
        try
        {
            await _core.EncryptFilesAsync(tempDir, "userId", cts);
        }
        catch
        {
            testResult = false;
        }
        Directory.Delete(tempDir);

        // Assert
        Assert.True(testResult);
    }

    [Fact]
    public async Task ResignFilesAsync_DoesNotThrow_WhenNoFiles()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);
        var testResult = true;

        // Act
        try
        {
            await _core.ResignFilesAsync(tempDir, "userIdInput", "userIdOutput", cts);
        }
        catch
        {
            testResult = false;
        }
        Directory.Delete(tempDir);

        // Assert
        Assert.True(testResult);
    }

    [Fact]
    public void DecryptFiles_DoesDecrypt()
    {
        // Arrange
        using var decryptor = new Bl4Decryptor(CalculatePrivateKey("76561197960265729"));

        // Act
        var decryptedData = decryptor.DecryptData(Properties.Resources.encryptedFile);

        // Assert
        Assert.Equal(decryptedData, Properties.Resources.decryptedFile);
    }
    
    [Fact]
    public void BruteforceFile_DoesBruteforce()
    {
        // Arrange
        using var bruteforcer = new Bl4BruteforceSteamId(76561197960265729);

        // Act
        var test = bruteforcer.TryAccountId(Properties.Resources.encryptedFile, 1);

        // Assert
        Assert.True(test);
    }
    
    [Fact]
    public void BruteforceFile_DoesFailWithWrongUserId()
    {
        // Arrange
        using var bruteforcer = new Bl4BruteforceSteamId(76561197960265729);

        // Act
        var test = bruteforcer.TryAccountId(Properties.Resources.encryptedFile, 2);

        // Assert
        Assert.False(test);
    }

    [Fact]
    public void EncryptFiles_DoesEncrypt()
    {
        // Arrange
        using var encryptor = new Bl4Encryptor(CalculatePrivateKey("76561197960265729"));
        using var decryptor = new Bl4Decryptor(CalculatePrivateKey("76561197960265729"));

        // Act
        var encryptedData = encryptor.EncryptData(Properties.Resources.decryptedFile);
        var decryptedData = decryptor.DecryptData(encryptedData);

        // Assert
        Assert.Equal(decryptedData, Properties.Resources.decryptedFile);
    }
}