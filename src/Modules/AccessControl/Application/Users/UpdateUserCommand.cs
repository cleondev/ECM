namespace ECM.Modules.AccessControl.Application.Users;

using System;

public sealed record UpdateUserCommand(
    Guid UserId,
    string DisplayName,
    string? Department,
    bool? IsActive);
