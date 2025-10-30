namespace AppGateway.Contracts.IAM.Users;

using System;
using System.Collections.Generic;
public sealed class UpdateUserRequestDto
{
    public string DisplayName { get; init; } = string.Empty;

    public IReadOnlyCollection<Guid> GroupIds { get; init; } = [];

    public Guid? PrimaryGroupId { get; init; }

    public bool? IsActive { get; init; }
}
