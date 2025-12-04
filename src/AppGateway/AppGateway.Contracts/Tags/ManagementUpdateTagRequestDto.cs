using System;

namespace AppGateway.Contracts.Tags;

public sealed record ManagementUpdateTagRequestDto(
    Guid NamespaceId,
    Guid? ParentId,
    string Name,
    int? SortOrder,
    string? Color,
    string? IconKey,
    bool IsActive,
    Guid? UpdatedBy);
