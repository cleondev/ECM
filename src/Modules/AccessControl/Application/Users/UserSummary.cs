namespace ECM.Modules.AccessControl.Application.Users;

using System;
using System.Collections.Generic;
using ECM.Modules.AccessControl.Application.Roles;

public sealed record UserSummary(
    Guid Id,
    string Email,
    string DisplayName,
    string? Department,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    IReadOnlyCollection<RoleSummary> Roles);
