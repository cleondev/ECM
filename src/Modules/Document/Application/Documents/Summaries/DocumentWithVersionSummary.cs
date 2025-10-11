namespace ECM.Document.Application.Documents.Summaries;

public sealed record DocumentWithVersionSummary(
    Guid Id,
    string Title,
    string DocType,
    string Status,
    string Sensitivity,
    Guid OwnerId,
    Guid CreatedBy,
    string? Department,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    Guid? DocumentTypeId,
    DocumentVersionSummary? LatestVersion);
