namespace Borderlands4SaveDataResignerCore.Deencryption.Aes256.Backends;

/// <summary>
/// Defines the contract for an AES-256 ECB mode encryption/decryption backend.
/// This interface abstracts the underlying cryptographic implementation, allowing hardware-accelerated (AES-NI)
/// and managed software backends to be used interchangeably for encrypting and decrypting data blocks in ECB mode.
/// Implementations must accept a 256-bit key and perform symmetric encryption or decryption on the provided input data.
/// </summary>
public interface IAesEcbBackend
{
    void SetKey(ReadOnlySpan<byte> key256);
    byte[] Encrypt(ReadOnlySpan<byte> data);
    byte[] Decrypt(ReadOnlySpan<byte> data);
}