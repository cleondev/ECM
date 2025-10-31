namespace AppGateway.Contracts.IAM.Users;

using System;
using System.Collections.Generic;
using AppGateway.Contracts.IAM.Groups;
using AppGateway.Contracts.IAM.Roles;

public sealed record UserSummaryDto(
    Guid Id,
    string Email,
    string DisplayName,
    bool IsActive,
    bool HasPassword,
    DateTimeOffset CreatedAtUtc,
    Guid? PrimaryGroupId,
    IReadOnlyCollection<Guid> GroupIds,
    IReadOnlyCollection<RoleSummaryDto> Roles,
    IReadOnlyCollection<GroupSummaryDto> Groups);
