using Borderlands4SaveDataResigner.Types;
using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Borderlands4SaveDataResigner.Logger;

namespace Borderlands4SaveDataResigner.Helpers;

public static class Bl4Deencryptor
{
    /// <summary>
    /// Represents the public key used for cryptographic operations.
    /// </summary>
    private static readonly byte[] PublicKey =
    [
        0x35, 0xEC, 0x33, 0x77, 0xF3, 0x5D, 0xB0, 0xEA, 0xBE, 0x6B, 0x83, 0x11, 0x54, 0x03, 0xEB, 0xFB,
        0x27, 0x25, 0x64, 0x2E, 0xD5, 0x49, 0x06, 0x29, 0x05, 0x78, 0xBD, 0x60, 0xBA, 0x4A, 0xA7, 0x87
    ];
    
    /// <summary>
    /// Creates and initializes an <see cref="Aes"/> instance with the specified key.
    /// </summary>
    /// <param name="key">The encryption key to be used.</param>
    /// <returns>A new <see cref="Aes"/> instance configured with ECB mode and no padding.</returns>
    private static Aes GetAes(ReadOnlySpan<byte> key)
    {
        var aes = Aes.Create();
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
        aes.Key = key.ToArray();
        return aes;
    }
    
    /// <summary>
    /// Decrypts the specified encrypted data using AES in ECB mode.
    /// </summary>
    /// <param name="encryptedBytes">The encrypted data to decrypt.</param>
    /// <param name="key">The encryption key to use for decryption.</param>
    /// <returns>A byte array containing the decrypted data.</returns>
    private static byte[] DecryptEcb(ReadOnlySpan<byte> encryptedBytes, ReadOnlySpan<byte> key)
    {
        using var aes = GetAes(key);
        using MemoryStream msi = new(encryptedBytes.ToArray());
        using var decryptor = aes.CreateDecryptor();
        using CryptoStream cs = new(msi, decryptor, CryptoStreamMode.Read);
        using MemoryStream mso = new();
        cs.CopyTo(mso);
        return mso.ToArray();
    }

