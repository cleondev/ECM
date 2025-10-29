namespace ECM.IAM.Api.Users;

using System;
using System.Collections.Generic;
using ECM.IAM.Api.Groups;

public sealed class UpdateUserProfileRequest
{
    public string DisplayName { get; set; } = string.Empty;

    public IReadOnlyCollection<GroupAssignmentRequest> Groups { get; set; }
        = Array.Empty<GroupAssignmentRequest>();
}
