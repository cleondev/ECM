using System;
using System.Collections.Generic;

namespace TestFixtures;

public sealed class DefaultGroupFixture
{
    public Guid GuestGroupId { get; } = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public Guid SystemGroupId { get; } = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public string GuestGroupName { get; } = "guest";

    public string SystemGroupName { get; } = "system";

    public Guid GuessGroupId { get; } = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public string GuessGroupName { get; } = "Guess User";

    public IReadOnlyCollection<string> DefaultGroupNames =>
        [GuestGroupName, GuessGroupName, SystemGroupName];
}
