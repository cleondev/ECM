using System;

namespace AppGateway.Contracts.Documents;

public sealed record DocumentTagDto(
    Guid Id,
    Guid NamespaceId,
    string? NamespaceDisplayName,
    Guid? ParentId,
    string Name,
    Guid[] PathIds,
    int SortOrder,
    string? Color,
    string? IconKey,
    bool IsActive,
    bool IsSystem,
    Guid? AppliedBy,
    DateTimeOffset AppliedAtUtc);
