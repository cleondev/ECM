using System;

namespace AppGateway.Contracts.Tags;

public sealed record ManagementCreateTagRequestDto(
    Guid NamespaceId,
    Guid? ParentId,
    string Name,
    int? SortOrder,
    string? Color,
    string? IconKey,
    Guid? CreatedBy,
    bool IsSystem = false);
