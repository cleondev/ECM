namespace ECM.IAM.Api.Users;

using System;
using System.Collections.Generic;
public sealed class UpdateUserRequest
{
    public string DisplayName { get; init; } = string.Empty;

    public IReadOnlyCollection<Guid> GroupIds { get; init; } = Array.Empty<Guid>();

    public Guid? PrimaryGroupId { get; init; }

    public bool? IsActive { get; init; }
}
