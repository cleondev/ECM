using System;

namespace ECM.Document.Api.Tags.Requests;

public sealed record CreateTagRequest(
    Guid NamespaceId,
    Guid? ParentId,
    string Name,
    int? SortOrder,
    string? Color,
    string? IconKey,
    Guid? CreatedBy,
    bool IsSystem = false);
