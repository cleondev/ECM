namespace AppGateway.Infrastructure.AccessControl;

using System;

public sealed class AccessControlOptions
{
    public Guid? DefaultRoleId { get; init; }

    public string? DefaultRoleName { get; init; }
}
