namespace ECM.IAM.Api.Users;

using System;
using System.Collections.Generic;
using ECM.IAM.Api.Groups;

public sealed class UpdateUserRequest
{
    public string DisplayName { get; init; } = string.Empty;

    public IReadOnlyCollection<GroupAssignmentRequest> Groups { get; init; } = Array.Empty<GroupAssignmentRequest>();

    public bool? IsActive { get; init; }
}
