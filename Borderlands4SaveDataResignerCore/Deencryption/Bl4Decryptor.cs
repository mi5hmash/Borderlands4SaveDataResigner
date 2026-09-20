using Mi5hmasH.AesCrypto.Flavors.Ecb;
using Mi5hmasH.AesCrypto.Helpers;
using Mi5hmasH.AesCrypto.Interfaces;
using static Borderlands4SaveDataResignerCore.Deencryption.Bl4DeencryptorHelper;

namespace Borderlands4SaveDataResignerCore.Deencryption;

/// <summary>
/// Provides functionality to decrypt Borderlands 4 SaveData using a user-specific private key.
/// Decryption is performed via AES in ECB mode, followed by padding removal and decompression.
/// </summary>
/// <remarks>
/// Instances of this class are not thread-safe and should be used from a single thread at a time.
/// In parallel processing scenarios, create one instance per thread and dispose each instance after use.
/// This class implements <see cref="IDisposable"/> to release the underlying cryptographic backend resources.
/// </remarks>
/// <param name="privateKey">The user-specific private key used to initialize the decryption backend.</param>
public sealed class Bl4Decryptor(ReadOnlySpan<byte> privateKey) : IDisposable
{
    /// <summary>
    /// AES-256 ECB backend used for decryption operations.
    /// Uses hardware-accelerated AES-NI instructions when available, otherwise falls back to the managed software implementation.
    /// </summary>
    private readonly IAesBackend _backend = AesHelper.IsAesNiSupported()
        ? new AesEcb256NiBackend(privateKey, true)
        : new AesEcb256ManagedBackend(GetAes(privateKey));
    
    /// <summary>
    /// Decrypts the specified encrypted data.
    /// </summary>
    /// <param name="encryptedData">The encrypted data to decrypt, represented as a read-only span of bytes.</param>
    /// <returns>A byte array containing the decrypted and decompressed data.</returns>
    /// <exception cref="InvalidDataException">Thrown if the decrypted data fails integrity checks, such as invalid length or checksum.</exception>
    public byte[] DecryptData(ReadOnlySpan<byte> encryptedData)
    {
        // Decrypt the data using AES in ECB mode
        var decryptedData = _backend.Decrypt(encryptedData);
        return RemovePaddingAndDecompress(decryptedData);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="Bl4Decryptor"/> instance and performs other cleanup operations.
    /// </summary>
    /// <remarks>
    /// Disposes the underlying AES ECB backend if it implements <see cref="IDisposable"/>.
    /// After calling this method, the instance cannot be used to perform decryption operations.
    /// </remarks>
    public void Dispose()
    {
        if (_backend is IDisposable d)
            d.Dispose();
    }
}