using System.Text;
using System.Text.RegularExpressions;
using Mi5hmasH.Logger;

namespace Borderlands4SaveDataResignerCore.Deencryption;

public static partial class Bl4SaveDataFileHelper
{
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