using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Borderlands4SaveDataResigner.Helpers;

namespace Borderlands4SaveDataResigner.Types;

/// <summary>
/// A class representing an Epic ID.
/// </summary>
public class EpicId
{
    private const int Length = 32;
    private readonly string _pattern = $"^[0-9a-fA-F]{{{Length}}}$";

    /// <summary>
    /// The account ID of the user.
    /// </summary>
    public string AccountId { get; private set; } = "00000000000000000000000000000000";

    /// <summary>
    /// Attempts to set the Epic ID if it matches the required pattern.
    /// </summary>
    /// <param name="epicId">The Epic ID to validate and set. Must match the required pattern.</param>
    /// <returns><see langword="true"/> if the Epic ID is valid and successfully set; otherwise, <see langword="false"/>.</returns>
    public bool TrySetEpicId(string epicId)
    {
        epicId = epicId.Trim();
        if (!Regex.IsMatch(epicId, _pattern)) return false;
        AccountId = epicId;
        return true;
    }

    /// <summary>
    /// Converts the <see cref="AccountId"/> string to a byte array representation.
    /// </summary>
    /// <returns>A byte array representing the <see cref="AccountId"/> string.</returns>
    public byte[] GetAsByteArray()
    {
        var bytes = new byte[AccountId.Length];
        for (var i = 0; i < AccountId.Length; i++)
            bytes[i] = (byte)AccountId[i];
        return bytes;
    }

    /// <summary>
    /// Opens the EGS profile URL in the default web browser.
    /// </summary>
    public void OpenEpicProfileUrl()
    {
        var url = $"https://store.epicgames.com/u/{AccountId}";
        try { url.OpenUrl(); }
        catch
        {
            // ignored
        }
    }

    public bool Equals(EpicId other)
        => AccountId == other.AccountId;
    
    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is EpicId castedObj && Equals(castedObj);

    public override int GetHashCode()
        => HashCode.Combine(AccountId);

    public static bool operator ==(EpicId left, EpicId right)
        => left.Equals(right);

    public static bool operator !=(EpicId left, EpicId right)
        => !(left == right);
}