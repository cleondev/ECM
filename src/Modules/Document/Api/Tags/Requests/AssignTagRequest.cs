namespace ECM.Document.Api.Tags.Requests;

public sealed record AssignTagRequest(
    Guid TagId,
    Guid? AppliedBy);
