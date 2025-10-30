using System.Collections.Generic;

namespace AppGateway.Contracts.Documents;

public sealed record DocumentDto(
    Guid Id,
    string Title,
    string DocType,
    string Status,
    string Sensitivity,
    Guid OwnerId,
    Guid CreatedBy,
    Guid? GroupId,
    IReadOnlyCollection<Guid> GroupIds,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    Guid? DocumentTypeId,
    DocumentVersionDto? LatestVersion,
    IReadOnlyCollection<DocumentTagDto> Tags);
