namespace Borderlands4SaveDataResignerCore.Deencryption.Aes256.Backends;

/// <summary>
/// Provides AES-256 ECB encryption and decryption backed by hardware-accelerated AES-NI instructions.
/// This implementation uses the CPU's native AES-NI instruction set for optimized block cipher
/// operations and is selected when the platform supports it, otherwise a managed fallback
/// backend is used.
/// </summary>
/// <remarks>
/// The backend pre-computes the forward round-key schedule during construction and,
/// when configured as a decryptor, additionally computes the inverted key schedule
/// for efficient decryption. Key material can be replaced at any time by calling
/// <see cref="SetKey"/>, which re-derives both schedules.
/// </remarks>
public sealed class AesNiBackend : IAesEcbBackend
{
    private readonly bool _isDecryptor;
    private readonly byte[]? _encKeys;
    private readonly byte[]? _decKeys;

    /// <summary>
    /// Hardware-accelerated AES-256 ECB backend that leverages AES-NI (Advanced Encryption Standard New Instructions)
    /// instructions for encryption and decryption operations.
    /// </summary>
    /// <remarks>
    /// This backend should only be instantiated when AES-NI support has been confirmed via
    /// <see cref="AesEcb256Ni.IsAesNiSupported()"/>. It expands the provided 256-bit key into
    /// round key buffers using unmanaged memory allocation. When configured as a decryptor,
    /// it additionally computes the inverted key schedule required for the decryption path.
    /// </remarks>
    /// <seealso cref="IAesEcbBackend" />
    public AesNiBackend(ReadOnlySpan<byte> key256, bool isDecryptor)
    {
        _isDecryptor = isDecryptor;
        _encKeys = GC.AllocateUninitializedArray<byte>(AesEcb256Ni.RoundKeyBufferLength);
        key256.ExpandKey256(_encKeys);
        if (!_isDecryptor) return;
        _decKeys = GC.AllocateUninitializedArray<byte>(AesEcb256Ni.RoundKeyBufferLength);
        _encKeys.InvertKeySchedule(_decKeys);
    }

    /// <summary>
    /// Replaces the current AES-256 key material and re-derives the forward and (if applicable)
    /// inverted round-key schedules used for encryption and decryption operations.
    /// </summary>
    /// <param name="key256">A read-only span containing the 256-bit (32-byte) key to expand into round keys.</param>
    public void SetKey(ReadOnlySpan<byte> key256)
    {
        key256.ExpandKey256(_encKeys);
        if (!_isDecryptor) return;
        _encKeys.InvertKeySchedule(_decKeys);
    }

    /// <summary>
    /// Encrypts the specified plaintext data using AES-256 in ECB mode via AES-NI hardware instructions.
    /// </summary>
    /// <param name="data">The plaintext byte sequence to be encrypted. The length must be a multiple of the AES block size (16 bytes).</param>
    /// <returns>A newly allocated byte array containing the ciphertext of the same length as the input data.</returns>
    public byte[] Encrypt(ReadOnlySpan<byte> data)
    {
        var output = GC.AllocateUninitializedArray<byte>(data.Length);
        AesEcb256Ni.Encrypt(_encKeys, data, output);
        return output;
    }

    /// <summary>
    /// Decrypts the provided ciphertext using the AES-256 ECB mode with hardware-accelerated AES-NI instructions.
    /// </summary>
    /// <param name="data">The ciphertext data to decrypt. Must be a multiple of the AES block size (16 bytes).</param>
    /// <returns>A newly allocated byte array containing the plaintext resulting from the decryption operation.</returns>
    public byte[] Decrypt(ReadOnlySpan<byte> data)
    {
        var output = GC.AllocateUninitializedArray<byte>(data.Length);
        AesEcb256Ni.Decrypt(_decKeys, data, output);
        return output;
    }
}