using System.Diagnostics.CodeAnalysis;
using Borderlands4SaveDataResigner.Helpers;
using static System.Globalization.NumberStyles;

namespace Borderlands4SaveDataResigner.Types;

/// <summary>
/// A class representing a Steam ID.
/// </summary>
public class SteamId
{
    private byte _accountType = 1;
    private uint _instance = 1;
    private byte _universe = 1;

    /// <summary>
    /// The prefix for Steam 2 IDs. This is used to identify the format of the ID.
    /// </summary>
    private const string Steam2IdPrefix = "STEAM_";

    /// <summary>
    /// The prefix for Steam 3 IDs in HEX. This is used to identify the format of the ID.
    /// </summary>
    private const string Steam3HexPrefix = "steam:";

    /// <summary>
    /// All 10 known account types.
    /// </summary>
    public enum AccountTypeEnum
    {
        Invalid,
        Individual,
        Multiseat,
        GameServer,
        AnonGameServer,
        Pending,
        ContentServer,
        Clan,
        Chat,
        P2PSuperSeeder,
        AnonUser
    }

    /// <summary>
    /// All 2 known instances. 
    /// </summary>
    public enum InstanceEnum
    {
        Group,
        Individual
    }

    /// <summary>
    /// All 6 known universes. 
    /// </summary>
    public enum UniverseEnum
    {
        IndividualUnspecified,
        Public,
        Beta,
        Internal,
        Dev,
        ReleaseCandidate // no such universe anymore (legacy)
    }

    /// <summary>
    /// The default value for the account type character dictionary.
    /// </summary>
    /// <returns></returns>
    private static char? AccountTypeCharDictionaryGetDefaultValue() => null;

    /// <summary>
    /// The default value for the account type number dictionary.
    /// </summary>
    /// <returns></returns>
    private static byte AccountTypeNumberDictionaryGetDefaultValue() => 9;

    /// <summary>
    /// Dictionary to convert account type numbers to characters.
    /// </summary>
    private static readonly Dictionary<byte, char?> AccountTypeCharDictionary = new()
    {
        { 9, null },
        { 0, 'I' },
        { 1, 'U' },
        { 2, 'M' },
        { 3, 'G' },
        { 4, 'A' },
        { 5, 'P' },
        { 6, 'C' },
        { 7, 'g' },
        { 8, 'T' },
        { 10, 'a' }
    };

    /// <summary>
    /// Dictionary to convert account type characters to numbers.
    /// </summary>
    private static readonly Dictionary<char, byte> AccountTypeNumberDictionary = new()
    {
        { 'I', 0 },
        { 'U', 1 },
        { 'M', 2 },
        { 'G', 3 },
        { 'A', 4 },
        { 'P', 5 },
        { 'C', 6 },
        { 'g', 7 },
        { 'T', 8 },
        { 'a', 10 }
    };
    
    /// <summary>
    /// The account ID of the user.
    /// </summary>
    public uint AccountId { get; set; }
    
    /// <summary>
    /// The type of account.
    /// </summary>
    public AccountTypeEnum AccountType
    {
        get => (AccountTypeEnum)_accountType;
        set => _accountType = (byte)value;
    }

    /// <summary>
    /// The instance of the account.
    /// </summary>
    public InstanceEnum Instance
    {
        get => (InstanceEnum)_instance;
        set => _instance = (uint)value;
    }

    /// <summary>
    /// The universe of the account.
    /// </summary>
    public UniverseEnum Universe
    {
        get => (UniverseEnum)_universe;
        set => _universe = (byte)value;
    }

    /// <summary>
    /// The parameterless constructor for the SteamId class.
    /// </summary>
    public SteamId()
    {
        // Default constructor
    }

    /// <summary>
    /// The constructor for the SteamId class.
    /// </summary>
    /// <param name="steamId64"></param>
    public SteamId(ulong steamId64)
    {
        Set(steamId64);
    }

    /// <summary>
    /// The constructor for the SteamId class.
    /// </summary>
    /// <param name="steamIdText"></param>
    public SteamId(string steamIdText)
    {
        Set(steamIdText);
    }

