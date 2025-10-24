using System;

namespace ECM.Document.Api.Documents;

public sealed record DocumentTagResponse(
    Guid Id,
    string NamespaceSlug,
    string Slug,
    string Path,
    bool IsActive,
    string DisplayName,
    Guid? AppliedBy,
    DateTimeOffset AppliedAtUtc);
