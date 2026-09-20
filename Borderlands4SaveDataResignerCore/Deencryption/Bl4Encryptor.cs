using System.Buffers.Binary;
using Mi5hmasH.AesCrypto.Flavors.Ecb;
using Mi5hmasH.AesCrypto.Helpers;
using Mi5hmasH.AesCrypto.Interfaces;
using static Borderlands4SaveDataResignerCore.Deencryption.Bl4DeencryptorHelper;
using Mi5hmasH.Compressors;

namespace Borderlands4SaveDataResignerCore.Deencryption;

/// <summary>
/// Represents a thread-specific encryptor that encrypts Borderlands 4 SaveData using AES in ECB mode.
/// Takes a user-derived private key and provides encryption with ZLib compression, Adler32 checksum validation, and
/// length metadata appended to the payload prior to encryption.
/// Implements <see cref="IDisposable"/> to release the underlying AES backend resources.
/// </summary>
/// <param name="privateKey">The derived private key used as the AES encryption key.</param>
public sealed class Bl4Encryptor(ReadOnlySpan<byte> privateKey) : IDisposable
{
    /// <summary>
    /// The AES-256 ECB backend used for encryption operations.
    /// Uses hardware-accelerated AES-NI instructions when available; otherwise falls back to the managed .NET cryptography implementation.
    /// </summary>
    private readonly IAesBackend _backend = AesHelper.IsAesNiSupported()
        ? new AesEcb256NiBackend(privateKey, false)
        : new AesEcb256ManagedBackend(GetAes(privateKey));
    
    /// <summary>
    /// Encrypts the specified data using AES encryption in ECB mode.
    /// </summary>
    /// <param name="decryptedData">The data to be encrypted, provided as a read-only span of bytes.</param>
    /// <returns>A byte array containing the encrypted data.</returns>
    public byte[] EncryptData(ReadOnlySpan<byte> decryptedData)
    {
        // Normalize line endings 
        decryptedData = decryptedData.CrLfToLf();
        // Calculate checksum and length of the decrypted data
        var decryptedDataChecksum = decryptedData.ComputeAdler32Checksum();
        var decryptedDataLength = decryptedData.Length;
        // Compress the decrypted data using Zlib
        var compressedData = decryptedData.ToArray().ZLibCompress();
        // Prepare the data to be encrypted by appending the checksum and length
        var dataToEncrypt = CreateSaveDataContainerWithPadding(compressedData.Length + 8);
        var dataToEncryptSpan = dataToEncrypt.AsSpan();
        compressedData.CopyTo(dataToEncryptSpan);
        BinaryPrimitives.WriteUInt32BigEndian(dataToEncryptSpan[^8..^4], decryptedDataChecksum);
        BinaryPrimitives.WriteInt32LittleEndian(dataToEncryptSpan[^4..], decryptedDataLength);
        // Encrypt the data using AES in ECB mode
        return _backend.Encrypt(dataToEncryptSpan);
    }

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="Bl4Encryptor"/> instance and disposes of the internal AES backend.
    /// </summary>
    public void Dispose()
    {
        if (_backend is IDisposable d)
            d.Dispose();
    }
}