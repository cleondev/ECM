using System;

namespace Shared.Contracts.Documents;

public sealed record TagLabelUpdatedContract(
    Guid TagId,
    Guid NamespaceId,
    Guid? ParentId,
    string Name,
    Guid[] PathIds,
    int SortOrder,
    string? Color,
    string? IconKey,
    bool IsActive,
    Guid? UpdatedBy,
    DateTimeOffset UpdatedAtUtc);
