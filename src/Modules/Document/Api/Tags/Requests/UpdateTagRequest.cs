namespace ECM.Document.Api.Tags.Requests;

public sealed record UpdateTagRequest(
    string NamespaceSlug,
    string Slug,
    string? Path,
    Guid? UpdatedBy);
