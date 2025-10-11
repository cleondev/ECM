namespace ECM.AccessControl.Application.Users.Commands;

using System;

public sealed record UpdateUserCommand(
    Guid UserId,
    string DisplayName,
    string? Department,
    bool? IsActive);
