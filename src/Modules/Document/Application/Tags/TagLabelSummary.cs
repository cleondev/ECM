namespace ECM.Document.Application.Tags;

public sealed record TagLabelSummary(
    Guid Id,
    string NamespaceSlug,
    string Slug,
    string Path,
    bool IsActive,
    Guid? CreatedBy,
    DateTimeOffset CreatedAtUtc);
