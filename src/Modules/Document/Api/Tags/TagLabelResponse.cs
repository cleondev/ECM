namespace ECM.Document.Api.Tags;

public sealed record TagLabelResponse(
    Guid Id,
    string NamespaceSlug,
    string Slug,
    string Path,
    bool IsActive,
    Guid? CreatedBy,
    DateTimeOffset CreatedAtUtc);
