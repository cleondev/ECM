using System;

namespace AppGateway.Contracts.Tags;

public sealed record TagNamespaceDto(
    Guid Id,
    string Scope,
    Guid? OwnerUserId,
    Guid? OwnerGroupId,
    string? DisplayName,
    bool IsSystem,
    DateTimeOffset CreatedAtUtc);
