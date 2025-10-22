using System;

namespace AppGateway.Contracts.Tags;

public sealed record TagLabelDto(
    Guid Id,
    string NamespaceSlug,
    string Slug,
    string Path,
    bool IsActive,
    Guid? CreatedBy,
    DateTimeOffset CreatedAtUtc);
