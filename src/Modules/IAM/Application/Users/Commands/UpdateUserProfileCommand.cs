namespace ECM.IAM.Application.Users.Commands;

using System.Collections.Generic;
using ECM.IAM.Application.Groups;

public sealed record UpdateUserProfileCommand(
    string Email,
    string DisplayName,
    IReadOnlyCollection<GroupAssignment> Groups);
