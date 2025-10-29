namespace ECM.IAM.Api.Users;

using System;
using System.Collections.Generic;
public sealed class UpdateUserProfileRequest
{
    public string DisplayName { get; set; } = string.Empty;

    public IReadOnlyCollection<Guid> GroupIds { get; set; }
        = Array.Empty<Guid>();

    public Guid? PrimaryGroupId { get; set; }
}
