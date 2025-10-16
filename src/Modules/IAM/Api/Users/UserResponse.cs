namespace ECM.IAM.Api.Users;

using System;
using System.Collections.Generic;
using ECM.IAM.Api.Roles;

public sealed record UserResponse(
    Guid Id,
    string Email,
    string DisplayName,
    string? Department,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyCollection<RoleResponse> Roles);
