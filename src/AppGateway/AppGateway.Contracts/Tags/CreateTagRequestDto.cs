using System;

namespace AppGateway.Contracts.Tags;

public sealed record CreateTagRequestDto(
    string NamespaceSlug,
    string Slug,
    string? Path,
    Guid? CreatedBy);
