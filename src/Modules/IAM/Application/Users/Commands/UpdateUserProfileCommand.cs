namespace ECM.IAM.Application.Users.Commands;

using System.Collections.Generic;
public sealed record UpdateUserProfileCommand(
    string Email,
    string DisplayName,
    IReadOnlyCollection<Guid> GroupIds,
    Guid? PrimaryGroupId);
