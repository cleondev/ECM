namespace ECM.Document.Api.Tags;

public sealed record AssignTagRequest(
    Guid TagId,
    Guid? AppliedBy);
