namespace AppGateway.Contracts.IAM.Users;

using System;

public sealed class UpdateUserRequestDto
{
    public string DisplayName { get; init; } = string.Empty;

    public string? Department { get; init; }

    public bool? IsActive { get; init; }
}
