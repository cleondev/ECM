namespace ECM.Document.Application.Tags.Commands;

public sealed record CreateTagLabelCommand(
    string NamespaceSlug,
    string Slug,
    string? Path,
    Guid? CreatedBy);
