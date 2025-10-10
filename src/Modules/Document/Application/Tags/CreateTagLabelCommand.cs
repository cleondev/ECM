namespace ECM.Document.Application.Tags;

public sealed record CreateTagLabelCommand(
    string NamespaceSlug,
    string Slug,
    string? Path,
    Guid? CreatedBy);
