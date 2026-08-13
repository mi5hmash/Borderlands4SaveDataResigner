using System.Security.Cryptography;
using static Borderlands4SaveDataResignerCore.Deencryption.Bl4DeencryptorHelper;

namespace Borderlands4SaveDataResignerCore.Deencryption.Aes256.Backends;

public sealed class AesManagedBackend(ReadOnlySpan<byte> key256) : IAesEcbBackend, IDisposable
{
    /// <summary>
    /// The underlying <see cref="Aes"/> instance configured in ECB mode with no padding, used to perform AES-256 encryption and decryption operations.
    /// </summary>
    private readonly Aes _aes = GetAes(key256);

    /// <summary>
    /// Sets the AES-256 key used for subsequent encryption or decryption operations.
    /// </summary>
    /// <param name="key256">A read-only span containing the 256-bit (32-byte) AES key to be assigned to the internal AES instance.</param>
    public void SetKey(ReadOnlySpan<byte> key256)
        => _aes.Key = key256.ToArray();

    /// <summary>
    /// Encrypts the provided plaintext data using AES-256 in ECB mode with the key assigned to this backend.
    /// </summary>
    /// <param name="data">A read-only span containing the plaintext byte array to be encrypted.</param>
    /// <returns>A new byte array containing the AES-256 ECB ciphertext.</returns>
    public byte[] Encrypt(ReadOnlySpan<byte> data)
        => EncryptEcb(data, _aes);

    /// <summary>
    /// Decrypts the provided ECB-mode ciphertext using the AES-256 key configured for this backend instance.
    /// </summary>
    /// <param name="data">A read-only span containing the ECB-mode encrypted byte sequence to be decrypted.</param>
    /// <returns>A new byte array containing the decrypted plaintext data.</returns>
    public byte[] Decrypt(ReadOnlySpan<byte> data)
        => DecryptEcb(data, _aes);

    /// <summary>
    /// Releases the unmanaged resources used by the underlying <see cref="Aes"/> instance and frees all associated resources held by this backend.
    /// </summary>
    public void Dispose() => _aes.Dispose();
}
