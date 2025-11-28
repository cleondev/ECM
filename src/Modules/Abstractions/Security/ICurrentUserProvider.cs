using System;
using System.Security.Claims;

namespace ECM.Abstractions.Security;

public interface ICurrentUserProvider
{
    ClaimsPrincipal? Current { get; }

    Guid? TryGetUserId();

    string? TryGetDisplayName();
}
