using System;

namespace AppGateway.Contracts.Tags;

public sealed record CreateTagRequestDto(
    Guid NamespaceId,
    Guid? ParentId,
    string Name,
    int? SortOrder,
    string? Color,
    string? IconKey,
    Guid? CreatedBy,
    bool IsSystem = false);
