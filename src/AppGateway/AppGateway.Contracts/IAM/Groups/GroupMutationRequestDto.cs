using System;

namespace AppGateway.Contracts.IAM.Groups;

public sealed record CreateGroupRequestDto(string Name, string? Kind, Guid? ParentGroupId);

public sealed record UpdateGroupRequestDto(string Name, string? Kind, Guid? ParentGroupId);
