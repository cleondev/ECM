namespace ECM.IAM.Api.Users;

using System;
using System.Collections.Generic;
using ECM.IAM.Api.Groups;
using ECM.IAM.Api.Roles;

public sealed record UserResponse(
    Guid Id,
    string Email,
    string DisplayName,
    bool IsActive,
    bool HasPassword,
    DateTimeOffset CreatedAtUtc,
    Guid? PrimaryGroupId,
    IReadOnlyCollection<Guid> GroupIds,
    IReadOnlyCollection<RoleResponse> Roles,
    IReadOnlyCollection<GroupResponse> Groups);
