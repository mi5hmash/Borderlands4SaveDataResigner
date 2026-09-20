using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Mi5hmasH.AesCrypto.Flavors.Ecb;
using Mi5hmasH.AesCrypto.Helpers;
using Mi5hmasH.AesCrypto.Interfaces;
using Mi5hmasH.Compressors;
using static Borderlands4SaveDataResignerCore.Deencryption.Bl4DeencryptorHelper;

namespace Borderlands4SaveDataResignerCore.Deencryption;

/// <summary>
/// Provides brute-force decryption of Borderlands 4 SaveData by iterating over Steam account identifiers to recover
/// the correct Steam ID used in AES-256-ECB key derivation. Constructs a key template from a given universe base value
/// and leverages hardware-accelerated AES-NI or a managed AES backend for candidate validation against encrypted SaveData.
/// </summary>
/// <remarks>
/// Instances of this class are thread-safe for concurrent use within a single account-ID trial, but each parallel
/// worker should construct its own instance to avoid shared mutable state. The class implements <see cref="IDisposable"/>
/// and must be disposed to release the underlying cryptographic backend resources.
/// </remarks>
public sealed class Bl4BruteforceSteamId : IDisposable
{
    private static readonly uint AccountIdMask = BinaryPrimitives.ReadUInt32LittleEndian(PublicKey);
    private readonly byte[] _keyTemplate;
    private readonly IAesBackend _backend;

    /// <summary>
    /// Initializes a new instance of the <see cref="Bl4BruteforceSteamId"/> class with the specified universe base value.
    /// </summary>
    /// <param name="universeBase">The universe base value used to construct the key template.</param>
    public Bl4BruteforceSteamId(ulong universeBase)
    {
        _keyTemplate = BuildSteamBruteforceKeyTemplate(universeBase);
        _backend = AesHelper.IsAesNiSupported()
            ? new AesEcb256NiBackend(_keyTemplate, true)
            : new AesEcb256ManagedBackend(GetAes(_keyTemplate));
    }

    /// <summary>
    /// Builds a key template for brute-forcing Steam IDs based on the provided universe base value.
    /// </summary>
    /// <param name="universeBase">The universe base value used to construct the key template.</param>
    /// <returns>A byte array representing the key template for brute-forcing Steam IDs.</returns>
    private static byte[] BuildSteamBruteforceKeyTemplate(ulong universeBase)
    {
        Span<byte> template = stackalloc byte[32];
        PublicKey.CopyTo(template);
        var upperSteam = (uint)(universeBase >> 32);
        var templateAsUlongSpan = MemoryMarshal.Cast<byte, uint>(template);
        templateAsUlongSpan[1] ^= upperSteam;
        return template.ToArray();
    }

    /// <summary>
    /// Computes the unmasked Steam ID by XORing the first 8 bytes of the AES key with the last 8 bytes of the public key.
    /// </summary>
    /// <returns>The unmasked Steam ID value derived from the encryption key and public key.</returns>
    public ulong GetUnmaskedSteamIdFromKey()
    {
        var maskedSteamId = BinaryPrimitives.ReadUInt64LittleEndian(_keyTemplate.AsSpan()[..8]);
        return maskedSteamId ^ BinaryPrimitives.ReadUInt64LittleEndian(PublicKey.AsSpan()[..8]);
    }

    /// <summary>
    /// Attempts to determine whether the specified Account ID can successfully decrypt the provided encrypted data using a brute-force approach.
    /// </summary>
    /// <param name="encryptedData">A read-only span of bytes containing the encrypted data to test for decryption.</param>
    /// <param name="accountId">An Account ID to use when calculating the decryption key.</param>
    /// <returns><see langword="true"/> if the encrypted data can be decrypted with the specified Account ID; otherwise, <see langword="false"/>.</returns>
    public bool TryAccountId(ReadOnlySpan<byte> encryptedData, uint accountId)
    {
        // Update keyTemplate
        var maskedAccountId = accountId ^ AccountIdMask;
        BinaryPrimitives.WriteUInt32LittleEndian(_keyTemplate, maskedAccountId);
        _backend.SetKey(_keyTemplate);
        // Decrypt the data using AES in ECB mode
        var decryptedData = _backend.Decrypt(encryptedData[..16]);
        // Check if the decrypted data starts with the expected Zlib header (0x78, 0x9C)
        if (!decryptedData.IsZLib()) return false;
        // Try to fully decrypt the data to ensure it's valid
        decryptedData = _backend.Decrypt(encryptedData);
        try { _ = RemovePaddingAndDecompress(decryptedData); }
        catch { return false; }
        return true;
    }

    /// <summary>
    /// Releases the unmanaged resources used by the <see cref="Bl4BruteforceSteamId"/> instance and disposes of any managed resources held by the underlying AES ECB backend.
    /// </summary>
    public void Dispose()
    {
        if (_backend is IDisposable d)
            d.Dispose();
    }
}