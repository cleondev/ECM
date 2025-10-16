namespace ECM.IAM.Api.Users;

using System;

public sealed class UpdateUserRequest
{
    public string DisplayName { get; init; } = string.Empty;

    public string? Department { get; init; }

    public bool? IsActive { get; init; }
}