    /// <summary>
    /// Sets the Steam ID using a 64-bit integer.
    /// </summary>
    /// <param name="steamId64"></param>
    /// <returns></returns>
    public bool Set(ulong steamId64)
    {
        // Example based on: https://whatsmysteamid.azurewebsites.net
        // Given 64-bit Steam ID: 76561197960287930    
        // Binary representation:
        // 0000 0001 0001 0000 0000 0000 0000 0001 0000 0000 0000 0000 0101 0110 1011 1010
        // ^       ^ ^  ^ ^                      ^ ^                                     ^
        // |_______| |__| |______________________| |_____________________________________|
        //    UNI     AT         INSTANCE                     32-BIT ACCOUNT ID

        const byte sizeInBits = sizeof(uint) * 8;
        var higherBits = (uint)(steamId64 >> sizeInBits);
        AccountId = (uint)steamId64;
        _universe = (byte)(higherBits >> (sizeInBits - 8));
        _accountType = (byte)((higherBits >> (sizeInBits - 12)) & 0xF);
        _instance = higherBits & 0xFFFFF;
        return true;
    }

    /// <summary>
    /// Sets the Steam ID using the account ID, universe, account type, and instance.
    /// </summary>
    /// <param name="accountId"></param>
    /// <param name="universe"></param>
    /// <param name="accountType"></param>
    /// <param name="instanceEnum"></param>
    /// <returns></returns>
    public bool Set(uint accountId, UniverseEnum universe = UniverseEnum.Public, AccountTypeEnum accountType = AccountTypeEnum.Individual, InstanceEnum instanceEnum = InstanceEnum.Individual)
    {
        AccountId = accountId;
        Universe = universe;
        AccountType = accountType;
        Instance = instanceEnum;
        return true;
    }

    /// <summary>
    /// Tries to set the Steam ID from a given string.
    /// </summary>
    /// <param name="steamId"></param>
    /// <returns>Returns execution status as boolean.</returns>
    public bool Set(string steamId)
    {
        // Check if the input string is null or empty
        if (string.IsNullOrEmpty(steamId)) return false;

        // Check if the input is numeric
        if (ulong.TryParse(steamId, out var numericSteamId)) 
            return numericSteamId > uint.MaxValue ? Set(numericSteamId) : Set((uint)numericSteamId);

        // Try to parse the string
        if (steamId.StartsWith(Steam2IdPrefix, StringComparison.OrdinalIgnoreCase)) return TrySetSteam2Id(steamId);
        if (steamId.StartsWith(Steam3HexPrefix, StringComparison.OrdinalIgnoreCase)) return TrySetSteam3HexId(steamId);
        if (steamId.StartsWith('[') && steamId.EndsWith(']')) return TrySetSteam3Id(steamId);
        return false;
    }

    /// <summary>
    /// Tries to set the Steam ID from a given Steam 2 ID.
    /// </summary>
    /// <param name="steamId"></param>
    /// <returns></returns>
    private bool TrySetSteam2Id(string steamId)
    {
        var steamParts = steamId.Split('_', ':');
        if (steamParts.Length != 4) return false;

        return uint.TryParse(steamParts[^1], out var numeric31BitsOfAccountId) &&
               uint.TryParse(steamParts[^2], out var numericLastBit) &&
               uint.TryParse(steamParts[1], out var numericUniverse) &&
               Set((numeric31BitsOfAccountId << 1) | numericLastBit, (UniverseEnum)numericUniverse);
    }

    /// <summary>
    /// Tries to set the Steam ID from a given Steam 3 HEX ID.
    /// </summary>
    /// <param name="steamId"></param>
    /// <returns></returns>
    private bool TrySetSteam3HexId(string steamId)
    {
        var steamParts = steamId.Split(':');
        if (steamParts.Length != 2 || steamParts[^1].Length > 16) return false;

        return ulong.TryParse(steamParts[^1], HexNumber, null, out var numericSteamId) &&
               Set(numericSteamId);
    }

    /// <summary>
    /// Tries to set the Steam ID from a given Steam 3 ID.
    /// </summary>
    /// <param name="steamId"></param>
    /// <returns></returns>
    private bool TrySetSteam3Id(string steamId)
    {
        var steamParts = steamId.Trim('[', ']').Split(':');
        if (steamParts.Length != 3) return false;
        var accountTypeNumber = string.IsNullOrEmpty(steamParts[0]) ? AccountTypeNumberDictionaryGetDefaultValue() : AccountTypeNumberDictionary.GetValueOrDefault(steamParts[0][0], AccountTypeNumberDictionaryGetDefaultValue());
        
        return uint.TryParse(steamParts[^1], out var numericAccountId) &&
               uint.TryParse(steamParts[^2], out var numericUniverse) &&
               Set(numericAccountId, (UniverseEnum)numericUniverse, (AccountTypeEnum)accountTypeNumber);
    }
    
