using System.Collections.Generic;

namespace ECM.Document.Api.Documents.Responses;

public sealed record DocumentResponse(
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
    string CreatedAtFormatted,
    string UpdatedAtFormatted,
    Guid? DocumentTypeId,
    DocumentVersionResponse? LatestVersion,
    IReadOnlyCollection<DocumentTagResponse> Tags);
