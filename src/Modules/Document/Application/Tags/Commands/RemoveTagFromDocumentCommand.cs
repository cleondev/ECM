namespace ECM.Document.Application.Tags.Commands;

public sealed record RemoveTagFromDocumentCommand(
    Guid DocumentId,
    Guid TagId);
