namespace ECM.IAM.Application.Users;

using System;
using System.Collections.Generic;
using ECM.IAM.Application.Groups;
using ECM.IAM.Application.Roles;

public sealed record UserSummaryResult(
    Guid Id,
    string Email,
    string DisplayName,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    Guid? PrimaryGroupId,
    IReadOnlyCollection<Guid> GroupIds,
    IReadOnlyCollection<RoleSummaryResult> Roles,
    IReadOnlyCollection<GroupSummaryResult> Groups,
    bool HasPassword);
