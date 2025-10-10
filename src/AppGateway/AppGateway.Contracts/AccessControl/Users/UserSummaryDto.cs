namespace AppGateway.Contracts.AccessControl.Users;

using System;
using System.Collections.Generic;
using AppGateway.Contracts.AccessControl.Roles;

public sealed record UserSummaryDto(
    Guid Id,
    string Email,
    string DisplayName,
    string? Department,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyCollection<RoleSummaryDto> Roles);
