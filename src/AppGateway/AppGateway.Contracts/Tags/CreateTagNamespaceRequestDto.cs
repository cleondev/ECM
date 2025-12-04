using System;

namespace AppGateway.Contracts.Tags;

public sealed record CreateTagNamespaceRequestDto(
    string Scope,
    string? DisplayName,
    Guid? OwnerGroupId,
    Guid? OwnerUserId,
    Guid? CreatedBy);
