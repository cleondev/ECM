using System;

namespace AppGateway.Contracts.Tags;

public sealed record UpdateTagRequestDto(
    string NamespaceSlug,
    string Slug,
    string? Path,
    Guid? UpdatedBy);
