namespace ECM.IAM.Domain.Groups;

public static class GroupDefaults
{
    public const string GuestName = "Guess User";

    public const string SystemName = "System";

    public static readonly Guid SystemId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static readonly Guid GuestId  = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static readonly IReadOnlyList<string> _names = [GuestName, SystemName];

    public static IReadOnlyList<string> Names => _names;

    public static bool TryGetIdForName(string name, out Guid id)
    {
        if (string.Equals(name, GuestName, StringComparison.OrdinalIgnoreCase))
        {
            id = GuestId;
            return true;
        }

        if (string.Equals(name, SystemName, StringComparison.OrdinalIgnoreCase))
        {
            id = SystemId;
            return true;
        }

        id = Guid.Empty;
        return false;
    }
}
