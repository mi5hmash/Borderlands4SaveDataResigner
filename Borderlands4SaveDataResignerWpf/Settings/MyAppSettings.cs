using System.Diagnostics.CodeAnalysis;

namespace Borderlands4SaveDataResignerWpf.Settings;

public class MyAppSettings
{
    public string UserIdInput { get; set; } = "0";
    public string UserIdOutput { get; set; } = "0";
    public bool IsSu { get; set; } = false;

    public bool Equals(MyAppSettings other)
    {
        return UserIdInput == other.UserIdInput &&
               UserIdOutput == other.UserIdOutput &&
               IsSu == other.IsSu;
    }

    public int GetHashCodeStable()
        => HashCode.Combine(UserIdInput, UserIdOutput, IsSu);

    // This is a workaround to avoid the default GetHashCode() implementation in objects where all fields are mutable.
    private readonly Guid _uniqueId = Guid.NewGuid();
    public override int GetHashCode()
        => _uniqueId.GetHashCode();

    public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is MyAppSettings castedObj && Equals(castedObj);

    public static bool operator ==(MyAppSettings left, MyAppSettings right)
        => left.Equals(right);

    public static bool operator !=(MyAppSettings left, MyAppSettings right)
        => !(left == right);
}