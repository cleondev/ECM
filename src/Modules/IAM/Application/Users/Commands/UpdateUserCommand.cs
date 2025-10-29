namespace ECM.IAM.Application.Users.Commands;

using System;
using System.Collections.Generic;
using ECM.IAM.Application.Groups;

public sealed record UpdateUserCommand(
    Guid UserId,
    string DisplayName,
    IReadOnlyCollection<GroupAssignment> Groups,
    bool? IsActive);
