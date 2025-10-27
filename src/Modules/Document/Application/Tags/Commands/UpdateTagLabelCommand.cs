namespace ECM.Document.Application.Tags.Commands;

public sealed record UpdateTagLabelCommand(
    Guid TagId,
    string NamespaceSlug,
    string Slug,
    string? Path,
    Guid? UpdatedBy);
