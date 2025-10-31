using System;

namespace AppGateway.Contracts.Tags;

public sealed record TagLabelDto(
    Guid Id,
    Guid NamespaceId,
    string? NamespaceScope,
    string? NamespaceDisplayName,
    Guid? ParentId,
    string Name,
    Guid[] PathIds,
    int SortOrder,
    string? Color,
    string? IconKey,
    bool IsActive,
    bool IsSystem,
    Guid? CreatedBy,
    DateTimeOffset CreatedAtUtc);
