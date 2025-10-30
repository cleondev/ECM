using System;

namespace Shared.Contracts.Documents;

public sealed record TagLabelCreatedContract(
    Guid TagId,
    Guid NamespaceId,
    Guid? ParentId,
    string Name,
    Guid[] PathIds,
    int SortOrder,
    string? Color,
    string? IconKey,
    bool IsSystem,
    Guid? CreatedBy,
    DateTimeOffset CreatedAtUtc);
