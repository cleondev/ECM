namespace ECM.Document.Application.Tags;

public sealed record AssignTagToDocumentCommand(
    Guid DocumentId,
    Guid TagId,
    Guid? AppliedBy);
