namespace AppGateway.Contracts.IAM.Users;

using System;
using System.Collections.Generic;
public sealed class UpdateUserProfileRequestDto
{
    public string DisplayName { get; set; } = string.Empty;

    public IReadOnlyCollection<Guid> GroupIds { get; set; }
        = Array.Empty<Guid>();

    public Guid? PrimaryGroupId { get; set; }
}
