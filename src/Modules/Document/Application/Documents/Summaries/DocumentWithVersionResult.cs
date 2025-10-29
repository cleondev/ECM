namespace ECM.Document.Application.Documents.Summaries;

public sealed record DocumentWithVersionResult(
    Guid Id,
    string Title,
    string DocType,
    string Status,
    string Sensitivity,
    Guid OwnerId,
    Guid CreatedBy,
    Guid? GroupId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    Guid? DocumentTypeId,
    DocumentVersionResult? LatestVersion);
