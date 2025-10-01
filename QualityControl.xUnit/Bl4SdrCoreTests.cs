using Borderlands4SaveDataResignerCore;
using Borderlands4SaveDataResignerCore.Helpers;
using Mi5hmasH.Logger;

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
        var privateKey = Bl4Deencryptor.CalculatePrivateKey("76561197960265729");

        // Act
        var decryptedData = Bl4Deencryptor.DecryptData(Properties.Resources.encryptedFile, privateKey);

        // Assert
        Assert.Equal(decryptedData, Properties.Resources.decryptedFile);
    }

    [Fact]
    public void EncryptFiles_DoesEncrypt()
    {
        // Arrange
        var privateKey = Bl4Deencryptor.CalculatePrivateKey("76561197960265729");

        // Act
        var encryptedData = Bl4Deencryptor.EncryptData(Properties.Resources.decryptedFile, privateKey);
        var decryptedData = Bl4Deencryptor.DecryptData(encryptedData, privateKey);

        // Assert
        Assert.Equal(decryptedData, Properties.Resources.decryptedFile);
    }
}