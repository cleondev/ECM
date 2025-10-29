namespace AppGateway.Contracts.IAM.Users;

using System;
using System.Collections.Generic;
using AppGateway.Contracts.IAM.Groups;
using AppGateway.Contracts.IAM.Roles;

public sealed record UserSummaryDto(
    Guid Id,
    string Email,
    string DisplayName,
    string? Department,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyCollection<RoleSummaryDto> Roles,
    IReadOnlyCollection<GroupSummaryDto> Groups);
