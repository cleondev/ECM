namespace ECM.Document.Api.Tags.Requests;

public sealed record CreateTagRequest(
    string NamespaceSlug,
    string Slug,
    string? Path,
    Guid? CreatedBy);
