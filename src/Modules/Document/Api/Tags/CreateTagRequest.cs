namespace ECM.Document.Api.Tags;

public sealed record CreateTagRequest(
    string NamespaceSlug,
    string Slug,
    string? Path,
    Guid? CreatedBy);
