using System;
using System.Collections.Generic;

namespace ECM.IAM.Domain.Groups;

public static class GroupDefaults
{
    public const string GuestName = "guest";

    public const string SystemName = "system";

    public const string GuessUserName = "Guess User";

    public static readonly Guid GuestId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static readonly Guid SystemId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public static readonly Guid GuessUserId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static readonly IReadOnlyList<string> _names = [GuestName, GuessUserName, SystemName];

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

        if (string.Equals(name, GuessUserName, StringComparison.OrdinalIgnoreCase))
        {
            id = GuessUserId;
            return true;
        }

        id = Guid.Empty;
        return false;
    }
}
