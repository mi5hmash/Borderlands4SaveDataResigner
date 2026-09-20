using System.Buffers.Binary;
using System.Security.Cryptography;
using Mi5hmasH.Compressors;
using Mi5hmasH.GameLaunchers.Epic.Types;
using Mi5hmasH.GameLaunchers.Steam.Types;
using static Mi5hmasH.AesCrypto.Helpers.AesHelper;

namespace Borderlands4SaveDataResignerCore.Deencryption;

public static class Bl4DeencryptorHelper
{
    /// <summary>
    /// Represents the public key used for cryptographic operations.
    /// </summary>
    public static readonly byte[] PublicKey =
    [
        0x35, 0xEC, 0x33, 0x77, 0xF3, 0x5D, 0xB0, 0xEA, 0xBE, 0x6B, 0x83, 0x11, 0x54, 0x03, 0xEB, 0xFB,
        0x27, 0x25, 0x64, 0x2E, 0xD5, 0x49, 0x06, 0x29, 0x05, 0x78, 0xBD, 0x60, 0xBA, 0x4A, 0xA7, 0x87
    ];
    
    /// <summary>
    /// Represents the default padding mode used for AES encryption and decryption operations. In this case, no padding is applied to the data.
    /// </summary>
    private const PaddingMode DefaultPadding = PaddingMode.None;
    
    /// <summary>
    /// Creates and initializes an <see cref="Aes"/> instance with the specified key.
    /// </summary>
    /// <param name="key">The encryption key to be used.</param>
    /// <returns>A new <see cref="Aes"/> instance configured with ECB mode and no padding.</returns>
    public static Aes GetAes(ReadOnlySpan<byte> key)
    {
        var aes = Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Padding = DefaultPadding;
        aes.Key = key.ToArray();
        return aes;
    }
    
    /// <summary>
    /// Parses the specified user ID string and converts it into a byte array.
    /// </summary>
    /// <param name="input">The user ID string to parse. Must be a valid Epic ID or Steam ID.</param>
    /// <returns>A byte array representation of the user ID. The format of the byte array depends on whether the input is an Epic ID or a Steam ID.</returns>
    /// <exception cref="FormatException">Thrown if the input is not a valid Epic ID or Steam ID, or if the input exceeds the maximum allowed length.</exception>
    private static byte[] ParseUserId(string input)
    {
        var epicId = new EpicId();
        var result = epicId.TrySetEpicId(input);
        if (result) return epicId.GetAsWideString();

        var steamId = new SteamId();
        result = steamId.Set(input);
        return result
            ? steamId.GetSteamId64AsByteArray()
            : throw new FormatException("Invalid user ID format. Must be a valid Epic ID or Steam ID.");
    }

    /// <summary>
    /// Calculates a private key based on the provided user ID.
    /// </summary>
    /// <param name="userId">The user ID used to generate the private key.</param>
    /// <returns>A byte array representing the calculated private key.</returns>
    public static byte[] CalculatePrivateKey(string userId)
    {
        var uid = ParseUserId(userId);
        return CalculatePrivateKey(uid);
    }
    /// <summary>
    /// Calculates a private key based on the provided user ID.
    /// </summary>
    /// <param name="userId">A read-only span of bytes representing the user identifier to be combined with the public key.</param>
    /// <returns>A byte array representing the calculated private key.</returns>
    private static byte[] CalculatePrivateKey(ReadOnlySpan<byte> userId)
    {
        Span<byte> keyContainer = stackalloc byte[PublicKey.Length];
        var length = Math.Min(userId.Length, keyContainer.Length);
        PublicKey.CopyTo(keyContainer);
        for (var i = 0; i < length; i++)
            keyContainer[i] = (byte)(keyContainer[i] ^ userId[i]);
        return keyContainer.ToArray();
    }
    
    /// <summary>
    /// Removes PKCS#7 padding from decrypted data, decompresses the Zlib-compressed payload, and validates integrity against embedded length and checksum metadata.
    /// </summary>
    /// <param name="decryptedData">The AES-decrypted byte array that may contain PKCS#7 padding followed by a 4-byte little-endian expected length, an 8-byte big-endian expected checksum header, and the remaining Zlib-compressed data.</param>
    /// <returns>The decompressed payload as a byte array with verified integrity.</returns>
    /// <exception cref="InvalidDataException">Thrown when the decompressed data's actual length or computed Adler-32 checksum does not match the embedded metadata after all recovery attempts fail.</exception>
    public static byte[] RemovePaddingAndDecompress(ReadOnlySpan<byte> decryptedData)
    {
        // Assume there is PKCS#7 padding and remove it
        var paddingLength = GetPossiblePkcs7PaddingLength(decryptedData);
        var isTherePadding = paddingLength > 0;
        var decryptedDataWithoutPadding = isTherePadding
            ? decryptedData[..^paddingLength]
            : decryptedData;
        // Get the expected length and expected checksum from the decrypted data without padding
        var expectedDataLength = BinaryPrimitives.ReadInt32LittleEndian(decryptedDataWithoutPadding[^4..]);
        // Checksum is stored in big-endian format
        var expectedChecksum = BinaryPrimitives.ReadUInt32BigEndian(decryptedDataWithoutPadding[^8..^4]);
        // Try to decompress the decrypted data using Zlib
        var decompressedData = decryptedDataWithoutPadding[..^8].ToArray().ZLibDecompress();
        // Verify the length and checksum of the decompressed data
        var isLengthValid = decompressedData.Length == expectedDataLength;
        var isChecksumValid = decompressedData.ComputeAdler32Checksum() == expectedChecksum;
        // If the length or checksum is invalid and there was padding, try without removing the padding
        if ((!isLengthValid || !isChecksumValid) && isTherePadding)
        {
            expectedDataLength = BinaryPrimitives.ReadInt32LittleEndian(decryptedData[^4..]);
            expectedChecksum = BinaryPrimitives.ReadUInt32BigEndian(decryptedData[^8..^4]);
            decompressedData = decryptedData[..^8].ToArray().ZLibDecompress();
            isLengthValid = decompressedData.Length == expectedDataLength;
            isChecksumValid = decompressedData.ComputeAdler32Checksum() == expectedChecksum;
            if (!isLengthValid || !isChecksumValid)
                throw new InvalidDataException("Decryption failed: Invalid length or checksum.");
        }
        return decompressedData;
    }

    /// <summary>
    /// Creates a byte array container for SaveData, padded to the next 16-byte boundary using PKCS#7-style padding.
    /// </summary>
    /// <param name="dataLength">The length of the original data that will occupy the container.
    /// The container is extended and padded so its total length is aligned to a 16-byte boundary.</param>
    /// <returns>
    /// A byte array of the padded length, where the trailing bytes contain the padding value indicating
    /// how many padding bytes were added. The remaining bytes are uninitialized.
    /// </returns>
    public static byte[] CreateSaveDataContainerWithPadding(int dataLength)
    {
        var padLen = CalculatePkcs7PaddingLength(dataLength);
        var dataContainer = GC.AllocateUninitializedArray<byte>(dataLength + padLen);
        AddPkcs7PaddingInPlace(dataContainer, padLen);
        
        return dataContainer;
    }
}