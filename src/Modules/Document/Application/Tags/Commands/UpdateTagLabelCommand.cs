using System;

namespace ECM.Document.Application.Tags.Commands;

public sealed record UpdateTagLabelCommand(
    Guid TagId,
    Guid NamespaceId,
    Guid? ParentId,
    string Name,
    int? SortOrder,
    string? Color,
    string? IconKey,
    bool IsActive,
    Guid? UpdatedBy);
