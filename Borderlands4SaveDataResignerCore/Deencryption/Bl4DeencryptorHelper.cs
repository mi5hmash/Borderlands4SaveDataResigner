using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Mi5hmasH.Compressors;
using Mi5hmasH.GameLaunchers.Epic.Types;
using Mi5hmasH.GameLaunchers.Steam.Types;
using Mi5hmasH.Logger;

namespace Borderlands4SaveDataResignerCore.Deencryption;

public static partial class Bl4DeencryptorHelper
{
    /// <summary>
    /// Represents the default file extension used for save files.
    /// </summary>
    public const string SaveFileExtension = ".sav";

    /// <summary>
    /// Represents the standard file extension for YAML files.
    /// </summary>
    public const string YamlFileExtension = ".yaml";

    /// <summary>
    /// Represents the public key used for cryptographic operations.
    /// </summary>
    public static readonly byte[] PublicKey =
    [
        0x35, 0xEC, 0x33, 0x77, 0xF3, 0x5D, 0xB0, 0xEA, 0xBE, 0x6B, 0x83, 0x11, 0x54, 0x03, 0xEB, 0xFB,
        0x27, 0x25, 0x64, 0x2E, 0xD5, 0x49, 0x06, 0x29, 0x05, 0x78, 0xBD, 0x60, 0xBA, 0x4A, 0xA7, 0x87
    ];
    
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
    /// Decrypts the specified encrypted data using AES in ECB mode.
    /// </summary>
    /// <param name="encryptedBytes">The encrypted data to decrypt.</param>
    /// <param name="aes">A preconfigured AES instance.</param>
    /// <returns>A byte array containing the decrypted data.</returns>
    public static byte[] DecryptEcb(ReadOnlySpan<byte> encryptedBytes, Aes aes)
    {
        var decrypted = GC.AllocateUninitializedArray<byte>(encryptedBytes.Length);
        aes.DecryptEcb(encryptedBytes, decrypted, DefaultPadding);
        return decrypted;
    }

    /// <summary>
    /// Encrypts the specified plaintext using AES encryption in ECB (Electronic Codebook) mode with PKCS7 padding.
    /// </summary>
    /// <param name="plainBytes">The plaintext data to encrypt, represented as a read-only span of bytes.</param>
    /// <param name="aes">A preconfigured AES instance.</param>
    /// <returns>A byte array containing the encrypted data.</returns>
    public static byte[] EncryptEcb(ReadOnlySpan<byte> plainBytes, Aes aes)
    {
        var plainBytesWithPadding = AddPkcs7Padding(plainBytes);
        var encrypted = GC.AllocateUninitializedArray<byte>(plainBytesWithPadding.Length);
        aes.EncryptEcb(plainBytesWithPadding, encrypted, DefaultPadding);
        return encrypted;
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
            ? RemovePkcs7Padding(decryptedData, paddingLength)
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
    /// Adds PKCS#7 padding to the specified data to ensure its length is a multiple of the block size.
    /// </summary>
    /// <param name="data">The input data to which padding will be added.</param>
    /// <returns>A new byte array containing the original data followed by PKCS#7 padding bytes.</returns>
    private static byte[] AddPkcs7Padding(ReadOnlySpan<byte> data)
    {
        const int blockSize = 16;
        var padLen = blockSize - data.Length % blockSize;
        var newDataLength = data.Length + padLen;
        var dataWithPadding = GC.AllocateUninitializedArray<byte>(newDataLength);
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
        // Thank you Dxian998 for proposing a cleaner loop.
        for (var i = 1; i <= padLen; i++)
            if (data[^i] != padLen) return 0;

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
    /// Converts all Windows-style line endings (CRLF) in the specified UTF-8 encoded YAML content to Unix-style line endings (LF).
    /// </summary>
    /// <param name="yamlContent">A byte array containing the YAML content encoded in UTF-8.</param>
    /// <returns>A byte array containing the YAML content with all CRLF sequences replaced by LF, encoded in UTF-8.</returns>
    public static byte[] CrLfToLf(this ReadOnlySpan<byte> yamlContent)
    {
        var yamlContentText = Encoding.UTF8.GetString(yamlContent);
        yamlContentText = yamlContentText.Replace("\r\n", "\n");
        return Encoding.UTF8.GetBytes(yamlContentText);
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
    /// Generates a new globally unique identifier (GUID) as a 32-character uppercase string without hyphens.
    /// </summary>
    /// <returns>A string containing the newly generated GUID in uppercase, with no hyphens.</returns>
    private static string NewGuid()
    {
        Span<char> buffer = stackalloc char[32];
        Guid.NewGuid().TryFormat(buffer, out _, "N");
        return new string(buffer).ToUpperInvariant();
    }
    
    /// <summary>
    /// Updates the value of the first occurrence of a 32-character hexadecimal GUID following the "char_guid:" prefix in the specified text.
    /// </summary>
    /// <param name="dataText">The text to search and update.</param>
    /// <returns><see langword="true"/> if at least one GUID was replaced; otherwise, <see langword="false"/>.</returns>
    private static bool UpdateCharGuid(ref string dataText)
    {
        var newGuid = NewGuid();
        var replacementsCount = 0;
        dataText = CharGuidRegex().Replace(dataText, _ =>
        {
            replacementsCount++;
            return newGuid;
        });
        return replacementsCount > 0;
    }
    [GeneratedRegex("""(?<=char_guid:\s)[A-Fa-f0-9]{32}""")]
    private static partial Regex CharGuidRegex();

    /// <summary>
    /// Updates the GUID in the save game header within the specified text.
    /// </summary>
    /// <param name="dataText">The text to search and update.</param>
    /// <returns><see langword="true"/> if at least one GUID was replaced; otherwise, <see langword="false"/>.</returns>
    private static bool UpdateSaveGuid(ref string dataText)
    {
        var newGuid = NewGuid();
        var replacementsCount = 0;
        dataText = SaveGameHeaderRegex().Replace(dataText, _ =>
        {
            replacementsCount++;
            return newGuid;
        });
        return replacementsCount > 0;
    }
    [GeneratedRegex("""(?<=save_game_header:\s*\r?\n\s*guid:\s)[A-Fa-f0-9]{32}""")]
    private static partial Regex SaveGameHeaderRegex();

    /// <summary>
    /// Anonymizes the "onlinecharacterprefs" section in the provided text.
    /// </summary>
    /// <param name="dataText">A reference to the string containing the text to be updated. The method searches for the "onlinecharacterprefs" section and replaces it with a predefined structure.</param>
    /// <returns><see langword="true"/> if the "onlinecharacterprefs" section was found and replaced; otherwise, <see langword="false"/>.</returns>
    private static bool AnonymizeOnlineCharacterPrefs(ref string dataText)
    {
        // Replacement string
        const string replacement = """
                                   onlinecharacterprefs:
                                     recentfriends:
                                     partyleader:
                                   """;
        var replacementsCount = 0;
        dataText = OnlineCharacterPrefsRegex().Replace(dataText, _ =>
        {
            replacementsCount++;
            return replacement;
        });
        return replacementsCount > 0;
    }
    [GeneratedRegex("""onlinecharacterprefs:\s*(?:\n\s+.*)+""")]
    private static partial Regex OnlineCharacterPrefsRegex();
}