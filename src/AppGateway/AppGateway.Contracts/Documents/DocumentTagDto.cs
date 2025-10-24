using System;

namespace AppGateway.Contracts.Documents;

public sealed record DocumentTagDto(
    Guid Id,
    string NamespaceSlug,
    string Slug,
    string Path,
    bool IsActive,
    string DisplayName,
    Guid? AppliedBy,
    DateTimeOffset AppliedAtUtc);
