using System;

namespace ECM.Document.Api.Documents.Responses;

public sealed record DocumentTagResponse(
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
    Guid? AppliedBy,
    DateTimeOffset AppliedAtUtc);
