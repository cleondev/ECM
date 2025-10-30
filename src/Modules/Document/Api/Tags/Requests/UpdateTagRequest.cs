using System;

namespace ECM.Document.Api.Tags.Requests;

public sealed record UpdateTagRequest(
    Guid NamespaceId,
    Guid? ParentId,
    string Name,
    int? SortOrder,
    string? Color,
    string? IconKey,
    bool IsActive,
    Guid? UpdatedBy);