    /// <summary>
    /// Gets the Steam ID as a 64-bit integer.
    /// </summary>
    /// <returns></returns>
    public ulong GetSteamId64()
        => AccountId | ((ulong)_instance << 32) | ((ulong)_accountType << 52) | ((ulong)_universe << 56);

    /// <summary>
    /// Converts the 64-bit Steam ID to a byte array representation.
    /// </summary>
    /// <returns>A byte array representing the 64-bit Steam ID.</returns>
    public byte[] GetSteamId64AsByteArray()
    {
        var bytes = BitConverter.GetBytes(GetSteamId64());
        return bytes;
    }

    /// <summary>
    /// Converts the Steam account ID to a byte array representation.
    /// </summary>
    /// <returns>A byte array representing the 64-bit Steam ID.</returns>
    public byte[] GetSteamIdAsByteArray()
    {
        var bytes = BitConverter.GetBytes(AccountId);
        return bytes;
    }

    /// <summary>
    /// Gets Steam ID in the format STEAM_X:Y:Z where X is the universe, Y is the lowest bit of the account ID, and Z is the highest 31 bits of the account ID.
    /// </summary>
    /// <returns></returns>
    public string GetSteam2Id()
        => $"{Steam2IdPrefix}{_universe}:{GetLowestBit()}:{GetHighest31BitsOfAccountId()}";

    /// <summary>
    /// Gets Steam ID in the format [X:Y:Z] where X is the account type represented by a letter, Y is the universe, and Z is the account ID.
    /// </summary>
    /// <returns></returns>
    public string GetSteam3Id()
        => $"[{AccountTypeCharDictionary.GetValueOrDefault(_accountType, AccountTypeCharDictionaryGetDefaultValue())}:{_universe}:{AccountId}]";
    
    /// <summary>
    /// Gets Steam ID in the format X:Y:Z where X is the account type represented by a letter, Y is the universe, and Z is the account ID.
    /// </summary>
    /// <returns></returns>
    public string GetSteam3IdWithoutBrackets()
        => $"{AccountTypeCharDictionary.GetValueOrDefault(_accountType, AccountTypeCharDictionaryGetDefaultValue())}:{_universe}:{AccountId}";

    /// <summary>
    /// Gets Steam ID in the hex format used by FiveM
    /// </summary>
    /// <returns></returns>
    public string GetSteam3Hex()
        => $"{Steam3HexPrefix}{GetSteamId64():x15}";

    /// <summary>
    /// Gets the lowest bit of <see cref="AccountId"/> (0 or 1).
    /// </summary>
    /// <returns></returns>
    public byte GetLowestBit() 
        => (byte)(AccountId & 1);

    /// <summary>
    /// Gets highest 31 bits of <see cref="AccountId"/>.
    /// </summary>
    /// <returns></returns>
    public uint GetHighest31BitsOfAccountId() 
        => AccountId >> 1;

    /// <summary>
    /// Opens the Steam profile URL in the default web browser.
    /// </summary>
    public void OpenSteamProfileUrl()
    {
        var url = $"https://steamcommunity.com/profiles/{GetSteamId64()}";
        try { url.OpenUrl(); }
        catch
        {
            // ignored
        }
    }
    
    public bool Equals(SteamId other)
    {
        return AccountId == other.AccountId && 
               _accountType == other._accountType &&
               _instance == other._instance &&
               _universe == other._universe;
    }

    public int GetHashCodeStable()
        => HashCode.Combine(AccountId, _accountType, _instance, _universe);

    // This is a workaround to avoid the default GetHashCode() implementation in objects where all fields are mutable.
    private readonly Guid _uniqueId = Guid.NewGuid();
    public override int GetHashCode()
        => _uniqueId.GetHashCode();

    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is SteamId castedObj && Equals(castedObj);

    public static bool operator ==(SteamId left, SteamId right)
        => left.Equals(right);

    public static bool operator !=(SteamId left, SteamId right)
        => !(left == right);
}