    /// <summary>
    /// Encrypts the specified plaintext using AES encryption in ECB (Electronic Codebook) mode with PKCS7 padding.
    /// </summary>
    /// <param name="plainBytes">The plaintext data to encrypt, represented as a read-only span of bytes.</param>
    /// <param name="key">The encryption key, represented as a read-only span of bytes.</param>
    /// <returns>A byte array containing the encrypted data.</returns>
    private static byte[] EncryptEcb(ReadOnlySpan<byte> plainBytes, ReadOnlySpan<byte> key)
    {
        var plainBytesWithPadding = AddPkcs7Padding(plainBytes);
        using var aes = GetAes(key);
        using var encryptor = aes.CreateEncryptor();
        using MemoryStream ms = new();
        using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write, true);
        cs.Write(plainBytesWithPadding, 0, plainBytesWithPadding.Length);
        cs.FlushFinalBlock();
        return ms.ToArray();
    }

    /// <summary>
    /// Adds PKCS#7 padding to the specified data to ensure its length is a multiple of the block size.
    /// </summary>
    /// <param name="data">The input data to which padding will be added.</param>
    /// <returns>A new byte array containing the original data followed by PKCS#7 padding bytes.</returns>
    private static byte[] AddPkcs7Padding(ReadOnlySpan<byte> data)
    {
        const int blockSize = 16;
        var padLen = blockSize - data.Length % blockSize;
        var newDataLength = data.Length + padLen;
        var dataWithPadding = new byte[newDataLength];
        var dataWithPaddingAsSpan = dataWithPadding.AsSpan();
        data.CopyTo(dataWithPaddingAsSpan);
        for (var i = 1; i < padLen + 1; i++)
            dataWithPaddingAsSpan[^i] = (byte)padLen;
        return dataWithPadding;
    }
    
    /// <summary>
    /// Determines the possible PKCS#7 padding length in the provided data.
    /// </summary>
    /// <param name="data">A read-only span of bytes representing the data to analyze. The data must be at least 16 bytes long to contain valid PKCS#7 padding.</param>
    /// <returns>The length of the PKCS#7 padding if valid padding is detected; otherwise, 0.</returns>
    private static int GetPossiblePkcs7PaddingLength(ReadOnlySpan<byte> data)
    {
        if (data.Length < 16) return 0;
        var padLen = data[^1];
        if (padLen is > 16 or < 1) return 0;
        for (var i = 2; i < padLen; i++)
        {
            var result = data[^1] == data[^(padLen - i)];
            if (!result) return 0;
        }
        return padLen;
    }

    /// <summary>
    /// Removes PKCS#7 padding from the specified data.
    /// </summary>
    /// <param name="data">The input data from which the padding will be removed.</param>
    /// <param name="paddingLength">The length of the padding to remove, in bytes.</param>
    /// <returns>A new byte array containing the data with the specified padding removed.</returns>
    private static byte[] RemovePkcs7Padding(ReadOnlySpan<byte> data, int paddingLength)
        => data[..^paddingLength].ToArray();

    /// <summary>
    /// Decompresses a byte array that was compressed using the Zlib compression format.
    /// </summary>
    /// <param name="compressedData">A byte array containing the data to decompress.</param>
    /// <returns>A byte array containing the decompressed data.</returns>
    private static byte[] DecompressZlib(byte[] compressedData)
    {
        using var inputStream = new MemoryStream(compressedData);
        using var zLibStream = new ZLibStream(inputStream, CompressionMode.Decompress);
        using var outputStream = new MemoryStream();
        zLibStream.CopyTo(outputStream);
        return outputStream.ToArray();
    }

    /// <summary>
    /// Compresses the specified data using the Zlib compression algorithm.
    /// </summary>
    /// <param name="data">The data to compress, represented as a read-only span of bytes.</param>
    /// <returns>A byte array containing the compressed data.</returns>
    private static byte[] CompressZlib(ReadOnlySpan<byte> data)
    {
        using var outputStream = new MemoryStream();
        using (var zLibStream = new ZLibStream(outputStream, CompressionLevel.Optimal, true))
        {
            zLibStream.Write(data);
        }
        return outputStream.ToArray();
    }
    
    /// <summary>
    /// Computes the Adler-32 checksum for the specified data.
    /// </summary>
    /// <param name="data">A read-only span of bytes representing the input data for which the checksum is calculated.</param>
    /// <returns>The computed Adler-32 checksum as an unsigned 32-bit integer.</returns>
    private static uint ComputeAdler32Checksum(ReadOnlySpan<byte> data)
    {
        const uint modAdler = 65521;
        uint a = 1, b = 0;
        foreach (var c in data)
        {
            a = (a + c) % modAdler;
            b = (b + a) % modAdler;
        }
        return (b << 16) | a;
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
        if (result) return epicId.GetAsWideStringArray();
        
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
        Span<byte> keyContainer = stackalloc byte[PublicKey.Length];
        var length = Math.Min(uid.Length, keyContainer.Length);
        PublicKey.CopyTo(keyContainer);
        for (var i = 0; i < length; i++)
            keyContainer[i] = (byte)(keyContainer[i] ^ uid[i]);
        return keyContainer.ToArray();
    }

    /// <summary>
    /// Decrypts the specified encrypted data using the provided private key.
    /// </summary>
    /// <param name="encryptedData">The encrypted data to decrypt, represented as a read-only span of bytes.</param>
    /// <param name="privateKey">The private key used for decryption, represented as a read-only span of bytes.</param>
    /// <returns>A byte array containing the decrypted and decompressed data.</returns>
    /// <exception cref="InvalidDataException">Thrown if the decrypted data fails integrity checks, such as invalid length or checksum.</exception>
    public static byte[] DecryptData(ReadOnlySpan<byte> encryptedData, ReadOnlySpan<byte> privateKey)
    {
        // Decrypt the data using AES in ECB mode
        var decryptedData = DecryptEcb(encryptedData, privateKey);
        // Assume there is PKCS#7 padding and remove it
        var paddingLength = GetPossiblePkcs7PaddingLength(decryptedData);
        var isTherePadding = paddingLength > 0;
        var decryptedDataWithoutPadding = isTherePadding 
            ? RemovePkcs7Padding(decryptedData, paddingLength) 
            : decryptedData;
        // Get the expected length and expected checksum from the decrypted data without padding
        var expectedDataLength = BitConverter.ToInt32(decryptedDataWithoutPadding[^4..], 0);
        // Checksum is stored in big-endian format so we need to reverse the byte order
        var expectedChecksum = BinaryPrimitives.ReadUInt32BigEndian(decryptedDataWithoutPadding.AsSpan()[^8..^4]);
        // Try to decompress the decrypted data using Zlib
        var decompressedData = DecompressZlib(decryptedDataWithoutPadding[..^8]);
        // Verify the length and checksum of the decompressed data
        var isLengthValid = decompressedData.Length == expectedDataLength;
        var isChecksumValid = ComputeAdler32Checksum(decompressedData) == expectedChecksum;
        // If the length or checksum is invalid and there was padding, try without removing the padding
        if ((!isLengthValid || !isChecksumValid) && isTherePadding)
        {
            expectedDataLength = BitConverter.ToInt32(decryptedData[^4..], 0);
            expectedChecksum = BinaryPrimitives.ReadUInt32BigEndian(decryptedData.AsSpan()[^8..^4]);
            decompressedData = DecompressZlib(decryptedData[..^8]);
            isLengthValid = decompressedData.Length == expectedDataLength;
            isChecksumValid = ComputeAdler32Checksum(decompressedData) == expectedChecksum;
            if (!isLengthValid || !isChecksumValid)
                throw new InvalidDataException("Decryption failed: Invalid length or checksum.");
        }
        return decompressedData;
    }

    /// <summary>
    /// Encrypts the specified data using AES encryption in ECB mode.
    /// </summary>
    /// <param name="decryptedData">The data to be encrypted, provided as a read-only span of bytes.</param>
    /// <param name="privateKey">The private key used for encryption, provided as a read-only span of bytes.</param>
    /// <returns>A byte array containing the encrypted data.</returns>
    public static byte[] EncryptData(ReadOnlySpan<byte> decryptedData, ReadOnlySpan<byte> privateKey)
    {
        // Calculate checksum and length of the decrypted data
        var decryptedDataChecksum = ComputeAdler32Checksum(decryptedData);
        var decryptedDataLength = decryptedData.Length;
        // Compress the decrypted data using Zlib
        var compressedData = CompressZlib(decryptedData);
        // Prepare the data to be encrypted by appending the checksum and length
        var dataToEncrypt = new byte[compressedData.Length + 8];
        var dataToEncryptSpan = dataToEncrypt.AsSpan();
        compressedData.CopyTo(dataToEncryptSpan);
        BinaryPrimitives.WriteInt32BigEndian(dataToEncryptSpan.Slice(compressedData.Length, 4), (int)decryptedDataChecksum);
        BitConverter.GetBytes(decryptedDataLength).CopyTo(dataToEncrypt, compressedData.Length + 4);
        // Encrypt the data using AES in ECB mode
        return EncryptEcb(dataToEncrypt, privateKey);
    }

    /// <summary>
    /// Anonymizes the data in the provided YAML content based on the specified file name.
    /// </summary>
    /// <param name="yamlContent">The YAML content as a byte array to be updated.</param>
    /// <param name="fileNameWithoutExtension">The name of the file, without its extension, used to determine the type of data updates to apply.</param>
    /// <param name="logger">An instance of <see cref="SimpleLogger"/> used to log warnings if the anonymization fail.</param>
    /// <param name="loggerGroup">The logger group name used to categorize log messages.</param>
    /// <returns>A byte array containing the updated YAML content.</returns>
    public static byte[] AnonymizeSaveData(byte[] yamlContent, string fileNameWithoutExtension, SimpleLogger logger, string loggerGroup)
    {
        var yamlContentText = Encoding.UTF8.GetString(yamlContent);
        if (fileNameWithoutExtension != "profile")
        {
            if (!UpdateCharGuid(ref yamlContentText))
                logger.LogWarning("Character GUID could NOT be updated!", loggerGroup);
        }
        if (!UpdateSaveGuid(ref yamlContentText))
            logger.LogWarning("Save GUID could NOT be updated!", loggerGroup);
        if (!AnonymizeOnlineCharacterPrefs(ref yamlContentText))
            logger.LogWarning("OnlineCharacterPrefs could NOT be updated!", loggerGroup);
        return Encoding.UTF8.GetBytes(yamlContentText);
    }

    /// <summary>
    /// Updates the value of the first occurrence of a 32-character hexadecimal GUID following the "char_guid:" prefix in the specified text.
    /// </summary>
    /// <param name="dataText">The text to search and update.</param>
    /// <returns><see langword="true"/> if at least one GUID was replaced; otherwise, <see langword="false"/>.</returns>
    private static bool UpdateCharGuid(ref string dataText)
    {
        const string pattern = @"(?<=char_guid:\s)[A-Fa-f0-9]{32}";
        var newGuid = Guid.NewGuid().ToString().Replace("-", "").ToUpper();
        var replacementsCount = 0;
        dataText = Regex.Replace(dataText, pattern, _ =>
        {
            replacementsCount++;
            return newGuid;
        });
        return replacementsCount > 0;
    }

    /// <summary>
    /// Updates the GUID in the save game header within the specified text.
    /// </summary>
    /// <param name="dataText">The text to search and update.</param>
    /// <returns><see langword="true"/> if at least one GUID was replaced; otherwise, <see langword="false"/>.</returns>
    private static bool UpdateSaveGuid(ref string dataText)
    {
        const string pattern = @"(?<=save_game_header:\s*\r?\n\s*guid:\s)[A-Fa-f0-9]{32}";
        var newGuid = Guid.NewGuid().ToString().Replace("-", "").ToUpper();
        var replacementsCount = 0;
        dataText = Regex.Replace(dataText, pattern, _ =>
        {
            replacementsCount++;
            return newGuid;
        });
        return replacementsCount > 0;
    }

    /// <summary>
    /// Anonymizes the "onlinecharacterprefs" section in the provided text.
    /// </summary>
    /// <param name="dataText">A reference to the string containing the text to be updated. The method searches for the "onlinecharacterprefs" section and replaces it with a predefined structure.</param>
    /// <returns><see langword="true"/> if the "onlinecharacterprefs" section was found and replaced; otherwise, <see langword="false"/>.</returns>
    private static bool AnonymizeOnlineCharacterPrefs(ref string dataText)
    {
        const string pattern = @"onlinecharacterprefs:\s*(?:\n\s+.*)+";
        // Replacement string
        var replacement = """
                          onlinecharacterprefs:
                            recentfriends:
                            partyleader:
                          """;
        var replacementsCount = 0;
        dataText = Regex.Replace(dataText, pattern, _ =>
        {
            replacementsCount++;
            return replacement;
        });
        return replacementsCount > 0;
    }
}