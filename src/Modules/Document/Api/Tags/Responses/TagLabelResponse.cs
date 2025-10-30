using System;

namespace ECM.Document.Api.Tags.Responses;

public sealed record TagLabelResponse(
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
    Guid? CreatedBy,
    DateTimeOffset CreatedAtUtc);
