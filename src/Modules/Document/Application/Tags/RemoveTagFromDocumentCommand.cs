namespace ECM.Document.Application.Tags;

public sealed record RemoveTagFromDocumentCommand(
    Guid DocumentId,
    Guid TagId);
