namespace ECM.IAM.Application.Users;

using System;
using System.Collections.Generic;
using ECM.IAM.Application.Groups;
using ECM.IAM.Application.Roles;

public sealed record UserSummaryResult(
    Guid Id,
    string Email,
    string DisplayName,
    string? Department,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyCollection<RoleSummaryResult> Roles,
    IReadOnlyCollection<GroupSummaryResult> Groups);
