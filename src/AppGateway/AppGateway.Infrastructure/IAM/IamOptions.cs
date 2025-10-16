namespace AppGateway.Infrastructure.IAM;

using System;

public sealed class IamOptions
{
    public Guid? DefaultRoleId { get; init; }

    public string? DefaultRoleName { get; init; }
}
