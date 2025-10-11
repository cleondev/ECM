namespace ECM.Document.Application.Tags.Commands;

public sealed record AssignTagToDocumentCommand(
    Guid DocumentId,
    Guid TagId,
    Guid? AppliedBy);
