namespace samples.EcmFileIntegrationSample;

public sealed record DocumentUploadRequest(
    Guid OwnerId,
    Guid CreatedBy,
    string DocType,
    string Status,
    string Sensitivity,
    string FilePath)
{
    public Guid? DocumentTypeId { get; init; }

    public string? Title { get; init; }

    public string? ContentType { get; init; }
}

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

public sealed record DocumentVersionDto(
    Guid Id,
    int VersionNo,
    string StorageKey,
    long Bytes,
    string MimeType,
    string Sha256,
    Guid CreatedBy,
    DateTimeOffset CreatedAtUtc);

public sealed record DocumentTagDto(Guid Id, string Name);

public sealed record UserProfile(
    Guid Id,
    string DisplayName,
    string Email,
    Guid? PrimaryGroupId,
    IReadOnlyCollection<Guid> GroupIds);
