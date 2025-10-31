using System;

namespace ECM.Document.Application.Tags.Commands;

public sealed record CreateTagLabelCommand(
    Guid? NamespaceId,
    Guid? ParentId,
    string Name,
    int? SortOrder,
    string? Color,
    string? IconKey,
    Guid? CreatedBy,
    bool IsSystem = false);
