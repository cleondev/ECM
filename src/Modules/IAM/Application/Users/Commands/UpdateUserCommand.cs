namespace ECM.IAM.Application.Users.Commands;

using System;
using System.Collections.Generic;
public sealed record UpdateUserCommand(
    Guid UserId,
    string DisplayName,
    IReadOnlyCollection<Guid> GroupIds,
    Guid? PrimaryGroupId,
    bool? IsActive);
