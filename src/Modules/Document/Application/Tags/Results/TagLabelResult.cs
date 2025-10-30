using System;

namespace ECM.Document.Application.Tags.Results;

public sealed record TagLabelResult(
    Guid Id,
    Guid NamespaceId,
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
