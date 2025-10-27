namespace ECM.Document.Api.Tags;

public sealed record UpdateTagRequest(
    string NamespaceSlug,
    string Slug,
    string? Path,
    Guid? UpdatedBy);
